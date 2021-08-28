using System;

namespace MinimalChessBoard
{
    static class PerftTable
    {
        const int ENTRY_SIZE = 24; //BYTES
        const int HASH_MEMORY = 256; //Megabytes
        const int TT_SIZE = (HASH_MEMORY * 1024 * 1024) / ENTRY_SIZE;

        static PerftHashEntry[] _table = new PerftHashEntry[TT_SIZE];

        struct PerftHashEntry
        {
            public ulong ZobristHash;
            public long ChildCount;
            public int Depth;
        }

        public static void Store(ulong zobristHash, int depth, long childCount)
        {
            int slot = (int)(zobristHash % TT_SIZE);
            _table[slot].ZobristHash = zobristHash;
            _table[slot].ChildCount = childCount;
            _table[slot].Depth = depth;
        }

        public static bool Retrieve(ulong zobristHash, int depth, out long childCount)
        {
            int slot = (int)(zobristHash % TT_SIZE);
            if (_table[slot].Depth == depth && _table[slot].ZobristHash == zobristHash)
            {
                childCount = _table[slot].ChildCount;
                return true;
            }
            childCount = 0;
            return false;
        }

        public static void Clear()
        {
            Array.Clear(_table, 0, TT_SIZE);
        }
    }
}
