using System;

namespace MinimalChess
{

    public class IterativeSearch
    {
        const int QUERY_TC_FREQUENCY = 25;
        const int MAX_GAIN_PER_PLY = 70;

        public long NodesVisited { get; private set; }
        public int Depth { get; private set; }
        public int Score { get; private set; }
        public Move[] PrincipalVariation { get; private set; } = Array.Empty<Move>();
        public bool Aborted => NodesVisited >= _maxNodes || _killSwitch.Get(NodesVisited % QUERY_TC_FREQUENCY == 0);
        public bool GameOver => Evaluation.IsCheckmate(Score);

        private Board _root;
        private KillerMoves _killers;
        private History _history;
        private KillSwitch _killSwitch;
        private long _maxNodes;

        public IterativeSearch(Board board, long maxNodes = long.MaxValue)
        {
            _root = new Board(board);
            _killers = new KillerMoves(4);
            _history = new History();
            _maxNodes = maxNodes;
        }

        public IterativeSearch(int searchDepth, Board board) : this(board)
        {
            while (!GameOver && Depth < searchDepth)
                SearchDeeper();
        }
        
        public void SearchDeeper(Func<bool> killSwitch = null)
        {
            Depth++;
            _killers.Resize(Depth);
            _history.Scale();
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
            bool isChecked = position.IsChecked(color);

            //should we try null move pruning?
            if (depth >= 2 && !isChecked)
            {
                const int R = 2;
                //skip making a move
                Board nullChild = Playmaker.PlayNullMove(position);
                //evaluate the position at reduced depth with a null-window around beta
                (int score, _) = EvalPositionTT(nullChild, depth - R - 1, window.GetUpperBound(color));
                //is the evaluation "too good" despite null-move? then don't waste time on a branch that is likely going to fail-high
                if (window.FailHigh(score, color))
                    return (score, Array.Empty<Move>());
            }

            //do a regular expansion...
            Move[] pv = Array.Empty<Move>();
            int expandedNodes = 0;
            foreach ((Move move, Board child) in Playmaker.Play(position, depth, _killers, _history))
            {
                expandedNodes++;

                //moves after the PV node are unlikely to raise alpha. try to avoid a full evaluation!
                if(expandedNodes > 1)
                {
                    bool tactical = isChecked || (move.Promotion > Piece.Queen) || child.IsChecked(child.SideToMove);

                    //some moves are hopeless and can be skipped without deeper evaluation
                    if (depth <= 4 && !tactical)
                    {
                        int futilityMargin = (int)color * depth * MAX_GAIN_PER_PLY;
                        if (window.FailLow(child.Score + futilityMargin, color))
                            continue;
                    }

                    //other moves are searched with a null-sized window and skipped if they don't raise alpha
                    if (depth >= 2)
                    {
                        //non-tactical late moves are searched at a reduced depth to make this test even faster!
                        int R = (tactical || expandedNodes < 4) ? 0 : 2;
                        (int score, _) = EvalPositionTT(child, depth - R - 1, window.GetLowerBound(color));
                        if (window.FailLow(score, color))
                            continue;
                    }
                }

                //this move is expected to raise alpha so we search at full depth!
                var eval = EvalPositionTT(child, depth - 1, window);
                if (window.FailLow(eval.Score, color))
                {
                    _history.Bad(position, move, depth);
                    continue;
                }

                //the position has a new best move and score!
                Transpositions.Store(position.ZobristHash, depth, window, eval.Score, move);
                //set the PV to this move, followed by the PV of the childnode
                pv = Merge(move, eval.PV);
                //...and maybe we even get a beta cutoff
                if (window.Cut(eval.Score, color))
                {
                    //we remember killers like hat!
                    if (position[move.ToSquare] == Piece.None)
                    {
                        _history.Good(position, move, depth);
                        _killers.Add(move, depth);
                    }

                    return (window.GetScore(color), pv);
                }
            }

            //checkmate or draw?
            if (expandedNodes == 0)
                return (position.IsChecked(color) ? Evaluation.Checkmate(color) : 0, Array.Empty<Move>());

            return (window.GetScore(color), pv);
        }

        private static Move[] Merge(Move move, Move[] pv)
        {
            Move[] result = new Move[pv.Length + 1];
            result[0] = move;
            Array.Copy(pv, 0, result, 1, pv.Length);
            return result;
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
                int standPatScore = Evaluation.DynamicScore + position.Score;
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
