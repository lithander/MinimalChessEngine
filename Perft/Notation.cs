﻿using System;

namespace Perft
{
    public static class Notation
    {
        public static char ToChar(Piece piece)
        {
            return piece switch
            {
                Piece.WhitePawn => 'P',
                Piece.WhiteKnight => 'N',
                Piece.WhiteBishop => 'B',
                Piece.WhiteRook => 'R',
                Piece.WhiteQueen => 'Q',
                Piece.WhiteKing => 'K',
                Piece.BlackPawn => 'p',
                Piece.BlackKnight => 'n',
                Piece.BlackBishop => 'b',
                Piece.BlackRook => 'r',
                Piece.BlackQueen => 'q',
                Piece.BlackKing => 'k',
                _ => ' ',
            };
        }

        public static Piece ToPiece(char ascii)
        {
            return ascii switch
            {
                'P' => Piece.WhitePawn,
                'N' => Piece.WhiteKnight,
                'B' => Piece.WhiteBishop,
                'R' => Piece.WhiteRook,
                'Q' => Piece.WhiteQueen,
                'K' => Piece.WhiteKing,
                'p' => Piece.BlackPawn,
                'n' => Piece.BlackKnight,
                'b' => Piece.BlackBishop,
                'r' => Piece.BlackRook,
                'q' => Piece.BlackQueen,
                'k' => Piece.BlackKing,
                _ => throw new ArgumentException($"Piece character {ascii} not supported."),
            };
        }

        internal static BoardState ToBoardState(string fen)
        {
            BoardState result = new BoardState();
            //Startpos in FEN looks like this: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            //https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
            string[] fields = fen.Split();
            if (fields.Length < 4)
                throw new ArgumentException($"FEN needs at least 4 fields. Has only {fields.Length} fields.");

            //Place pieces on board
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
                        result.SetBit(rank * 8 + file, Notation.ToPiece(piece));
                        file++;
                    }
                }
                rank--;
            }

            //Set enPassant flags
            int enPassant = fields[3] == "-" ? 0 : ToSquare(fields[3]);
            result.Flags = 1UL << enPassant;

            //Set sideToMove
            if (fields[1].Equals("w", StringComparison.CurrentCultureIgnoreCase))
                result.Flags |= BoardState.WhiteToMoveBit;
            
            //Set castling rights
            if (fields[2].IndexOf("K", StringComparison.Ordinal) > -1)
                result.CastleFlags |= BoardState.WhiteKingsideRookBit;

            if (fields[2].IndexOf("Q", StringComparison.Ordinal) > -1)
                result.CastleFlags |= BoardState.WhiteQueensideRookBit;

            if (fields[2].IndexOf("k", StringComparison.Ordinal) > -1)
                result.CastleFlags |= BoardState.BlackKingsideRookBit;

            if (fields[2].IndexOf("q", StringComparison.Ordinal) > -1)
                result.CastleFlags |= BoardState.BlackQueensideRookBit;

            return result;
        }

        public static string ToSquareName(byte squareIndex)
        {
            //This is the reverse of the ToSquareIndex()
            int rank = squareIndex / 8;
            int file = squareIndex % 8;

            //Map file [0..7] to letters [a..h] and rank [0..7] to [1..8]
            string squareNotation = $"{(char)('a' + file)}{rank + 1}";
            return squareNotation;
        }

        public static byte ToSquare(string squareNotation)
        {
            //Each square has a unique identification of file letter followed by rank number.
            //https://en.wikipedia.org/wiki/Algebraic_notation_(chess)
            //Examples: White's king starts the game on square e1; Black's knight on b8 can move to open squares a6 or c6.

            //Map letters [a..h] to [0..7] with ASCII('a') == 97
            int file = squareNotation[0] - 'a';
            //Map numbers [1..8] to [0..7] with ASCII('1') == 49
            int rank = squareNotation[1] - '1';
            int index = rank * 8 + file;

            if (index >= 0 && index <= 63)
                return (byte)index;

            throw new ArgumentException($"The given square notation {squareNotation} does not map to a valid index between 0 and 63");
        }
    }
}
