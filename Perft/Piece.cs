using System;

namespace Perft
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
        EnPassant = 32 + Pawn, //001_TM_CM
        KnightPromotion = 64 + Pawn, //010_TM_CM
        BishopPromotion = 96 + Pawn, //011_TM_CM
        RookPromotion = 128 + Pawn, //100_TM_CM
        QueenPromotion = 160 + Pawn, //101_TM_CM
        CastleShort = 192 + King, //110_TM_CM
        CastleLong = 224 + King, //111_TM_CM

        //Mask
        ColorMask = 3,
        TypeMask = 28,      //11100
    }
}
