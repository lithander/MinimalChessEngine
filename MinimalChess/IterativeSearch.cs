using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class IterativeSearch
    {
        public int Depth { get; private set; }
        public long EvalCount { get; private set; }
        public int Score { get; private set; }
        public Move[][] Lines => _bestMoves.ToArray();
        public Move[] Moves => _bestMoves.Select(line => line.First()).ToArray();

        Board _root = null;
        List<Move[]> _bestMoves = new List<Move[]>();
        PrincipalVariation _pv;

        public IterativeSearch(Board board)
        {
            _root = board;
        }

        public void Search(int maxDepth)
        {
            _pv = new PrincipalVariation(maxDepth);
            Depth = 0;
            while (Depth < maxDepth)
                SearchDeeper();
        }

        private void SearchDeeper()
        {
            Depth++;

            _pv.Clear();
            var moves = new LegalMoves(_root);
            var window = SearchWindow.Infinite;

            List<Move> killers = _bestMoves.Select(line => line[0]).ToList();
            _bestMoves.Clear();

            foreach (var killer in killers)
                if (moves.Contains(killer))
                    EvalMove(killer, ref window);

            foreach (var move in moves)
                if (!killers.Contains(move))
                    EvalMove(move, ref window);

            //the window is limited to allow moves of the same score to pass.
            Score = window.GetScoreFromLimit(_root.ActiveColor);
        }

        public void EvalMove(Move move, ref SearchWindow window)
        {
            Color color = _root.ActiveColor;
            Board next = new Board(_root, move);
            int score = Evaluate(next, Depth - 1, window);
            if (window.Outside(score, color))
                return;

            if (window.Limit(score, color))
                _bestMoves.Clear();

            //the move's pv is among the best
            _pv[Depth] = move;
            _bestMoves.Add(_pv.GetLine(Depth));
        }

        public int Evaluate(Board board, int depth, SearchWindow window)
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
            {
                _pv.Clear(depth);
                return board.IsChecked(board.ActiveColor) ? (int)color * Evaluation.MinValue : 0;
            }

            var killer = _pv[depth];
            if (moves.Contains(killer))
            {
                int score = Evaluate(new Board(board, killer), depth - 1, window);
                if (window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = killer;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            foreach (var move in moves)
            {
                if (move == killer)
                    continue;

                int score = Evaluate(new Board(board, move), depth - 1, window);
                if (window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = move;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            return window.GetScore(color);
        }
    }
}
