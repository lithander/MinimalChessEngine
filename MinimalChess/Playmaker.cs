using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public class Killers
    {
        List<Move> _moves = new List<Move>();
        List<Move> _backup = new List<Move>();
        int _depth = -1;

        public void Grow(int depth)
        {
            _depth = depth;
            while (_moves.Count < _depth)
                _moves.Add(default);

            while (_backup.Count < _depth)
                _backup.Add(default);
        }

        public void Consider(Board position, Move move, int depth)
        {
            if (position[move.ToIndex] != Piece.None)
                return; //not a quiet move!

            Store(move, depth);
        }

        public void Store(Move move, int depth)
        {
            int index = _depth - depth;
            if (_moves[index] != move)
            {
                _backup[index] = _moves[index];
                _moves[index] = move;
            }
        }

        public bool Contains(Move move, int depth)
        {
            int index = _depth - depth;
            return (_moves[index] == move || _backup[index] == move);
        }

        public int Index(int depth) => _depth - depth;
    }

    public class History
    {
        const int SIZE = 64 * 64;
        long[] _history = new long[SIZE];

        int Index(Move move) => move.FromIndex + (move.ToIndex << 6);

        public void Increase(Move move, int delta)
        {
            _history[Index(move)] += delta;
        }

        public void Decrease(Move move, int delta)
        {
            _history[Index(move)] -= delta;
        }

        public long GetValue(Move move)
        {
            int index = Index(move);
            return _history[index];
        }

        public void PrintContent()
        {
            for (int i = 0; i < 64; i++)
                for (int j = 0; j < 64; j++)
                {
                    int index = i + (j << 6);
                    if (_history[index] != 0)
                    {
                        long value = _history[index];
                        Move move = new Move(i, j);
                        Console.WriteLine($"{move} Value: {value}");
                    }
                }
        }

        public void PrintStats()
        {
            int good = 0;
            int bad = 0;
            long max = 0;
            long min = 0;
            for (int i = 0; i < 64; i++)
                for (int j = 0; j < 64; j++)
                {
                    int index = i + (j << 6);
                    long value = _history[index];

                    max = Math.Max(max, value);
                    min = Math.Min(min, value);
                    if (value > 0)
                        good++;
                    else if(value < 0)
                        bad++;
                }
            Console.WriteLine($"History contains {good} good and {bad} bad moves! Max score: {max}, Min score {min}");
        }
    }

    class Playmaker
    {
        PrincipalVariation _pv;
        Killers _killers;
        History _history;
        int _depth = -1;
        List<Move> _rootMoves = null;


        public Playmaker(PrincipalVariation pv, IEnumerable<Move> rootMoves)
        {
            _pv = pv;
            _killers = new Killers();
            _history = new History();
            _rootMoves = new List<Move>(rootMoves);

        }

        public Playmaker(PrincipalVariation pv)
        {
            _pv = pv;
            _killers = new Killers();
            _history = new History();
        }

        public IEnumerable<(Move, Board)> Play(Board position, int depth)
        {
            while(depth > _depth)
                _killers.Grow(++_depth);

            MoveSequence2 moves = new MoveSequence2(_pv, _killers, _history, depth);
            if (depth == _depth && _rootMoves != null)
                moves.FromList(position, _rootMoves);
            else
                moves.AllMoves(position);

            return moves.PlayMoves();
        }

        internal void NotifyCutoff(Move move, int depth)
        {
            if (move.HasFlags(MoveFlags.Capture))
                return; //only quiet cutoffs are stored

            _history.Increase(move, depth*depth);
            _killers.Store(move, depth);
        }

        internal void NotifyBest(Move move, int depth)
        {
            if (!move.HasFlags(MoveFlags.Capture))
                _history.Increase(move, depth * depth);
        }

        internal void NotifyBad(Move move, int depth)
        {
            if (!move.HasFlags(MoveFlags.Capture))
                _history.Decrease(move, depth);
        }

        public void PrintStats()
        {
            _history.PrintStats();
        }
    }
}
