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
            public ushort Depth;     //2 Bytes
            public ScoreType Type;   //1 Byte
            public Move BestMove;    //3 Bytes
            //==================================
            //                        16 Bytes
        }

        public const short MAX_DEPTH = 99;
        public const short HISTORY = 99;
        public const int DEFAULT_SIZE_MB = 50;
        
        const short BUCKETS = 4;
        const int ENTRY_SIZE = 16; //BYTES
        static HashEntry[] _table;
        public static int[] _count = new int[MAX_DEPTH+1];//MAX_DEPTH must be legal index

        static bool Index(in ulong hash, out int index)
        {
            //four indices form a cluster of buckets. All 4 indices serve as entry point into the cluster
            int i0 = (int)(hash % (ulong)_table.Length) & ~3; //"& ~3" discards the last 2 bits to get i0
            for (index = i0; index < i0 + BUCKETS; index++)
                if (_table[index].Hash == hash)
                    return true;

            return false;
        }

        static int Index(in ulong hash, ushort depth)
        {
            //four indices form a cluster of buckets. All 4 indices serve as entry point into the cluster
            int i0 = (int)(hash % (ulong)_table.Length) & ~3; //"& ~3" discards the last 2 bits to get i0
            int max = 0;
            int index = 0;
            for (int i = i0; i < i0 + BUCKETS; i++)
            {
                if (_table[i].Hash == hash)
                {
                    index = i;
                    break;
                }

                //positions at depth 10 get 1/(1+10) = 9% of the hash slots than leaf nodes.
                int draft = _table[i].Depth;
                int load = (1 + draft) * _count[draft];
                //Console.WriteLine($"(1 + {draft}) * {_count[draft]} = {load}");
                if (load > max)
                {
                    index = i;
                    max = load;
                }
            }

            //hash wasn't found! 
            _count[depth]++;
            _count[_table[index].Depth]--;
            return index;
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
            int index = Index(zobristHash, (ushort)depth);
            ref HashEntry entry = ref _table[index];

            //don't overwrite a bestmove with 'default' unless it's a new position
            if (entry.Hash != zobristHash || bestMove != default)
                entry.BestMove = bestMove;

            entry.Hash = zobristHash;
            entry.Depth = (ushort)depth;

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

        public static bool GetScore(ulong zobristHash, int depth, SearchWindow window, out int score)
        {
            score = 0;
            if (!Index(zobristHash, out int index))
                return false;

            ref HashEntry entry = ref _table[index];
            if (entry.Depth < depth)
                return false;

            score = entry.Score;
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