using MinimalChess;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;


namespace MinimalChessEngine
{
    class Engine
    {
        const int REPEAT_POSITION_THRESHOLD = -50;
        const int MOVE_TIME_MARGIN = 10;
        const int BRANCHING_FACTOR_ESTIMATE = 5;

        IterativeSearch _search = null;
        Thread _searching = null;
        Move _best = default;
        long _t0 = -1;
        long _tN = -1;
        int _timeBudget = 0;
        int _searchDepth = 0;
        Board _board = new Board(Board.STARTING_POS_FEN);
        List<Move> _moves = new List<Move>();
        List<Move> _repetitions = new List<Move>();
        List<Board> _history = new List<Board>();

        public bool Running { get; private set; }

        public Engine()
        {
        }

        public void Start()
        {
            Stop();
            Running = true;
        }

        internal void Quit()
        {
            Stop();
            Running = false;
        }

        //*************
        //*** SETUP ***
        //*************

        internal void SetupPosition(Board board)
        {
            Stop();
            _board = new Board(board);//make a copy
            _history.Clear();
            _history.Add(new Board(_board));
        }

        internal void Play(Move move)
        {
            Stop();
            Piece captured = _board.Play(move);
            if (captured != Piece.None)
                _history.Clear();//after capture the previous positions can't be replicated anyway so we don't need to remember them

            _history.Add(new Board(_board));
        }

        //**************
        //*** Search ***
        //**************

        internal void Go()
        {
            Stop();
            _searchDepth = int.MaxValue;
            _timeBudget = int.MaxValue;
            StartSearch();
        }

        internal void Go(int maxSearchDepth)
        {
            Stop();
            _searchDepth = maxSearchDepth;
            _timeBudget = int.MaxValue;
            StartSearch();
        }

        internal void Go(int timePerMove, int maxSearchDepth)
        {
            Stop();
            _searchDepth = maxSearchDepth;
            _timeBudget = timePerMove - MOVE_TIME_MARGIN;
            StartSearch();
        }

        internal void Go(int blackTime, int whiteTime, int blackIncrement, int whiteIncrement, int movesToGo, int maxSearchDepth)
        {
            Stop();
            _searchDepth = maxSearchDepth;
            int myTime = _board.ActiveColor == Color.Black ? blackTime : whiteTime;
            int myIncrement = _board.ActiveColor == Color.Black ? blackIncrement : whiteIncrement;
            int totalTime = myTime + myIncrement * (movesToGo - 1) - MOVE_TIME_MARGIN;
            _timeBudget = totalTime / movesToGo;
            Uci.Log($"Search budget set to {_timeBudget}ms!");
            StartSearch();
        }

        public void Stop()
        {
            if (_searching != null)
            {
                _timeBudget = 0; //this will cause the thread to terminate
                _searching.Join();
                _searching = null;
            }
        }

        //*****************
        //*** INTERNALS ***
        //*****************

        private void Search()
        {
            while (true)
            {
                _tN = Now;
                _search.SearchDeeper(() => RemainingTimeBudget < 0);

                //aborted?
                if (_search.Aborted)
                {
                    Uci.Log($"WASTED {MilliSeconds(Now - _tN)}ms on an aborted a search!");
                    break;
                }

                //collect PV
                Collect();

                //return now to save time?
                int estimate = BRANCHING_FACTOR_ESTIMATE * MilliSeconds(Now - _tN);
                if (RemainingTimeBudget < estimate)
                {
                    Uci.Log($"Estimate of {estimate}ms EXCEEDS budget of {RemainingTimeBudget}ms. Quit!");
                    break;
                }

                //max depth reached or game over?
                if (_search.Depth >= _searchDepth || _search.GameOver)
                    break;
            }
            //Done searching!
            Uci.BestMove(_best);
            _search = null;
        }

        private void Collect()
        {
            int score = (int)_search.Position.ActiveColor * _search.Score;
            Uci.Info(_search.Depth, score, _search.PositionsEvaluated, ElapsedMilliseconds, _search.PrincipalVariation);

            //Go for a draw?
            if (_repetitions.Count > 0 && score < REPEAT_POSITION_THRESHOLD)
                _best = _repetitions[0];
            else
                _best = _search.PrincipalVariation[0];
        }

        private void StartSearch()
        {
            _t0 = Now;

            //find the root moves
            _moves = new LegalMoves(_board);
            //find root moves that would result in a previous position
            _repetitions = _moves.Where(move => _history.Contains(new Board(_board, move))).ToList();
            //if we have enough moves don't consider repetitions
            if (_repetitions.Count < _moves.Count)
                _moves.RemoveAll(move => _repetitions.Contains(move));

            _search = new IterativeSearch(_board, _moves);
            _search.SearchDeeper(); //do the first iteration. it's cheap, no time check, no thread
            Collect();

            _searching = new Thread(Search);
            _searching.Priority = ThreadPriority.Highest;
            _searching.Start();
        }

        private long Now => Stopwatch.GetTimestamp();

        private int MilliSeconds(long ticks)
        {
            double dt = ticks / (double)Stopwatch.Frequency;
            return (int)(1000 * dt);
        }

        private int ElapsedMilliseconds => MilliSeconds(Now - _t0);

        private int RemainingTimeBudget => _timeBudget - ElapsedMilliseconds;
    }
}
