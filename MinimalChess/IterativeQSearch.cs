using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class IterativeQSearch : ISearch
    {
        public long NodesVisited { get; private set; }
        public long MovesGenerated { get; private set; }
        public long MovesPlayed { get; private set; }

        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Board Position => new Board(_root); //return copy, _root must not be modified during search!
        public Move[] PrincipalVariation => _pv.GetLine(Depth);
        public bool Aborted => _killSwitch.Triggered;
        public bool GameOver => PrincipalVariation?.Length < Depth;

        Board _root = null;
        List<Move> _rootMoves = null;
        PrincipalVariation _pv;
        KillSwitch _killSwitch;

        public IterativeQSearch(Board board)
        {
            _root = new Board(board);
            _rootMoves = new LegalMoves(board);
            _pv = new PrincipalVariation();
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
            //Print PV
            //for (int i = Depth; i >= 0; i--)
            //{
            //    var line = _pv.GetLine(i);
            //    Console.WriteLine(string.Join(' ', line));
            //}
            _killSwitch = new KillSwitch(killSwitch);
            var window = SearchWindow.Infinite;
            Score = EvalPosition(_root, Depth, window);
        }

        private IEnumerable<(Move, Board)> Expand(Board position, int depth = 0)
        {
            MoveSequence moves = (depth == Depth) ? MoveSequence.FromList(position, _rootMoves) : MoveSequence.AllMoves(position);
            MovesGenerated += moves.Count;
            return moves.Boost(_pv[depth]).SortCaptures().PlayMoves();
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
                return QEval(position, window);

            NodesVisited++;
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
                    //this is a new best score!
                    _pv[depth] = move;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }
            MovesPlayed += expandedNodes;

            if (expandedNodes == 0) //no expansion happened from this node!
            {
                //having no legal moves can mean two things: (1) lost or (2) draw?
                _pv.Clear(depth);
                return position.IsChecked(position.ActiveColor) ? (int)color * Eval.LostValue : 0;
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
                Debug.Assert(Eval.GetEvaluation(position).Score == position.Score);
                int standPatScore = position.Score;
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
                return (int)color * Eval.LostValue;

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
