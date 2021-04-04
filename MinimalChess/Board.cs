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
        static readonly int BlackKingSquare = Notation.ToSquareIndex("e8");
        static readonly int WhiteKingSquare = Notation.ToSquareIndex("e1");
        static readonly int BlackQueensideRookSquare = Notation.ToSquareIndex("a8");
        static readonly int BlackKingsideRookSquare = Notation.ToSquareIndex("h8");
        static readonly int WhiteQueensideRookSquare = Notation.ToSquareIndex("a1");
        static readonly int WhiteKingsideRookSquare = Notation.ToSquareIndex("h1");

        public const string STARTING_POS_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        [Flags]
        enum CastlingRights
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
        private Color _activeColor = Color.White;
        private int _enPassantSquare = -1;
        private int _blackKingSquare = 0;
        private int _whiteKingSquare = 0;
        /*** STATE DATA ***/

        public Color ActiveColor => _activeColor;

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
            _activeColor = board._activeColor;
            _enPassantSquare = board._enPassantSquare;
            _castlingRights = board._castlingRights;
            _blackKingSquare = board._blackKingSquare;
            _whiteKingSquare = board._whiteKingSquare;
            ValidateKingSquares();
        }

        public Piece this[int index]
        {
            get => _state[index];
            set => _state[index] = value;
        }

        //Rank - the eight horizontal rows of the chess board are called ranks.
        //File - the eight vertical columns of the chess board are called files.
        public Piece this[int rank, int file]
        {
            get => _state[rank * 8 + file];
            set => _state[rank * 8 + file] = value;
        }

        public void SetupPosition(string fen)
        {
            //Startpos in FEN looks like this: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
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
                        continue;
                    }
                    this[rank, file++] = Notation.ToPiece(piece);
                }
                rank--;
            }

            //Set side to move
            _activeColor = fields[1].Equals("w", StringComparison.CurrentCultureIgnoreCase) ? Color.White : Color.Black;

            //Set castling rights
            SetCastlingRights(CastlingRights.WhiteKingside, fields[2].IndexOf("K") > -1);
            SetCastlingRights(CastlingRights.WhiteQueenside, fields[2].IndexOf("Q") > -1);
            SetCastlingRights(CastlingRights.BlackKingside, fields[2].IndexOf("k") > -1);
            SetCastlingRights(CastlingRights.BlackQueenside, fields[2].IndexOf("q") > -1);

            //Set en-passant square
            _enPassantSquare = fields[3] == "-" ? -1 : Notation.ToSquareIndex(fields[3]);

            //Move counts
            if (fields.Length == 6)
            {
                // Set half move count.
                int halfMoveCount = int.Parse(fields[4]);
                // Set full move number.
                int fullMoveNumber = int.Parse(fields[5]);
            }

            //Init king squares
            _whiteKingSquare = -1;
            _blackKingSquare = -1;
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                if (_state[squareIndex] == Piece.BlackKing)
                    _blackKingSquare = squareIndex;
                if (_state[squareIndex] == Piece.WhiteKing)
                    _whiteKingSquare = squareIndex;
            }

            ValidateKingSquares();
        }

        [Conditional("DEBUG")]
        private void ValidateKingSquares()
        {
            if (_whiteKingSquare < 0 || _whiteKingSquare >= 64 || _state[_whiteKingSquare] != Piece.WhiteKing)
                throw new Exception($"No WhiteKing found on square {_whiteKingSquare}!");

            if (_blackKingSquare < 0 || _blackKingSquare >= 64 || _state[_blackKingSquare] != Piece.BlackKing)
                throw new Exception($"No BlackKing found on squre {_blackKingSquare}!");
        }

        //*****************
        //** PLAY MOVES ***
        //*****************

        public Piece Play(Move move)
        {
            ValidateKingSquares();

            Piece capturedPiece = _state[move.ToIndex];
            Piece movingPiece = _state[move.FromIndex];
            if (move.Promotion != Piece.None)
                movingPiece = move.Promotion.OfColor(_activeColor);

            //move the correct piece to the target square
            _state[move.ToIndex] = movingPiece;
            //...and clear the square it was previously located
            _state[move.FromIndex] = Piece.None;

            if (IsEnPassant(movingPiece, move, out int captureIndex))
            {
                //capture the pawn
                capturedPiece = _state[captureIndex];
                _state[captureIndex] = Piece.None;
            }

            //handle castling special case
            if (IsCastling(movingPiece, move, out Move rookMove))
            {
                //move the rook to the target square and clear from square
                _state[rookMove.ToIndex] = _state[rookMove.FromIndex];
                _state[rookMove.FromIndex] = Piece.None;
            }

            //update board state
            UpdateEnPassent(move);
            UpdateKingSquares(move);
            UpdateCastlingRights(move.FromIndex);
            UpdateCastlingRights(move.ToIndex);

            //toggle active color!
            _activeColor = Pieces.Flip(_activeColor);
            return capturedPiece;
        }

        private void UpdateCastlingRights(int squareIndex)
        {
            //any move from or to king or rook squares will effect castling right
            if (squareIndex == WhiteKingSquare || squareIndex == WhiteQueensideRookSquare)
                SetCastlingRights(CastlingRights.WhiteQueenside, false);
            if (squareIndex == WhiteKingSquare || squareIndex == WhiteKingsideRookSquare)
                SetCastlingRights(CastlingRights.WhiteKingside, false);

            if (squareIndex == BlackKingSquare || squareIndex == BlackQueensideRookSquare)
                SetCastlingRights(CastlingRights.BlackQueenside, false);
            if (squareIndex == BlackKingSquare || squareIndex == BlackKingsideRookSquare)
                SetCastlingRights(CastlingRights.BlackKingside, false);
        }

        private void UpdateKingSquares(Move move)
        {
            int to = move.ToIndex;
            Piece movingPiece = _state[move.ToIndex];

            if (movingPiece == Piece.WhiteKing)
                _whiteKingSquare = move.ToIndex;
            if (movingPiece == Piece.BlackKing)
                _blackKingSquare = move.ToIndex;
        }

        private void UpdateEnPassent(Move move)
        {
            int to = move.ToIndex;
            int from = move.FromIndex;
            Piece movingPiece = _state[to];

            //movingPiece needs to be either a BlackPawn...
            if (movingPiece == Piece.BlackPawn && Rank(to) == Rank(from) - 2)
                _enPassantSquare = Down(from);
            else if (movingPiece == Piece.WhitePawn && Rank(to) == Rank(from) + 2)
                _enPassantSquare = Up(from);
            else
                _enPassantSquare = -1;
        }

        private bool IsEnPassant(Piece movingPiece, Move move, out int captureIndex)
        {
            if (movingPiece == Piece.BlackPawn && move.ToIndex == _enPassantSquare)
            {
                captureIndex = Up(_enPassantSquare);
                return true;
            }
            else if (movingPiece == Piece.WhitePawn && move.ToIndex == _enPassantSquare)
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

        public void CollectMoves(Action<Move> moves)
        {
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                CollectMoves(moves, squareIndex);
        }

        public void CollectQuiets(Action<Move> moves)
        {
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                CollectQuiets(moves, squareIndex);
        }

        public void CollectCaptures(Action<Move> moves)
        {
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                CollectCaptures(moves, squareIndex);
        }

        public void CollectMoves(Action<Move> moves, int squareIndex)
        {
            CollectQuiets(moves, squareIndex);
            CollectCaptures(moves, squareIndex);
        }

        public void CollectQuiets(Action<Move> moves, int squareIndex)
        {
            if (IsActivePiece(_state[squareIndex]))
                AddQuiets(moves, squareIndex);
        }

        public void CollectCaptures(Action<Move> moves, int squareIndex)
        {
            if (IsActivePiece(_state[squareIndex]))
                AddCaptures(moves, squareIndex);
        }

        private void AddQuiets(Action<Move> moves, int squareIndex)
        {
            switch (_state[squareIndex])
            {
                case Piece.BlackPawn:
                    AddBlackPawnMoves(moves, squareIndex);
                    break;
                case Piece.WhitePawn:
                    AddWhitePawnMoves(moves, squareIndex);
                    break;
                case Piece.BlackKing:
                    AddBlackCastlingMoves(moves);
                    AddKingMoves(moves, squareIndex);
                    break;
                case Piece.WhiteKing:
                    AddWhiteCastlingMoves(moves);
                    AddKingMoves(moves, squareIndex);
                    break;
                case Piece.BlackKnight:
                case Piece.WhiteKnight:
                    AddKnightMoves(moves, squareIndex);
                    break;
                case Piece.BlackRook:
                case Piece.WhiteRook:
                    AddRookMoves(moves, squareIndex);
                    break;
                case Piece.BlackBishop:
                case Piece.WhiteBishop:
                    AddBishopMoves(moves, squareIndex);
                    break;
                case Piece.BlackQueen:
                case Piece.WhiteQueen:
                    AddRookMoves(moves, squareIndex);
                    AddBishopMoves(moves, squareIndex);
                    break;
            }
        }

        private void AddCaptures(Action<Move> moves, int squareIndex)
        {
            switch (_state[squareIndex])
            {
                case Piece.BlackPawn:
                    AddBlackPawnAttacks(moves, squareIndex);
                    break;
                case Piece.WhitePawn:
                    AddWhitePawnAttacks(moves, squareIndex);
                    break;
                case Piece.BlackKing:
                case Piece.WhiteKing:
                    AddKingCaptures(moves, squareIndex);
                    break;
                case Piece.BlackKnight:
                case Piece.WhiteKnight:
                    AddKnightCaptures(moves, squareIndex);
                    break;
                case Piece.BlackRook:
                case Piece.WhiteRook:
                    AddRookCaptures(moves, squareIndex);
                    break;
                case Piece.BlackBishop:
                case Piece.WhiteBishop:
                    AddBishopCaptures(moves, squareIndex);
                    break;
                case Piece.BlackQueen:
                case Piece.WhiteQueen:
                    AddRookCaptures(moves, squareIndex);
                    AddBishopCaptures(moves, squareIndex);
                    break;
            }
        }

        public void AddMove(Action<Move> moves, int from, int to)
        {
            moves(new Move(from, to));
        }

        public void AddPromotion(Action<Move> moves, int from, int to, Piece promotion)
        {
            moves(new Move(from, to, promotion));
        }

        //*****************
        //** CHECK TEST ***
        //*****************

        public bool IsChecked(Color color)
        {
            if (color == Color.White)
                return IsSquareAttacked(_whiteKingSquare, Color.Black);
            else
                return IsSquareAttacked(_blackKingSquare, Color.White);
        }

        private bool IsSquareAttacked(int index, Color attackedBy)
        {
            Piece color = Pieces.Color(attackedBy);
            //1. Pawns? (if attacker is white, pawns move up and the square is attacked from below. squares below == Attacks.BlackPawn)
            var pawnAttacks = attackedBy == Color.White ? Attacks.BlackPawn : Attacks.WhitePawn;
            foreach (int target in pawnAttacks[index])
                if (_state[target] == (Piece.Pawn | color))
                    return true;

            //2. Knight
            foreach (int target in Attacks.Knight[index])
                if (_state[target] == (Piece.Knight | color))
                    return true;

            //3. Queen or Bishops on diagonals lines
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Diagonal[index, dir])
                {
                    if (_state[target] == (Piece.Bishop | color) || _state[target] == (Piece.Queen | color))
                        return true;
                    if (_state[target] != Piece.None)
                        break;
                }

            //4. Queen or Rook on straight lines
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Straight[index, dir])
                {
                    if (_state[target] == (Piece.Rook | color) || _state[target] == (Piece.Queen | color))
                        return true;
                    if (_state[target] != Piece.None)
                        break;
                }

            //5. King
            foreach (int target in Attacks.King[index])
                if (_state[target] == (Piece.King | color))
                    return true;

            return false; //not threatened by anyone!
        }


        //****************
        //** CAPTURES **
        //****************

        private void AddKingCaptures(Action<Move> moves, int index)
        {
            foreach (int target in Attacks.King[index])
                if (IsOpponentPiece(_state[target]))
                    AddMove(moves, index, target);
        }

        private void AddKnightCaptures(Action<Move> moves, int index)
        {
            foreach (int target in Attacks.Knight[index])
                if (IsOpponentPiece(_state[target]))
                    AddMove(moves, index, target);
        }

        private void AddBishopCaptures(Action<Move> moves, int index)
        {
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Diagonal[index, dir])
                    if (_state[target] != Piece.None)
                    {
                        if (IsOpponentPiece(_state[target]))
                            AddMove(moves, index, target);
                        break;
                    }
        }

        private void AddRookCaptures(Action<Move> moves, int index)
        {
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Straight[index, dir])
                    if (_state[target] != Piece.None)
                    {
                        if (IsOpponentPiece(_state[target]))
                            AddMove(moves, index, target);
                        break;
                    }
        }

        //****************
        //** KING MOVES **
        //****************

        private void AddKingMoves(Action<Move> moves, int index)
        {
            foreach (int target in Attacks.King[index])
                if (_state[target] == Piece.None)
                    AddMove(moves, index, target);
        }
        private void AddWhiteCastlingMoves(Action<Move> moves)
        {
            //Castling is only possible if it's associated CastlingRight flag is set? it get's cleared when either the king or the matching rook move and provide a cheap early out
            if (HasCastlingRight(CastlingRights.WhiteQueenside) && CanCastle(WhiteKingSquare, WhiteQueensideRookSquare, Color.White))
                moves(Move.WhiteCastlingLong);

            if (HasCastlingRight(CastlingRights.WhiteKingside) && CanCastle(WhiteKingSquare, WhiteKingsideRookSquare, Color.White))
                moves(Move.WhiteCastlingShort);
        }


        private void AddBlackCastlingMoves(Action<Move> moves)
        {
            if (HasCastlingRight(CastlingRights.BlackQueenside) && CanCastle(BlackKingSquare, BlackQueensideRookSquare, Color.Black))
                moves(Move.BlackCastlingLong);

            if (HasCastlingRight(CastlingRights.BlackKingside) && CanCastle(BlackKingSquare, BlackKingsideRookSquare, Color.Black))
                moves(Move.BlackCastlingShort);
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
                if (IsSquareAttacked(kingSquare + i * dir, enemyColor))
                    return false;

            return true;
        }

        //*******************
        //** KNIGHT MOVES ***
        //*******************

        private void AddKnightMoves(Action<Move> moves, int index)
        {
            foreach (int target in Attacks.Knight[index])
                if (_state[target] == Piece.None)
                    AddMove(moves, index, target);
        }

        //********************************
        //** QUEEN, ROOK, BISHOP MOVES ***
        //********************************

        private void AddBishopMoves(Action<Move> moves, int index)
        {
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Diagonal[index, dir])
                    if (_state[target] == Piece.None)
                        AddMove(moves, index, target);
                    else
                        break;
        }

        private void AddRookMoves(Action<Move> moves, int index)
        {
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Straight[index, dir])
                    if (_state[target] == Piece.None)
                        AddMove(moves, index, target);
                    else
                        break;

        }

        //*****************
        //** PAWN MOVES ***
        //*****************

        private void AddWhitePawnMoves(Action<Move> moves, int index)
        {
            //if the square above isn't free there are no legal moves
            if (_state[Up(index)] != Piece.None)
                return;

            AddWhitePawnMove(moves, index, Up(index));

            //START POS? => consider double move
            if (Rank(index) == 1 && _state[Up(index, 2)] == Piece.None)
                AddMove(moves, index, Up(index, 2));
        }

        private void AddBlackPawnMoves(Action<Move> moves, int index)
        {
            //if the square below isn't free there are no legal moves
            if (_state[Down(index)] != Piece.None)
                return;

            AddBlackPawnMove(moves, index, Down(index));
            //START POS? => consider double move
            if (Rank(index) == 6 && _state[Down(index, 2)] == Piece.None)
                AddMove(moves, index, Down(index, 2));
        }


        private void AddWhitePawnAttacks(Action<Move> moves, int index)
        {
            foreach (int target in Attacks.WhitePawn[index])
                if (Pieces.IsBlack(_state[target]) || target == _enPassantSquare)
                    AddWhitePawnMove(moves, index, target);
        }

        private void AddBlackPawnAttacks(Action<Move> moves, int index)
        {
            foreach (int target in Attacks.BlackPawn[index])
                if (Pieces.IsWhite(_state[target]) || target == _enPassantSquare)
                    AddBlackPawnMove(moves, index, target);
        }

        private void AddBlackPawnMove(Action<Move> moves, int from, int to)
        {
            if (Rank(to) == 0) //Promotion?
            {
                AddPromotion(moves, from, to, Piece.BlackQueen);
                AddPromotion(moves, from, to, Piece.BlackRook);
                AddPromotion(moves, from, to, Piece.BlackBishop);
                AddPromotion(moves, from, to, Piece.BlackKnight);
            }
            else
                AddMove(moves, from, to);
        }

        private void AddWhitePawnMove(Action<Move> moves, int from, int to)
        {
            if (Rank(to) == 7) //Promotion?
            {
                AddPromotion(moves, from, to, Piece.WhiteQueen);
                AddPromotion(moves, from, to, Piece.WhiteRook);
                AddPromotion(moves, from, to, Piece.WhiteBishop);
                AddPromotion(moves, from, to, Piece.WhiteKnight);
            }
            else
                AddMove(moves, from, to);
        }

        //**************
        //** Utility ***
        //**************
        public bool HasLegalMoves => AnyLegalMoves.HasMoves(this);
        public bool CanPlay(Move move) => MoveProbe.IsPseudoLegal(this, move);
        private int Rank(int index) => index / 8;
        private int Up(int index, int steps = 1) => index + steps * 8;
        private int Down(int index, int steps = 1) => index - steps * 8;
        private bool IsActivePiece(Piece piece) => Pieces.Color(piece) == Pieces.Color(_activeColor);
        private bool IsOpponentPiece(Piece piece) => Pieces.Color(piece) == Pieces.OtherColor(_activeColor);
        private bool HasCastlingRight(CastlingRights flag) => (_castlingRights & flag) == flag;

        private void SetCastlingRights(CastlingRights flag, bool state)
        {
            if (state)
                _castlingRights |= flag;
            else
                _castlingRights &= ~flag;
        }

        public override bool Equals(object obj)
        {
            if (obj is Board board)
                return Equals(board);

            return false;
        }

        public bool Equals(Board other)
        {
            //Used for detecting repetition
            //From: https://en.wikipedia.org/wiki/Threefold_repetition
            //Two positions are by definition "the same" if... 
            //1.) the same player has the move 
            if (other._activeColor != _activeColor)
                return false;
            //2.) the remaining castling rights are the same and 
            if (other._castlingRights != _castlingRights)
                return false;
            //3.) the possibility to capture en passant is the same. 
            if (other._enPassantSquare != _enPassantSquare)
                return false;
            //4.) the same types of pieces occupy the same squares
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                if (other._state[squareIndex] != _state[squareIndex])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            //Boards that are equal should return the same hashcode!
            uint hash = 0;
            for (int squareIndex = 0; squareIndex < 32; squareIndex++)
            {
                if (_state[squareIndex] != Piece.None || _state[squareIndex + 32] != Piece.None)
                {
                    uint bit = (uint)(1 << squareIndex);
                    hash |= bit;
                }
            }
            return (int)hash;
        }
    }
}
