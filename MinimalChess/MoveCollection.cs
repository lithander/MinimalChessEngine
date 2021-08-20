using System.Collections.Generic;

namespace MinimalChess
{
    public class LegalMoves : List<Move>
    {
        private static Board _tempBoard = new Board();

        public LegalMoves(Board reference) : base(40)
        {
            reference.CollectMoves(move =>
            {
                //only add if the move doesn't result in a check for active color
                _tempBoard.Copy(reference);
                _tempBoard.Play(move);
                if (!_tempBoard.IsChecked(reference.SideToMove))
                    Add(move);
            });
        }

        public static bool HasMoves(Board position)
        {
            bool canMove = false;
            for (int i = 0; i < 64 && !canMove; i++)
            {
                position.CollectMoves(i, move =>
                {
                    if (canMove) return;

                    _tempBoard.Copy(position);
                    _tempBoard.Play(move);
                    canMove = !_tempBoard.IsChecked(position.SideToMove);
                });
            }
            return canMove;
        }
    }

    public class MoveList : List<Move>
    {
        internal static MoveList Quiets(Board position)
        {
            MoveList quietMoves = new MoveList();
            position.CollectQuiets(quietMoves.Add);
            return quietMoves;
        }

        internal static MoveList SortedCaptures(Board position)
        {
            MoveList captures = new MoveList();
            position.CollectCaptures(captures.Add);
            captures.SortMvvLva(position);
            return captures;
        }

        public void SortMvvLva(Board context)
        {
            int Score(Move move)
            {
                Piece victim = context[move.ToSquare];
                Piece attacker = context[move.FromSquare];
                return Pieces.MaxOrder * Pieces.Order(victim) - Pieces.Order(attacker);
            }
            Sort((a, b) => Score(b).CompareTo(Score(a)));
        }

        public void SortHistory(Board context)
        {
            float Score(Move move)
            {
                return History.Value(context[move.FromSquare], move.ToSquare);
            }
            Sort((a, b) => Score(b).CompareTo(Score(a)));
        }
    }
}
