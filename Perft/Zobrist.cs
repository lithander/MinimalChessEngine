using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Perft
{
    public static class Zobrist
    {
        public static ulong[][] BoardTable = new ulong[64][];
        static ulong[] FlagsTable = new ulong[64];
        public static ulong SideToMove;

        static Zobrist()
        {
            Random rnd = new Random(228126);
            for (int square = 0; square < 64; square++)
            {
                //6 black pieces + 6 white pieces
                BoardTable[square] = new ulong[14];
                for (int piece = 2; piece < 14; piece++)
                    BoardTable[square][piece] = RandomUInt64(rnd);
                //Castling & EnPassant
                FlagsTable[square] = RandomUInt64(rnd);
            }
            //Side to Move
            SideToMove = RandomUInt64(rnd);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong PieceSquare(Piece piece, int square)
        {
            return BoardTable[square][PieceIndex(piece)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PieceIndex(Piece piece)
        {
            return (int)piece >> 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Castling(int square)
        {
            return FlagsTable[square];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RandomUInt64(Random rnd)
        {
            byte[] bytes = new byte[8];
            rnd.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
