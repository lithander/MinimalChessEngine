using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public enum ScoreType : byte
    {
        GreaterOrEqual,
        LessOrEqual,
        Exact
    }

    public static class Transpositions
    {
        public static long HashOverwrites = 0;
        public static long HashWrites = 0;

        public static short REPETITION = 9999;

        const int ENTRY_SIZE = 16; //BYTES
        const int HASH_MEMORY = 256; //Megabytes
        public const int TT_SIZE = (HASH_MEMORY * 1024 * 1024) / ENTRY_SIZE;

        static HashEntry[] _table = new HashEntry[TT_SIZE];

        public struct HashEntry
        {
            public ulong ZobristHash;  //8 Bytes
            public short Score;        //2 Bytes
            public short Depth;        //2 Bytes
            public ScoreType Type;     //1 Byte
            public Move BestMove;      //3 Bytes
            //==================================
            //                          16 Bytes
        }

        public static void Store(ulong zobristHash, int depth, SearchWindow window, int score, Move bestMove)
        {
            int slot = (int)(zobristHash % TT_SIZE);

            if (_table[slot].Depth == REPETITION)
                return;

            HashWrites++;
            if (_table[slot].ZobristHash != default && _table[slot].ZobristHash != zobristHash)
                HashOverwrites++;

            //don't overwrite a bestmove unless it's a new position OR the new bestMove is explored to a greater depth
            if (_table[slot].ZobristHash != zobristHash || (bestMove != default && depth >= _table[slot].Depth))
                _table[slot].BestMove = bestMove;

            _table[slot].ZobristHash = zobristHash;
            _table[slot].Depth = (short)Math.Max(0, depth);

            if (score >= window.Ceiling)
            {
                _table[slot].Type = ScoreType.GreaterOrEqual;
                _table[slot].Score = (short)window.Ceiling;
            }
            else if(score <= window.Floor)
            {
                _table[slot].Type = ScoreType.LessOrEqual;
                _table[slot].Score = (short)window.Floor;
            }
            else
            {
                _table[slot].Type = ScoreType.Exact;
                _table[slot].Score = (short)score;
            }
        }

        internal static bool GetBestMove(Board position, out Move bestMove)
        {
            ulong zobristHash = position.ZobristHash;
            int slot = (int)(zobristHash % TT_SIZE);
            if (_table[slot].ZobristHash == zobristHash)
            {
                bestMove = _table[slot].BestMove;
                return bestMove != default;
            }
            bestMove = default;
            return false;
        }

        public static bool GetScore(Board position, int depth, SearchWindow window, out int score)
        {
            ulong zobristHash = position.ZobristHash;
            int slot = (int)(zobristHash % TT_SIZE);
            score = 0;
            if (_table[slot].ZobristHash == zobristHash)
            {
                if (_table[slot].Depth < depth)
                    return false;

                score = _table[slot].Score;
                ScoreType type = _table[slot].Type;
                //1.) score is exact and within window
                if (type == ScoreType.Exact)
                    return true;
                //2.) score is below floor
                if (type == ScoreType.LessOrEqual && score <= window.Floor)
                    return true; //failLow
                //3.) score is above ceiling
                if (type == ScoreType.GreaterOrEqual && score >= window.Ceiling)
                    return true; //failHigh
            }
            return false;
        }

        public static void Clear()
        {
            Array.Clear(_table, 0, TT_SIZE);
        }
    }
}
