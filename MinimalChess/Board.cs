using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;

namespace MinimalChess
{
    public enum Piece
    {
        None = 0,
        WhitePawn = 1,
        WhiteKnight = 2,
        WhiteBishop = 3,
        WhiteRook = 4,
        WhiteQueen = 5,
        WhiteKing = 6,
        BlackPawn = 7,
        BlackKnight = 8,
        BlackBishop = 9,
        BlackRook = 10,
        BlackQueen = 11,
        BlackKing = 12,
    }

    public struct Move
    {
        public byte FromIndex;
        public byte ToIndex;
        public Piece Promotion;

        public Move(byte fromIndex, byte toIndex, Piece promotion)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Promotion = promotion;
        }

        public Move(string uciMoveNotation)
        {
            //The expected format is the oneto specify the move is in long algebraic notation without piece names
            https://en.wikipedia.org/wiki/Algebraic_notation_(chess)
            //Examples: e2e4, e7e5, e1g1(white short castling), e7e8q(for promotion)
            string fromSquare = uciMoveNotation.Substring(0, 2);
            string toSquare = uciMoveNotation.Substring(2, 2);
            FromIndex = Notation.ToSquareIndex(fromSquare);
            ToIndex = Notation.ToSquareIndex(toSquare);
            //the presence of a 5th character should mean promotion
            Promotion = (uciMoveNotation.Length == 5) ? Notation.ToPiece(uciMoveNotation[4]) : Piece.None;
        }

        public override bool Equals(object obj)
        {
            if (obj is Move move)
                return this.Equals(move);

            return false;
        }

        public bool Equals(Move other)
        {
            return (FromIndex == other.FromIndex) && (ToIndex == other.ToIndex) && (Promotion == other.Promotion);
        }

        public override int GetHashCode()
        {
            //int is big enough to represent move fully. maybe use that for optimization at some point
            return FromIndex + (ToIndex << 8) + ((int)Promotion << 16);
        }

        public static bool operator ==(Move lhs, Move rhs) => lhs.Equals(rhs);

        public static bool operator !=(Move lhs, Move rhs) => !lhs.Equals(rhs);

        public static Move BlackCastlingShort = new Move("e8g8");
        public static Move BlackCastlingLong = new Move("e8c8");
        public static Move WhiteCastlingShort = new Move("e1g1");
        public static Move WhiteCastlingLong = new Move("e1c1");

        public static Move BlackCastlingShortRook = new Move("h8f8");
        public static Move BlackCastlingLongRook = new Move("a8d8");
        public static Move WhiteCastlingShortRook = new Move("h1f1");
        public static Move WhiteCastlingLongRook = new Move("a1d1");
    }

    public static class Notation
    {
        public static char ToChar(Piece piece)
        {
            switch (piece)
            {
                case Piece.WhitePawn:
                    return 'P';
                case Piece.WhiteKnight:
                    return 'N';
                case Piece.WhiteBishop:
                    return 'B';
                case Piece.WhiteRook:
                    return 'R';
                case Piece.WhiteQueen:
                    return 'Q';
                case Piece.WhiteKing:
                    return 'K';
                case Piece.BlackPawn:
                    return 'p';
                case Piece.BlackKnight:
                    return 'n';
                case Piece.BlackBishop:
                    return 'b';
                case Piece.BlackRook:
                    return 'r';
                case Piece.BlackQueen:
                    return 'q';
                case Piece.BlackKing:
                    return 'k';
                default:
                    return ' ';
            }
        }

        public static Piece ToPiece(char ascii)
        {
            switch (ascii)
            {
                case 'P':
                    return Piece.WhitePawn;
                case 'N':
                    return Piece.WhiteKnight;
                case 'B':
                    return Piece.WhiteBishop;
                case 'R':
                    return Piece.WhiteRook;
                case 'Q':
                    return Piece.WhiteQueen;
                case 'K':
                    return Piece.WhiteKing;
                case 'p':
                    return Piece.BlackPawn;
                case 'n':
                    return Piece.BlackKnight;
                case 'b':
                    return Piece.BlackBishop;
                case 'r':
                    return Piece.BlackRook;
                case 'q':
                    return Piece.BlackQueen;
                case 'k':
                    return Piece.BlackKing;
                default:
                    throw new ArgumentException($"Piece character {ascii} not supported.");
            }
        }

        public static byte ToSquareIndex(string squareNotation)
        {
            //Each square has a unique identification of file letter followed by rank number.
            https://en.wikipedia.org/wiki/Algebraic_notation_(chess)
            //Examples: White's king starts the game on square e1; Black's knight on b8 can move to open squares a6 or c6.

            //Map letters [a..h] to [0..7] with ASCII('a') == 97
            int file = squareNotation[0] - 97;
            //Map numbers [1..8] to [0..7] with ASCII('1') == 49
            int rank = squareNotation[1] - 49;
            int index = rank * 8 + file;

            if(index >= 0 && index <= 63)
                return (byte)index;

            throw new ArgumentException($"The given square notation {squareNotation} does not map to a valid index between 0 and 63");
        }
    }

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
        public const string STARTING_POS_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public Board(string fen)
        {
            SetupPosition(fen);
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
            bool whiteMoves = fields[1].Equals("w", StringComparison.CurrentCultureIgnoreCase);
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
                Play(rookMove);
        }

        public bool IsCastling(Piece moving, Move move, out Move rookMove)
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

    }
}
