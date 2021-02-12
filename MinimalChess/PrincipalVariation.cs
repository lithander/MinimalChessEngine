using System;

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
            int d = depth - 1;
            return (d * d + d) / 2;
        }

        public void Clear(int depth)
        {
            int iDepth = Index(depth);
            for (int i = 0; i < depth; i++)
                _moves[iDepth + i] = default;
        }

        public void Grow(int depth)
        {
            Clear(depth);
            if (depth <= 1)
                return;

            //copy previous line
            int start = Index(depth - 1);
            int nullMove = Array.IndexOf(_moves, default, start, depth);
            int count = nullMove - start;

            Clear(depth);
            int target = Index(depth);
            for (int i = 0; i < count; i++)
                _moves[target + i] = _moves[start + i];

            //copy to sub-pv's
            for (int i = 1; i < depth; i++)
            {
                this[i] = _moves[target + depth - i];
            }
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

                int b = Index(depth - 1);
                for (int i = 0; i < depth - 1; i++)
                    _moves[a + i + 1] = _moves[b + i];
            }
        }
    }
}
