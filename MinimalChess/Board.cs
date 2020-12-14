using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;

namespace MinimalChess
{
    //    A  B  C  D  E  F  G  H
    // 8  00 01 02 03 04 05 06 07  8
    // 7  08 09 10 11 12 13 14 15  7
    // 6  16 17 18 19 20 21 22 23  6
    // 5  24 25 26 27 28 29 30 31  5
    // 4  32 33 34 35 36 37 38 39  4
    // 3  40 41 42 43 44 45 46 47  3
    // 2  48 49 50 51 52 53 54 55  2
    // 1  56 57 58 59 60 61 62 63  1
    //    A  B  C  D  E  F  G  H

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
                    AppendBlackPawnMoves(moves, squareIndex);
                    break;
                case Piece.WhitePawn:
                    AddWhitePawnMoves(moves, squareIndex);
                    break;
            }
        }

        private void AddWhitePawnMoves(List<Move> moves, int fromIndex)
        {
            //if the square above is free it's a legal move
            int aboveIndex = fromIndex + 8; //white moves up
            if (aboveIndex > 63)
                return;

            //TODO: handle promotion && handle diagonal attacks && and first moves
            if (_state[aboveIndex] == Piece.None)
                moves.Add(new Move((byte)fromIndex, (byte)aboveIndex, Piece.None));
        }

        private void AppendBlackPawnMoves(List<Move> moves, int fromIndex)
        {
            //if the square above is free it's a legal move
            int belowIndex = fromIndex - 8; //black moves down
            if (belowIndex < 0)
                return;

            //TODO: handle promotion && handle diagonal attacks && and first moves
            if (_state[belowIndex] == Piece.None)
                moves.Add(new Move((byte)fromIndex, (byte)belowIndex, Piece.None));
        }
    }
}
