using System;

namespace MinimalChess
{
    public enum Color
    {
        Black = -1,
        White = +1
    }

    public enum Piece
    {
        //1st Bit = Piece or None?
        None = 0,

        //2nd Bit = White or Black?
        Black = 1,
        White = 3,

        //3rd+ Bits = Type of Piece
        Pawn = 1 << 2,
        Knight = 2 << 2,
        Bishop = 3 << 2,
        Rook = 4 << 2,
        Queen = 5 << 2,
        King = 6 << 2,

        //White + Type = White Pieces
        WhitePawn = White + Pawn,
        WhiteKnight = White + Knight,
        WhiteBishop = White + Bishop,
        WhiteRook = White + Rook,
        WhiteQueen = White + Queen,
        WhiteKing = White + King,

        //Black + Type = Black Pieces
        BlackPawn = Black + Pawn,
        BlackKnight = Black + Knight,
        BlackBishop = Black + Bishop,
        BlackRook = Black + Rook,
        BlackQueen = Black + Queen,
        BlackKing = Black + King,

        //Mask
        ColorMask = 3,
        TypeMask = 255 - 3
    }

    public static class Pieces
    {
        //adding 2 maps Color.White (1) to Piece.White (3) and Color.Black (-1) to Piece.Black (1)
        public static Piece ColorFlags(Color color) => (Piece)(color + 2);

        //Use Piece.ColorMask to clear all bits execept the ones for color, then convert from Piece to Color by subtracting 2
        //subtracting 2 maps Piece.White (3) to Color.White (1) and Piece.Black (1) to Color.Black (-1)
        public static Color GetColor(this Piece piece) => (Color)((piece & Piece.ColorMask) - 2);

        //Use Piece.TypeMask to clear the two bits used for color, then set correct color bits
        public static Piece OfColor(this Piece piece, Color color) => (piece & Piece.TypeMask) | ColorFlags(color);

        internal static bool IsWhite(Piece piece) => (piece & Piece.ColorMask) == Piece.White;

        internal static bool IsBlack(Piece piece) => (piece & Piece.ColorMask) == Piece.Black;

        public static Color Flip(Color color) => (Color)(-(int)color);
    }
}
