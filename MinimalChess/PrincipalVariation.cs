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
            int d = depth - 1;
            return (d * d + d) / 2;
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

        public void Grow(int depth)
        {
            //TODO: if depth > maxdepth return or else we get a crash. but that's fine for now as it will probably a bug has caused an "endless" search and we want to find those!
            Clear(depth);
            if (depth <= 1)
                return;

            //copy previous line
            int start = Index(depth-1);
            int nullMove = Array.IndexOf(_moves, default, start, depth);
            int count = nullMove - start;

            Clear(depth);
            int target = Index(depth);
            for (int i = 0; i < count; i++)
                _moves[target + i] = _moves[start + i];
            
            //copy to sub-pv's
            for(int i = 1; i < depth; i++)
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

        public bool IsGameOver(int depth)
        {
            int start = Index(depth);
            int nullMove = Array.IndexOf(_moves, default, start, depth);
            return nullMove != -1;
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

                int b = Index(depth -1);
                for (int i = 0; i < depth - 1; i++)
                    _moves[a + i + 1] = _moves[b + i];
            }
        }
    }

    public class PrincipalVariation2
    {
        public Move[] _moves = new Move[1];
        public int _depth = 0;

        public PrincipalVariation2()
        {
        }

        public Move[] GetLine(int depth, int length)
        {
            int start = Index(depth);
            length = Math.Min(length, Length(depth));

            int nullMove = Array.IndexOf(_moves, default, start, length);
            int count = (nullMove == -1) ? length : nullMove - start;

            Move[] line = new Move[count];
            Array.Copy(_moves, start, line, 0, count);
            return line;
        }

        public Move this[int depth]
        {
            get
            {
                int index = Index(depth);
                return _moves[index];
            }
            set
            {
                int a = Index(depth);
                _moves[a] = value;
                if (depth < _depth)
                {
                    //copy the pv line of the next depth
                    int start = Index(depth + 1);
                    int length = Length(depth + 1);
                    for (int i = 0; i < length; i++)
                        _moves[a + i + 1] = _moves[start + i];
                }
            }
        }

        private int Length(int depth)
        {
            return _depth - depth + 1;
        }

        private int Index(int depth)
        {
            while (_depth < depth)
                Grow();

            //the line starts where the subtree starts
            int lineIndex = _moves.Length - Size(_depth - depth);
            return lineIndex;
        }

        private int Size(int depth)
        {
            int d = depth + 1;
            return (d * d + d) / 2;
        }

        public void Grow()
        {
            int size = Size(_depth + 1);
            Move[] moves = new Move[size];
            //copy pv lines to new array
            int from = 0;
            int to = 0;
            for(int d = 0; d <= _depth; d++)
            {
                //how long is the line?
                int count = Length(d);
                //copy the line 
                for(int i = 0; i < count; i++)
                    moves[to++] = _moves[from++];
                //leave one free
                to++;
            }
            _moves = moves;
            _depth++;
        }

        public void Cleanup(int depth)
        {
            //copy pv at depth to all sub-pv's
            int from = Index(depth);
            for (int offset = 1; depth + offset <= _depth; offset++)
            {
                //how long is the line?
                int start = Index(depth + offset);
                int length = Length(depth + offset);
                //copy values from root
                for (int i = 0; i < length; i++)
                    _moves[start + i] = _moves[from + offset + i];
            }
        }
    }
}
