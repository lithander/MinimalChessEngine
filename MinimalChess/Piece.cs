using System;

namespace MinimalChess
{
    public enum Color
    {
        Black = -1,
        White = +1
    }

    [Flags]
    public enum Piece : sbyte
    {
        //1st Bit = Piece or None?
        None = 0,

        //2nd Bit = White or Black? (implies the Piece bit to be set to)
        Black = 1,
        White = 3,

        //3rd+ Bits = Type of Piece
        Pawn = 4,
        Knight = 8,
        Bishop = 12,
        Rook = 16,
        Queen = 20,
        King = 24,

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
        TypeMask = 127 - 3
    }

    public static class Pieces
    {
        public const int MaxOrder = 6;

        //Pawn = 1, Knight = 2, Bishop = 3; Rook = 4, Queen = 5, King = 6
        public static int Order(Piece piece) => (int)piece >> 2;

        //subtracting 2 maps Piece.White (3) to Color.White (1) and Piece.Black (1) to Color.Black (-1)
        public static Color Color(this Piece piece) => (Color)((piece & Piece.ColorMask) - 2);

        public static bool IsWhite(this Piece piece) => (piece & Piece.ColorMask) == Piece.White;
        public static bool IsBlack(this Piece piece) => (piece & Piece.ColorMask) == Piece.Black;

        //Use Piece.TypeMask to clear the two bits used for color, then set correct color bits
        //adding 2 maps Color.White (1) to Piece.White (3) and Color.Black (-1) to Piece.Black (1)
        public static Piece OfColor(this Piece piece, Color color) => (piece & Piece.TypeMask) | (Piece)(color + 2);

        public static Color Flip(Color color) => (Color)(-(int)color);

        public static bool IsColor(this Piece piece, Piece other) => (piece & Piece.ColorMask) == (other & Piece.ColorMask);
    }
}
