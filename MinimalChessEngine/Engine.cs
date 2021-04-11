using MinimalChess;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace MinimalChessEngine
{
    class Engine
    {
        const int CONTEMPT = 0;

        DebugSearch _search = null;
        Thread _searching = null;
        Move _best = default;
        int _maxSearchDepth;
        TimeControl _time = new TimeControl();
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
            _maxSearchDepth = int.MaxValue;
            _time.Go();
            StartSearch();
        }

        internal void Go(int maxSearchDepth)
        {
            Stop();
            _maxSearchDepth = maxSearchDepth;
            _time.Go(maxSearchDepth);
            StartSearch();
        }

        internal void Go(int timePerMove, int maxSearchDepth)
        {
            Stop();
            _maxSearchDepth = maxSearchDepth;
            _time.Go(timePerMove);
            StartSearch();
        }

        internal void Go(int blackTime, int whiteTime, int blackIncrement, int whiteIncrement, int movesToGo, int maxSearchDepth)
        {
            Stop();
            _maxSearchDepth = maxSearchDepth;
            bool isBlack = _board.ActiveColor == Color.Black;
            int time = isBlack ? blackTime : whiteTime;
            int increment = isBlack ? blackIncrement : whiteIncrement;
            _time.Go(time, increment, movesToGo);
            StartSearch();
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

        private void StartSearch()
        {
            InitSearch();

            //do the first iteration. it's cheap, no time check, no thread
            Uci.Log($"Search scheduled to take {_time.TimePerMoveWithMargin}ms!");
            _time.StartInterval();
            _search.SearchDeeper();
            Collect();

            //start the search thread
            _searching = new Thread(Search);
            _searching.Priority = ThreadPriority.Highest;
            _searching.Start();
        }

        private void InitSearch()
        {
            //find the root moves
            _moves = new LegalMoves(_board);
            //find root moves that would result in a previous position
            _repetitions = _moves.Where(move => _history.Contains(new Board(_board, move))).ToList();
            //if we don't have enough moves don't consider repetitions
            if (_repetitions.Count < _moves.Count)
                _moves.RemoveAll(move => _repetitions.Contains(move));

            _search = new DebugSearch(_board, _moves);
        }

        private void Search()
        {
            while (CanSearchDeeper())
            {
                _time.StartInterval();
                _search.SearchDeeper(_time.CheckTimeBudget);

                //aborted?
                if (_search.Aborted)
                {
                    Uci.Log($"Wasted {_time.ElapsedInterval}ms partially searching ply {_search.Depth}!");
                    break;
                }

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

            //Go for a draw?
            if (_repetitions.Count > 0 && score < CONTEMPT)
                _best = _repetitions[0];
            else
                _best = _search.PrincipalVariation[0];
        }
    }
}
