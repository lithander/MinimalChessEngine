using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public class Killers
    {
        Move[] _moves;
        int _depth = 0;
        int _width = 0;
        
        public Killers(int width)
        {
            _moves = new Move[0];
            _depth = 0;
            _width = width;
        }

        public void Grow(int depth)
        {
            _depth = Math.Max(_depth, depth);
            Array.Resize(ref _moves, _depth * _width);
        }

        public void Remember(Move move, int depth)
        {
            int index0 = _width * (_depth - depth);
            //We shift all moves by one slot to make room but overwrite a potential dublicate of 'move' then store the new 'move' at [0] 
            int last = index0;
            for (; last < index0 + _width - 1; last++)
                if (_moves[last] == move) //if 'move' is present we want to overwrite it instead of the the one at [_width-1]
                    break;
            //2. start with last slot and 'save' the previous values until the first slot got dublicated
            for (int index = last; index >= index0; index--)
                _moves[index] = _moves[index - 1];
            //3. store new 'move' in the first slot
            _moves[index0] = move;

            //make sure there are no dublicates other than 'default'
            Debug.Assert(_moves.Skip(index0).Take(_width).Where(move => move != default).Distinct().Count() == _moves.Skip(index0).Take(_width).Where(move => move != default).Count());
        }

        public Move[] Get(int depth)
        {
            Move[] line = new Move[_width];
            int index0 = _width * (_depth - depth);
            Array.Copy(_moves, index0, line, 0, _width);
            return line;
        }
    }

    public class History
    {
        const int SIZE = 64 * 64;
        long[] _history = new long[SIZE];

        int Index(Move move) => move.FromIndex + (move.ToIndex << 6);

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

        internal void RememberCutoff(Move move, int depth)
        {
            //if (!move.HasFlags(MoveFlags.Capture))
            _history[Index(move)] += depth * depth;
        }
        internal void RememberBest(Move move, int depth)
        {
            //if (!move.HasFlags(MoveFlags.Capture))
            _history[Index(move)] += depth * depth;
        }

        internal void RememberWeak(Move move, int depth)
        {
            //if (!move.HasFlags(MoveFlags.Capture))
             _history[Index(move)] -= depth;
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
                    else if (value < 0)
                        bad++;
                }
            Console.WriteLine($"History contains {good} good and {bad} bad moves! Max score: {max}, Min score {min}");
        }
    }

    static class Playmaker
    {
        public static IEnumerable<(Move Move, Board Board)> Play(Board position, int depth, PrincipalVariation pv, Killers killers)
        {
            //1. PV if available
            Move pvMove = pv[depth];
            if (position.CanPlay(pvMove))
            {
                var nextPosition = new Board(position, pvMove);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return (pvMove, nextPosition);
            }

            //2. Captures Mvv-Lva
            MoveList captures = MoveList.CollectCaptures(position);
            captures.Remove(pvMove);
            MoveOrdering.SortMvvLva(captures, position);

//            foreach (var capture in captures
//                .OrderByDescending(move => Pieces.Rank(position[move.ToIndex]))  //most valuabe victim first                                      
//                .ThenBy(move => Pieces.Rank(position[move.FromIndex]))) //least valuable attacker first

            foreach (var capture in captures)
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return (capture, nextPosition);
            }

            //3. Killers if available
            Move[] killerMoves = killers.Get(depth);
            foreach (Move killer in killerMoves)
            {
                if (killer != pvMove && position[killer.ToIndex] == Piece.None && position.CanPlay(killer))
                {
                    var nextPosition = new Board(position, killer);
                    if (!nextPosition.IsChecked(position.ActiveColor))
                        yield return (killer, nextPosition);
                }
            }

            MoveList nonCaptures = MoveList.CollectQuiets(position);
            foreach (var move in nonCaptures)
                if (move != pvMove && Array.IndexOf(killerMoves, move) == -1)
                {
                    var nextPosition = new Board(position, move);
                    if (!nextPosition.IsChecked(position.ActiveColor))
                        yield return (move, nextPosition);
                }
        }

        internal static IEnumerable<Board> Play(Board position)
        {
            MoveList captures = MoveList.CollectCaptures(position);
            MoveOrdering.SortMvvLva(captures, position);
            foreach (var capture in captures)
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }

            MoveList nonCaptures = MoveList.CollectQuiets(position);
            foreach (var move in nonCaptures)
            {
                var nextPosition = new Board(position, move);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }
        }

        internal static IEnumerable<Board> PlayCaptures(Board position)
        {
            MoveList captures = MoveList.CollectCaptures(position);
            MoveOrdering.SortMvvLva(captures, position);

            foreach (var capture in captures)
            {
                var nextPosition = new Board(position, capture);
                if (!nextPosition.IsChecked(position.ActiveColor))
                    yield return nextPosition;
            }
        }
    }
}
