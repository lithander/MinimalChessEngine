using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class AlphaBetaSearch : ISearch
    {
        public long PositionsEvaluated { get; private set; }
        public long MovesGenerated { get; private set; }
        public long MovesPlayed { get; private set; }

        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Board Position => new Board(_root); //return copy, _root must not be modified during search!
        public Move[] PrincipalVariation => Depth > 0 ? _pv.GetLine(Depth) : null;
        public bool GameOver => _pv.IsGameOver(Depth);

        Board _root = null;
        LegalMoves _rootMoves = null;
        PrincipalVariation _pv;

        public AlphaBetaSearch(Board board)
        {
            _root = new Board(board);
            _rootMoves = new LegalMoves(board);
        }

        public AlphaBetaSearch(Board board, Action<LegalMoves> rootMovesModifier) : this(board)
        {
            rootMovesModifier(_rootMoves);
        }

        public void Search(int maxDepth)
        {
            Depth = maxDepth;
            _pv = new PrincipalVariation(Depth);
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

            Color color = position.ActiveColor;
            var moves = (depth == Depth) ? _rootMoves : new LegalMoves(position);
            MovesGenerated += moves.Count;

            //having no legal moves can mean two things: (1) lost or (2) draw?
            if (moves.Count == 0)
            {
                _pv.Clear(depth);
                return position.IsChecked(position.ActiveColor) ? (int)color * Evaluation.MinValue : 0;
            }

            foreach (var move in moves)
            {
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
