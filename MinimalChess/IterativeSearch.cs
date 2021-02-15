using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class IterativeSearch
    {
        public long PositionsEvaluated { get; private set; }
        public long MovesGenerated { get; private set; }
        public long MovesPlayed { get; private set; }

        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Move[][] Lines => _bestMoves.ToArray();
        public Move[] Moves => _bestMoves.Select(line => line.First()).ToArray();

        int _rootScore = 0;
        int _rootColor = 0;
        Board _root = null;
        List<Move[]> _bestMoves = new List<Move[]>();
        PrincipalVariation _pv;

        public IterativeSearch(Board board)
        {
            _root = board;
            _rootScore = Evaluation.Evaluate(board);
            _rootColor = (int)board.ActiveColor;
            Depth = 0;
        }

        public void Search(int maxDepth)
        {
            while (Depth < maxDepth)
                SearchDeeper();
        }

        public void SearchDeeper()
        {
            Depth++;

            var moves = new LegalMoves(_root);
            var window = SearchWindow.Infinite;

            List<Move> killers = _bestMoves.Select(line => line[0]).ToList();
            _bestMoves.Clear();
            _pv = new PrincipalVariation(Depth);

            foreach (var killer in killers)
                if (moves.Contains(killer))
                    EvalMove(killer, ref window);

            foreach (var move in moves)
                if (!killers.Contains(move))
                    EvalMove(move, ref window);

            //the window is limited to allow moves of the same score to pass.
            Score = window.GetScoreFromLimit(_root.ActiveColor);
        }

        public void EvalMove(Move move, ref SearchWindow window)
        {
            Color color = _root.ActiveColor;
            Board next = new Board(_root, move);
            int score = Evaluate(next, Depth - 1, window);
            if (window.Outside(score, color))
                return;

            if (window.Limit(score, color))
                _bestMoves.Clear();

            //the move's pv is among the best
            _pv[Depth] = move;
            _bestMoves.Add(_pv.GetLine(Depth));
        }

        public int Evaluate(Board board, int depth, SearchWindow window)
        {
            if (depth == 0)
            {
                PositionsEvaluated++;
                return Evaluation.Evaluate(board);
            }

            Color color = board.ActiveColor;
            var moves = new LegalMoves(board);
            MovesGenerated += moves.Count;

            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
            {
                _pv.Clear(depth);
                return board.IsChecked(board.ActiveColor) ? (int)color * Evaluation.MinValue : 0;
            }

            var killer = _pv[depth];
            if (moves.Contains(killer))
            {
                MovesPlayed++;
                int score = Evaluate(new Board(board, killer), depth - 1, window);
                if (window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = killer;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            foreach (var move in moves)
            {
                if (move == killer)
                    continue;

                MovesPlayed++;
                int score = Evaluate(new Board(board, move), depth - 1, window);
                if (window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = move;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            return window.GetScore(color);
        }
    }

    public struct KillSwitch
    {
        Func<bool> _killSwitch;
        bool _aborted;

        public KillSwitch(Func<bool> killSwitch = null)
        {
            _killSwitch = killSwitch;
            _aborted = _killSwitch == null ? false : _killSwitch();
        }

        public bool Triggered
        {
            get
            {
                if (!_aborted && _killSwitch != null)
                    _aborted = _killSwitch();
                return _aborted;
            }
        }
    }

    public class IterativeSearch2
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

        public IterativeSearch2(Board board)
        {
            _root = new Board(board);
            _rootMoves = new LegalMoves(board);
            _pv = new PrincipalVariation(20);
        }

        public IterativeSearch2(Board board, Action<LegalMoves> rootMovesModifier) : this(board)
        {
            rootMovesModifier(_rootMoves);
        }

        public void Search(int maxDepth)
        {
            while (maxDepth > Depth)
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

        private int EvalMove(Board position, Move move, int depth, SearchWindow window)
        {
            MovesPlayed++;
            Board resultingPosition = new Board(position, move);
            return EvalPosition(resultingPosition, depth - 1, window);
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
            var moves = (depth == Depth) ? _rootMoves : new LegalMoves(position);
            MovesGenerated += moves.Count;

            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
            {
                _pv.Clear(depth);
                return position.IsChecked(position.ActiveColor) ? (int)color * Evaluation.MinValue : 0;
            }

            var killer = _pv[depth];
            if (moves.Contains(killer))
            {
                int score = EvalMove(position, killer, depth, window);
                if (window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = killer;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            foreach (var move in moves)
            {
                if (move == killer)
                    continue;

                int score = EvalMove(position, move, depth, window);
                if (window.Inside(score, color))
                {
                    //this is a new best score!
                    _pv[depth] = move;
                    if (window.Cut(score, color))
                        return window.GetScore(color);
                }
            }

            return window.GetScore(color);
        }
    }
}
