using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public static class Search
    {
        const int NEGATIVE_INFINITY = short.MinValue;
        const int DRAW = 0;

        public static Move GetBestMove(Board board)
        {
            if (board.ActiveColor == Color.Black)
                return GetBestMove(board, -1);
            else
                return GetBestMove(board, +1);
        }

        private static Move GetBestMove(Board board, int color)
        {
            var moves = new LegalMoves(board);
            List<Move> best = new List<Move>();
            int bestScore = NEGATIVE_INFINITY;
            foreach (var move in moves)
            {
                int score = color * EvaluatePosition(new Board(board, move), -color);
                                
                if (score > bestScore)
                {
                    //this move has a better score than what we stored as 'best' so far
                    bestScore = score;
                    best.Clear();
                    best.Add(move);
                }
                else if(score == bestScore)
                {
                    //it's one of the best moves so far
                    best.Add(move);
                }
            }
            //with a simple heuristic there are probably many best moves - pick one randomly
            return best.GetRandom();
        }

        private static int EvaluatePosition(Board board, int color)
        {
            var moves = new LegalMoves(board);
            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
                return board.IsChecked(board.ActiveColor) ? (color * NEGATIVE_INFINITY) : DRAW;

            int bestScore = NEGATIVE_INFINITY;
            foreach (var move in moves)
            {
                int score = color * Evaluation.Evaluate(new Board(board, move));
                bestScore = Math.Max(score, bestScore);
            }
            //we've been using the sign to always maximize but now we need to return the signed result
            return color * bestScore;
        }

        private static Move GetRandom(this List<Move> moves)
        {
            var rnd = new Random();
            int index = rnd.Next(moves.Count);
            return moves[index];
        }
    }
}
