using System.Collections.Generic;

namespace MinimalChess
{
    public interface IMovesVisitor
    {
        public bool Done { get; }
        public void Consider(Move move);
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

        public void AddUnchecked(Move move)
        {
            Add(move);
        }
    }

    public class AnyLegalMoves : IMovesVisitor
    {
        private static Board _tempBoard = new Board();
        private Board _reference;

        public bool CanMove { get; private set; }

        private AnyLegalMoves(Board reference)
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

        public void AddUnchecked(Move move)
        {
            CanMove = true;
        }

        public static bool HasMoves(Board position)
        {
            var moves = new AnyLegalMoves(position);
            return moves.CanMove;
        }
    }

    public class MoveProbe : IMovesVisitor
    {
        public bool Done { get; private set; }
        private Move _move;

        private MoveProbe(Move reference)
        {
            _move = reference;
        }

        public static bool IsPseudoLegal(Board position, Move move)
        {
            MoveProbe probe = new MoveProbe(move);
            position.CollectMoves(probe, move.FromIndex);
            return probe.Done;
        }

        public void Consider(Move move)
        {
            if (_move == move)
                Done = true;
        }

        public void AddUnchecked(Move move)
        {
            if (_move == move)
                Done = true;
        }
    }

    public class MoveList : List<Move>, IMovesVisitor
    {
        public bool Done => false;

        public void Consider(Move move)
        {
            Add(move);
        }

        public void AddUnchecked(Move move)
        {
            Add(move);
        }

        internal static MoveList CollectQuiets(Board position)
        {
            MoveList quietMoves = new MoveList();
            position.CollectQuiets(quietMoves);
            return quietMoves;
        }

        internal static MoveList CollectCaptures(Board position)
        {
            MoveList captures = new MoveList();
            position.CollectCaptures(captures);
            return captures;
        }
    }
}
