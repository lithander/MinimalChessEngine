﻿using MinimalChess;
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
        List<Board> _history = new List<Board>();

        public bool Running { get; private set; }
        public Color SideToMove => _board.SideToMove;

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
            _board.Play(move);
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

            //clear a rolling quarter of the TT so that it doesn't get filled with high-depth but obsolete old positions
            Transpositions.ClearChunk(_history.Count, 8);
            //add all history positions with a score of 0 (Draw through 3-fold repetition) and freeze them by setting a depth that is never going to be overwritten
            foreach (var position in _history)
                Transpositions.Store(position.ZobristHash, Transpositions.HISTORY, SearchWindow.Infinite, 0, default);
            
            _search = new IterativeSearch(_board, maxNodes);
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
            Uci.Info(_search.Depth, (int)SideToMove * _search.Score, _search.NodesVisited, _time.Elapsed, _search.PrincipalVariation);
            _best = _search.PrincipalVariation[0];
        }
    }
}
