using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{   

    public class DebugSearch : ISearch
    {
        public long NodesVisited { get; private set; }

        public long MovesGenerated => 0;
        public long MovesPlayed => 0;

        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Board Position => new Board(_root); //return copy, _root must not be modified during search!
        public Move[] PrincipalVariation => _pv.GetLine(Depth);
        public bool Aborted => _killSwitch.Triggered;
        public bool GameOver => PrincipalVariation?.Length < Depth;

        Board _root = null;
        PrincipalVariation _pv;
        KillSwitch _killSwitch;
        History _history;
        Killers _killers;
        Playmaker _playmaker;

        public DebugSearch(Board board, List<Move> rootMoves = null)
        {
            _root = new Board(board);
            _pv = new PrincipalVariation();
            _history = new History();
            _killers = new Killers();
            _playmaker = new Playmaker(_pv, _killers, _history, rootMoves);
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

            Depth++;
            _pv.Grow(Depth);
            _killers.Grow(Depth);
            _killSwitch = new KillSwitch(killSwitch);
            var window = SearchWindow.Infinite;
            Score = EvalPosition(_root, Depth, window);
            _history.PrintStats();
        }

        private IEnumerable<Board> Expand(Board position, bool escapeCheck)
        {
            MoveSequence nodes = escapeCheck ? MoveSequence.AllMoves(position) : MoveSequence.CapturesOnly(position);
            return nodes.SortCaptures().Play();
        }

        private int EvalPosition(Board position, int depth, SearchWindow window)
        {
            if (_killSwitch.Triggered)
                return 0;

            if (depth == 0)
            {
                //Console.WriteLine(depth + new string(' ', Depth - depth) + "QEval!");
                return QEval(position, window);
            }

            NodesVisited++;
            Color color = position.ActiveColor;

            int expandedNodes = 0;
            foreach ((Move move, Board child) in _playmaker.Play(position, depth))
            {
                expandedNodes++;

                //For all rootmoves after the first search with "null window"
                if (expandedNodes > 1 && depth == Depth)
                {
                    SearchWindow nullWindow = window.GetNullWindow(color);
                    int nullScore = EvalPosition(child, depth - 1, nullWindow);
                    if (!nullWindow.Inside(nullScore, color))
                        continue;
                }

                int score = EvalPosition(child, depth - 1, window);
                if (window.Inside(score, color))
                {
                    _pv[depth] = move;
                    _history.RememberBest(move, depth);
                    if (window.Cut(score, color))
                    {
                        _history.RememberCutoff(move, depth);
                        _killers.Remember(move, depth);
                        return window.GetScore(color);
                    }
                }
                else
                    _history.RememberWeak(move, depth);
            }

            if (expandedNodes == 0) //no expansion happened from this node!
            {
                //having no legal moves can mean two things: (1) lost or (2) draw?
                _pv.Clear(depth);
                return position.IsChecked(position.ActiveColor) ? (int)color * PeSTO.LostValue : 0;
            }

            return window.GetScore(color);
        }

        private int QEval(Board position, SearchWindow window)
        {
            NodesVisited++;
            Color color = position.ActiveColor;

            //if inCheck we can't use standPat, need to escape check!
            bool inCheck = position.IsChecked(color);
            if (!inCheck)
            {
                int standPatScore = PeSTO.Evaluate(position);
                //Cut will raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(standPatScore, color))
                    return window.GetScore(color);
            }

            int expandedNodes = 0;
            //play remaining captures (or any moves if king is in check)
            foreach (Board child in Expand(position, inCheck))
            {
                expandedNodes++;
                //recursively evaluate the resulting position (after the capture) with QEval
                int score = QEval(child, window);

                //Cut will raise alpha and perform beta cutoff when the move is too good
                if (window.Cut(score, color))
                    break;
            }

            //checkmate?
            if (expandedNodes == 0 && inCheck)
                return (int)color * PeSTO.LostValue;

            //stalemate?
            if (expandedNodes == 0 && !AnyLegalMoves(position))
                return 0;

            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            return window.GetScore(color);
        }

        public bool AnyLegalMoves(Board position)
        {
            var moves = new AnyLegalMoves(position);
            return moves.CanMove;
        }
    }
}
