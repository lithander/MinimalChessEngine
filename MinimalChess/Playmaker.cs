using System;
using System.Collections.Generic;

namespace MinimalChess
{
    public static class Playmaker
    {
        public static long CanPlayPV = 0;
        public static long Expansions = 0;
        public static long BestMove = 0;

        internal static IEnumerable<(Move Move, Board Board)> Play(Board position, int depth, PrincipalVariation pv, KillerMoves killers)
        {
            Expansions++;

            if (Transpositions.GetBestMove(position, out Move bestMove))
            {
                BestMove++;
                var nextPosition = new Board(position, bestMove);
                yield return (bestMove, nextPosition);
            }

            //1. PV if available
            Move pvMove = pv[depth];
            if (pvMove != bestMove && position.CanPlay(pvMove))
            {
                CanPlayPV++;
                var nextPosition = new Board(position, pvMove);
                if (!nextPosition.IsChecked(position.SideToMove))
                    yield return (pvMove, nextPosition);
            }

            //2. Captures Mvv-Lva, PV excluded
            MoveList captures = MoveList.Captures(position);
            captures.Remove(pvMove);
            captures.Remove(bestMove);
            captures.SortMvvLva(position);
            foreach (var capture in captures)
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.SideToMove))
                    yield return (capture, nextPosition);
            }

            //3. Killers if available
            foreach (Move killer in killers.Get(depth))
                if (killer != pvMove && killer != bestMove && position[killer.ToSquare] == Piece.None && position.CanPlay(killer))
                {
                    var nextPosition = new Board(position, killer);
                    if (!nextPosition.IsChecked(position.SideToMove))
                        yield return (killer, nextPosition);
                }

            //4. Play quiet moves that aren't known killers
            foreach (var move in MoveList.Quiets(position))
                if (move != pvMove && move != bestMove && !killers.Contains(depth, move))
                {
                    var nextPosition = new Board(position, move);
                    if (!nextPosition.IsChecked(position.SideToMove))
                        yield return (move, nextPosition);
                }
        }

        internal static IEnumerable<Board> Play(Board position)
        {
            foreach (var capture in MoveList.SortedCaptures(position))
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.SideToMove))
                    yield return nextPosition;
            }

            foreach (var move in MoveList.Quiets(position))
            {
                var nextPosition = new Board(position, move);
                if (!nextPosition.IsChecked(position.SideToMove))
                    yield return nextPosition;
            }
        }

        internal static IEnumerable<Board> PlayCaptures(Board position)
        {
            foreach (var capture in MoveList.SortedCaptures(position))
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.SideToMove))
                    yield return nextPosition;
            }
        }

        internal static Board PlayNullMove(Board position)
        {
            Board copy = new Board(position);
            copy.PlayNullMove();
            return copy;
        }
    }
}
