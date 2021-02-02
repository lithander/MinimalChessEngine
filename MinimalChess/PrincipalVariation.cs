using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MinimalChess
{
    class PrincipalVariation
    {
        Move[] _moves;
        int _length;
        int _depth;

        public PrincipalVariation(int searchDepth)
        {
            _depth = searchDepth;
            _length = TotalMoves(_depth);
            _moves = new Move[_length];

            Debug.Assert(Index(searchDepth) == 0);
            Debug.Assert(Index(searchDepth - 1) == searchDepth);
            Debug.Assert(Index(0) == _length); //out of bounds! min/max on this depth should return eval(state)
        }

        private int TotalMoves(int depth)
        {
            //return depth + (depth - 1) + (depth - 2) + ... + 1;
            return (depth * depth + depth) / 2;
        }

        private int Index(int depth)
        {
            return _length - TotalMoves(depth);
        }

        public void Clear(int depth)
        {
            int iDepth = Index(depth);
            for (int i = 0; i < depth; i++)
                _moves[iDepth + i] = default;
        }

        public void Promote(int depth, Move move)
        {
            int iDepth = Index(depth);
            _moves[iDepth] = move;
            for (int i = 0; i < depth - 1; i++)
                _moves[iDepth + i + 1] = _moves[iDepth + depth + i];
        }

        public Move[] Line
        {
            get
            {
                int start = Index(_depth);
                int nullMove = Array.IndexOf(_moves, default, start, _depth);
                int count = (nullMove == -1) ? _depth : nullMove - start;

                Move[] line = new Move[count];
                Array.Copy(_moves, Index(_depth), line, 0, count);
                return line;
            }
        }

        public void Prepare(int depth)
        {
            for (int i = depth; i > 1; i--)
                _moves[Index(i)] = _moves[Index(i - 1)];
        }

        public Move this[int depth]
        {
            get { return _moves[Index(depth)]; }
            set { _moves[Index(depth)] = value; }
        }

        public Move this[int depth, int offset]
        {
            get { return _moves[Index(depth) + offset]; }
            set { _moves[Index(depth) + offset] = value; }
        }

        public void Clear()
        {
            for (int i = 0; i < _moves.Length; i++)
                _moves[i] = default;
        }
    }
}
