using System;
using System.Diagnostics;

namespace MinimalChess
{
    //    A  B  C  D  E  F  G  H        BLACK
    // 8  56 57 58 59 60 61 62 63  8
    // 7  48 49 50 51 52 53 54 55  7
    // 6  40 41 42 43 44 45 46 47  6
    // 5  32 33 34 35 36 37 38 39  5
    // 4  24 25 26 27 28 29 30 31  4
    // 3  16 17 18 19 20 21 22 23  3
    // 2  08 09 10 11 12 13 14 15  2
    // 1  00 01 02 03 04 05 06 07  1
    //    A  B  C  D  E  F  G  H        WHITE

    public class Board
    {
        static readonly int BlackKingSquare = Notation.ToSquare("e8");
        static readonly int WhiteKingSquare = Notation.ToSquare("e1");
        static readonly int BlackQueensideRookSquare = Notation.ToSquare("a8");
        static readonly int BlackKingsideRookSquare = Notation.ToSquare("h8");
        static readonly int WhiteQueensideRookSquare = Notation.ToSquare("a1");
        static readonly int WhiteKingsideRookSquare = Notation.ToSquare("h1");

        public const string STARTING_POS_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        [Flags]
        public enum CastlingRights
        {
            None = 0,
            WhiteKingside = 1,
            WhiteQueenside = 2,
            BlackKingside = 4,
            BlackQueenside = 8,
            All = 15
        }

        /*** STATE DATA ***/
        private Piece[] _state = new Piece[64];
        private CastlingRights _castlingRights = CastlingRights.All;
        private Color _sideToMove = Color.White;
        private int _enPassantSquare = -1;
        private ulong _zobristHash = 0;
        private Evaluation.Eval _eval;
        /*** STATE DATA ***/

        public int Score => _eval.Score;

        public ulong ZobristHash => _zobristHash;

        public Color SideToMove
        {
            get => _sideToMove;
            private set 
            {
                _zobristHash ^= Zobrist.SideToMove(_sideToMove);
                _sideToMove = value;
                _zobristHash ^= Zobrist.SideToMove(_sideToMove);
            }
        }

        public Board() { }

        public Board(string fen)
        {
            SetupPosition(fen);
        }

        public Board(Board board)
        {
            Copy(board);
        }

        public Board(Board board, Move move)
        {
            Copy(board);
            Play(move);
        }

        public void Copy(Board board)
        {
            //Array.Copy(board._state, _state, 64);
            board._state.AsSpan().CopyTo(_state.AsSpan());
            _sideToMove = board._sideToMove;
            _enPassantSquare = board._enPassantSquare;
            _castlingRights = board._castlingRights;
            _zobristHash = board._zobristHash;
            _eval = board._eval;
        }

        public Piece this[int square]
        {
            get => _state[square];
            private set
            {
                _eval.Update(_state[square], value, square);
                _zobristHash ^= Zobrist.PieceSquare(_state[square], square);
                _state[square] = value;
                _zobristHash ^= Zobrist.PieceSquare(_state[square], square);
                Debug.Assert(Score == new Evaluation.Eval(this).Score);
            }
        }

        //Rank - the eight horizontal rows of the chess board are called ranks.
        //File - the eight vertical columns of the chess board are called files.
        public Piece this[int rank, int file] => _state[rank * 8 + file];

        public void SetupPosition(string fen)
        {
            //Startpos in FEN looks like this: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            //https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
            string[] fields = fen.Split();
            if (fields.Length < 4)
                throw new ArgumentException($"FEN needs at least 4 fields. Has only {fields.Length} fields.");

            //Place pieces on board
            Array.Clear(_state, 0, 64);
            string[] fenPosition = fields[0].Split('/');
            int rank = 7;
            foreach (string row in fenPosition)
            {
                int file = 0;
                foreach (char piece in row)
                {
                    if (char.IsNumber(piece))
                    {
                        int emptySquares = (int)char.GetNumericValue(piece);
                        file += emptySquares;
                    }
                    else
                    {
                        _state[rank * 8 + file] = Notation.ToPiece(piece);
                        file++;
                    }
                }
                rank--;
            }

            //Set side to move
            _sideToMove = fields[1].Equals("w", StringComparison.CurrentCultureIgnoreCase) ? Color.White : Color.Black;

            //Set castling rights
            SetCastlingRights(CastlingRights.WhiteKingside, fields[2].IndexOf("K") > -1);
            SetCastlingRights(CastlingRights.WhiteQueenside, fields[2].IndexOf("Q") > -1);
            SetCastlingRights(CastlingRights.BlackKingside, fields[2].IndexOf("k") > -1);
            SetCastlingRights(CastlingRights.BlackQueenside, fields[2].IndexOf("q") > -1);

            //Set en-passant square
            _enPassantSquare = fields[3] == "-" ? -1 : Notation.ToSquare(fields[3]);

            //Init incremental eval
            _eval = new Evaluation.Eval(this);

            //Initialze Hash
            InitZobristHash();
        }


