using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public struct Move
    {
        public byte FromIndex;
        public byte ToIndex;
        public Piece Promotion;

        public Move(byte fromIndex, byte toIndex, Piece promotion)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Promotion = promotion;
        }

        public Move(int fromIndex, int toIndex)
        {
            FromIndex = (byte)fromIndex;
            ToIndex = (byte)toIndex;
            Promotion = Piece.None;
        }

        public Move(int fromIndex, int toIndex, Piece promotion)
        {
            FromIndex = (byte)fromIndex;
            ToIndex = (byte)toIndex;
            Promotion = promotion;
        }

        public Move(string uciMoveNotation)
        {
            if (uciMoveNotation.Length < 4)
                throw new ArgumentException($"Long algebraic notation expected. '{uciMoveNotation}' is too short!");
            if (uciMoveNotation.Length > 5)
                throw new ArgumentException($"Long algebraic notation expected. '{uciMoveNotation}' is too long!");

            //expected format is the long algebraic notation without piece names
            https://en.wikipedia.org/wiki/Algebraic_notation_(chess)
            //Examples: e2e4, e7e5, e1g1(white short castling), e7e8q(for promotion)
            string fromSquare = uciMoveNotation.Substring(0, 2);
            string toSquare = uciMoveNotation.Substring(2, 2);
            FromIndex = Notation.ToSquareIndex(fromSquare);
            ToIndex = Notation.ToSquareIndex(toSquare);
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
            return (FromIndex == other.FromIndex) && (ToIndex == other.ToIndex) && (Promotion == other.Promotion);
        }

        public override int GetHashCode()
        {
            //int is big enough to represent move fully. maybe use that for optimization at some point
            return FromIndex + (ToIndex << 8) + ((int)Promotion << 16);
        }

        public static bool operator ==(Move lhs, Move rhs) => lhs.Equals(rhs);

        public static bool operator !=(Move lhs, Move rhs) => !lhs.Equals(rhs);

        public override string ToString()
        {
            //result represents the move in the long algebraic notation (without piece names)
            string result = Notation.ToSquareName(FromIndex);
            result += Notation.ToSquareName(ToIndex);
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

    public interface IMovesVisitor
    {
        public bool Done { get; }
        public void Consider(Move move);
        public void Consider(int from, int to);
        public void Consider(int from, int to, Piece promotion);
        void AddUnchecked(Move move);
    }

    public class LegalMoves : List<Move>, IMovesVisitor
    {
        private static Board _tempBoard = new Board();
        private Board _reference;

        public LegalMoves(Board reference) : base(40)
        {
            _reference = reference;
            _reference.CollectMoves(this);
            _reference = null;
        }

        public bool Done => false;

        public void Consider(Move move)
        {
            //only add if the move doesn't result in a check for active color
            _tempBoard.Copy(_reference);
            _tempBoard.Play(move);
            if (_tempBoard.IsChecked(_reference.ActiveColor))
                return;

            Add(move);
        }

        public void Consider(int from, int to, Piece promotion)
        {
            Consider(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Consider(new Move(from, to));
        }

        public void AddUnchecked(Move move)
        {
            Add(move);
        }

        public void Randomize()
        {
            Random rnd = new Random();
            for(int i = 0; i < Count; i++)
            {
                int j = rnd.Next(0, Count);
                //swap i with j
                Move temp = this[i];
                this[i] = this[j];
                this[j] = temp;
            }
        }
    }

    public class PseudoLegalMoves : List<Move>, IMovesVisitor
    {
        private static Board _tempBoard = new Board();
        private Board _reference;

        public PseudoLegalMoves(Board reference) : base(40)
        {
            _reference = reference;
            _reference.CollectMoves(this);
            _reference = null;
        }

        public bool Done => false;

        public void Consider(Move move)
        {
            //only add if the move doesn't result in a check for active color
            Add(move);
        }

        public void Consider(int from, int to, Piece promotion)
        {
            Add(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Add(new Move(from, to));
        }

        public void AddUnchecked(Move move)
        {
            Add(move);
        }

        public void Randomize()
        {
            Random rnd = new Random();
            for (int i = 0; i < Count; i++)
            {
                int j = rnd.Next(0, Count);
                //swap i with j
                Move temp = this[i];
                this[i] = this[j];
                this[j] = temp;
            }
        }
    }

    public class AnyLegalMoves : IMovesVisitor
    {
        private static Board _tempBoard = new Board();
        private Board _reference;

        public bool CanMove { get; private set; }

        public AnyLegalMoves(Board reference)
        {
            _reference = reference;
            _reference.CollectMoves(this);
            _reference = null;
        }

        public bool Done => CanMove;

        public void Consider(Move move)
        {
            if (CanMove)//no need to look at any more moves if we got our answer already!
                return;

            //only add if the move doesn't result in a check for active color
            _tempBoard.Copy(_reference);
            _tempBoard.Play(move);
            if (_tempBoard.IsChecked(_reference.ActiveColor))
                return;

            CanMove = true;
        }

        public void Consider(int from, int to, Piece promotion)
        {
            Consider(new Move(from, to, promotion));
        }

        public void Consider(int from, int to)
        {
            Consider(new Move(from, to));
        }

        public void AddUnchecked(Move move)
        {
            CanMove = true;
        }
    }
}
