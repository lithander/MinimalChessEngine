using System.Runtime.CompilerServices;
using static Perft.Bitboard;

namespace Perft
{
    public class BoardState
    {
        //TODO: Occupied & SideToMove instead of White/Black?
        public ulong White;
        public ulong Black;
        public ulong Pawns;
        public ulong Knights;
        //public ulong Bishops;
        //public ulong Rooks;
        //public ulong Queens;
        public ulong Diagonals;
        public ulong Orthogonals;
        public ulong Kings;
        public ulong Flags;

        public const ulong BlackQueensideRookBit = 0x0100000000000000UL;//1UL << Notation.ToSquare("a8");
        public const ulong BlackKingsideRookBit = 0x8000000000000000UL;//1UL << Notation.ToSquare("h8");
        public const ulong WhiteQueensideRookBit = 0x0000000000000001UL;//1UL << Notation.ToSquare("a1");
        public const ulong WhiteKingsideRookBit = 0x0000000000000080UL;//1UL << Notation.ToSquare("h1");
        public const ulong WhiteToMoveBit = 0x0000000001000000UL;//a4

        public const ulong BlackCastlingBits = BlackQueensideRookBit | BlackKingsideRookBit;
        public const ulong WhiteCastlingBits = WhiteQueensideRookBit | WhiteKingsideRookBit;

