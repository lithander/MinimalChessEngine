using System;
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

            //2. Captures Mvv-Lva
            MoveList captures = MoveList.CollectCaptures(position);
            captures.Remove(pvMove);
            SortMvvLva(captures, position);
            foreach (var capture in captures)
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return (capture, nextPosition);
            }

            //3. Killers if available
            Move[] killerMoves = killers.Get(depth);
            foreach (Move killer in killerMoves)
            {
                if (killer != pvMove && position[killer.ToIndex] == Piece.None && position.CanPlay(killer))
                {
                    var nextPosition = new Board(position, killer);
                    if (!nextPosition.IsChecked(position.ActiveColor))
                        yield return (killer, nextPosition);
                }
            }

            MoveList nonCaptures = MoveList.CollectQuiets(position);
            foreach (var move in nonCaptures)
                if (move != pvMove && Array.IndexOf(killerMoves, move) == -1)
                {
                    var nextPosition = new Board(position, move);
                    if (!nextPosition.IsChecked(position.ActiveColor))
                        yield return (move, nextPosition);
                }
        }

        internal static IEnumerable<Board> Play(Board position)
        {
            MoveList captures = MoveList.CollectCaptures(position);
            SortMvvLva(captures, position);
            foreach (var capture in captures)
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }

            MoveList nonCaptures = MoveList.CollectQuiets(position);
            foreach (var move in nonCaptures)
            {
                var nextPosition = new Board(position, move);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }
        }

        internal static IEnumerable<Board> PlayCaptures(Board position)
        {
            MoveList captures = MoveList.CollectCaptures(position);
            SortMvvLva(captures, position);

            foreach (var capture in captures)
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }
        }

        public static void SortMvvLva(List<Move> moves, Board context)
        {
            int Score(Move move)
            {
                Piece victim = context[move.ToIndex];
                Piece attacker = context[move.FromIndex];
                return Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
            }
            moves.Sort((a, b) => Score(b).CompareTo(Score(a)));
        }
    }
}
