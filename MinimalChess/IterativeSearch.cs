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
        private int _mobilityBonus;

        public IterativeSearch(Board board, long maxNodes = long.MaxValue)
        {
            _root = new Board(board);
            _killers = new KillerMoves(4);
            _history = new History();
            _maxNodes = maxNodes;
        }

        public IterativeSearch(int searchDepth, Board board) : this(board)
        {
            while (Depth < searchDepth)
                SearchDeeper();
        }
        
        public void SearchDeeper(Func<bool> killSwitch = null)
        {
            Depth++;
            _killers.Expand(Depth);
            _history.Scale();
            StorePVinTT(PrincipalVariation, Depth);
            _killSwitch = new KillSwitch(killSwitch);
            (Score, PrincipalVariation) = EvalPosition(_root, 0, Depth, SearchWindow.Infinite);
        }

        private void StorePVinTT(Move[] pv, int depth)
        {
            Board position = new Board(_root);
            for (int ply = 0; ply < pv.Length; ply++)
            {
                Move move = pv[ply];
                Transpositions.Store(position.ZobristHash, --depth, ply, SearchWindow.Infinite, Score, move);
                position.Play(move);
            }
        }

        private (int Score, Move[] PV) EvalPositionTT(Board position, int ply, int depth, SearchWindow window)
        {
            if (Transpositions.GetScore(position.ZobristHash, depth, ply, window, out int ttScore))
                return (ttScore, Array.Empty<Move>());

            var result = EvalPosition(position, ply, depth, window);
            Transpositions.Store(position.ZobristHash, depth, ply, window, result.Score, result.PV.Length > 0 ? result.PV[0] : default);
            return result;
        }

        private (int Score, Move[] PV) EvalPosition(Board position, int ply, int depth, SearchWindow window)
        {
            if (depth <= 0)
            {
                _mobilityBonus = Evaluation.ComputeMobility(position);
                return (QEval(position, ply, window), Array.Empty<Move>());
            }

            NodesVisited++;
            if (Aborted)
                return (0, Array.Empty<Move>());

            Color color = position.SideToMove;
            bool isChecked = position.IsChecked(color);
            //if the previous iteration found a mate we check the first few plys without null move to try and find the shortest mate or escape
            bool allowNullMove = Evaluation.IsCheckmate(Score) ? (ply > Depth/4) : true;

            //should we try null move pruning?           
            if (allowNullMove && depth >= 2 && !isChecked && window.CanFailHigh(color))
            {
                const int R = 2;
                //evaluate the position at reduced depth with a null-window around beta
                SearchWindow beta = window.GetUpperBound(color);
                //skip making a move
                Board nullChild = Playmaker.PlayNullMove(position);
                (int score, _) = EvalPositionTT(nullChild, ply + 1, depth - R - 1, beta);
                //is the evaluation "too good" despite null-move? then don't waste time on a branch that is likely going to fail-high
                //if the static eval look much worse the alpha also skip it
                if (window.FailHigh(score, color))
                    return (score, Array.Empty<Move>());
            }

            //do a regular expansion...
            Move[] pv = Array.Empty<Move>();
            int expandedNodes = 0;
            foreach ((Move move, Board child) in Playmaker.Play(position, depth, _killers, _history))
            {
                expandedNodes++;
                bool interesting = expandedNodes == 1 || isChecked || child.IsChecked(child.SideToMove);

                //some near the leaves that appear hopeless can be skipped without evaluation
                if (depth <= 4 && !interesting)
                {
                    //if the static eval look much worse the alpha also skip it
                    int futilityMargin = (int)color * depth * MAX_GAIN_PER_PLY;
                    if (window.FailLow(child.Score + futilityMargin, color))
                        continue;
                }
                
                //moves after the PV node are unlikely to raise alpha. 
                //avoid a full evaluation by searching with a null-sized window around alpha first
                //...we expect it to fail low but if it does not we have to research it!
                if (depth >= 2 && expandedNodes > 1)
                {
                    //non-tactical late moves are searched at a reduced depth to make this test even faster!
                    int R = (interesting || expandedNodes < 4) ? 0 : 2;
                    (int score, _) = EvalPositionTT(child, ply + 1, depth - R - 1, window.GetLowerBound(color));
                    if (window.FailLow(score, color))
                        continue;
                }

                //this move is expected to raise alpha so we search it at full depth!
                var eval = EvalPositionTT(child, ply + 1, depth - 1, window);
                if (window.FailLow(eval.Score, color))
                {
                    _history.Bad(position, move, depth);
                    continue;
                }

                //the position has a new best move and score!
                Transpositions.Store(position.ZobristHash, depth, ply, window, eval.Score, move);
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

                    return (GetScore(window, color), pv);
                }
            }

            //checkmate or draw?
            if (expandedNodes == 0)
                return (position.IsChecked(color) ? Evaluation.Checkmate(color, ply) : 0, Array.Empty<Move>());

            return (GetScore(window, color), pv);
        }

        private static int GetScore(SearchWindow window, Color color)
        {
            int score = window.GetScore(color);
            return score;
        }

        private static Move[] Merge(Move move, Move[] pv)
        {
            Move[] result = new Move[pv.Length + 1];
            result[0] = move;
            Array.Copy(pv, 0, result, 1, pv.Length);
            return result;
        }

        private int QEval(Board position, int ply, SearchWindow window)
        {
            NodesVisited++;
            if (Aborted)
                return 0;

            Color color = position.SideToMove;
            bool inCheck = position.IsChecked(color);
            //if inCheck we can't use standPat, need to escape check!
            if (!inCheck)
            {
                int standPatScore = position.Score + _mobilityBonus;
                //Cut will raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(standPatScore, color))
                    return GetScore(window, color);
            }

            int expandedNodes = 0;
            //play remaining captures (or any moves if king is in check)
            foreach (Board child in inCheck ? Playmaker.Play(position) : Playmaker.PlayCaptures(position))
            {
                expandedNodes++;
                //recursively evaluate the resulting position (after the capture) with QEval
                int score = QEval(child, ply + 1, window);

                //Cut will raise alpha and perform beta cutoff when the move is too good
                if (window.Cut(score, color))
                    break;
            }

            //checkmate?
            if (expandedNodes == 0 && inCheck)
                return Evaluation.Checkmate(color, ply);

            //stalemate?
            if (expandedNodes == 0 && !LegalMoves.HasMoves(position))
                return 0;

            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            return GetScore(window, color);
        }
    }
}
