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
                if (!_tempBoard.IsChecked(reference.ActiveColor))
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
                    canMove = !_tempBoard.IsChecked(position.ActiveColor);
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

        internal static MoveList Captures(Board position)
        {
            MoveList captures = new MoveList();
            position.CollectCaptures(captures.Add);
            return captures;
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
                return Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
            }
            Sort((a, b) => Score(b).CompareTo(Score(a)));
        }
    }
}
