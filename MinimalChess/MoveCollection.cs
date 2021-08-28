using System;
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

    public struct SortedMove : IComparable<SortedMove>
    {
        public float Priority;
        public Move Move;
        public int CompareTo(SortedMove other) => other.Priority.CompareTo(Priority);
        public static implicit operator Move(SortedMove m) => m.Move;
    }

    public class MoveList : List<SortedMove>
    {
        internal static MoveList Quiets(Board position)
        {
            MoveList quietMoves = new MoveList();
            position.CollectQuiets(m => quietMoves.Add(m, 0));
            return quietMoves;
        }

        internal static MoveList SortedCaptures(Board position)
        {
            MoveList captures = new MoveList();
            position.CollectCaptures(m => captures.Add(m, ScoreMvvLva(m, position)));
            captures.Sort();
            return captures;
        }

        public static MoveList SortedQuiets(Board position, History history)
        {
            MoveList quiets = new MoveList();
            position.CollectQuiets(m => quiets.Add(m, history.Value(position, m)));
            quiets.Sort();
            return quiets;
        }

        private static int ScoreMvvLva(Move move, Board context)
        {
            Piece victim = context[move.ToSquare];
            Piece attacker = context[move.FromSquare];
            return Pieces.MaxOrder * Pieces.Order(victim) - Pieces.Order(attacker);
        }

        private void Add(Move move, float priority) => Add(new SortedMove { Move = move, Priority = priority });
    }
}
