using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public enum Color
    {
        Black = -1,
        White = +1
    }

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

    public enum PieceType
    {
        Pawn = 0,
        Knight = 1,
        Bishop = 2,
        Rook = 3,
        Queen = 4,
        King = 5
    }

    public static class Pieces
    {
        public static Color GetColor(Piece piece)
        {
            if (piece >= Piece.BlackPawn)
                return Color.Black;
            else if (piece >= Piece.WhitePawn)
                return Color.White;

            throw new ArgumentException(piece + " has no color!");
        }

        public static PieceType GetType(Piece piece)
        {
            if (piece >= Piece.BlackPawn)
                return (PieceType)piece - 7;
            else if (piece >= Piece.WhitePawn)
                return (PieceType)piece - 1;

            throw new ArgumentException(piece + " has no type!");
        }

        public static Piece GetPiece(PieceType type, Color color)
        {
            //if black offset type by 7 (-1 & 7 == 7) otherwise by 1 (1 & 7 == 1)
            return (Piece)(type + ((int)color & 7));
        }

        public static Color Flip(Color color)
        {
            return (Color)(-(int)color);
        }

        internal static bool IsWhite(Piece piece)
        {
            return piece >= Piece.WhitePawn && piece < Piece.BlackPawn;
        }

        internal static bool IsBlack(Piece piece)
        {
            return piece >= Piece.BlackPawn;
        }
    }
}
