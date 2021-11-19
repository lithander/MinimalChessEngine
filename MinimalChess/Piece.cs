using System;

namespace MinimalChess
{
    public enum Color
    {
        Black = -1,
        White = +1
    }

    [Flags]
    public enum Piece
    {
        //1st Bit = Piece or None?
        None = 0,

        //2nd Bit = White or Black? (implies the Piece bit to be set to)
        Black = 1,      //01
        White = 3,      //11

        //3rd+ Bits = Type of Piece
        Pawn = 4,       //00100
        Knight = 8,     //01000
        Bishop = 12,    //01100
        Rook = 16,      //10000
        Queen = 20,     //10100
        King = 24,      //11000

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

        //Flags
        Capture = 32,
        EnPassant = 64,
        Promotion = 128,
        Castle = 256,

        //Mask
        ColorMask = 3,
        TypeMask = 28      //11100
    }

    public static class Pieces
    {
        public const int MaxOrder = 6;

        //Pawn = 1, Knight = 2, Bishop = 3; Rook = 4, Queen = 5, King = 6
        public static int Order(Piece piece) => (int)piece >> 2;

        public static Piece Type(Piece piece) => piece & Piece.TypeMask;

        //subtracting 2 maps Piece.White (3) to Color.White (1) and Piece.Black (1) to Color.Black (-1)
        public static Color Color(this Piece piece) => (Color)((piece & Piece.ColorMask) - 2);

        //Use Piece.TypeMask to clear the two bits used for color, then set correct color bits
        //adding 2 maps Color.White (1) to Piece.White (3) and Color.Black (-1) to Piece.Black (1)
        public static Piece OfColor(this Piece piece, Color color) => Type(piece) | (Piece)(color + 2);

        public static Color Flip(Color color) => (Color)(-(int)color);

        public static bool IsColor(this Piece piece, Piece other) => (piece & Piece.ColorMask) == (other & Piece.ColorMask);
    }
}
