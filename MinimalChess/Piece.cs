using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public enum Color
    {
        Black = 0,
        White = 1
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
            return (Piece)(type + ((color == Color.Black) ? 7 : 1));
        }

        public static Color Flip(Color color)
        {
            return (color ^ Color.White);
        }

        public static Piece Flip(Piece piece)
        {
            return GetPiece(GetType(piece), Flip(GetColor(piece)));
        }

        internal static bool HasColor(Piece piece, Color nextMove)
        {
            if (piece == Piece.None)
                return false;

            //if (piece >= Piece.BlackPawn)
            //    return nextMove == Color.Black;
            //else
            //    return nextMove == Color.White;

            return (piece >= Piece.BlackPawn) ^ (nextMove == Color.White);
        }
    }
}
