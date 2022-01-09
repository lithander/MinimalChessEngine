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
        static ulong[] EnPassantTable = new ulong[64];
        static ulong[] CastlingTable = new ulong[16]; //all permutations of castling rights, CastlingRights.All == 15
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
                //En passent
                EnPassantTable[square] = RandomUInt64(rnd);
            }
            //Side to Move
            SideToMove = RandomUInt64(rnd);
            //Castling
            for (int i = 0; i < 16; i++)
                CastlingTable[i] = RandomUInt64(rnd);
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
        public static ulong Castling(ulong castlingRights)
        {
            //map the castling rights mask to a 4-bit integer 0x0000 to 0x1111 = 15
            int entry = Bit(castlingRights, 0, 0) | 
                        Bit(castlingRights, 7, 1) | 
                        Bit(castlingRights, 56, 2) | 
                        Bit(castlingRights, 63, 3);
            //Console.WriteLine($"bit1={bit1} | bit2={bit2} |bit3={bit3} | bit4={bit4} Total={entry}");
            return CastlingTable[entry];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Bit(ulong bb, int square, int shift) => (int)((bb >> square) & 1) << shift;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong EnPassant(int square)
        {
            return (square < 64) ? EnPassantTable[square] : 0;
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
