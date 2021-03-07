using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class DebugSearch : ISearch
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
        List<Move> _rootMoves = null;
        PrincipalVariation _pv;
        KillSwitch _killSwitch;

        public DebugSearch(Board board)
        {
            _root = new Board(board);
            _rootMoves = new LegalMoves(board);
            _pv = new PrincipalVariation(20);
        }

        public DebugSearch(Board board, List<Move> rootMoves)
        {
            _root = new Board(board);
            _rootMoves = rootMoves;
            _pv = new PrincipalVariation(20);
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

        private IEnumerable<(Move, Board)> Expand(Board position, int depth = 0)
        {
            ChildNodes5 nodes = (depth == Depth) ? new ChildNodes5(position, _rootMoves) : new ChildNodes5(position, true);
            MovesGenerated += nodes.Count;
            nodes.PlayFirst(_pv[depth]);
            nodes.SortMoves();
            return nodes;
        }

        private IEnumerable<Board> Expand(Board position, bool includeNonCaptures)
        {
            ChildNodes5 nodes = new ChildNodes5(position, includeNonCaptures);
            nodes.SortMoves();
            return nodes.Select(tuple => tuple.Board);
        }

        private int EvalPosition(Board position, int depth, SearchWindow window)
        {
            if (_killSwitch.Triggered)
                return 0;

            if (depth == 0)
                return QEval(position, window);

            PositionsEvaluated++;
            Color color = position.ActiveColor;

            int expandedNodes = 0;
            foreach ((Move move, Board board) in Expand(position, depth))
            {
                expandedNodes++;

                //For all rootmoves after the first search with "null window"
                if (expandedNodes > 1 && depth == Depth)
                {
                    SearchWindow nullWindow = window.GetNullWindow(color);
                    int nullScore = EvalPosition(board, depth - 1, nullWindow);
                    if (nullWindow.Outside(nullScore, color))
                        continue;
                }

                int score = EvalPosition(board, depth - 1, window);

                if (window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = move;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            if (expandedNodes == 0) //no expansion happened from this node!
            {
                //having no legal moves can mean two things: (1) lost or (2) draw?
                _pv.Clear(depth);
                return position.IsChecked(position.ActiveColor) ? (int)color * Evaluation.MinValue : 0;

            }

            return window.GetScore(color);
        }

        private int QEval(Board position, SearchWindow window)
        {
            PositionsEvaluated++;
            Color color = position.ActiveColor;

            //if inCheck we can't use standPat, need to escape check!
            bool inCheck = position.IsChecked(color);
            if (!inCheck)
            {
                int standPatScore = Evaluation.Evaluate(position);
                //Cut will raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(standPatScore, color))
                    return window.GetScore(color);
            }

            bool canMove = false;
            //play remaining captures (or any moves if king is in check)
            foreach (var childNode in Expand(position, inCheck))
            {
                canMove = true;
                //recursively evaluate the resulting position (after the capture) with QEval
                int score = QEval(childNode, window);

                //Cut will raise alpha and perform beta cutoff when the move is too good
                if (window.Cut(score, color))
                    break;
            }

            //checkmate?
            if (!canMove && inCheck)
                return (int)color * Evaluation.MinValue;

            //stalemate?
            if (!canMove && !AnyLegalMoves(position))
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
