using MinimalChess;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace MinimalChessEngine
{
    class Engine
    {
        IterativeSearch _search = null;
        Thread _searching = null;
        Move _best = default;
        int _maxSearchDepth;
        TimeControl _time = new TimeControl();
        Board _board = new Board(Board.STARTING_POS_FEN);
        HashSet<Board> _history = new HashSet<Board>();

        public bool Running { get; private set; }
        public Color ColorToPlay => _board.ActiveColor;

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

        internal void Go(int maxDepth, int maxTime, int maxNodes)
        {
            Stop();
            _time.Go(maxTime);
            StartSearch(maxDepth, maxNodes);
        }

        internal void Go(int maxTime, int increment, int movesToGo, int maxDepth, int maxNodes)
        {
            Stop();
            _time.Go(maxTime, increment, movesToGo);
            StartSearch(maxDepth, maxNodes);
        }

        public void Stop()
        {
            if (_searching != null)
            {
                //this will cause the thread to terminate via CheckTimeBudget
                _time.Stop();
                _searching.Join();
                _searching = null;
            }
        }

        //*****************
        //*** INTERNALS ***
        //*****************

        private void StartSearch(int maxDepth, int maxNodes)
        {
            //do the first iteration. it's cheap, no time check, no thread
            Uci.Log($"Search scheduled to take {_time.TimePerMoveWithMargin}ms!");

            _search = new IterativeSearch(_board, maxNodes, _history);
            _time.StartInterval();
            _search.SearchDeeper();
            Collect();

            //start the search thread
            _maxSearchDepth = maxDepth;
            _searching = new Thread(Search);
            _searching.Priority = ThreadPriority.Highest;
            _searching.Start();
        }

        private void Search()
        {
            while (CanSearchDeeper())
            {
                _time.StartInterval();
                _search.SearchDeeper(_time.CheckTimeBudget);

                //aborted?
                if (_search.Aborted)
                    break;

                //collect PV
                Collect();
            }
            //Done searching!
            Uci.BestMove(_best);
            _search = null;
        }

        private bool CanSearchDeeper()
        {
            //max depth reached or game over?
            if (_search.Depth >= _maxSearchDepth || _search.GameOver)
                return false;

            //otherwise it's only time that can stop us!
            return _time.CanSearchDeeper();
        }

        private void Collect()
        {
            int score = (int)_search.Position.ActiveColor * _search.Score;
            Uci.Info(_search.Depth, score, _search.NodesVisited, _time.Elapsed, _search.PrincipalVariation);
            _best = _search.PrincipalVariation[0];
        }
    }
}
