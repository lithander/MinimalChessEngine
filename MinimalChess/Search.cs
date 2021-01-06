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
            List<Move> best = new List<Move>();
            int bestScore = Evaluation.MinValue;
            foreach (var move in moves)
            {
                int score = color * EvaluatePosition(new Board(board, move), depth - 1);
                                
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

        public static int EvaluatePosition(Board board, int depth)
        {
            if(depth == 0)
                return Evaluation.Evaluate(board);

            int color = (board.ActiveColor == Color.Black) ? -1 : 1;
            var moves = new LegalMoves(board);
            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
                return board.IsChecked(board.ActiveColor) ? (color * Evaluation.MinValue) : Evaluation.DrawValue;

            int bestScore = Evaluation.MinValue;
            foreach (var move in moves)
            {
                //multiply score with color (-1 for black, 1 for white) so we can always maximize the score here, otherwise we'd have to minimize for black
                int score = color * EvaluatePosition(new Board(board, move), depth - 1);
                bestScore = Math.Max(score, bestScore);
            }
            //we've been multiplying with color (-1 for black, 1 for white) but now we need to return the real, signed result
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
