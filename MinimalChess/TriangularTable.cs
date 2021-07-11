using System;

namespace MinimalChess
{
    public static class TriangularTable
    {
        static Move[] _moves = Array.Empty<Move>();

        public static void Init(int maxDepth)
        {
            int size = Index(maxDepth + 1);
            _moves = new Move[size];
        }

        private static int Index(int depth)
        {
            //return depth + (depth - 1) + (depth - 2) + ... + 1;
            int d = depth - 1;
            return (d * d + d) / 2;
        }

        public static Move[] GetLine(int depth)
        {
            int start = Index(depth);
            int nullMove = Array.IndexOf(_moves, default, start, depth);
            int count = (nullMove == -1) ? depth : nullMove - start;

            Move[] line = new Move[count];
            Array.Copy(_moves, start, line, 0, count);
            return line;
        }

        public static void Store(int depth, Move move)
        {
            int a = Index(depth);
            _moves[a] = move;
            //remember the continuation
            int b = Index(depth - 1);
            for (int i = 0; i < depth - 1; i++)
                _moves[a + i + 1] = _moves[b + i];
        }

        public static void Truncate(int depth)
        {
            Array.Clear(_moves, 0, Index(depth+1));
        }
    }
}
