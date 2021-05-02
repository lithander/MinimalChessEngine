using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{   

    public class DebugSearch
    {
        const int QUERY_TC_FREQUENCY = 25;

        public long NodesVisited { get; private set; }
        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Board Position => new Board(_root); //return copy, _root must not be modified during search!
        public Move[] PrincipalVariation => _pv.GetLine(Depth);
        public bool Aborted => NodesVisited >= _maxNodes || _killSwitch.Get(NodesVisited % QUERY_TC_FREQUENCY == 0);
        public bool GameOver => PrincipalVariation?.Length < Depth;

        Board _root = null;
        List<Move> _rootMoves = null;
        PrincipalVariation _pv;
        KillerMoves _killers;
        KillSwitch _killSwitch;
        long _maxNodes;

        public DebugSearch(Board board, long maxNodes = long.MaxValue, List<Move> rootMoves = null)
        {
            _root = new Board(board);
            _pv = new PrincipalVariation();
            _killers = new KillerMoves(4);
            _rootMoves = rootMoves;
            _maxNodes = maxNodes;
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
        }

        private IEnumerable<Board> Expand(Board position, bool escapeCheck)
        {
            if (escapeCheck)
                return Playmaker.Play(position);
            else
                return Playmaker.PlayCaptures(position);
        }

        private IEnumerable<(Move, Board)> Expand(Board position, int depth)
        {
            var moveSequence = Playmaker.Play(position, depth, _pv, _killers);
            if (_rootMoves != null && depth == Depth) //Filter moves that are not whitelisted via rootMoves
                return moveSequence.Where(entry => _rootMoves.Contains(entry.Move));
            else
                return moveSequence;
        }

        private int EvalPosition(Board position, int depth, SearchWindow window)
        {
            if (depth == 0)
                return QEval(position, window);

            NodesVisited++;
            if (Aborted)
                return 0;

            Color color = position.ActiveColor;
            int expandedNodes = 0;
            foreach ((Move move, Board child) in Expand(position, depth))
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
                    if (window.Cut(score, color))
                    {
                        if (position[move.ToSquare] == Piece.None)
                            _killers.Add(move, depth);
                        return window.GetScore(color);
                    }
                }
            }

            if (expandedNodes == 0) //no expansion happened from this node!
            {
                //having no legal moves can mean two things: (1) lost or (2) draw?
                _pv[depth] = default;
                return position.IsChecked(position.ActiveColor) ? (int)color * Evaluation.LostValue : 0;
            }

            return window.GetScore(color);
        }

        private int QEval(Board position, SearchWindow window)
        {
            NodesVisited++;
            if (Aborted)
                return 0;

            Color color = position.ActiveColor;
            bool inCheck = position.IsChecked(color);
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = Evaluation.Evaluate(position);
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
                return (int)color * Evaluation.LostValue;

            //stalemate?
            if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
                return 0;

            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            return window.GetScore(color);
        }
    }
}
