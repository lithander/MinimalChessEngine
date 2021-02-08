using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MinimalChess;


namespace MinimalChessEngine
{
    class Engine
    {
        const int MOVE_TIME_MARGIN = 10;

        IterativeSearch2 _search = null;
        Thread _searching = null;
        Move _best = default;
        long _t0 = -1;
        long _tN = -1;
        int _timeBudget = 0;
        bool _useEstimates = false;
        Dictionary<int, int> _searchTimes = new Dictionary<int, int>();
        Board _board = new Board(Board.STARTING_POS_FEN);
        List<Board> _history = new List<Board>();

        public bool Running { get; private set; }

        public Engine()
        {
        }

        public void Start()
        {
            Running = true;
        }

        internal void Quit()
        {
            Running = false;
        }

        //*************
        //*** SETUP ***
        //*************

        internal void SetupPosition(Board board)
        {
            _board = new Board(board);//make a copy
            _history.Clear();
            _history.Add(new Board(_board));
        }

        internal void Play(Move move)
        {
            Piece captured = _board.Play(move);
            if (captured != Piece.None)
                _history.Clear();//after capture the previous positions can't be replicated anyway so we don't need to remember them

            _history.Add(new Board(_board));
        }

        //**************
        //*** Search ***
        //**************

        internal void Update()
        {
            if (_search == null || _searching == null || _searching.IsAlive)
            {
                //Thread.Sleep(1);//sleeps "at least" 1ms, measured to be ~16ms in practice which is way too long
                return;
            }

            //aborted?
            if (_search.Aborted)
            {
                _searchTimes[_search.Depth] = (int)(1.10 * MilliSeconds(Now - _tN));
                Uci.Log($"WASTED {MilliSeconds(Now - _tN)}ms on an aborted a search!");
                Uci.BestMove(_best);
                _search = null;
                return;
            }

            //collect PV
            Collect();
            _searchTimes[_search.Depth] = MilliSeconds(Now - _tN);
            Uci.Log($"Searching depth {_search.Depth} took {MilliSeconds(Now - _tN)}ms!");

            //return now to save time?
            if (RemainingTimeBudget < EstimateRequiredTime(_search.Depth + 1))
            {
                Uci.Log($"Estimate of {EstimateRequiredTime(_search.Depth + 1)}ms EXCEEDS budget of {RemainingTimeBudget}ms. Quit!");
                Uci.BestMove(_best);
                _search = null;
                return;
            }

            //Search deeper...
            LaunchSearchThread();
            Uci.Log($"Estimate of {EstimateRequiredTime(_search.Depth + 1)}ms BELOW budget of {RemainingTimeBudget}ms. Go on...");
        }

        private void Collect()
        {
            int score = (int)_search.Position.ActiveColor * _search.Score;
            Uci.Info(_search.Depth, score, _search.EvalCount, ElapsedMilliseconds, _search.PrincipalVariation);
            _best = _search.PrincipalVariation[0];
        }

        internal void Go()
        {
            _timeBudget = int.MaxValue;
            _useEstimates = false;
            StartSearch();
        }

        internal void Go(int timePerMove)
        {
            _timeBudget = timePerMove - MOVE_TIME_MARGIN;
            _useEstimates = false;
            StartSearch();
        }

        internal void Go(int blackTime, int whiteTime, int blackIncrement, int whiteIncrement, int movesToGo)
        {
            int myTime = _board.ActiveColor == Color.Black ? blackTime : whiteTime;
            int myIncrement = _board.ActiveColor == Color.Black ? blackIncrement : whiteIncrement;
            int totalTime = myTime + myIncrement * (movesToGo - 1) - MOVE_TIME_MARGIN;
            _timeBudget = totalTime / movesToGo;
            Uci.Log($"Search budget set to {_timeBudget}ms!");
            _useEstimates = true;
            StartSearch();
        }

        public void Stop()
        {
            if (_search == null)
                return;

            if (_searching != null)
            {
                _timeBudget = 0; //this will cause the thread to terminate
                _searching.Join(); //this will wait for the thread to terminate
                _searching = null;
            }

            _search = null;
            Uci.BestMove(_best);
        }

        private void StartSearch()
        {
            _t0 = Now;
            _search = new IterativeSearch2(_board, moves => AvoidRepetitionAndRandomize(_board, moves, _history));
            _search.SearchDeeper(); //do the first iteration. it's cheap, no time check, no thread
            Collect();
            LaunchSearchThread();
        }

        private void LaunchSearchThread()
        {
            _tN = Now;
            _searching = new Thread(() => _search.SearchDeeper(() => RemainingTimeBudget < 0));
            _searching.Start();
        }
                
        private int EstimateRequiredTime(int depth)
        {
            if (!_useEstimates)
                return 0;

            if (_searchTimes.TryGetValue(depth, out int result))
                return result;

            return 10; //don't bother searching with less then 10ms on the clock
        }

        private long Now => Stopwatch.GetTimestamp();

        private int MilliSeconds(long ticks)
        {
            double dt = ticks / (double)Stopwatch.Frequency;
            return (int)(1000 * dt);
        }

        private int ElapsedMilliseconds => MilliSeconds(Now - _t0);

        private int RemainingTimeBudget => _timeBudget - ElapsedMilliseconds;

        private void AvoidRepetitionAndRandomize(Board root, LegalMoves moves, List<Board> history)
        {
            //while there are more then 1 moves iterate backwards and remove those that lead to a repetition
            for (int i = moves.Count - 1; i >= 0 && moves.Count > 1; i--)
            {
                Move move = moves[i];
                Board test = new Board(root, move);
                //is the board in the history? skip the move!
                if (history.Contains(test))
                {
                    Console.WriteLine($"Move {move} would repeat a position. Skipped!");
                    moves.RemoveAt(i);
                }
            }
            moves.Randomize();
        }
    }
}
