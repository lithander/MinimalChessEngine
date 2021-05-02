using System.Collections.Generic;

namespace MinimalChess
{
    static class Playmaker
    {
        public static IEnumerable<(Move Move, Board Board)> Play(Board position, int depth, PrincipalVariation pv, KillerMoves killers)
        {
            //1. PV if available
            Move pvMove = pv[depth];
            if (position.CanPlay(pvMove))
            {
                var nextPosition = new Board(position, pvMove);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return (pvMove, nextPosition);
            }

            //2. Captures Mvv-Lva, PV excluded
            MoveList captures = MoveList.Captures(position);
            captures.Remove(pvMove);
            captures.SortMvvLva(position);
            foreach (var capture in captures)
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return (capture, nextPosition);
            }

            //3. Killers if available
            foreach (Move killer in killers.Get(depth))
                if (killer != pvMove && position[killer.ToSquare] == Piece.None && position.CanPlay(killer))
                {
                    var nextPosition = new Board(position, killer);
                    if (!nextPosition.IsChecked(position.ActiveColor))
                        yield return (killer, nextPosition);
                }

            //4. Play quiet moves that aren't known killers
            foreach (var move in MoveList.Quiets(position))
                if (move != pvMove && !killers.Contains(depth, move))
                {
                    var nextPosition = new Board(position, move);
                    if (!nextPosition.IsChecked(position.ActiveColor))
                        yield return (move, nextPosition);
                }
        }

        internal static IEnumerable<Board> Play(Board position)
        {
            foreach (var capture in MoveList.SortedCaptures(position))
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }

            foreach (var move in MoveList.Quiets(position))
            {
                var nextPosition = new Board(position, move);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }
        }

        internal static IEnumerable<Board> PlayCaptures(Board position)
        {
            foreach (var capture in MoveList.SortedCaptures(position))
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }
        }
    }
}
