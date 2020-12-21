using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        Piece[] _state = new Piece[64];
        Color _activeColor = Color.White;

        public Color ActiveColor => _activeColor;

        public const string STARTING_POS_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public Board(string fen)
        {
            SetupPosition(fen);
        }

        public Board(Board board)
        {
            Array.Copy(board._state, _state, 64);
            _activeColor = board._activeColor;
        }

        public Board(Board board, Move move)
        {
            Array.Copy(board._state, _state, 64);
            _activeColor = board._activeColor;
            Play(move);
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

        public void Setup(Board board, Move move)
        {
            Array.Copy(board._state, _state, 64);
            _activeColor = board._activeColor;
            Play(move);
        }

        public void SetupPosition(string fen)
        {
            //Startpos in FEN looks like this: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
            string[] fields = fen.Split();
            if (fields.Length < 4)
                throw new ArgumentException($"FEN needs at least 4 fields. Has only {fields.Length} fields.");

            Array.Clear(_state, 0, 64);
            // Place pieces on board.
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
            bool whiteKingside = fields[2].IndexOf("K", StringComparison.CurrentCulture) > -1;
            bool whiteQueenside = fields[2].IndexOf("Q", StringComparison.CurrentCulture) > -1;
            bool blackKingside = fields[2].IndexOf("k", StringComparison.CurrentCulture) > -1;
            bool blackQueenside = fields[2].IndexOf("q", StringComparison.CurrentCulture) > -1;
            // Set en passant square.
            int enPassantSquare = fields[3] == "-" ? -1 : Notation.ToSquareIndex(fields[3]);
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

            //handle castling special case
            if (IsCastling(movingPiece, move, out Move rookMove))
            {
                //move the rook to the target square and clear from square
                _state[rookMove.ToIndex] = _state[rookMove.FromIndex];
                _state[rookMove.FromIndex] = Piece.None;
            }

            //toggle active color!
            _activeColor = Pieces.Flip(_activeColor);
        }

        private bool IsCastling(Piece moving, Move move, out Move rookMove)
        {
            if (moving == Piece.BlackKing && move == Move.BlackCastlingLong)
            {
                rookMove = Move.BlackCastlingLongRook;
                return true;
            }
            
            if(moving == Piece.BlackKing && move == Move.BlackCastlingShort)
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

        int[] DIAGONALS_X = new int[4] { -1, 1, 1, -1 };
        int[] DIAGONALS_Y = new int[4] { -1, -1, 1, 1 };

        int[] STRAIGHTS_X = new int[4] { -1, 0, 1, 0 };
        int[] STRAIGHTS_Y = new int[4] { -0, -1, 0, 1 };

        int[] KING_MOVES_X = new int[8] { -1, 0, 1, 1, 1, 0, -1, -1 };
        int[] KING_MOVES_Y = new int[8] { -1, -1, -1, 0, 1, 1, 1, 0 };

        int[] KNIGHT_MOVES_X = new int[8] { -1, -2, 1, 2, -1, -2, 1, 2 };
        int[] KNIGHT_MOVES_Y = new int[8] { -2, -1, -2, -1, 2, 1, 2, 1 };

        public List<Move> GetLegalMoves()
        {
            List<Move> moves = new List<Move>();

            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                if (Pieces.HasColor(_state[squareIndex], _activeColor))
                    AddLegalMoves(moves, squareIndex);

            return moves;
        }

        private void AddLegalMoves(List<Move> moves, int squareIndex)
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
                case Piece.WhiteKing:
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

        private static Board _tempBoard = new Board(STARTING_POS_FEN);
        private void Add(List<Move> moves, Move move)
        {
            //only add if the move doesn't result in a check for active color
            _tempBoard.Setup(this, move);
            if (!_tempBoard.IsChecked(_activeColor))
                moves.Add(move);
        }

        //*****************
        //** CHECK TEST ***
        //*****************

        public bool IsChecked(Color color)
        {
            Piece king = Pieces.GetPiece(PieceType.King, color);
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
                if (_state[squareIndex] == king)
                    return IsKingSquareInCheck(squareIndex);

            throw new Exception($"Board state is missing a {king}!");
        }

        private bool IsKingSquareInCheck(int index)
        {
            Piece king = _state[index];
            Debug.Assert(king == Piece.BlackKing || king == Piece.WhiteKing);
            Color otherColor = Pieces.Flip(Pieces.GetColor(king));
            //King could be threatened by...

            //1. Pawns
            if (king == Piece.BlackKing)
            {
                //white pawns move up so king is threatened from below, only
                if (TryTransform(index, -1, -1, out int downLeft) && _state[downLeft] == Piece.WhitePawn)
                    return true;

                if (TryTransform(index, 1, -1, out int downRight) && _state[downRight] == Piece.WhitePawn)
                    return true;
            }
            else if (king == Piece.WhiteKing)
            {
                //black pawns move down so white king is threatened from above, only
                if (TryTransform(index, -1, 1, out int upLeft) && _state[upLeft] == Piece.BlackPawn)
                    return true;

                if (TryTransform(index, 1, 1, out int upRight) && _state[upRight] == Piece.BlackPawn)
                    return true;
            }
            else
                return false; //Because it's not a King's square, duh

            //2. Look for King on surrounding squares
            Piece otherKing = Pieces.Flip(king);
            for (int i = 0; i < 8; i++)
                if (TryTransform(index, KING_MOVES_X[i], KING_MOVES_Y[i], out int target) && _state[target] == otherKing)
                    return true;

            //3. Look for Queen or Bishops on diagonals lines
            Piece otherQueen = Pieces.GetPiece(PieceType.Queen, otherColor);
            Piece otherBishop = Pieces.GetPiece(PieceType.Bishop, otherColor);
            for (int dir = 0; dir < 4; dir++)
            {
                int current = index;
                while (TryTransform(ref current, DIAGONALS_X[dir], DIAGONALS_Y[dir]))
                {
                    Piece piece = _state[current];
                    if (piece == otherBishop || piece == otherQueen)
                        return true;
                    if (piece != Piece.None)
                        break;
                }
            }

            //4. Look for Queen or Rooks on straight lines
            Piece otherRook = Pieces.GetPiece(PieceType.Rook, otherColor);
            for (int dir = 0; dir < 4; dir++)
            {
                int current = index;
                while (TryTransform(ref current, STRAIGHTS_X[dir], STRAIGHTS_Y[dir]))
                {
                    Piece piece = _state[current];
                    if (piece == otherRook || piece == otherQueen)
                        return true;
                    if (piece != Piece.None)
                        break;
                }
            }

            //5. Knight on the Knight-Squares
            Piece otherKnight = Pieces.GetPiece(PieceType.Knight, otherColor);
            for (int i = 0; i < 8; i++)
                if (TryTransform(index, KNIGHT_MOVES_X[i], KNIGHT_MOVES_Y[i], out int target) && _state[target] == otherKnight)
                    return true;

            //...else
            return false;
        }

        //*****************
        //** KING MOVES ***
        //*****************

        private void AddKingMoves(List<Move> moves, int index)
        {
            for(int i = 0; i < 8; i++)
                if (TryTransform(index, KING_MOVES_X[i], KING_MOVES_Y[i], out int current))
                {
                    Piece target = _state[current];
                    if (target == Piece.None || Pieces.GetColor(target) != _activeColor)
                        Add(moves, new Move(index, current));
                }
        }

        //*******************
        //** KNIGHT MOVES ***
        //*******************

        private void AddKnightMoves(List<Move> moves, int index)
        {
            for (int i = 0; i < 8; i++)
                if (TryTransform(index, KNIGHT_MOVES_X[i], KNIGHT_MOVES_Y[i], out int current))
                {
                    Piece target = _state[current];
                    if (target == Piece.None || Pieces.GetColor(target) != _activeColor)
                        Add(moves, new Move(index, current));
                }
        }

        //******************
        //** QUEEN MOVES ***
        //******************

        private void AddQueenMoves(List<Move> moves, int index)
        {
            //Queen moves are the union of bishop & rook
            AddBishopMoves(moves, index);
            AddRookMoves(moves, index);
        }

        //*******************
        //** BISHOP MOVES ***
        //*******************

        private void AddBishopMoves(List<Move> moves, int index)
        {
            //3. Look for Queen or Bishops on diagonals lines
            for (int dir = 0; dir < 4; dir++)
            {
                int current = index;
                while (TryTransform(ref current, DIAGONALS_X[dir], DIAGONALS_Y[dir]))
                {
                    Piece target = _state[current];

                    if (target == Piece.None || Pieces.GetColor(target) != _activeColor)
                        Add(moves, new Move(index, current));

                    if(target != Piece.None)
                        break;
                }
            }
        }

        //*****************
        //** ROOK MOVES ***
        //*****************

        private void AddRookMoves(List<Move> moves, int index)
        {
            for (int dir = 0; dir < 4; dir++)
            {
                int current = index;
                while (TryTransform(ref current, STRAIGHTS_X[dir], STRAIGHTS_Y[dir]))
                {
                    Piece target = _state[current];

                    if (target == Piece.None || Pieces.GetColor(target) != _activeColor)
                        Add(moves, new Move(index, current));

                    if (target != Piece.None)
                        break;
                }
            }
        }

        //*****************
        //** PAWN MOVES ***
        //*****************

        private void AddWhitePawnMoves(List<Move> moves, int index)
        {
            //if the square above isn't free there are no legal moves
            if (_state[Up(index)] != Piece.None)
                return;

            AddWhitePawnMove(moves, new Move(index, Up(index)));

            //START POS? => consider double move
            if (IsRank(2, index) && _state[Up(index, 2)] == Piece.None)
                Add(moves, new Move(index, Up(index, 2)));
        }

        private void AddBlackPawnMoves(List<Move> moves, int index)
        {
            //if the square below isn't free there are no legal moves
            if (_state[index - 8] != Piece.None)
                return;

            AddBlackPawnMove(moves, new Move(index, Down(index)));
            //START POS? => consider double move
            if (IsRank(7, index) && _state[Down(index, 2)] == Piece.None)
                Add(moves, new Move(index, Down(index, 2)));
        }

        private void AddWhitePawnAttacks(List<Move> moves, int index)
        {
            if(TryTransform(index, -1, 1, out int upLeft) && IsBlackPiece(upLeft))
                AddWhitePawnMove(moves, new Move(index, upLeft));

            if (TryTransform(index, 1, 1, out int upRight) && IsBlackPiece(upRight))
                AddWhitePawnMove(moves, new Move(index, upRight));
        }

        private void AddBlackPawnAttacks(List<Move> moves, int index)
        {
            if (TryTransform(index, -1, -1, out int downLeft) && IsWhitePiece(downLeft))
                AddBlackPawnMove(moves, new Move(index, downLeft));

            if (TryTransform(index, 1, -1, out int downRight) && IsWhitePiece(downRight))
                AddBlackPawnMove(moves, new Move(index, downRight));
        }

        private void AddBlackPawnMove(List<Move> moves, Move move)
        {
            if(IsRank(1, move.ToIndex)) ///Promotion?
            {
                Add(moves, new Move(move.FromIndex, move.ToIndex, Piece.BlackQueen));
                Add(moves, new Move(move.FromIndex, move.ToIndex, Piece.BlackRook));
                Add(moves, new Move(move.FromIndex, move.ToIndex, Piece.BlackBishop));
                Add(moves, new Move(move.FromIndex, move.ToIndex, Piece.BlackKnight));
            }
            else
                Add(moves, move);
        }

        private void AddWhitePawnMove(List<Move> moves, Move move)
        {
            if (IsRank(8, move.ToIndex)) //Promotion?
            {
                Add(moves, new Move(move.FromIndex, move.ToIndex, Piece.WhiteQueen));
                Add(moves, new Move(move.FromIndex, move.ToIndex, Piece.WhiteRook));
                Add(moves, new Move(move.FromIndex, move.ToIndex, Piece.WhiteBishop));
                Add(moves, new Move(move.FromIndex, move.ToIndex, Piece.WhiteKnight));
            }
            else
                Add(moves, move);
        }

        //**************
        //** Utility ***
        //**************

        private int Up(in int index) => index + 8;
        private int Up(in int index, int steps) => index + steps * 8;
        private int Down(in int index) => index - 8;
        private int Down(in int index, int steps) => index - steps * 8;

        private bool TryTransform(in int index, int files, int ranks, out int result)
        {
            int rank = index / 8 + ranks;
            int file = index % 8 + files;
            result = rank * 8 + file;
            return IsValidSquare(rank, file);
        }

        private bool TryTransform(ref int index, int files, int ranks)
        {
            int rank = index / 8 + ranks;
            int file = index % 8 + files;
            index = rank * 8 + file;
            return IsValidSquare(rank, file);
        }

        private bool IsValidSquare(in int rank, in int file) => (rank >= 0 && rank <= 7) && (file >= 0 && file <= 7);

        private bool IsRank(in int rank, in int index) => (index / 8) + 1 == rank;

        private bool IsBlackPiece(in int index) => _state[index] >= Piece.BlackPawn;

        private bool IsWhitePiece(in int index) => _state[index] != Piece.None && _state[index] < Piece.BlackPawn;
    }
}
