using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public struct SearchWindow
    {
        public int Floor;//Alpha
        public int Ceiling;//Beta

        public static SearchWindow Infinite = new SearchWindow(Evaluation.MinValue, Evaluation.MaxValue);

        public SearchWindow(int floor, int ceiling)
        {
            Floor = floor;
            Ceiling = ceiling;
        }

        public bool Cut(int score, int offset, Color color)
        {
            if (color == Color.White)
                return CutFloor(score + offset);
            else
                return CutCeiling(score - offset);
        }

        public bool Cut(int score, Color color)
        {
            if (color == Color.White)
                return CutFloor(score);
            else
                return CutCeiling(score);
        }

        private bool CutFloor(int score)
        {
            if (score <= Floor)
                return false; //outside search window

            Floor = score;
            return Floor >= Ceiling; //Cutoff?
        }

        private bool CutCeiling(int score)
        {
            if (score >= Ceiling)
                return false; //outside search window

            Ceiling = score;
            return Ceiling <= Floor; //Cutoff?
        }

        public int BestScore(Color color) => color == Color.White ? Floor : Ceiling;
    }

    public static class Search
    {
        public static long EvalCount = 0;

        public static Move GetBestMoveMinMax(Board board, int depth)
        {
            List<Move> bestMoves = GetBestMovesMinMax(board, depth, out _);
            //with a simple heuristic there are probably many best moves - pick one randomly
            var random = new Random();
            int index = random.Next(bestMoves.Count);
            return bestMoves[index];
        }

        public static List<Move> GetBestMovesMinMax(Board board, int depth, out int bestScore)
        {
            int color = (int)board.ActiveColor;
            var moves = new LegalMoves(board);
            bestScore = Evaluation.MinValue;
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
            return bestMoves;
        }


        public static int Evaluate(Board board, int depth)
        {
            if (depth == 0)
            {
                EvalCount++;
                return Evaluation.Evaluate(board);
            }

            int color = (int)board.ActiveColor;
            var moves = new LegalMoves(board);
            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
                return board.IsChecked(board.ActiveColor) ? color * Evaluation.MinValue : 0;

            int bestScore = Evaluation.MinValue;
            foreach (var move in moves)
            {
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

        public static List<Move> GetBestMovesAlphaBeta(Board board, int depth, out int bestScore)
        {
            bestScore = Evaluation.MinValue;
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
                    window.Cut(eval, -1, board.ActiveColor);
                }
                else if (score == bestScore)
                    bestMoves.Add(move);
            }
            return bestMoves;
        }

        public static int Evaluate(Board board, int depth, SearchWindow window)
        {
            if (depth == 0)
            {
                EvalCount++;
                return Evaluation.Evaluate(board);
            }

            Color color = board.ActiveColor;
            var moves = new LegalMoves(board);

            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
                return board.IsChecked(board.ActiveColor) ? (int)color * Evaluation.MinValue : 0;

            foreach (var move in moves)
            {
                int score = Evaluate(new Board(board, move), depth - 1, window);
                if (window.Cut(score, color))
                    break;
            }

            return window.BestScore(color);
        }

        /*
        public static int Evaluate(Board board, int depth, int whiteFloor, int blackCeil)
        {
            if (depth == 0)
            {
                EvalCount++;
                return Evaluation.Evaluate(board);
            }

            int color = (int)board.ActiveColor;
            var moves = new LegalMoves(board);
            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
                return board.IsChecked(board.ActiveColor) ? color * Evaluation.MinValue : 0;

            foreach (var move in moves)
            {
                Board next = new Board(board, move);
                int score = Evaluate(next, depth - 1, whiteFloor, blackCeil);

                if (board.ActiveColor == Color.White && score > whiteFloor)//Max
                {
                    whiteFloor = score;
                    if (whiteFloor >= blackCeil)
                        break;
                }
                else if (board.ActiveColor == Color.Black && score < blackCeil)//Min
                {
                    blackCeil = score;
                    if (blackCeil <= whiteFloor)
                        break;
                }
            }

            return board.ActiveColor == Color.White ? whiteFloor : blackCeil;
        }
        */
    }
}
