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
        bool _whiteMovesNext = true;

        public bool WhiteMoves => _whiteMovesNext;
        public bool BlackMoves => !_whiteMovesNext;

        public const string STARTING_POS_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public Board(string fen)
        {
            SetupPosition(fen);
        }

        public Board(Board board)
        {
            Array.Copy(board._state, _state, 64);
            _whiteMovesNext = board._whiteMovesNext;
        }

        public Board(Board board, Move move)
        {
            Array.Copy(board._state, _state, 64);
            _whiteMovesNext = board._whiteMovesNext;
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
            _whiteMovesNext = board._whiteMovesNext;
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
            _whiteMovesNext = fields[1].Equals("w", StringComparison.CurrentCultureIgnoreCase);
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
                movingPiece = move.Promotion;

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
            _whiteMovesNext = !_whiteMovesNext;
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

        public List<Move> GetLegalMoves()
        {
            List<Move> moves = new List<Move>();
            for(int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
	            //depending on _whiteMovesNext filter all our own pieces
                if (_state[squareIndex] == Piece.None)
                    continue;

                //Piece is of the activeColor?
                if ((_state[squareIndex] < Piece.BlackPawn) ^ _whiteMovesNext) //XOR
                    continue;

                AddLegalMoves(moves, squareIndex);
            }
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
            }
        }

        //*****************
        //** KING MOVES ***
        //*****************

        int[] KING_MOVES_X = new int[8] { -1, 0, 1, 1, 1, 0, -1, -1 };
        int[] KING_MOVES_Y = new int[8] { -1, -1, -1, 0, 1, 1, 1, 0 };

        private void AddKingMoves(List<Move> moves, int index)
        {
            Piece self = _state[index];
            for(int i = 0; i < 8; i++)
                if (TryTransform(index, KING_MOVES_X[i], KING_MOVES_Y[i], out int target) && IsValidTarget(self, target))
                        moves.Add(new Move(index, target));
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
                moves.Add(new Move(index, Up(index, 2)));
        }

        private void AddBlackPawnMoves(List<Move> moves, int index)
        {
            //if the square below isn't free there are no legal moves
            if (_state[index - 8] != Piece.None)
                return;

            AddBlackPawnMove(moves, new Move(index, Down(index)));
            //START POS? => consider double move
            if (IsRank(7, index) && _state[Down(index, 2)] == Piece.None)
                moves.Add(new Move(index, Down(index, 2)));
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
            if(IsRank(1, move.ToIndex))
            {
                moves.Add(new Move(move.FromIndex, move.ToIndex, Piece.BlackQueen));
                moves.Add(new Move(move.FromIndex, move.ToIndex, Piece.BlackRook));
                moves.Add(new Move(move.FromIndex, move.ToIndex, Piece.BlackBishop));
                moves.Add(new Move(move.FromIndex, move.ToIndex, Piece.BlackKnight));
            }
            else
                moves.Add(move);
        }

        private void AddWhitePawnMove(List<Move> moves, Move move)
        {
            if (IsRank(8, move.ToIndex))
            {
                moves.Add(new Move(move.FromIndex, move.ToIndex, Piece.WhiteQueen));
                moves.Add(new Move(move.FromIndex, move.ToIndex, Piece.WhiteRook));
                moves.Add(new Move(move.FromIndex, move.ToIndex, Piece.WhiteBishop));
                moves.Add(new Move(move.FromIndex, move.ToIndex, Piece.WhiteKnight));
            }
            else
                moves.Add(move);
        }

        //Helper

        private bool IsBlackPiece(in int index) => _state[index] >= Piece.BlackPawn;

        private bool IsWhitePiece(in int index) => _state[index] != Piece.None && _state[index] < Piece.BlackPawn;

        private bool IsValidTarget(Piece self, int index)
        {
            //white * white > 0 && <  5 * 6
            //black * black >
            Piece target = _state[index];
            if (target == Piece.None)
                return true;
            if (target < Piece.BlackPawn)//occupied by white!
                return self >= Piece.BlackPawn; //self must be black or it can't land there
            else //occupied by black
                return self < Piece.BlackPawn;
        }

        //Index Helper

        private int Up(in int index) => index + 8;
        private int Up(in int index, int steps) => index + steps * 8;
        private int Down(in int index) => index - 8;
        private int Down(in int index, int steps) => index - steps * 8;

        private bool TryTransform(in int index, int files, int ranks, out int result)
        {
            int rank = index / 8 + ranks;
            int file = index % 8 + files;
            result = rank * 8 + file;
            return IsValid(rank, file);
        }

        private bool IsValid(in int rank, in int file) => (rank >= 0 && rank <= 7) && (file >= 0 && file <= 7);

        private bool IsRank(in int rank, in int index) => (index / 8) + 1 == rank;
    }
}
