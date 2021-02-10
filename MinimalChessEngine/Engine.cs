using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MinimalChess;


namespace MinimalChessEngine
{
    class Engine
    {
        const int MOVE_TIME_MARGIN = 10;
        const int MIN_BRANCHING_FACTOR = 5;

        IterativeSearch2 _search = null;
        Task _searching = null;
        Move _best = default;
        long _t0 = -1;
        long _tN = -1;
        int _timeBudget = 0;
        Board _board = new Board(Board.STARTING_POS_FEN);
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
            _timeBudget = int.MaxValue;
            StartSearch();
        }

        internal void Go(int timePerMove)
        {
            Stop();
            _timeBudget = timePerMove - MOVE_TIME_MARGIN;
            StartSearch();
        }

        internal void Go(int blackTime, int whiteTime, int blackIncrement, int whiteIncrement, int movesToGo)
        {
            Stop();
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
                _searching.Wait();
                _searching = null;
            }
        }

        //*****************
        //*** INTERNALS ***
        //*****************

        private void Search()
        {
            _tN = Now;
            _search.SearchDeeper(() => RemainingTimeBudget < 0);

            //aborted?
            if (_search.Aborted)
            {
                Uci.Log($"WASTED {MilliSeconds(Now - _tN)}ms on an aborted a search!");
                Uci.BestMove(_best);
                _search = null;
                return;
            }

            //collect PV
            Collect();

            //return now to save time?
            int estimate = BRANCHING_FACTOR_ESTIMATE * MilliSeconds(Now - _tN);
            if (RemainingTimeBudget < estimate)
            {
                Uci.Log($"Estimate of {estimate}ms EXCEEDS budget of {RemainingTimeBudget}ms. Quit!");
                Uci.BestMove(_best);
                _search = null;
                return;
            }

            //Search deeper...
            Search();
        }

        private void Collect()
        {
            int score = (int)_search.Position.ActiveColor * _search.Score;
            Uci.Info(_search.Depth, score, _search.EvalCount, ElapsedMilliseconds, _search.PrincipalVariation);
            _best = _search.PrincipalVariation[0];
        }

        private void StartSearch()
        {
            _t0 = Now;
            _search = new IterativeSearch2(_board, moves => AvoidRepetitionAndRandomize(_board, moves, _history));
            _search.SearchDeeper(); //do the first iteration. it's cheap, no time check, no thread
            Collect();
            _searching = Task.Run(Search);
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
