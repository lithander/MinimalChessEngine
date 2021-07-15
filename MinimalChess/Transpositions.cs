using System;
using System.Diagnostics;

namespace MinimalChess
{
    public static class Transpositions
    {
        public enum ScoreType : byte
        {
            GreaterOrEqual,
            LessOrEqual,
            Exact
        }

        public struct HashEntry
        {
            public ulong Hash;       //8 Bytes
            public short Score;      //2 Bytes
            public short Depth;      //2 Bytes
            public ScoreType Type;   //1 Byte
            public Move BestMove;    //3 Bytes
            //==================================
            //                        16 Bytes
        }

        public const short HISTORY = 99;
        public const int DEFAULT_SIZE_MB = 50;
        const int ENTRY_SIZE = 16; //BYTES
        static HashEntry[] _table;
        public static long[] _count = new long[100];

        static bool Index(in ulong hash, out int index)
        {
            index = (int)(hash % (ulong)_table.Length);
            if (_table[index].Hash == hash)
                return true;

            //try other 'bucket' if not in first one
            index ^= 1;
            if (_table[index].Hash == hash)
                return true;

            return false;
        }

        static int Index(in ulong hash, int depth)
        {
            _count[depth]++;

            int i0 = (int)(hash % (ulong)_table.Length);

            int d0 = _table[i0].Depth;
            if (_table[i0].Hash == hash)
            {
                _count[d0]--;
                return i0;
            }

            //try other 'bucket' if not in first one
            int i1 = i0 ^ 1;
            int d1 = _table[i1].Depth;
            if (_table[i1].Hash == hash || _count[d1] > _count[d0])
            {
                _count[d1]--;
                return i1;
            }

            _count[d0]--;
            return i0;
        }

        static Transpositions()
        {
            Resize(DEFAULT_SIZE_MB);
        }

        public static void Resize(int hashSizeMBytes)
        {
            int length = (hashSizeMBytes * 1024 * 1024) / ENTRY_SIZE;
            _table = new HashEntry[length];
            Array.Clear(_count, 0, _count.Length);
            _count[0] = _table.Length;
        }

        public static void Clear()
        {
            Array.Clear(_table, 0, _table.Length);
            Array.Clear(_count, 0, _count.Length);
            _count[0] = _table.Length;
        }

        public static void Store(ulong zobristHash, int depth, SearchWindow window, int score, Move bestMove)
        {
            depth = Math.Max(depth, 0);
            int index = Index(zobristHash, depth);
            ref HashEntry entry = ref _table[index];

            //don't overwrite a bestmove with 'default' unless it's a new position
            if (entry.Hash != zobristHash || bestMove != default)
                entry.BestMove = bestMove;

            entry.Hash = zobristHash;
            entry.Depth = (short)depth;

            if (score >= window.Ceiling)
            {
                entry.Type = ScoreType.GreaterOrEqual;
                entry.Score = (short)window.Ceiling;
            }
            else if(score <= window.Floor)
            {
                entry.Type = ScoreType.LessOrEqual;
                entry.Score = (short)window.Floor;
            }
            else
            {
                entry.Type = ScoreType.Exact;
                entry.Score = (short)score;
            }
        }

        internal static Move GetBestMove(Board position)
        {
            if(Index(position.ZobristHash, out int index))
                return _table[index].BestMove;

            return default;
        }

        public static bool GetScore(Board position, int depth, SearchWindow window, out int score)
        {
            score = 0;
            if (!Index(position.ZobristHash, out int index))
                return false;

            ref HashEntry entry = ref _table[index];
            score = entry.Score;
            if (entry.Depth < depth)
                return false;

            //1.) score is exact and within window
            if (entry.Type == ScoreType.Exact)
                return true;
            //2.) score is below floor
            if (entry.Type == ScoreType.LessOrEqual && score <= window.Floor)
                return true; //failLow
            //3.) score is above ceiling
            if (entry.Type == ScoreType.GreaterOrEqual && score >= window.Ceiling)
                return true; //failHigh

            return false;
        }
    }
}
