using System.Collections.Generic;

namespace MinimalChess
{
    public static class Playmaker
    {
        internal static IEnumerable<(Move Move, Board Board)> Play(Board position, int depth, KillerMoves killers, History history)
        {
            //1. Is there a known best move for this position? (PV Node)
            if (Transpositions.GetBestMove(position, out Move bestMove))
            {
                var nextPosition = new Board(position, bestMove);
                yield return (bestMove, nextPosition);
            }

            //2. Try all captures ordered by Mvv-Lva
            foreach (var capture in MoveList.SortedCaptures(position))
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.SideToMove))
                    yield return (capture, nextPosition);
            }

            //3. Play quiet moves that have caused a beta cutoff elsewhere if available
            foreach (Move killer in killers.Get(depth))
            {
                if (position[killer.ToSquare] != Piece.None || !position.IsPlayable(killer))
                    continue;

                var nextPosition = new Board(position, killer);
                if (!nextPosition.IsChecked(position.SideToMove))
                    yield return (killer, nextPosition);
            }

            //4. Play the remaining quiet moves ordered by history
            foreach (var move in MoveList.SortedQuiets(position, history))
            {
                if (killers.Contains(depth, move))
                    continue;

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
