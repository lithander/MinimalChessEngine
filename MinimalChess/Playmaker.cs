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

        public void Remember(Move move, int depth)
        {
            if (move.HasFlags(MoveFlags.Capture))
                return;

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
            if (!move.HasFlags(MoveFlags.Capture))
                _history[Index(move)] += depth * depth;
        }
        internal void RememberBest(Move move, int depth)
        {
            if (!move.HasFlags(MoveFlags.Capture))
                _history[Index(move)] += depth * depth;
        }

        internal void RememberWeak(Move move, int depth)
        {
            if (!move.HasFlags(MoveFlags.Capture))
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

    class Playmaker
    {
        PrincipalVariation _pv;
        Killers _killers;
        History _history;
        int _depth = -1;
        List<Move> _rootMoves = null;

        public Playmaker(PrincipalVariation pv, Killers killers, History history, IEnumerable<Move> rootMoves)
        {
            _pv = pv;
            _killers = killers;
            _history = history;

            if (rootMoves != null)
                _rootMoves = new List<Move>(rootMoves);
        }

        public IEnumerable<(Move, Board)> Play(Board position, int depth)
        {
            _depth = Math.Max(_depth, depth);
            bool isRoot = depth == _depth;

            //there are multiple MoveSequence being played at the same time, so we create a MoveSequence instance to keep track
            MoveSequence moves = new MoveSequence(_pv, _killers, _history, depth);
            return moves.PlayMoves(position, isRoot ? _rootMoves : null);
        }

        public class MoveSequence : IMovesVisitor
        {
            List<(int Score, Move Move)> _priority;
            List<(int Score, Move Move)> _later = null;
            Board _position;
            PrincipalVariation _pv;
            Killers _killers;
            History _history;
            int _depth;

            const int PV_SCORE = Pieces.MaxRank * Pieces.MaxRank;

            public MoveSequence(PrincipalVariation pv, Killers killer, History history, int depth)
            {
                _depth = depth;
                _pv = pv;
                _killers = killer;
                _history = history;

                _priority = new List<(int, Move)>(10);
                _later = new List<(int, Move)>(40);
            }

            internal IEnumerable<(Move, Board)> PlayMoves(Board position, List<Move> moves)
            {
                _position = position;

                if (moves != null)
                {
                    foreach (var move in moves)
                        Add(move);
                }
                else
                {
                    _position.CollectMoves(this);
                }

                _priority.Sort((a, b) => b.Score.CompareTo(a.Score));

                //Return the best capture and remove it until captures are depleated
                foreach (var capture in _priority)
                    if (TryMove(capture.Move, out Board childNode))
                    {
                        //Console.WriteLine(_depth + new string(' ', _killers.Index(_depth)) + capture.Score + ": " + capture.Move);
                        yield return (capture.Move, childNode);
                    }

                _later.Sort((a, b) => b.Score.CompareTo(a.Score));

                foreach (var quiet in _later)
                    if (TryMove(quiet.Move, out Board childNode))
                        yield return (quiet.Move, childNode);
            }

            private bool TryMove(Move move, out Board childNode)
            {
                //because the move was 'pseudolegal' we make sure it doesnt result in a position 
                //where our (color) king is in check - in that case we can't play it
                childNode = new Board(_position, move);
                return !childNode.IsChecked(_position.ActiveColor);
            }

            private void Add(Move move)
            {
                Piece victim = _position[move.ToIndex];
                if (victim != Piece.None)
                    move.Flags |= MoveFlags.Capture;

                if (move == _pv[_depth])
                {
                    _priority.Add((PV_SCORE, move));
                }
                else if (victim == Piece.None && _killers.Contains(move, _depth))
                {
                    _priority.Add((0, move));
                }
                else if (victim != Piece.None)
                {
                    Piece attacker = _position[move.FromIndex];
                    //*** MVV-LVA ***
                    //Sort by the value of the victim in descending order. Ties are broken by playing the highes valued attacker first.
                    //We can compute a rating that produces this order in one sorting pass:
                    int mvvlva = Pieces.MaxRank * Pieces.Rank(victim) - Pieces.Rank(attacker);
                    _priority.Add((mvvlva, move));
                }
                else
                {
                    long value = _history.GetValue(move);
                    _later.Add(((int)value, move));
                }
            }

            public bool Done => false;

            public void Consider(Move move) => Add(move);

            public void AddUnchecked(Move move) => Add(move);
        }
    }
}
