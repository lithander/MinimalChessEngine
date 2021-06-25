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
        public Move[] PrincipalVariation => _pv.GetLine(Depth);
        public bool Aborted => NodesVisited >= _maxNodes || _killSwitch.Get(NodesVisited % QUERY_TC_FREQUENCY == 0);
        public bool GameOver => Evaluation.IsCheckmate(Score);

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
            Transpositions.Clear();
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

        private int EvalPositionTT(Board position, int depth, SearchWindow window, bool isNullMove = false)
        {
            if (Transpositions.GetScore(position, depth, window, out int score))
            {
                _pv.Truncate(depth);
                return score;
            }

            score = EvalPosition(position, depth, window, isNullMove);
            Transpositions.Store(position.ZobristHash, depth, window, score, default);
            return score;
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

            //is this position a repetition?
            if (depth < Depth && _history.Contains(position))
            {
                _pv[depth] = default;
                return 0; //draw through 3-fold repetition
            }

            Color color = position.SideToMove;
            //should we try null move pruning?
            if (depth >= 2 && !isNullMove && !position.IsChecked(color))
            {
                const int R = 2;
                //skip making a move
                Board nullChild = Playmaker.PlayNullMove(position);
                //evaluate the position at reduced depth with a null-window around beta
                SearchWindow nullWindow = window.GetUpperBound(color);
                int nullScore = EvalPositionTT(nullChild, depth - R - 1, nullWindow, true);
                //is the evaluation "too good" despite null-move? then don't waste time on a branch that is likely going to fail-high
                if (nullWindow.Cut(nullScore, color))
                    return nullScore;
            }

            //do a regular expansion...
            int expandedNodes = 0;
            foreach ((Move move, Board child) in Playmaker.Play(position, depth, _pv, _killers))
            {
                expandedNodes++;

                //moves after the PV node are unlikely to raise alpha.
                if (expandedNodes > 1 && depth >= 3 && window.Width > 0)
                {
                    //we can save a lot of nodes by searching with "null window" first, proving cheaply that the score is below alpha...
                    SearchWindow nullWindow = window.GetLowerBound(color);
                    int nullScore = EvalPositionTT(child, depth - 1, nullWindow);
                    if (!nullWindow.Inside(nullScore, color))
                        continue;
                }

                //this node may raise alpha!
                int score = EvalPositionTT(child, depth - 1, window);
                if (window.Inside(score, color))
                {
                    Transpositions.Store(position.ZobristHash, depth, window, score, move);
                    _pv[depth] = move;
                    //...and maybe get a beta cutoff
                    if (window.Cut(score, color))
                    {
                        //we remember killers like hat!
                        if (position[move.ToSquare] == Piece.None)
                            _killers.Add(move, depth);

                        return window.GetScore(color);
                    }
                }
            }

            //no playable moves in this position?
            if (expandedNodes == 0)
            {
                //clear PV because the game is over
                _pv[depth] = default;

                if (position.IsChecked(color))
                    return Evaluation.Checkmate(color); //lost!
                else
                    return 0; //draw!
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
                return Evaluation.Checkmate(color);

            //stalemate?
            if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
                return 0;

            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            return window.GetScore(color);
        }
    }
}
