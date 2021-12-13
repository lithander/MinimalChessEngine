using System;

namespace Perft
{
    public struct Move
    {
        public readonly Piece Flags;
        public readonly byte FromSquare;
        public readonly byte ToSquare;
        public readonly Piece Promotion;

        public Move(Piece flags, int fromIndex, int toIndex, Piece promotion)
        {
            Flags = flags | Piece.Promotion;
            FromSquare = (byte)fromIndex;
            ToSquare = (byte)toIndex;
            Promotion = promotion;
        }

        public Move(Piece flags, int fromIndex, int toIndex)
        {
            Flags = flags;
            FromSquare = (byte)fromIndex;
            ToSquare = (byte)toIndex;
            Promotion = Piece.None;
        }

        public Move(string uciMoveNotation, Piece flags = Piece.None)
        {
            Flags = flags;
            if (uciMoveNotation.Length < 4)
                throw new ArgumentException($"Long algebraic notation expected. '{uciMoveNotation}' is too short!");
            if (uciMoveNotation.Length > 5)
                throw new ArgumentException($"Long algebraic notation expected. '{uciMoveNotation}' is too long!");

            //expected format is the long algebraic notation without piece names
            //https://en.wikipedia.org/wiki/Algebraic_notation_(chess)
            //Examples: e2e4, e7e5, e1g1(white short castling), e7e8q(for promotion)
            string fromSquare = uciMoveNotation.Substring(0, 2);
            string toSquare = uciMoveNotation.Substring(2, 2);
            FromSquare = Notation.ToSquare(fromSquare);
            ToSquare = Notation.ToSquare(toSquare);
            //the presence of a 5th character should mean promotion
            Promotion = (uciMoveNotation.Length == 5) ? Notation.ToPiece(uciMoveNotation[4]) : Piece.None;
        }

        public static Move BlackCastlingShort = new("e8g8", Piece.BlackKing | Piece.Castle);
        public static Move BlackCastlingLong = new("e8c8", Piece.BlackKing | Piece.Castle);
        public static Move WhiteCastlingShort = new("e1g1", Piece.WhiteKing | Piece.Castle);
        public static Move WhiteCastlingLong = new("e1c1", Piece.WhiteKing | Piece.Castle);
    }
}
