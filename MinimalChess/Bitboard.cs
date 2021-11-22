using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace MinimalChess
{
    public static class Bitboard
    {
        public static readonly ulong[] KingTargets =
        {
            0x0000000000000302UL, 0x0000000000000705UL, 0x0000000000000E0AUL, 0x0000000000001C14UL,
            0x0000000000003828UL, 0x0000000000007050UL, 0x000000000000E0A0UL, 0x000000000000C040UL,
            0x0000000000030203UL, 0x0000000000070507UL, 0x00000000000E0A0EUL, 0x00000000001C141CUL,
            0x0000000000382838UL, 0x0000000000705070UL, 0x0000000000E0A0E0UL, 0x0000000000C040C0UL,
            0x0000000003020300UL, 0x0000000007050700UL, 0x000000000E0A0E00UL, 0x000000001C141C00UL,
            0x0000000038283800UL, 0x0000000070507000UL, 0x00000000E0A0E000UL, 0x00000000C040C000UL,
            0x0000000302030000UL, 0x0000000705070000UL, 0x0000000E0A0E0000UL, 0x0000001C141C0000UL,
            0x0000003828380000UL, 0x0000007050700000UL, 0x000000E0A0E00000UL, 0x000000C040C00000UL,
            0x0000030203000000UL, 0x0000070507000000UL, 0x00000E0A0E000000UL, 0x00001C141C000000UL,
            0x0000382838000000UL, 0x0000705070000000UL, 0x0000E0A0E0000000UL, 0x0000C040C0000000UL,
            0x0003020300000000UL, 0x0007050700000000UL, 0x000E0A0E00000000UL, 0x001C141C00000000UL,
            0x0038283800000000UL, 0x0070507000000000UL, 0x00E0A0E000000000UL, 0x00C040C000000000UL,
            0x0302030000000000UL, 0x0705070000000000UL, 0x0E0A0E0000000000UL, 0x1C141C0000000000UL,
            0x3828380000000000UL, 0x7050700000000000UL, 0xE0A0E00000000000UL, 0xC040C00000000000UL,
            0x0203000000000000UL, 0x0507000000000000UL, 0x0A0E000000000000UL, 0x141C000000000000UL,
            0x2838000000000000UL, 0x5070000000000000UL, 0xA0E0000000000000UL, 0x40C0000000000000UL
        };

        public static readonly ulong[] KnightTargets =
        {
            0x0000000000020400UL, 0x0000000000050800UL, 0x00000000000A1100UL, 0x0000000000142200UL,
            0x0000000000284400UL, 0x0000000000508800UL, 0x0000000000A01000UL, 0x0000000000402000UL,
            0x0000000002040004UL, 0x0000000005080008UL, 0x000000000A110011UL, 0x0000000014220022UL,
            0x0000000028440044UL, 0x0000000050880088UL, 0x00000000A0100010UL, 0x0000000040200020UL,
            0x0000000204000402UL, 0x0000000508000805UL, 0x0000000A1100110AUL, 0x0000001422002214UL,
            0x0000002844004428UL, 0x0000005088008850UL, 0x000000A0100010A0UL, 0x0000004020002040UL,
            0x0000020400040200UL, 0x0000050800080500UL, 0x00000A1100110A00UL, 0x0000142200221400UL,
            0x0000284400442800UL, 0x0000508800885000UL, 0x0000A0100010A000UL, 0x0000402000204000UL,
            0x0002040004020000UL, 0x0005080008050000UL, 0x000A1100110A0000UL, 0x0014220022140000UL,
            0x0028440044280000UL, 0x0050880088500000UL, 0x00A0100010A00000UL, 0x0040200020400000UL,
            0x0204000402000000UL, 0x0508000805000000UL, 0x0A1100110A000000UL, 0x1422002214000000UL,
            0x2844004428000000UL, 0x5088008850000000UL, 0xA0100010A0000000UL, 0x4020002040000000UL,
            0x0400040200000000UL, 0x0800080500000000UL, 0x1100110A00000000UL, 0x2200221400000000UL,
            0x4400442800000000UL, 0x8800885000000000UL, 0x100010A000000000UL, 0x2000204000000000UL,
            0x0004020000000000UL, 0x0008050000000000UL, 0x00110A0000000000UL, 0x0022140000000000UL,
            0x0044280000000000UL, 0x0088500000000000UL, 0x0010A00000000000UL, 0x0020400000000000UL
        };

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
        public static ulong GetBishopTargets(in ulong occupation, in int square)
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
        public static ulong GetRookTargets(in ulong occupation, in int square)
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
