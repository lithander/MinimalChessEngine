using System;

namespace MinimalChess
{
    public struct Move
    {
        public readonly byte FromSquare;
        public readonly byte ToSquare;
        public readonly Piece Promotion;

        public Move(int fromIndex, int toIndex)
        {
            FromSquare = (byte)fromIndex;
            ToSquare = (byte)toIndex;
            Promotion = Piece.None;
        }

        public Move(int fromIndex, int toIndex, Piece promotion)
        {
            FromSquare = (byte)fromIndex;
            ToSquare = (byte)toIndex;
            Promotion = promotion;
        }

        public Move(string uciMoveNotation)
        {
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

        public override bool Equals(object obj)
        {
            if (obj is Move move)
                return this.Equals(move);

            return false;
        }

        public bool Equals(Move other)
        {
            return (FromSquare == other.FromSquare) && (ToSquare == other.ToSquare) && (Promotion == other.Promotion);
        }

        public override int GetHashCode()
        {
            //int is big enough to represent move fully. maybe use that for optimization at some point
            return FromSquare + (ToSquare << 8) + ((int)Promotion << 16);
        }

        public static bool operator ==(Move lhs, Move rhs) => lhs.Equals(rhs);

        public static bool operator !=(Move lhs, Move rhs) => !lhs.Equals(rhs);

        public override string ToString()
        {
            //result represents the move in the long algebraic notation (without piece names)
            string result = Notation.ToSquareName(FromSquare);
            result += Notation.ToSquareName(ToSquare);
            //the presence of a 5th character should mean promotion
            if (Promotion != Piece.None)
                result += Notation.ToChar(Promotion);

            return result;
        }

        public static Move BlackCastlingShort = new Move("e8g8");
        public static Move BlackCastlingLong = new Move("e8c8");
        public static Move WhiteCastlingShort = new Move("e1g1");
        public static Move WhiteCastlingLong = new Move("e1c1");

        public static Move BlackCastlingShortRook = new Move("h8f8");
        public static Move BlackCastlingLongRook = new Move("a8d8");
        public static Move WhiteCastlingShortRook = new Move("h1f1");
        public static Move WhiteCastlingLongRook = new Move("a1d1");
    }
}
