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
            public byte Depth;       //1 Byte
            public byte Age;         //1 Byte
            public ScoreType Type;   //1 Byte
            public Move BestMove;    //3 Bytes
            //==================================
            //                        16 Bytes
        }

        public const short MAX_DEPTH = 99;
        public const short HISTORY = 99;
        public const int DEFAULT_SIZE_MB = 50;
        
        const int ENTRY_SIZE = 16; //BYTES
        static HashEntry[] _table;
        public static int[] _count = new int[MAX_DEPTH+1];//MAX_DEPTH must be legal index

        static bool Index(in ulong hash, out int index)
        {
            index = (int)(hash % (ulong)_table.Length);
            if (_table[index].Hash != hash)
                index ^= 1;

            if (_table[index].Hash != hash)
                return false;

            _table[index].Age = 0;
            return true;
        }

        static int Index(in ulong hash, ushort depth)
        {
            _count[depth]++;
            int index = (int) (hash % (ulong) _table.Length);
            ref HashEntry e0 = ref _table[index];
            ref HashEntry e1 = ref _table[index^1];

            if (e0.Hash == hash)
            {
                _count[e0.Depth]--;
                return index;
            }

            if (e1.Hash == hash || (++e0.Age) * _count[e0.Depth] < (++e1.Age) * _count[e1.Depth])
            {
                _count[e1.Depth]--;
                return index^1;
            }

            _count[e0.Depth]--;
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
            entry.Depth = (byte)depth;
            entry.Age = 0;

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