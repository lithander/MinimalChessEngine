using System;
using System.Collections.Generic;
using System.Text;

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

        public static short PERSISTENT = 9999;
        public const int DEFAULT_SIZE_MB = 50;
        const int ENTRY_SIZE = 16; //BYTES
        static HashEntry[] _table;

        static int Index(in ulong hash) => (int)(hash % (ulong)_table.Length);

        static Transpositions()
        {
            Resize(DEFAULT_SIZE_MB);
        }

        public static void Resize(int hashSizeMBytes)
        {
            int length = (hashSizeMBytes * 1024 * 1024) / ENTRY_SIZE;
            _table = new HashEntry[length];
        }

        public static void Clear()
        {
            Array.Clear(_table, 0, _table.Length);
        }

        public static void Store(ulong zobristHash, int depth, SearchWindow window, int score, Move bestMove)
        {
            int index = Index(zobristHash);
            ref HashEntry entry = ref _table[index];
            if (entry.Depth == PERSISTENT)
                return;

            //don't overwrite a bestmove unless it's a new position OR the new bestMove is explored to a greater depth
            if (entry.Hash != zobristHash || (depth >= entry.Depth && bestMove != default))
                _table[index].BestMove = bestMove;

            entry.Hash = zobristHash;
            entry.Depth = (short)Math.Max(0, depth);

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
            ulong zobristHash = position.ZobristHash;
            int index = Index(zobristHash);
            if (_table[index].Hash == zobristHash)
                return _table[index].BestMove;
            else
                return default;
        }

        public static bool GetScore(Board position, int depth, SearchWindow window, out int score)
        {
            ulong zobristHash = position.ZobristHash;
            int index = Index(zobristHash);
            ref HashEntry entry = ref _table[index];

            score = entry.Score;
            if (entry.Hash != zobristHash || entry.Depth < depth)
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
