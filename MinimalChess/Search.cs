using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public interface ISearch
    {
        long NodesVisited { get; }
        long MovesGenerated { get; }
        long MovesPlayed { get; }
        Move[] PrincipalVariation { get; }
        int Score { get; }

        void Search(int maxDepth);
    }

    public static class Search
    {
        public static long PositionsEvaluated = 0;
        public static long MovesGenerated = 0;
        public static long MovesPlayed = 0;

        public static void ClearStats()
        {
            PositionsEvaluated = 0;
            MovesGenerated = 0;
            MovesPlayed = 0;
        }

        public static Move GetBestMoveMinMax(Board board, int depth)
        {
            List<Move> bestMoves = GetBestMovesMinMax(board, depth, out _);
            //with a simple heuristic there are probably many best moves - pick one randomly
            var random = new Random();
            int index = random.Next(bestMoves.Count);
            return bestMoves[index];
        }

        public static List<Move> GetBestMovesMinMax(Board board, int depth, out int pvEval)
        {
            int color = (int)board.ActiveColor;
            var moves = new LegalMoves(board);
            int bestScore = Evaluation.MinValue;
            List<Move> bestMoves = new List<Move>();
            foreach (var move in moves)
            {
                Board next = new Board(board, move);
                int score = color * Evaluate(next, depth - 1);
                if (score > bestScore)
                {
                    bestMoves.Clear();
                    bestMoves.Add(move);
                    bestScore = score;
                }
                else if (score == bestScore)
                    bestMoves.Add(move);
            }
            pvEval = color * bestScore;
            return bestMoves;
        }


        public static int Evaluate(Board board, int depth)
        {
            if (depth == 0)
            {
                PositionsEvaluated++;
                return Evaluation.Evaluate(board);
            }

            int color = (int)board.ActiveColor;
            var moves = new LegalMoves(board);
            MovesGenerated += moves.Count;
            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
                return board.IsChecked(board.ActiveColor) ? color * Evaluation.MinValue : 0;

            int bestScore = Evaluation.MinValue;
            foreach (var move in moves)
            {
                MovesPlayed++;
                Board next = new Board(board, move);
                //we multiply with color so we can always maximize
                int score = color * Evaluate(next, depth - 1);
                if (score > bestScore)
                    bestScore = score;
            }
            //so here we need to multiply again! so that black gets it's sign back!
            return color * bestScore;
        }

        //********************
        //*** AlphaBeta    ***
        //********************

        public static Move GetBestMoveAlphaBeta(Board board, int depth)
        {
            List<Move> bestMoves = GetBestMovesAlphaBeta(board, depth, out _);
            //with a simple heuristic there are probably many best moves - pick one randomly
            var random = new Random();
            int index = random.Next(bestMoves.Count);
            return bestMoves[index];
        }

        public static List<Move> GetBestMovesAlphaBeta(Board board, int depth, out int pvEval)
        {
            int bestScore = Evaluation.MinValue;
            int color = (int)board.ActiveColor;
            var moves = new LegalMoves(board);
            var window = SearchWindow.Infinite;
            List<Move> bestMoves = new List<Move>();
            foreach (var move in moves)
            {
                Board next = new Board(board, move);
                int eval = Evaluate(next, depth - 1, window);
                int score = color * eval;
                if (score > bestScore)
                {
                    bestMoves.Clear();
                    bestMoves.Add(move);
                    bestScore = score;
                    window.Limit(eval, board.ActiveColor);
                }
                else if (score == bestScore)
                    bestMoves.Add(move);
            }
            pvEval = color * bestScore;
            return bestMoves;
        }

        public static int Evaluate(Board board, int depth, SearchWindow window)
        {
            if (depth == 0)
            {
                PositionsEvaluated++;
                return Evaluation.Evaluate(board);
            }

            Color color = board.ActiveColor;
            var moves = new LegalMoves(board);
            MovesGenerated += moves.Count;

            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
                return board.IsChecked(board.ActiveColor) ? (int)color * Evaluation.MinValue : 0;

            foreach (var move in moves)
            {
                MovesPlayed++;
                int score = Evaluate(new Board(board, move), depth - 1, window);
                if (window.Cut(score, color))
                    break;
            }

            return window.GetScore(color);
        }
    }
}
