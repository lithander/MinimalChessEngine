﻿using System;
using System.Diagnostics;

namespace MinimalChessEngine
{
    class TimeControl
    {
        const int BASE_MARGIN = 20;
        const int BRANCHING_FACTOR_ESTIMATE = 5;
        const int MAX_TIME_REMAINING = int.MaxValue / 3; //large but not too large to cause overlow issues

        private int _movesToGo;
        private int _increment;
        private int _remaining;
        private long _t0 = -1;
        private long _tN = -1;

        public int TimePerMove => (_remaining + (_movesToGo - 1) * _increment) / _movesToGo;
        public int TimePerMoveWithMargin => WithMargin(TimePerMove);
        public int TimeRemainingWithMargin => WithMargin(_remaining);
        public int Elapsed => MilliSeconds(Now - _t0);
        public int ElapsedInterval => MilliSeconds(Now - _tN);

        private long Now => Stopwatch.GetTimestamp();
        private int WithMargin(int time) => time - BASE_MARGIN - (int)Math.Sqrt(time);

        private int MilliSeconds(long ticks)
        {
            double dt = ticks / (double)Stopwatch.Frequency;
            return (int)(1000 * dt);
        }

        private void Reset()
        {
            _movesToGo = 1;
            _increment = 0;
            _remaining = MAX_TIME_REMAINING; 
            _t0 = Now;
            _tN = _t0;
        }

        public void StartInterval()
        {
            _tN = Now;
        }

        public void Stop()
        {
            //this will cause CanSearchDeeper() and CheckTimeBudget() to evaluate to 'false'
            _remaining = 0;
        }

        internal void Go(int timePerMove)
        {
            Reset();
            _remaining = Math.Min(timePerMove, MAX_TIME_REMAINING);
        }

        internal void Go(int time, int increment, int movesToGo)
        {
            Reset();
            _remaining = Math.Min(time, MAX_TIME_REMAINING);
            _increment = increment;
            _movesToGo = movesToGo;
        }

        public bool CanSearchDeeper()
        {
            int elapsed = Elapsed;

            //estimate the branching factor, if only one move to go we yolo with a low estimate
            int multi = (_movesToGo == 1) ? 3 : BRANCHING_FACTOR_ESTIMATE;
            int estimate = multi * ElapsedInterval;
            int total = elapsed + estimate;

            //no increment... we need to stay within the per-move time budget
            if (_increment == 0 && total > TimePerMoveWithMargin)
                return false;
            //we have already exceeded the average move
            if (elapsed > TimePerMoveWithMargin)
                return false;
            //shouldn't spend more then the 2x the average on a move
            if (total > 2 * TimePerMoveWithMargin)
                return false;
            //can't afford the estimate
            if (total > TimeRemainingWithMargin)
                return false;

            //all conditions fulfilled
            return true;
        }

        public bool CheckTimeBudget()
        {
            if (_increment == 0)
                return Elapsed > TimePerMoveWithMargin;
            else
                return Elapsed > TimeRemainingWithMargin;
        }
    }
}