        public const ulong CastlingBits = BlackCastlingBits | WhiteCastlingBits;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsWhiteToMove() => (Flags & WhiteToMoveBit) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetSideToMove() => IsWhiteToMove() ? White : Black;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Piece GetSideToMoveColor() => IsWhiteToMove() ? Piece.White : Piece.Black; //(Flags & WhiteToMoveBit) >> 23 + 1

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanWhiteCastleLong()
        {
            return (Flags & WhiteQueensideRookBit) != 0 && ((Black | White) & 0x000000000000000EUL) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanWhiteCastleShort()
        {
            return (Flags & WhiteKingsideRookBit) != 0 && ((Black | White) & 0x0000000000000060UL) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBlackCastleLong()
        {
            return (Flags & BlackQueensideRookBit) != 0 && ((Black | White) & 0x0E00000000000000UL) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBlackCastleShort()
        {
            return (Flags & BlackKingsideRookBit) != 0 && ((Black | White) & 0x6000000000000000UL) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBit(int square, Piece piece)
        {
            ulong bbPiece = 1UL << square;
            switch (piece & Piece.ColorMask)
            {
                case Piece.Black:
                    Black |= bbPiece;
                    break;
                case Piece.White:
                    White |= bbPiece;
                    break;
            }
            switch (piece & Piece.TypeMask)
            {
                case Piece.Pawn:
                    Pawns |= bbPiece;
                    break;
                case Piece.Knight:
                    Knights |= bbPiece;
                    break;
                case Piece.Bishop:
                    Diagonals |= bbPiece;
                    break;
                case Piece.Rook:
                    Orthogonals |= bbPiece;
                    break;
                case Piece.Queen:
                    Diagonals |= bbPiece;
                    Orthogonals |= bbPiece;
                    break;
                case Piece.King:
                    Kings |= bbPiece;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Copy(BoardState from)
        {
            White = from.White;
            Black = from.Black;
            Pawns = from.Pawns;
            Knights = from.Knights;
            Diagonals = from.Diagonals;
            Orthogonals = from.Orthogonals;
            Kings = from.Kings;
            Flags = from.Flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPlay(BoardState from, ref Move move)
        {
            if (from.IsWhiteToMove())
                return TryPlayWhite(from, ref move);
            else
                return TryPlayBlack(from, ref move);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPlayBlack(BoardState from, ref Move move)
        {
            PlayBlack(from, ref move);
            return !IsAttackedByWhite(LSB(Kings & Black));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPlayWhite(BoardState from, ref Move move)
        {
            PlayWhite(from, ref move);
            return !IsAttackedByBlack(LSB(Kings & White));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Play(BoardState from, ref Move move)
        {
            if(from.IsWhiteToMove())
                PlayWhite(from, ref move);
            else
                PlayBlack(from, ref move);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PlayBlack(BoardState from, ref Move move)
        {            
            ulong bbTo = 1UL << move.ToSquare;
            ulong bbFrom = 1UL << move.FromSquare;
            CopyBitboards(from, from.White & bbTo);
            Black ^= bbFrom | bbTo;

            switch (move.Flags & ~Piece.ColorMask)
            {
                case Piece.Pawn:
                    Pawns ^= bbFrom | bbTo;
                    Flags |= (bbTo << 8) & (bbFrom >> 8);
                    break;
                case Piece.QueenPromotion:
                    Pawns &= ~bbFrom;
                    Orthogonals |= bbTo;
                    Diagonals |= bbTo;
                    break;
                case Piece.RookPromotion:
                    Pawns &= ~bbFrom;
                    Orthogonals |= bbTo;
                    break;
                case Piece.BishopPromotion:
                    Pawns &= ~bbFrom;
                    Diagonals |= bbTo;
                    break;
                case Piece.KnightPromotion:
                    Pawns &= ~bbFrom;
                    Knights |= bbTo;
                    break;
                case Piece.EnPassant:
                    Pawns ^= bbFrom | bbTo | bbTo << 8;
                    White &= ~(bbTo << 8);
                    break;
                case Piece.Knight:
                    Knights ^= bbFrom | bbTo;
                    break;
                case Piece.Bishop:
                    Diagonals ^= bbFrom | bbTo;
                    break;
                case Piece.Rook:
                    Orthogonals ^= bbFrom | bbTo;
                    Flags &= ~bbFrom;
                    break;
                case Piece.Queen:
                    Orthogonals ^= bbFrom | bbTo;
                    Diagonals ^= bbFrom | bbTo;
                    break;
                case Piece.King:
                    Kings ^= bbFrom | bbTo;
                    Flags &= ~BlackCastlingBits;
                    break;
                case Piece.CastleShort:
                    Kings ^= bbFrom | bbTo;
                    Flags &= ~BlackCastlingBits;
                    Orthogonals ^= 0xA000000000000000UL;
                    Black ^= 0xA000000000000000UL;
                    break;
                case Piece.CastleLong:
                    Kings ^= bbFrom | bbTo;
                    Flags &= ~BlackCastlingBits;
                    Orthogonals ^= 0x0900000000000000UL;
                    Black ^= 0x0900000000000000UL;
                    break;
            }

            Flags |= WhiteToMoveBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PlayWhite(BoardState from, ref Move move)
        {
            ulong bbTo = 1UL << move.ToSquare;
            ulong bbFrom = 1UL << move.FromSquare;

            CopyBitboards(from, from.Black & bbTo);
            White ^= bbFrom | bbTo;

            switch (move.Flags & ~Piece.ColorMask)
            {
                case Piece.Pawn:
                    Pawns ^= bbFrom | bbTo;
                    Flags |= (bbTo >> 8) & (bbFrom << 8);
                    break;
                case Piece.QueenPromotion:
                    Pawns &= ~bbFrom;
                    Orthogonals |= bbTo;
                    Diagonals |= bbTo;
                    break;
                case Piece.RookPromotion:
                    Pawns &= ~bbFrom;
                    Orthogonals |= bbTo;
                    break;
                case Piece.BishopPromotion:
                    Pawns &= ~bbFrom;
                    Diagonals |= bbTo;
                    break;
                case Piece.KnightPromotion:
                    Pawns &= ~bbFrom;
                    Knights |= bbTo;
                    break;
                case Piece.EnPassant:
                    Pawns ^= bbFrom | bbTo | bbTo >> 8;
                    Black &= ~(bbTo >> 8);
                    break;
                case Piece.Knight:
                    Knights ^= bbFrom | bbTo;
                    break;
                case Piece.Bishop:
                    Diagonals ^= bbFrom | bbTo;
                    break;
                case Piece.Rook:
                    Orthogonals ^= bbFrom | bbTo;
                    Flags &= ~bbFrom;
                    break;
                case Piece.Queen:
                    Orthogonals ^= bbFrom | bbTo;
                    Diagonals ^= bbFrom | bbTo;
                    break;
                case Piece.King:
                    Kings ^= bbFrom | bbTo;
                    Flags &= ~WhiteCastlingBits;
                    break;
                case Piece.CastleShort:
                    Kings ^= bbFrom | bbTo;
                    Flags &= ~WhiteCastlingBits;
                    Orthogonals ^= 0x00000000000000A0UL;
                    White ^= 0x00000000000000A0UL;
                    break;
                case Piece.CastleLong:
                    Kings ^= bbFrom | bbTo;
                    Flags &= ~WhiteCastlingBits;
                    Orthogonals ^= 0x0000000000000009UL;
                    White ^= 0x0000000000000009UL;
                    break;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyBitboards(BoardState from, ulong mask)
        {
            if (mask == 0)
                CopyUnmasked(from);
            else
                CopyMasked(from, ~mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyUnmasked(BoardState from)
        {
            White = from.White;
            Black = from.Black;
            Pawns = from.Pawns;
            Knights = from.Knights;
            Diagonals = from.Diagonals;
            Orthogonals = from.Orthogonals;
            Kings = from.Kings;
            Flags = from.Flags & CastlingBits;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyMasked(BoardState from, ulong mask)
        {
            White = from.White & mask;
            Black = from.Black & mask;
            Pawns = from.Pawns & mask;
            Knights = from.Knights & mask;
            Diagonals = from.Diagonals & mask;
            Orthogonals = from.Orthogonals & mask;
            Kings = from.Kings & mask;
            Flags = from.Flags & CastlingBits & mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsChecked(Color color)
        {
            if(color == Color.White)
                return IsAttackedByBlack(LSB(Kings & White));
            else
                return IsAttackedByWhite(LSB(Kings & Black));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsOpponentChecked()
        {
            if (IsWhiteToMove())
                return IsAttackedByWhite(LSB(Kings & Black)); //Is Black checked?
            else
                return IsAttackedByBlack(LSB(Kings & White)); //Is White checked?
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAttackedByWhite(int square)
        {
            ulong pieces = White & Knights;
            if (pieces > 0 && (pieces & KnightTargets[square]) > 0)
                return true;

            pieces = White & Kings;
            if (pieces > 0 && (pieces & KingTargets[square]) > 0)
                return true;

            pieces = White & Diagonals;
            if (pieces > 0 && (pieces & DiagonalTargets[square]) > 0 && (pieces & GetDiagonalTargets(Black | White, square)) > 0)
                return true;

            pieces = White & Orthogonals;
            if (pieces > 0 && (pieces & OrthogonalTargets[square]) > 0 && (pieces & GetOrthogonalTargets(Black | White, square)) > 0)
                return true;

            //Warning: pawn attacks do not consider en-passent!
            pieces = White & Pawns;
            ulong left = (pieces & 0xFEFEFEFEFEFEFEFEUL) << 7;
            ulong right = (pieces & 0x7F7F7F7F7F7F7F7FUL) << 9;
            return ((left | right) & 1UL << square) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAttackedByBlack(int square)
        {
            ulong pieces = Black & Knights;
            if (pieces > 0 && (pieces & KnightTargets[square]) > 0)
                return true;

            pieces = Black & Kings;
            if (pieces > 0 && (pieces & KingTargets[square]) > 0)
                return true;

            pieces = Black & Diagonals;
            if (pieces > 0 && (pieces & DiagonalTargets[square]) > 0 && (pieces & GetDiagonalTargets(Black | White, square)) > 0)
                return true;

            pieces = Black & Orthogonals;
            if (pieces > 0 && (pieces & OrthogonalTargets[square]) > 0 && (pieces & GetOrthogonalTargets(Black | White, square)) > 0)
                return true;

            //Warning: pawn attacks do not consider en-passent!
            pieces = Black & Pawns;
            ulong left = (pieces & 0xFEFEFEFEFEFEFEFEUL) >> 9;
            ulong right = (pieces & 0x7F7F7F7F7F7F7F7FUL) >> 7;
            return ((left | right) & 1UL << square) > 0;
        }
    }
}
