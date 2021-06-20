using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public static class Zobrist
    {
        public static ulong[][] Board = new ulong[64][];
        public static ulong[] EnPassant = new ulong[64];
        public static ulong[] Castling = new ulong[16]; //all permutations of castling rights, CastlingRights.All == 15
        public static ulong Black;
        public static ulong White;

        static Zobrist()
        {
            Random rnd = new Random(150482);
            //6 black pieces + 6 white pieces
            for (int square = 0; square < 64; square++)
            {
                Board[square] = new ulong[12];
                for (int piece = 0; piece < 12; piece++)
                    Board[square][piece] = RandomUInt64(rnd);
            }
            //Side to Move
            Black = RandomUInt64(rnd);
            White = RandomUInt64(rnd);
            //En passent
            for (int square = 0; square < 64; square++)
                EnPassant[square] = RandomUInt64(rnd);
            //Castling
            for (int i = 0; i < 16; i++)
                Castling[i] = RandomUInt64(rnd);
        }

        private static ulong RandomUInt64(Random rnd)
        {
            byte[] bytes = new byte[8];
            rnd.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