        //*****************
        //** PLAY MOVES ***
        //*****************

        public void PlayNullMove()
        {
            SideToMove = Pieces.Flip(_sideToMove);
            //Clear en passent
            _zobristHash ^= Zobrist.EnPassant(_enPassantSquare);
            _enPassantSquare = -1;
        }

        public void Play(Move move)
        {
            Piece movingPiece = this[move.FromSquare];
            if (move.Promotion != Piece.None)
                movingPiece = move.Promotion.OfColor(_sideToMove);

            //move the correct piece to the target square
            this[move.ToSquare] = movingPiece;
            //...and clear the square it was previously located
            this[move.FromSquare] = Piece.None;

            if (IsEnPassant(movingPiece, move, out int captureIndex))
            {
                //capture the pawn
                this[captureIndex] = Piece.None;
            }

            //handle castling special case
            if (IsCastling(movingPiece, move, out Move rookMove))
            {
                //move the rook to the target square and clear from square
                this[rookMove.ToSquare] = this[rookMove.FromSquare];
                this[rookMove.FromSquare] = Piece.None;
            }

            //update board state
            UpdateEnPassent(move);
            UpdateCastlingRights(move.FromSquare);
            UpdateCastlingRights(move.ToSquare);

            //toggle active color!
            SideToMove = Pieces.Flip(_sideToMove);
        }

        private void UpdateCastlingRights(int square)
        {
            //any move from or to king or rook squares will effect castling right
            if (square == WhiteKingSquare || square == WhiteQueensideRookSquare)
                SetCastlingRights(CastlingRights.WhiteQueenside, false);
            if (square == WhiteKingSquare || square == WhiteKingsideRookSquare)
                SetCastlingRights(CastlingRights.WhiteKingside, false);

            if (square == BlackKingSquare || square == BlackQueensideRookSquare)
                SetCastlingRights(CastlingRights.BlackQueenside, false);
            if (square == BlackKingSquare || square == BlackKingsideRookSquare)
                SetCastlingRights(CastlingRights.BlackKingside, false);
        }

        private void UpdateEnPassent(Move move)
        {
            _zobristHash ^= Zobrist.EnPassant(_enPassantSquare);

            int to = move.ToSquare;
            int from = move.FromSquare;
            Piece movingPiece = _state[to];

            //movingPiece needs to be either a BlackPawn...
            if (movingPiece == Piece.BlackPawn && Rank(to) == Rank(from) - 2)
                _enPassantSquare = Down(from);
            else if (movingPiece == Piece.WhitePawn && Rank(to) == Rank(from) + 2)
                _enPassantSquare = Up(from);
            else
                _enPassantSquare = -1;

            _zobristHash ^= Zobrist.EnPassant(_enPassantSquare);
        }

        private bool IsEnPassant(Piece movingPiece, Move move, out int captureIndex)
        {
            if (movingPiece == Piece.BlackPawn && move.ToSquare == _enPassantSquare)
            {
                captureIndex = Up(_enPassantSquare);
                return true;
            }
            else if (movingPiece == Piece.WhitePawn && move.ToSquare == _enPassantSquare)
            {
                captureIndex = Down(_enPassantSquare);
                return true;
            }

            //not en passant
            captureIndex = -1;
            return false;
        }

        private bool IsCastling(Piece moving, Move move, out Move rookMove)
        {
            if (moving == Piece.BlackKing && move == Move.BlackCastlingLong)
            {
                rookMove = Move.BlackCastlingLongRook;
                return true;
            }
            if (moving == Piece.BlackKing && move == Move.BlackCastlingShort)
            {
                rookMove = Move.BlackCastlingShortRook;
                return true;
            }
            if (moving == Piece.WhiteKing && move == Move.WhiteCastlingLong)
            {
                rookMove = Move.WhiteCastlingLongRook;
                return true;
            }
            if (moving == Piece.WhiteKing && move == Move.WhiteCastlingShort)
            {
                rookMove = Move.WhiteCastlingShortRook;
                return true;
            }

            //not castling
            rookMove = default;
            return false;
        }

