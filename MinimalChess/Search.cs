using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public static class Search
    {
        public static Move GetBestMove(Board board, int depth)
        {
            int color = (int)board.ActiveColor;
            var moves = new LegalMoves(board);
            int bestScore = Evaluation.MinValue;
            List<Move> bestMoves = new List<Move>();
            foreach (var move in moves)
            {
                Board next = new Board(board, move);
                int score = color * Evaluate(next, depth -1);
                if (score > bestScore)
                {
                    bestMoves.Clear();
                    bestMoves.Add(move);
                    bestScore = score;
                }
                else if (score == bestScore)
                    bestMoves.Add(move);
            }
            //with a simple heuristic there are probably many best moves - pick one randomly
            var random = new Random();
            int index = random.Next(bestMoves.Count);
            return bestMoves[index];
        }

        public static int Evaluate(Board board, int depth)
        {
            if (depth == 0)
                return Evaluation.Evaluate(board);

            int color = (int)board.ActiveColor;
            var moves = new LegalMoves(board);
            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
                return board.IsChecked(board.ActiveColor) ? color * Evaluation.MinValue : 0;

            int bestScore = Evaluation.MinValue;
            foreach (var move in moves)
            {
                Board next = new Board(board, move);
                //we multiply with color so we can always maximize (aka Negamax)
                int score = color * Evaluate(next, depth - 1);
                if (score > bestScore)
                    bestScore = score;
            }
            //so here we need to multiply again! so that black gets it's sign back!
            return color * bestScore;
        }
    }
}
