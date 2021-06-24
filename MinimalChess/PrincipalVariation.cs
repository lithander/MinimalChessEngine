using System;

namespace MinimalChess
{
    class PrincipalVariation
    {
        Move[] _moves = new Move[0];
        int _depth = 0;

        private int Index(int depth)
        {
            //return depth + (depth - 1) + (depth - 2) + ... + 1;
            int d = depth - 1;
            return (d * d + d) / 2;
        }

        public void Grow(int depth)
        {
            while (_depth < depth)
                Grow();
        }

        private void Grow()
        {
            _depth++;
            int size = Index(_depth + 1);
            Move[] moves = new Move[size];
            //copy pv lines to new array
            int to = 0;
            for (int depth = 0; depth < _depth; depth++)
            {
                //copy the last d elements of the mainline 
                for (int i = 0; i < depth; i++)
                    moves[to++] = _moves[^(depth - i)];
                //leave one free
                to++;
            }
            _moves = moves;
        }

        public Move[] GetLine(int depth)
        {
            int start = Index(depth);
            int nullMove = Array.IndexOf(_moves, default, start, depth);
            int count = (nullMove == -1) ? depth : nullMove - start;

            Move[] line = new Move[count];
            Array.Copy(_moves, start, line, 0, count);
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
                //remember the continuation
                int b = Index(depth - 1);
                for (int i = 0; i < depth - 1; i++)
                    _moves[a + i + 1] = _moves[b + i];
            }
        }

        public void Truncate(int depth)
        {
            for (int i = 1; i <= Math.Max(depth, 1); i++)
                this[i] = default;
        }
    }
}