        //**********************
        //** MOVE GENERATION ***
        //**********************

        public bool IsPlayable(Move move)
        {
            bool found = false;
            CollectMoves(move.FromSquare, m => found |= (m == move));
            return found;
        }

        public void CollectMoves(Action<Move> moveHandler)
        {
            for (int square = 0; square < 64; square++)
                CollectMoves(square, moveHandler);
        }

        public void CollectQuiets(Action<Move> moveHandler)
        {
            for (int square = 0; square < 64; square++)
                CollectQuiets(square, moveHandler);
        }

        public void CollectCaptures(Action<Move> moveHandler)
        {
            for (int square = 0; square < 64; square++)
                CollectCaptures(square, moveHandler);
        }

        public void CollectMoves(int square, Action<Move> moveHandler)
        {
            CollectQuiets(square, moveHandler);
            CollectCaptures(square, moveHandler);
        }

        public void CollectQuiets(int square, Action<Move> moveHandler)
        {
            if (IsActivePiece(_state[square]))
                AddQuiets(square, moveHandler);
        }

        public void CollectCaptures(int square, Action<Move> moveHandler)
        {
            if (IsActivePiece(_state[square]))
                AddCaptures(square, moveHandler);
        }

        private void AddQuiets(int square, Action<Move> moveHandler)
        {
            switch (_state[square])
            {
                case Piece.BlackPawn:
                    AddBlackPawnMoves(moveHandler, square);
                    break;
                case Piece.WhitePawn:
                    AddWhitePawnMoves(moveHandler, square);
                    break;
                case Piece.BlackKing:
                    AddBlackCastlingMoves(moveHandler);
                    AddMoves(moveHandler, square, Attacks.King);
                    break;
                case Piece.WhiteKing:
                    AddWhiteCastlingMoves(moveHandler);
                    AddMoves(moveHandler, square, Attacks.King);
                    break;
                case Piece.BlackKnight:
                case Piece.WhiteKnight:
                    AddMoves(moveHandler, square, Attacks.Knight);
                    break;
                case Piece.BlackRook:
                case Piece.WhiteRook:
                    AddMoves(moveHandler, square, Attacks.Rook);
                    break;
                case Piece.BlackBishop:
                case Piece.WhiteBishop:
                    AddMoves(moveHandler, square, Attacks.Bishop);
                    break;
                case Piece.BlackQueen:
                case Piece.WhiteQueen:
                    AddMoves(moveHandler, square, Attacks.Queen);
                    break;
            }
        }

        private void AddCaptures(int square, Action<Move> moveHandler)
        {
            switch (_state[square])
            {
                case Piece.BlackPawn:
                    AddBlackPawnAttacks(moveHandler, square);
                    break;
                case Piece.WhitePawn:
                    AddWhitePawnAttacks(moveHandler, square);
                    break;
                case Piece.BlackKing:
                case Piece.WhiteKing:
                    AddCaptures(moveHandler, square, Attacks.King);
                    break;
                case Piece.BlackKnight:
                case Piece.WhiteKnight:
                    AddCaptures(moveHandler, square, Attacks.Knight);
                    break;
                case Piece.BlackRook:
                case Piece.WhiteRook:
                    AddCaptures(moveHandler, square, Attacks.Rook);
                    break;
                case Piece.BlackBishop:
                case Piece.WhiteBishop:
                    AddCaptures(moveHandler, square, Attacks.Bishop);
                    break;
                case Piece.BlackQueen:
                case Piece.WhiteQueen:
                    AddCaptures(moveHandler, square, Attacks.Queen);
                    break;
            }
        }

        private void AddMove(Action<Move> moveHandler, int from, int to) => moveHandler(new Move(from, to));

        private void AddPromotion(Action<Move> moveHandler, int from, int to, Piece promotion) => moveHandler(new Move(from, to, promotion));

        //*****************
        //** CHECK TEST ***
        //*****************

        public bool IsChecked(Color color)
        {
            Piece king = Piece.King.OfColor(color);
            for (int square = 0; square < 64; square++)
                if(_state[square] == king)
                    return IsSquareAttackedBy(square, Pieces.Flip(color));

            throw new Exception($"No {color} King found!");
        }

