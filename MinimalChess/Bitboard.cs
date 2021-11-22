using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace MinimalChess
{
    public static class Bitboard
    {
        //returns the index of the least significant bit of the bitboard, bb can't be 0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong LSB(ulong bb) => Bmi1.X64.TrailingZeroCount(bb);

        //resets the least significant bit of the bitboard
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ClearLSB(ulong bb) => Bmi1.X64.ResetLowestSetBit(bb);

        const ulong DIAGONAL = 0x8040201008040201UL;
        const ulong ANTIDIAGONAL = 0x0102040810204080UL;
        const ulong HORIZONTAL = 0x00000000000000FFUL;
        const ulong VERTICAL = 0x0101010101010101UL;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GenBishop(in ulong occupation, in int square)
        {
            ulong bbPiece = 1UL << square;
            ulong bbBlocker = occupation & ~bbPiece;
            //mask the bits below bbPiece
            ulong bbBelow = bbPiece - 1;
            //compute rank and file of square
            int rank = square >> 3;
            int file = square & 7;
            //diagonal line through square
            ulong bbDiagonal = VerticalShift(DIAGONAL, file - rank);
            //antidiagonal line through square
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
            //horizontal line through square
            ulong bbHorizontal = HORIZONTAL << (square & 56);
            //vertical line through square
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
        //sign of 'ranks' decides between left shift or right shift. Then convert signed ranks to a positiver number of bits to shift by. Each rank has 8 bits e.g. 1 << 3 == 8
        private static ulong VerticalShift(in ulong bb, in int ranks) => ranks > 0 ? bb >> (ranks << 3) : bb << -(ranks << 3);
    }
}
