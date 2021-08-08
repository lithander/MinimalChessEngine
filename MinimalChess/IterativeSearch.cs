using System;

namespace MinimalChess
{

    public class IterativeSearch
    {
        const int QUERY_TC_FREQUENCY = 25;

        public long NodesVisited { get; private set; }
        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Move[] PrincipalVariation { get; private set; } = Array.Empty<Move>();
        public bool Aborted => NodesVisited >= _maxNodes || _killSwitch.Get(NodesVisited % QUERY_TC_FREQUENCY == 0);
        public bool GameOver => Evaluation.IsCheckmate(Score);

        Board _root;
        KillerMoves _killers;
        KillSwitch _killSwitch;
        long _maxNodes;

        public IterativeSearch(Board board, long maxNodes = long.MaxValue)
        {
            _root = new Board(board);
            _killers = new KillerMoves(4);
            _maxNodes = maxNodes;
        }

        public IterativeSearch(int searchDepth, Board board) : this(board)
        {
            while (!GameOver && Depth < searchDepth)
                SearchDeeper();
        }
        
        public void SearchDeeper(Func<bool> killSwitch = null)
        {
            if (GameOver)
                return;

            Depth++;
            _killers.Resize(Depth);
            StorePVinTT(PrincipalVariation, Depth);
            _killSwitch = new KillSwitch(killSwitch);
            (Score, PrincipalVariation) = EvalPosition(_root, Depth, SearchWindow.Infinite);
        }

        private void StorePVinTT(Move[] pv, int depth)
        {
            Board position = new Board(_root);
            foreach (Move move in pv)
            {
                Transpositions.Store(position.ZobristHash, --depth, SearchWindow.Infinite, Score, move);
                position.Play(move);
            }
        }

        private (int Score, Move[] PV) EvalPositionTT(Board position, int depth, SearchWindow window)
        {
            if (Transpositions.GetScore(position.ZobristHash, depth, window, out int ttScore))
                return (ttScore, Array.Empty<Move>());

            var result = EvalPosition(position, depth, window);
            Transpositions.Store(position.ZobristHash, depth, window, result.Score, default);
            return result;
        }

        private (int Score, Move[] PV) EvalPosition(Board position, int depth, SearchWindow window)
        {
            if (depth <= 0)
            {
                Evaluation.DynamicScore = Evaluation.ComputeMobility(position);
                return (QEval(position, window), Array.Empty<Move>());
            }

            NodesVisited++;
            if (Aborted)
                return (0, Array.Empty<Move>());

            Color color = position.SideToMove;
            //should we try null move pruning?
            if (depth >= 2 && !position.IsChecked(color))
            {
                const int R = 2;
                //skip making a move
                Board nullChild = Playmaker.PlayNullMove(position);
                //evaluate the position at reduced depth with a null-window around beta
                SearchWindow nullWindow = window.GetUpperBound(color);
                (int nullScore, _) = EvalPositionTT(nullChild, depth - R - 1, nullWindow);
                //is the evaluation "too good" despite null-move? then don't waste time on a branch that is likely going to fail-high
                if (nullWindow.Cut(nullScore, color))
                    return (nullScore, Array.Empty<Move>());
            }

            //do a regular expansion...
            Move[] pv = Array.Empty<Move>();
            int expandedNodes = 0;
            foreach ((Move move, Board child) in Playmaker.Play(position, depth, _killers))
            {
                expandedNodes++;

                // 0.5.8f --- 1568 - 1748 - 684  [0.477] 4000 vs AbsoluteZero
                //if (depth > 1)
                //{
                //    int see = (int)position.SideToMove * SEE.EvaluateSign(position, move);
                //    if (see < 0)
                //        R = 1;
                //}

                // 0.5.8g --- 186 - 250 - 84  [0.438] 520 vs AbsoluteZero
                //if (depth <= 2 && expandedNodes > 1 && !isChecked && !child.IsChecked(child.SideToMove))
                //{
                //    int see = (int)position.SideToMove * SEE.EvaluateSign(position, move);
                //    if (see < 0)
                //        continue;
                //}

                //moves after the PV node are unlikely to raise alpha.
                if (expandedNodes > 1 && depth >= 3 && window.Width > 0)
                {
                    //we can save a lot of nodes by searching with "null window" first, proving cheaply that the score is below alpha...
                    SearchWindow nullWindow = window.GetLowerBound(color);
                    var nullResult = EvalPositionTT(child, depth - 1, nullWindow);
                    if (!nullWindow.Inside(nullResult.Score, color))
                        continue;
                }

                //this node may raise alpha!
                var eval = EvalPositionTT(child, depth - 1, window);
                if (window.Inside(eval.Score, color))
                {
                    Transpositions.Store(position.ZobristHash, depth, window, eval.Score, move);
                    //store the PV beginning with move, followed by the PV of the childnode
                    pv = new Move[eval.PV.Length + 1];
                    pv[0] = move;
                    Array.Copy(eval.PV, 0, pv, 1, eval.PV.Length);
                    //...and maybe get a beta cutoff
                    if (window.Cut(eval.Score, color))
                    {
                        //we remember killers like hat!
                        if (position[move.ToSquare] == Piece.None)
                            _killers.Add(move, depth);

                        return (window.GetScore(color), pv);
                    }
                }
            }

            //checkmate or draw?
            if (expandedNodes == 0)
                return (position.IsChecked(color) ? Evaluation.Checkmate(color) : 0, Array.Empty<Move>());

            return (window.GetScore(color), pv);
        }

        private int QEval(Board position, SearchWindow window)
        {
            NodesVisited++;
            if (Aborted)
                return 0;

            Color color = position.SideToMove;
            int expandedNodes = 0;
            if (position.IsChecked(color))
            {
                //if inCheck we can't use standPat, play any move to escape check!
                foreach (Board child in Playmaker.Play(position))
                {
                    expandedNodes++;
                    //recursively evaluate the resulting position (after the capture) with QEval
                    int score = QEval(child, window);

                    //Cut will raise alpha and perform beta cutoff when the move is too good
                    if (window.Cut(score, color))
                        break;
                }

                //checkmate?
                if (expandedNodes == 0)
                    return Evaluation.Checkmate(color);
            }
            else
            {
                int standPatScore = Evaluation.DynamicScore + Evaluation.Evaluate(position);
                //Cut will raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(standPatScore, color))
                    return window.GetScore(color);

                //play remaining captures
                foreach (Board child in Playmaker.PlayGoodCaptures(position))
                {
                    expandedNodes++;

                    //int margin = (int)color * 150;
                    //if (!window.Inside(standPatScore + see + margin, color))
                    //    continue;

                    //if (window.Cut(standPatScore + see, color))
                    //    break;

                    //recursively evaluate the resulting position (after the capture) with QEval
                    int score = QEval(child, window);

                    //Cut will raise alpha and perform beta cutoff when the move is too good
                    if (window.Cut(score, color))
                        break;
                }

                //stalemate?
                if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
                    return 0;
            }

            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            return window.GetScore(color);
        }
    }
}
