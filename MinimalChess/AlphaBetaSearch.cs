using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class AlphaBetaSearch
    {
        public int Depth { get; private set; }
        public long EvalCount { get; private set; }
        public int Score { get; private set; }
        public Move[][] Lines => _bestMoves.ToArray();
        public Move[] Moves => _bestMoves.Select(line => line.First()).ToArray();

        Board _root = null;
        List<Move[]> _bestMoves = new List<Move[]>();
        PrincipalVariation _pv;

        public AlphaBetaSearch(Board board)
        {
            _root = board;
        }

        public void Search(int depth)
        {
            _pv = new PrincipalVariation(depth);
            int bestScore = short.MinValue;
            int color = (int)_root.ActiveColor;
            var moves = new LegalMoves(_root);
            var window = SearchWindow.Infinite;
            foreach (var move in moves)
            {
                Board next = new Board(_root, move);
                int eval = Evaluate(next, depth - 1, window);
                int score = color * eval;

                if (score < bestScore)
                    continue;

                if (score > bestScore)
                {
                    _bestMoves.Clear();
                    bestScore = score;
                    //add -1 offset so that other root-moves with the same value are not discarded
                    window.Limit(eval, _root.ActiveColor);
                }

                //add the move's pv
                _pv.Promote(depth, move);
                _bestMoves.Add(_pv.Line);
            }
            Score = color * bestScore;
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

            foreach (var move in moves)
            {
                int score = Evaluate(new Board(board, move), depth - 1, window);
                if (window.Outside(score, color))
                    continue;

                //this is a new best score!
                _pv.Promote(depth, move);
                if (window.Cut(score, color))
                    return window.GetScore(color);
            }

            return window.GetScore(color);
        }
    }
}
