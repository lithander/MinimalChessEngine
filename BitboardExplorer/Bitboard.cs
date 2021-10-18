using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace BitboardExplorer
{
    static class Bitboard
    {
        const ulong DIAGONAL = 0x8040201008040201UL;
        const ulong ANTIDIAGONAL = 0x0102040810204080UL;

        const ulong HORIZONTAL = 0x00000000000000FFUL;
        const ulong VERTICAL = 0x0101010101010101UL;

        private static readonly ulong[] DIAGONALS = {
            0x1, 0x102, 0x10204, 0x1020408, 0x102040810, 0x10204081020, 0x1020408102040,
            0x102040810204080, 0x204081020408000, 0x408102040800000, 0x810204080000000,
            0x1020408000000000, 0x2040800000000000, 0x4080000000000000, 0x8000000000000000
        };

        private static readonly ulong[] ANTIDIAGONALS = {
            0x80, 0x8040, 0x804020, 0x80402010, 0x8040201008, 0x804020100804, 0x80402010080402,
            0x8040201008040201, 0x4020100804020100, 0x2010080402010000, 0x1008040201000000,
            0x804020100000000, 0x402010000000000, 0x201000000000000, 0x100000000000000
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GenBishop____(in ulong occupation, in int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            //mask the bits below bbPiece
            ulong bbBelow = bbPiece - 1;

            int rank = square >> 3;
            int file = square & 7;
            //diagonal full line
            ulong bbDiagonal = DIAGONALS[file + rank];
            //antidiagonal full line 
            ulong bbAntiDiagonal = ANTIDIAGONALS[rank + 7 - file];

            return GenLines(bbDiagonal, bbAntiDiagonal, bbBlocker, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GenBishop__(in ulong occupation, in int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            //mask the bits below bbPiece
            ulong bbBelow = bbPiece - 1;

            int diag = 8 * (square & 7) - (square & 56);
            ulong bbDiagonal = (DIAGONAL >> (diag & (-diag >> 31))) << (-diag & (diag >> 31));

            diag = 56 - 8 * (square & 7) - (square & 56);
            ulong bbAntiDiagonal = (ANTIDIAGONAL >> (diag & (-diag >> 31))) << (-diag & (diag >> 31));

            return GenLines(bbDiagonal, bbAntiDiagonal, bbBlocker, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GenBishop(in ulong occupation, in int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            //mask the bits below bbPiece
            ulong bbBelow = bbPiece - 1;

            int rank = square >> 3;
            int file = square & 7;
            //diagonal full line
            ulong bbDiagonal = VerticalShift(DIAGONAL, file - rank);
            //antidiagonal full line 
            ulong bbAntiDiagonal = VerticalShift(ANTIDIAGONAL, 7 - file - rank);

            return GenLines(bbDiagonal, bbAntiDiagonal, bbBlocker, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GenRook(in ulong occupation, in int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            //mask the bits below bbPiece
            ulong bbBelow = bbPiece - 1;
            //horizontal full line
            ulong bbHorizontal = HORIZONTAL << (square & 56);
            //vertical full line
            ulong bbVertical = VERTICAL << (square & 7);

            return GenLines(bbHorizontal, bbVertical, bbBlocker, bbBelow);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GenLines(in ulong bbLineA, in ulong bbLineB, in ulong bbBlocker, in ulong bbBelow) =>
            GenLine(bbLineA, bbBlocker & bbLineA, bbBelow) |
            GenLine(bbLineB, bbBlocker & bbLineB, bbBelow);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GenLine(in ulong bbLine, in ulong bbBlocker, in ulong bbBelow)
        {
            //MaskLow sets all low bits up to and including the lowest blocker above orgin, the rest are zeroed out.
            //MaskHigh sets all low bits up to and including the highest blocker below origin, the rest are zerored out.
            //The bits of the line that are different between the two masks are the valid targets (including the first blockers on each side)
            return (MaskLow(bbBlocker & ~bbBelow) ^ MaskHigh(bbBlocker & bbBelow)) & bbLine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //identify the highest set bit and shift a mask so the bits below are set and the rest are zeroed
        private static ulong MaskHigh(in ulong bb) => 0x7FFFFFFFFFFFFFFFUL >> (int)Lzcnt.X64.LeadingZeroCount(bb | 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //identify the lowest set bit and set all bits below while zeroing the rest
        private static ulong MaskLow(in ulong bb) => bb ^ (bb - 1);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //use sign of 'ranks' to decide between a left shift or right shift, then convert signed ranks to a positiver number of bits. each rank has 8 bits e.g. 1 << 3
        private static ulong VerticalShift(in ulong bb, int ranks) => ranks > 0 ? bb >> (ranks << 3) : bb << -(ranks << 3);
    }
}
