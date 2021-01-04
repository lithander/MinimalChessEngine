using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;

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
        Piece[] _state = new Piece[64];
        CastlingRights _castlingRights = CastlingRights.All;
        Color _activeColor = Color.White;
        int _enPassantSquare = -1;
        /*** STATE DATA ***/

        public Color ActiveColor => _activeColor;

        public const string STARTING_POS_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

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

            // Place pieces on board.
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
            
            // Set side to move.
            _activeColor = fields[1].Equals("w", StringComparison.CurrentCultureIgnoreCase) ? Color.White : Color.Black;
            
            // Set castling rights.
            SetCastlingRights(CastlingRights.WhiteKingside,  fields[2].IndexOf("K") > -1);
            SetCastlingRights(CastlingRights.WhiteQueenside, fields[2].IndexOf("Q") > -1);
            SetCastlingRights(CastlingRights.BlackKingside,  fields[2].IndexOf("k") > -1);
            SetCastlingRights(CastlingRights.BlackQueenside, fields[2].IndexOf("q") > -1);
            //TODO: Validate that pieces are at the places indicated by Castlling rights!

            // Set en passant square.
            _enPassantSquare = fields[3] == "-" ? -1 : Notation.ToSquareIndex(fields[3]);

            //Move counts
            if(fields.Length == 6)
            {
                // Set half move count.
                int halfMoveCount = int.Parse(fields[4]);
                // Set full move number.
                int fullMoveNumber = int.Parse(fields[5]);
            }
        }

        public void Play(Move move)
        {
            Piece movingPiece = _state[move.FromIndex];
            if (move.Promotion != Piece.None)
                movingPiece = Pieces.GetPiece(Pieces.GetType(move.Promotion), _activeColor);

            //move the correct piece to the target square
            _state[move.ToIndex] = movingPiece;
            //...and clear the square it was previously located
            _state[move.FromIndex] = Piece.None;

            if (IsEnPassant(movingPiece, move, out int captureIndex))
            {
                //capture the pawn
                _state[captureIndex] = Piece.None;
            }

            //handle castling special case
            if (IsCastling(movingPiece, move, out Move rookMove))
            {
                //move the rook to the target square and clear from square
                _state[rookMove.ToIndex] = _state[rookMove.FromIndex];
                _state[rookMove.FromIndex] = Piece.None;
            }

            //remember if move enables en passant
            UpdateEnPassent(move);

            //update castling rights
            UpdateCastlingRights(move.FromIndex);
            UpdateCastlingRights(move.ToIndex);

            //toggle active color!
            _activeColor = Pieces.Flip(_activeColor);
        }

        static readonly int BlackKingSquare = Notation.ToSquareIndex("e8");
        static readonly int WhiteKingSquare = Notation.ToSquareIndex("e1");
        static readonly int BlackQueensideRookSquare = Notation.ToSquareIndex("a8");
        static readonly int BlackKingsideRookSquare = Notation.ToSquareIndex("h8");
        static readonly int WhiteQueensideRookSquare = Notation.ToSquareIndex("a1");
        static readonly int WhiteKingsideRookSquare = Notation.ToSquareIndex("h1");

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
            else if(movingPiece == Piece.WhitePawn && move.ToIndex == _enPassantSquare)
            {
                captureIndex = Down(_enPassantSquare);
                return true;
            }
            else //not en passant
            {
                captureIndex = -1;
                return false;
            }
        }

        private bool IsCastling(Piece moving, Move move, out Move rookMove)
        {
            if (moving == Piece.BlackKing && move == Move.BlackCastlingLong)
            {
                rookMove = Move.BlackCastlingLongRook;
                return true;
            }
            else if(moving == Piece.BlackKing && move == Move.BlackCastlingShort)
            {
                rookMove = Move.BlackCastlingShortRook;
                return true;
            }
            else if (moving == Piece.WhiteKing && move == Move.WhiteCastlingLong)
            {
                rookMove = Move.WhiteCastlingLongRook;
                return true;
            }
            else if (moving == Piece.WhiteKing && move == Move.WhiteCastlingShort)
            {
                rookMove = Move.WhiteCastlingShortRook;
                return true;
            }
            else //not castling
            {
                rookMove = default;
                return false;
            }
        }

        //**********************
        //** MOVE GENERATION ***
        //**********************


        public void CollectMoves(IMovesVisitor visitor)
        {
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                if (IsActivePiece(_state[squareIndex]))
                    AddMoves(visitor, squareIndex);
        }

        private void AddMoves(IMovesVisitor moves, int squareIndex)
        {
            switch (_state[squareIndex])
            {
                case Piece.BlackPawn:
                    AddBlackPawnMoves(moves, squareIndex);
                    AddBlackPawnAttacks(moves, squareIndex);
                    break;
                case Piece.WhitePawn:
                    AddWhitePawnMoves(moves, squareIndex);
                    AddWhitePawnAttacks(moves, squareIndex);
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
                    AddQueenMoves(moves, squareIndex);
                    break;
            }
        }

        //*****************
        //** CHECK TEST ***
        //*****************

        public bool IsChecked(Color color)
        {
            //TODO: searching for the king takes time, maybe the bord state could store it
            Piece king = Pieces.GetPiece(PieceType.King, color);
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                if (_state[squareIndex] == king)
                {
                    Color enemyColor = Pieces.Flip(color);
                    return IsSquareAttacked(squareIndex, enemyColor);
                }

            throw new Exception($"Board state is missing a {king}!");
        }

        private bool IsSquareAttacked(int index, Color enemyColor)
        {
            int rank = Rank(index);
            int file = File(index);
            //Square could be threatened by...

            //1. Pawns
            if (enemyColor == Color.White)
            {
                //white pawns move up so king is threatened from below, only
                foreach (int target in Attacks.BlackPawn[index])
                    if (_state[target] == Piece.WhitePawn)
                        return true;
            }
            else if (enemyColor == Color.Black)
            {
                //black pawns move down so white king is threatened from above, only
                foreach (int target in Attacks.WhitePawn[index])
                    if (_state[target] == Piece.BlackPawn)
                        return true;
            }
            else
                return false; //Because it's not a King's square, duh

            //2. Knight
            Piece knight = Pieces.GetPiece(PieceType.Knight, enemyColor);
            foreach (int target in Attacks.Knight[index])
                if (_state[target] == knight)
                    return true;

            //3. King
            Piece king = Pieces.GetPiece(PieceType.King, enemyColor);
            foreach (int target in Attacks.King[index])
                if (_state[target] == king)
                    return true;

            Piece queen = Pieces.GetPiece(PieceType.Queen, enemyColor);
            Piece bishop = Pieces.GetPiece(PieceType.Bishop, enemyColor);
            Piece rook = Pieces.GetPiece(PieceType.Rook, enemyColor);
            for (int dir = 0; dir < 4; dir++)
            {
                //4. Queen or Bishops on diagonals lines
                foreach (int target in Attacks.Diagonal[index, dir])
                {
                    Piece piece = _state[target];
                    if (piece == bishop || piece == queen)
                        return true;
                    if (piece != Piece.None)
                        break;
                }

                //5. Queen or Rook on diagonals lines
                foreach (int target in Attacks.Straight[index, dir])
                {
                    Piece piece = _state[target];
                    if (piece == rook || piece == queen)
                        return true;
                    if (piece != Piece.None)
                        break;
                }
            }

            //...else
            return false;
        }

        //****************
        //** KING OVES ***
        //****************

        private void AddKingMoves(IMovesVisitor moves, int index)
        {
            foreach (int target in Attacks.King[index])
                if(IsValidTarget(_state[target]))
                    moves.Consider(index, target);
        }

        private void AddWhiteCastlingMoves(IMovesVisitor moves)
        {
            //Castling is only possible if it's associated CastlingRight flag is set? it get's cleared when either the king or the matching rook move and provide a cheap early out
            if (HasCastlingRight(CastlingRights.WhiteQueenside) && CanCastle(WhiteKingSquare, WhiteQueensideRookSquare, Color.White))
                moves.AddUnchecked(Move.WhiteCastlingLong);                    

            if (HasCastlingRight(CastlingRights.WhiteKingside) && CanCastle(WhiteKingSquare, WhiteKingsideRookSquare, Color.White))
                moves.AddUnchecked(Move.WhiteCastlingShort);
        }


        private void AddBlackCastlingMoves(IMovesVisitor moves)
        {
            if (HasCastlingRight(CastlingRights.BlackQueenside) && CanCastle(BlackKingSquare, BlackQueensideRookSquare, Color.Black))
                moves.AddUnchecked(Move.BlackCastlingLong);

            if (HasCastlingRight(CastlingRights.BlackKingside) && CanCastle(BlackKingSquare, BlackKingsideRookSquare, Color.Black))
                moves.AddUnchecked(Move.BlackCastlingShort);
        }

        private bool CanCastle(int kingSquare, int rookSquare, Color color)
        {
            Debug.Assert(_state[kingSquare] == Pieces.GetPiece(PieceType.King, color), "CanCastle shouldn't be called if castling right has been lost!");
            Debug.Assert(_state[rookSquare] == Pieces.GetPiece(PieceType.Rook, color), "CanCastle shouldn't be called if castling right has been lost!");

            Color enemyColor = Pieces.Flip(color);
            int gap = Math.Abs(rookSquare - kingSquare) - 1;
            int dir = Math.Sign(rookSquare - kingSquare);

            // the squares *between* the king and the rook need to be unoccupied
            for (int i = 1; i <= gap; i++)
                if (_state[kingSquare + i * dir] != Piece.None)
                    return false;

            //the king must not start, end or pass through a square that is attacked by an enemy piece. (but the rook and the square next to the rook on queenside may be attacked)
            for(int i = 0; i < 3; i++)
                if (IsSquareAttacked(kingSquare + i * dir, enemyColor))
                    return false;

            return true;
        }

        //*******************
        //** KNIGHT MOVES ***
        //*******************

        private void AddKnightMoves(IMovesVisitor moves, int index)
        {
            foreach (int target in Attacks.Knight[index])
                if (IsValidTarget(_state[target]))
                    moves.Consider(index, target);
        }

        //********************************
        //** QUEEN, ROOK, BISHOP MOVES ***
        //********************************

        private void AddQueenMoves(IMovesVisitor moves, int index)
        {
            //Queen moves are the union of bishop & rook
            AddBishopMoves(moves, index);
            AddRookMoves(moves, index);
        }

        private void AddBishopMoves(IMovesVisitor moves, int index)
        {
            for(int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Diagonal[index, dir])
                {
                    Piece piece = _state[target];
                    if (IsValidTarget(piece))
                        moves.Consider(index, target);

                    if (piece != Piece.None)
                        break;
                }
        }

        private void AddRookMoves(IMovesVisitor moves, int index)
        {
            for (int dir = 0; dir < 4; dir++)
                foreach (int target in Attacks.Straight[index, dir])
                {
                    Piece piece = _state[target];
                    if (IsValidTarget(piece))
                        moves.Consider(index, target);

                    if (piece != Piece.None)
                        break;
                }
        }

        //*****************
        //** PAWN MOVES ***
        //*****************

        private void AddWhitePawnMoves(IMovesVisitor moves, int index)
        {
            //if the square above isn't free there are no legal moves
            if (_state[Up(index)] != Piece.None)
                return;

            AddWhitePawnMove(moves, index, Up(index));

            //START POS? => consider double move
            if (Rank(index) == 1 && _state[Up(index, 2)] == Piece.None)
                moves.Consider(index, Up(index, 2));
        }

        private void AddBlackPawnMoves(IMovesVisitor moves, int index)
        {
            //if the square below isn't free there are no legal moves
            if (_state[Down(index)] != Piece.None)
                return;

            AddBlackPawnMove(moves, index, Down(index));
            //START POS? => consider double move
            if (Rank(index) == 6 && _state[Down(index, 2)] == Piece.None)
                moves.Consider(index, Down(index, 2));
        }


        private void AddWhitePawnAttacks(IMovesVisitor moves, int index)
        {
            foreach (int target in Attacks.WhitePawn[index])
                if (Pieces.IsBlack(_state[target]) || target == _enPassantSquare)
                    AddWhitePawnMove(moves, index, target);
        }

        private void AddBlackPawnAttacks(IMovesVisitor moves, int index)
        {
            foreach (int target in Attacks.BlackPawn[index])
                if (Pieces.IsWhite(_state[target]) || target == _enPassantSquare)
                    AddBlackPawnMove(moves, index, target);
        }

        private void AddBlackPawnMove(IMovesVisitor moves, int from, int to)
        {
            if(Rank(to) == 0) ///Promotion?
            {
                moves.Consider(from, to, Piece.BlackQueen);
                moves.Consider(from, to, Piece.BlackRook);
                moves.Consider(from, to, Piece.BlackBishop);
                moves.Consider(from, to, Piece.BlackKnight);
            }
            else
                moves.Consider(from, to);
        }

        private void AddWhitePawnMove(IMovesVisitor moves, int from, int to)
        {
            if (Rank(to) == 7) //Promotion?
            {
                moves.Consider(from, to, Piece.WhiteQueen);
                moves.Consider(from, to, Piece.WhiteRook);
                moves.Consider(from, to, Piece.WhiteBishop);
                moves.Consider(from, to, Piece.WhiteKnight);
            }
            else
                moves.Consider(from, to);
        }

        //**************
        //** Utility ***
        //**************
        private int Rank(int index) => index / 8;
        private int File(int index) => index % 8;
        private int Up(int index, int steps = 1) => index + steps * 8;
        private int Down(int index, int steps = 1) => index - steps * 8;

        private bool IsValidTarget(Piece piece) => piece == Piece.None || ((piece >= Piece.BlackPawn) ^ (_activeColor == Color.Black));

        private bool IsActivePiece(Piece piece) => piece > Piece.None && ((piece >= Piece.BlackPawn) ^ (_activeColor == Color.White));

        private void SetCastlingRights(CastlingRights flag, bool state)
        {
            if (state)
                _castlingRights |= flag;
            else
                _castlingRights &= ~flag;
        }

        private bool HasCastlingRight(CastlingRights flag) => (_castlingRights & flag) == flag;
    }
}
