using System.Collections.Generic;
using System.Linq;

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

        internal static MoveList SortedCapturesSEE(Board position)
        {
            MoveList captures = new MoveList();
            MoveList bad = new MoveList();
            position.CollectCaptures(m => (IsBadCaptureSEE(position, m) ? bad : captures).Add(m));
            captures.SortMvvLva(position);
            captures.AddRange(bad);
            return captures;
        }

        internal static MoveList SortedCapturesSEE(Board position, out MoveList badCaptures)
        {
            MoveList captures = new MoveList();
            MoveList bad = new MoveList();
            position.CollectCaptures(m => (IsBadCaptureSEE(position, m) ? bad : captures).Add(m));
            captures.SortMvvLva(position);
            //captures.AddRange(bad);
            badCaptures = bad;
            return captures;
        }

        internal static MoveList SortedCapturesBlind(Board position, out MoveList badCaptures)
        {
            //BLIND stands for 'Better, or Lower If Not Defended'.
            //You initially only try those captures from the MVV/ LVA ordering that are Low x High or Equal x Equal, and High x Low only if the victim is completely unprotected.
            //Other captures are returned as 'badCaptures'.
            //http://www.talkchess.com/forum3/viewtopic.php?f=7&t=48609
            MoveList captures = new MoveList();
            MoveList bad = new MoveList();
            position.CollectCaptures(m => (IsBadCaptureBlind(position, m) ? bad : captures).Add(m));
            captures.SortMvvLva(position);
            //captures.AddRange(bad);
            badCaptures = bad;
            return captures;
        }

        internal static MoveList SortedCapturesBlind(Board position)
        {
            //BLIND stands for 'Better, or Lower If Not Defended'.
            //You initially only try those captures from the MVV/ LVA ordering that are Low x High or Equal x Equal, and High x Low only if the victim is completely unprotected.
            //Other captures are returned as 'badCaptures'.
            //http://www.talkchess.com/forum3/viewtopic.php?f=7&t=48609
            MoveList captures = new MoveList();
            void AddGood(Move move)
            {
                if (!IsBadCaptureBlind(position, move))
                    captures.Add(move);
            }
            position.CollectCaptures(AddGood);
            captures.SortMvvLva(position);
            //captures.AddRange(bad);
            return captures;
        }

        public static bool IsBadCaptureBlind(Board position, Move move)
        {
            Piece victim = position[move.ToSquare];
            Piece attacker = position[move.FromSquare];
            if (Pieces.Order(victim) >= Pieces.Order(attacker))
                return false;

            //is victim protected? (square "attacked" by victim's color
            int protector = position.GetLeastValuableAttacker(move.ToSquare, victim.Color());
            return protector >= 0;
        }

        public static bool IsBadCaptureSEE(Board position, Move move)
        {
            Piece victim = position[move.ToSquare];
            Piece attacker = position[move.FromSquare];
            if (Pieces.Order(victim) >= Pieces.Order(attacker))
                return false;

            int see = SEE.EvaluateSign(position, move);
            int color = (int)attacker.Color();
            return (color * see) < 0;
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
    }
}
