using System;
using System.Collections.Generic;
using System.Linq;

namespace MinimalChess
{

    public class IterativeSearch
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
        HashSet<Board> _history = null;
        PrincipalVariation _pv;
        KillerMoves _killers;
        KillSwitch _killSwitch;
        long _maxNodes;

        public IterativeSearch(Board board, long maxNodes = long.MaxValue, HashSet<Board> history = null)
        {
            _root = new Board(board);
            _pv = new PrincipalVariation();
            _killers = new KillerMoves(4);
            _history = history ?? new HashSet<Board>();
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

        private int EvalPosition(Board position, int depth, SearchWindow window, bool isNullMove = false)
        {
            if (depth <= 0)
            {
                Evaluation.DynamicScore = Evaluation.ComputeMobility(position);
                return QEval(position, window);
            }

            NodesVisited++;
            if (Aborted)
                return 0;

            if (depth < Depth && _history.Contains(position))
            {
                _pv[depth] = default;
                return 0; //draw through 3-fold repetition
            }

            Color color = position.SideToMove;
            bool isInCheck = position.IsChecked(color);
            if (!isInCheck && !isNullMove && depth >= 2)
            {
                const int R = 2;
                SearchWindow nullWindow = window.GetUpperBound(color);
                //skip a move
                Board nullChild = new Board(position, Pieces.Flip(color));
                //evaluate the position at reduced depth
                int nullScore = EvalPosition(nullChild, depth - R - 1, nullWindow, true);
                //is the evaluation "too good"? then don't waste time on what is likely a beta-cutoff
                if (nullWindow.Cut(nullScore, color))
                    return nullScore;
            }

            int expandedNodes = 0;
            foreach ((Move move, Board child) in Playmaker.Play(position, depth, _pv, _killers))
            {
                expandedNodes++;

                //moves after the first are unlikely to raise alpha.
                //if that's true we can save a lot of nodes by searching with "null window" first...
                if (expandedNodes > 1 && depth > 3 && window.Width > 0)
                {
                    SearchWindow nullWindow = window.GetLowerBound(color);
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
                return isInCheck ? (int)color * Evaluation.Checkmate : 0;
            }

            return window.GetScore(color);
        }

        private int QEval(Board position, SearchWindow window)
        {
            NodesVisited++;
            if (Aborted)
                return 0;

            Color color = position.SideToMove;
            bool inCheck = position.IsChecked(color);
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = Evaluation.DynamicScore + Evaluation.Evaluate(position);
                //Cut will raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(standPatScore, color))
                    return window.GetScore(color);
            }

            int expandedNodes = 0;
            //play remaining captures (or any moves if king is in check)
            foreach (Board child in inCheck ? Playmaker.Play(position) : Playmaker.PlayCaptures(position))
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
                return (int)color * Evaluation.Checkmate;

            //stalemate?
            if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
                return 0;

            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            return window.GetScore(color);
        }
    }
}
