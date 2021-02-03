using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MinimalChess
{
    class PrincipalVariation
    {
        readonly Move[] _moves;

        public PrincipalVariation(int maxDepth)
        {
            int length = Index(maxDepth + 1);
            _moves = new Move[length];
        }

        private int Index(int depth)
        {
            //return depth + (depth - 1) + (depth - 2) + ... + 1;
            return (depth * depth + depth) / 2;
        }

        public void Clear()
        {
            Array.Clear(_moves, 0, _moves.Length);
        }

        public void Clear(int depth)
        {
            int iDepth = Index(depth);
            for (int i = 0; i < depth; i++)
                _moves[iDepth + i] = default;
        }

        public Move[] GetLine(int depth)
        {
            int start = Index(depth);
            int nullMove = Array.IndexOf(_moves, default, start, depth);
            int count = (nullMove == -1) ? depth : nullMove - start;

            Move[] line = new Move[count];
            Array.Copy(_moves, Index(depth), line, 0, count);
            return line;
        }

        public Move this[int depth]
        {
            get 
            { 
                return _moves[Index(depth)]; 
            }
            set
            {
                int a = Index(depth);
                _moves[a] = value;

                int b = a - depth;
                for (int i = 0; i < depth - 1; i++)
                    _moves[a + i + 1] = _moves[b + i];
            }
        }
    }
}
