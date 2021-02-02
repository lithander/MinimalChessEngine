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
        
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                _bestMoves.Clear();
                _pv.Prepare(depth);

                Color color = _root.ActiveColor;
                var moves = new LegalMoves(_root);
                var window = SearchWindow.Infinite;
        
                Move killer = _pv[depth];
                if (moves.Contains(killer))
                {
                    Board next = new Board(_root, killer);
                    int eval = Evaluate(next, depth - 1, window);
                    //limit window so that other root-moves with the same value are not discarded
                    window.Limit(eval, color);
                    //add the move's pv
                    _pv.Promote(depth, killer);
                    _bestMoves.Add(_pv.Line);
                }
        
                foreach (var move in moves)
                {
                    if (move == killer)
                        continue;
        
                    Board next = new Board(_root, move);
                    int score = Evaluate(next, depth - 1, window);
        
                    if(window.Outside(score, color))
                        continue;
        
                    if (window.Limit(score, color))
                        _bestMoves.Clear();
    
                    //add the move's pv
                    _pv.Promote(depth, move);
                    _bestMoves.Add(_pv.Line);
                }
                //the window is limited to allow moves of the same score to pass.
                Score = window.GetScoreFromLimit(color);
            }
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
            if(moves.Contains(killer))
            {
                int score = Evaluate(new Board(board, killer), depth - 1, window);
                if (window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv.Promote(depth, killer);
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
                    _pv.Promote(depth, move);
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }      
            }
        
            return window.GetScore(color);
        }
    }
}
