using System;
using System.Collections.Generic;
using System.Globalization;

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
                    return 'k';
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

        public static int Location(string notation)
        {
            //Convert from ASCII to rank & file
            int file = notation[0] - 97;// [a..h] => [0..7] with ASCII('a') == 97
            int rank = notation[1] - 49;// [1..8] => [0..7] with ASCII('1') == 49
            int index = rank * 8 + file;
            return index;
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
            int square = 0;
            foreach (string rank in fenPosition)
            {
                foreach (char piece in rank)
                {
                    if (char.IsNumber(piece))
                    {
                        int emptySquares = (int)char.GetNumericValue(piece);
                        square += emptySquares;
                        continue;
                    }

                    this[square++] = Notation.ToPiece(piece);
                }
            }
            // Set side to move.
            bool whiteMoves = fields[1].Equals("w", StringComparison.CurrentCultureIgnoreCase);
            // Set castling rights.
            bool whiteKingside = fields[2].IndexOf("K", StringComparison.CurrentCulture) > -1;
            bool whiteQueenside = fields[2].IndexOf("Q", StringComparison.CurrentCulture) > -1;
            bool blackKingside = fields[2].IndexOf("k", StringComparison.CurrentCulture) > -1;
            bool blackQueenside = fields[2].IndexOf("q", StringComparison.CurrentCulture) > -1;
            // Set en passant square.
            int enPassantSquare = fields[3] == "-" ? -1 : Location(fields[3]);
            if(fields.Length == 6)
            {
                // Set half move count.
                int halfMoveCount = int.Parse(fields[4]);
                // Set full move number.
                int fullMoveNumber = int.Parse(fields[5]);

            }
        }
    }
}