        public bool IsSquareAttackedBy(int square, Color color)
        {
            //1. Pawns? (if attacker is white, pawns move up and the square is attacked from below. squares below == Attacks.BlackPawn)
            var pawnAttacks = color == Color.White ? Attacks.BlackPawn : Attacks.WhitePawn;
            foreach (int target in pawnAttacks[square])
                if (_state[target] == Piece.Pawn.OfColor(color))
                    return true;

            //2. Knight
            foreach (int target in Attacks.Knight[square])
                if (_state[target] == Piece.Knight.OfColor(color))
                    return true;

            //3. Queen or Bishops on diagonals lines
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Bishop[square][dir])
                {
                    if (_state[target] == Piece.Bishop.OfColor(color) || _state[target] == Piece.Queen.OfColor(color))
                        return true;
                    if (_state[target] != Piece.None)
                        break;
                }

            //4. Queen or Rook on straight lines
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Rook[square][dir])
                {
                    if (_state[target] == Piece.Rook.OfColor(color) || _state[target] == Piece.Queen.OfColor(color))
                        return true;
                    if (_state[target] != Piece.None)
                        break;
                }

            //5. King
            foreach (int target in Attacks.King[square])
                if (_state[target] == Piece.King.OfColor(color))
                    return true;

            return false; //not threatened by anyone!
        }


        //****************
        //** CAPTURES **
        //****************

        private void AddCaptures(Action<Move> moveHandler, int square, byte[][] targets)
        {
            foreach (int target in targets[square])
                if (IsOpponentPiece(_state[target]))
                    AddMove(moveHandler, square, target);
        }

        private void AddCaptures(Action<Move> moveHandler, int square, byte[][][] targets)
        {
            foreach (var axis in targets[square])
                foreach (int target in axis)
                    if (_state[target] != Piece.None)
                    {
                        if (IsOpponentPiece(_state[target]))
                            AddMove(moveHandler, square, target);
                        break;
                    }
        }

        //****************
        //** MOVES **
        //****************

        private void AddMoves(Action<Move> moveHandler, int square, byte[][] targets)
        {
            foreach (int target in targets[square])
                if (_state[target] == Piece.None)
                    AddMove(moveHandler, square, target);
        }

        private void AddMoves(Action<Move> moveHandler, int square, byte[][][] targets)
        {
            foreach (var axis in targets[square])
                foreach (int target in axis)
                    if (_state[target] == Piece.None)
                        AddMove(moveHandler, square, target);
                    else
                        break;
        }

        //****************
        //** KING MOVES **
        //****************


        private void AddWhiteCastlingMoves(Action<Move> moveHandler)
        {
            //Castling is only possible if it's associated CastlingRight flag is set? it get's cleared when either the king or the matching rook move and provide a cheap early out
            if (HasCastlingRight(CastlingRights.WhiteQueenside) && CanCastle(WhiteKingSquare, WhiteQueensideRookSquare, Color.White))
                moveHandler(Move.WhiteCastlingLong);

            if (HasCastlingRight(CastlingRights.WhiteKingside) && CanCastle(WhiteKingSquare, WhiteKingsideRookSquare, Color.White))
                moveHandler(Move.WhiteCastlingShort);
        }


        private void AddBlackCastlingMoves(Action<Move> moveHandler)
        {
            if (HasCastlingRight(CastlingRights.BlackQueenside) && CanCastle(BlackKingSquare, BlackQueensideRookSquare, Color.Black))
                moveHandler(Move.BlackCastlingLong);

            if (HasCastlingRight(CastlingRights.BlackKingside) && CanCastle(BlackKingSquare, BlackKingsideRookSquare, Color.Black))
                moveHandler(Move.BlackCastlingShort);
        }

        private bool CanCastle(int kingSquare, int rookSquare, Color color)
        {
            Debug.Assert(_state[kingSquare] == Piece.King.OfColor(color), "CanCastle shouldn't be called if castling right has been lost!");
            Debug.Assert(_state[rookSquare] == Piece.Rook.OfColor(color), "CanCastle shouldn't be called if castling right has been lost!");

            Color enemyColor = Pieces.Flip(color);
            int gap = Math.Abs(rookSquare - kingSquare) - 1;
            int dir = Math.Sign(rookSquare - kingSquare);

            // the squares *between* the king and the rook need to be unoccupied
            for (int i = 1; i <= gap; i++)
                if (_state[kingSquare + i * dir] != Piece.None)
                    return false;

            //the king must not start, end or pass through a square that is attacked by an enemy piece. (but the rook and the square next to the rook on queenside may be attacked)
            for (int i = 0; i < 3; i++)
                if (IsSquareAttackedBy(kingSquare + i * dir, enemyColor))
                    return false;

            return true;
        }

        //*****************
        //** PAWN MOVES ***
        //*****************

        private void AddWhitePawnMoves(Action<Move> moveHandler, int square)
        {
            //if the square above isn't free there are no legal moves
            if (_state[Up(square)] != Piece.None)
                return;

            AddWhitePawnMove(moveHandler, square, Up(square));

            //START POS? => consider double move
            if (Rank(square) == 1 && _state[Up(square, 2)] == Piece.None)
                AddMove(moveHandler, square, Up(square, 2));
        }

        private void AddBlackPawnMoves(Action<Move> moveHandler, int square)
        {
            //if the square below isn't free there are no legal moves
            if (_state[Down(square)] != Piece.None)
                return;

            AddBlackPawnMove(moveHandler, square, Down(square));
            //START POS? => consider double move
            if (Rank(square) == 6 && _state[Down(square, 2)] == Piece.None)
                AddMove(moveHandler, square, Down(square, 2));
        }


        private void AddWhitePawnAttacks(Action<Move> moveHandler, int square)
        {
            foreach (int target in Attacks.WhitePawn[square])
                if (_state[target].IsBlack() || target == _enPassantSquare)
                    AddWhitePawnMove(moveHandler, square, target);
        }

        private void AddBlackPawnAttacks(Action<Move> moveHandler, int square)
        {
            foreach (int target in Attacks.BlackPawn[square])
                if (_state[target].IsWhite() || target == _enPassantSquare)
                    AddBlackPawnMove(moveHandler, square, target);
        }

        private void AddBlackPawnMove(Action<Move> moveHandler, int from, int to)
        {
            if (Rank(to) == 0) //Promotion?
            {
                AddPromotion(moveHandler, from, to, Piece.BlackQueen);
                AddPromotion(moveHandler, from, to, Piece.BlackRook);
                AddPromotion(moveHandler, from, to, Piece.BlackBishop);
                AddPromotion(moveHandler, from, to, Piece.BlackKnight);
            }
            else
                AddMove(moveHandler, from, to);
        }

        private void AddWhitePawnMove(Action<Move> moveHandler, int from, int to)
        {
            if (Rank(to) == 7) //Promotion?
            {
                AddPromotion(moveHandler, from, to, Piece.WhiteQueen);
                AddPromotion(moveHandler, from, to, Piece.WhiteRook);
                AddPromotion(moveHandler, from, to, Piece.WhiteBishop);
                AddPromotion(moveHandler, from, to, Piece.WhiteKnight);
            }
            else
                AddMove(moveHandler, from, to);
        }

        //**************
        //** Utility ***
        //**************
        private int Rank(int square) => square / 8;
        private int Up(int square, int steps = 1) => square + steps * 8;
        private int Down(int square, int steps = 1) => square - steps * 8;
        private bool IsActivePiece(Piece piece) => piece.Color() == _sideToMove;
        private bool IsOpponentPiece(Piece piece) => piece.Color() == Pieces.Flip(_sideToMove);
        private bool HasCastlingRight(CastlingRights flag) => (_castlingRights & flag) == flag;

        private void SetCastlingRights(CastlingRights flag, bool state)
        {
            _zobristHash ^= Zobrist.Castling(_castlingRights);

            if (state)
                _castlingRights |= flag;
            else
                _castlingRights &= ~flag;

            _zobristHash ^= Zobrist.Castling(_castlingRights);
        }

        private void InitZobristHash()
        {
            //Side to move
            _zobristHash = Zobrist.SideToMove(_sideToMove);
            //Pieces
            for (int square = 0; square < 64; square++)
                _zobristHash ^= Zobrist.PieceSquare(_state[square], square);
            //En passent
            _zobristHash ^= Zobrist.Castling(_castlingRights);
            //Castling
            _zobristHash ^= Zobrist.EnPassant(_enPassantSquare);
        }
    }
}
