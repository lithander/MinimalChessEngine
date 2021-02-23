using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class FastIterativeSearch : ISearch
    {
        public long PositionsEvaluated { get; private set; }
        public long MovesGenerated { get; private set; }
        public long MovesPlayed { get; private set; }

        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Board Position => new Board(_root); //return copy, _root must not be modified during search!
        public Move[] PrincipalVariation => Depth > 0 ? _pv.GetLine(Depth) : null;
        public bool Aborted => _killSwitch.Triggered;
        public bool GameOver => _pv.IsGameOver(Depth);

        Board _root = null;
        LegalMoves _rootMoves = null;
        PrincipalVariation _pv;
        KillSwitch _killSwitch;

        public FastIterativeSearch(Board board)
        {
            _root = new Board(board);
            _rootMoves = new LegalMoves(board);
            _pv = new PrincipalVariation(20);
        }

        public FastIterativeSearch(Board board, Action<LegalMoves> rootMovesModifier) : this(board)
        {
            rootMovesModifier(_rootMoves);
        }

        public void Search(int maxDepth)
        {
            while (!GameOver && Depth < maxDepth)
                SearchDeeper();
        }
        
        public void SearchDeeper(Func<bool> killSwitch = null)
        {
            if (GameOver)
                return;

            _pv.Grow(++Depth);
            _killSwitch = new KillSwitch(killSwitch);
            var window = SearchWindow.Infinite;
            Score = EvalPosition(_root, Depth, window);
        }

        private bool TryEvalMove(Board position, Move move, int depth, SearchWindow window, out int score)
        {
            score = 0;
            Board resultingPosition = new Board(position, move);
            if (resultingPosition.IsChecked(position.ActiveColor))
                return false;

            MovesPlayed++;
            score = EvalPosition(resultingPosition, depth - 1, window);
            return true;
        }

        private int EvalPosition(Board position, int depth, SearchWindow window)
        {
            if (depth == 0)
            {
                PositionsEvaluated++;
                return Evaluation.Evaluate(position);
            }

            if (_killSwitch.Triggered) return 0;

            Color color = position.ActiveColor;
            List<Move> moves;
            if (depth == Depth)
                moves = _rootMoves;
            else
                moves = new PseudoLegalMoves(position);

            MovesGenerated += moves.Count;
            long oldMovesPlayed = MovesPlayed;

            var killer = _pv[depth];
            if (moves.Contains(killer))
            {
                if(TryEvalMove(position, killer, depth, window, out int score) && window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = killer;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            MoveOrdering.SortMvvLva(moves, position);

            foreach (var move in moves)
            {
                if (move == killer)
                    continue;

                if (depth == Depth)
                {
                    //Search null window!
                    SearchWindow nullWindow = window.GetNullWindow(color);
                    if(TryEvalMove(position, move, depth, nullWindow, out int nullScore) && nullWindow.IsWorseOrEqual(color, nullScore))
                        continue;
                }

                if (TryEvalMove(position, move, depth, window, out int score) && window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = move;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            if(oldMovesPlayed == MovesPlayed) //no expansion happened from this node!
            {
                //having no legal moves can mean two things: (1) lost or (2) draw?
                _pv.Clear(depth);
                return position.IsChecked(position.ActiveColor) ? (int)color * Evaluation.MinValue : 0;

            }

            return window.GetScore(color);
        }
    }
}
