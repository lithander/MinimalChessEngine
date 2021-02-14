using System;

namespace MinimalChess
{
    public class IterativeSearch
    {
        public int Depth { get; private set; }
        public long EvalCount { get; private set; }
        public int Score { get; private set; }
        public Board Position => new Board(_root); //return copy, _root must not be modified during search!
        public Move[] PrincipalVariation => Depth > 0 ? _pv.GetLine(Depth) : null;
        public bool Aborted => _killSwitch.Triggered;
        public bool GameOver => _pv.IsGameOver(Depth);

        Board _root = null;
        LegalMoves _rootMoves = null;
        PrincipalVariation _pv;
        KillSwitch _killSwitch;

        public IterativeSearch(Board board)
        {
            _root = new Board(board);
            _rootMoves = new LegalMoves(board);
            _pv = new PrincipalVariation(20);
        }

        public IterativeSearch(Board board, Action<LegalMoves> rootMovesModifier) : this(board)
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
            Board resultingPosition = new Board(position, move);
            return EvalPosition(resultingPosition, depth - 1, window);
        }

        private int EvalPosition(Board position, int depth, SearchWindow window)
        {
            if (depth == 0)
            {
                EvalCount++;
                return Evaluation.Evaluate(position);
            }

            if (_killSwitch.Triggered) return 0;

            Color color = position.ActiveColor;
            var moves = (depth == Depth) ? _rootMoves : new LegalMoves(position);

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
