using System.Collections.Generic;

namespace MinimalChess
{
    public class LegalMoves : List<Move>
    {
        private static Board _tempBoard = new Board();
        private Board _reference;

        public LegalMoves(Board reference) : base(40)
        {
            _reference = reference;
            _reference.CollectMoves(Consider);
            _reference = null;
        }

        public void Consider(Move move)
        {
            //only add if the move doesn't result in a check for active color
            _tempBoard.Copy(_reference);
            _tempBoard.Play(move);
            if (_tempBoard.IsChecked(_reference.ActiveColor))
                return;

            Add(move);
        }
    }

    public class AnyLegalMoves
    {
        private static Board _tempBoard = new Board();
        private Board _reference;

        public bool CanMove { get; private set; }

        public AnyLegalMoves(Board reference)
        {
            _reference = reference;
            _reference.CollectQuiets(Consider);
            if (!CanMove)
                _reference.CollectCaptures(Consider);
            _reference = null;
        }

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

        public static bool HasMoves(Board position)
        {
            var moves = new AnyLegalMoves(position);
            return moves.CanMove;
        }
    }

    public class MoveProbe
    {
        public static bool IsPseudoLegal(Board position, Move move)
        {
            bool found = false;
            position.CollectMoves(m => found |= (m == move), move.FromIndex);
            return found;
        }
    }

    public class MoveList : List<Move>
    {
        internal static MoveList CollectQuiets(Board position)
        {
            MoveList quietMoves = new MoveList();
            position.CollectQuiets(quietMoves.Add);
            return quietMoves;
        }

        internal static MoveList CollectCaptures(Board position)
        {
            MoveList captures = new MoveList();
            position.CollectCaptures(captures.Add);
            return captures;
        }
    }
}
