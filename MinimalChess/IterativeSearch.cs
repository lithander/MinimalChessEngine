using System;
using System.Collections.Generic;
using System.Linq;

namespace MinimalChess
{

    public class IterativeSearch
    {
        public long NodesVisited { get; private set; }
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
        KillerMoves _killers;

        public IterativeSearch(Board board, List<Move> rootMoves = null)
        {
            _root = new Board(board);
            _pv = new PrincipalVariation();
            _killers = new KillerMoves(4);
            _rootMoves = rootMoves;
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
            if (expandedNodes == 0 && !AnyLegalMoves.HasMoves(position))
                return 0;

            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            return window.GetScore(color);
        }
    }
}
