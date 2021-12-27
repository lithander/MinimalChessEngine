using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Perft
{
    public struct BoardState
    {
        //TODO: Occupied & SideToMove instead of White/Black?
        public ulong White;
        public ulong Black;
        public ulong Pawns;
        public ulong Knights;
        public ulong Bishops;
        public ulong Rooks;
        public ulong Queens;
        public ulong Kings;
        public ulong CastleFlags;
        public ulong EnPassant;

        public Color SideToMove;

        //TODO: use consts
        public const ulong BlackQueensideRookSquare = 0x0100000000000000UL;//1UL << Notation.ToSquare("a8");
        public const ulong BlackKingsideRookSquare = 0x8000000000000000UL;//1UL << Notation.ToSquare("h8");
        public const ulong BlackCastling = BlackQueensideRookSquare | BlackKingsideRookSquare;
        public const ulong WhiteQueensideRookSquare = 0x0000000000000001UL;//1UL << Notation.ToSquare("a1");
        public const ulong WhiteKingsideRookSquare = 0x0000000000000080UL;//1UL << Notation.ToSquare("h1");
        public const ulong WhiteCastling = WhiteQueensideRookSquare | WhiteKingsideRookSquare;

        public BoardState(string fen)
        {
            this = Notation.ToBoardState(fen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanWhiteCastleLong()
        {
            return (CastleFlags & WhiteQueensideRookSquare) != 0 && ((Black | White) & 0x000000000000000EUL) == 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanWhiteCastleShort()
        {
            return (CastleFlags & WhiteKingsideRookSquare) != 0 && ((Black | White) & 0x0000000000000060UL) == 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBlackCastleLong()
        {
            return (CastleFlags & BlackQueensideRookSquare) != 0 && ((Black | White) & 0x0E00000000000000UL) == 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBlackCastleShort()
        {
            return (CastleFlags & BlackKingsideRookSquare) != 0 && ((Black | White) & 0x6000000000000000UL) == 0;
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
                    Bishops |= bbPiece;
                    break;
                case Piece.Rook:
                    Rooks |= bbPiece;
                    break;
                case Piece.Queen:
                    Queens |= bbPiece;
                    break;
                case Piece.King:
                    Kings |= bbPiece;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Copy(ref BoardState other)
        {
            White = other.White;
            Black = other.Black;
            Pawns = other.Pawns;
            Knights = other.Knights;
            Bishops = other.Bishops;
            Rooks = other.Rooks;
            Queens = other.Queens;
            Kings = other.Kings;
            CastleFlags = other.CastleFlags;
            EnPassant = other.EnPassant;
            SideToMove = other.SideToMove;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearBit(int square, Piece piece)
        {
            ulong bbPiece = ~(1UL << square);
            switch (piece & Piece.ColorMask)
            {
                case Piece.Black:
                    Black &= bbPiece;
                    break;
                case Piece.White:
                    White &= bbPiece;
                    break;
            }
            switch (piece & Piece.TypeMask)
            {
                case Piece.Pawn:
                    Pawns &= bbPiece;
                    break;
                case Piece.Knight:
                    Knights &= bbPiece;
                    break;
                case Piece.Bishop:
                    Bishops &= bbPiece;
                    break;
                case Piece.Rook:
                    Rooks &= bbPiece;
                    break;
                case Piece.Queen:
                    Queens &= bbPiece;
                    break;
                case Piece.King:
                    Kings &= bbPiece;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ClearBits(int square)
        {
            ulong bbPiece = ~(1UL << square);
            Black &= bbPiece;
            White &= bbPiece;
            Pawns &= bbPiece;
            Knights &= bbPiece;
            Bishops &= bbPiece;
            Rooks &= bbPiece;
            Queens &= bbPiece;
            Kings &= bbPiece;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Play(ref Move move)
        {
            switch (SideToMove)
            {
                case Color.White:
                    PlayWhite(ref move);
                    break;
                case Color.Black:
                    PlayBlack(ref move);
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PlayBlack(ref Move move)
        {
            EnPassant = 0;
            SideToMove = Color.White;

            ulong bbTo = 1UL << move.ToSquare;
            ulong bbFrom = 1UL << move.FromSquare;

            if ((White & bbTo) > 0)
            {
                White &= ~bbTo;
                Pawns &= ~bbTo;
                Knights &= ~bbTo;
                Bishops &= ~bbTo;
                Rooks &= ~bbTo;
                Queens &= ~bbTo;
                Kings &= ~bbTo;
                CastleFlags &= ~bbTo;
            }

            Black ^= bbFrom | bbTo;

            switch (move.Flags & ~Piece.ColorMask)
            {
                case Piece.Pawn:
                    Pawns ^= bbFrom | bbTo;
                    EnPassant = (bbTo << 8) & (bbFrom >> 8);
                    break;
                case Piece.QueenPromotion:
                    Pawns &= ~bbFrom;
                    Queens |= bbTo;
                    break;
                case Piece.RookPromotion:
                    Pawns &= ~bbFrom;
                    Rooks |= bbTo;
                    break;
                case Piece.BishopPromotion:
                    Pawns &= ~bbFrom;
                    Bishops |= bbTo;
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
                    Bishops ^= bbFrom | bbTo;
                    break;
                case Piece.Rook:
                    Rooks ^= bbFrom | bbTo;
                    CastleFlags &= ~bbFrom;
                    break;
                case Piece.Queen:
                    Queens ^= bbFrom | bbTo;
                    break;
                case Piece.King:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~BlackCastling;
                    break;
                case Piece.CastleShort:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~BlackCastling;
                    Rooks ^= 0xA000000000000000UL;
                    Black ^= 0xA000000000000000UL;
                    break;
                case Piece.CastleLong:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~BlackCastling;
                    Rooks ^= 0x0900000000000000UL;
                    Black ^= 0x0900000000000000UL;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PlayWhite(ref Move move)
        {
            EnPassant = 0;
            SideToMove = Color.Black;

            ulong bbTo = 1UL << move.ToSquare;
            ulong bbFrom = 1UL << move.FromSquare;

            if ((Black & bbTo) > 0)
            {
                Black &= ~bbTo;
                Pawns &= ~bbTo;
                Knights &= ~bbTo;
                Bishops &= ~bbTo;
                Rooks &= ~bbTo;
                Queens &= ~bbTo;
                Kings &= ~bbTo;
                CastleFlags &= ~bbTo;
            }

            White ^= bbFrom | bbTo;
            switch (move.Flags & ~Piece.ColorMask)
            {
                case Piece.Pawn:
                    Pawns ^= bbFrom | bbTo;
                    EnPassant = (bbTo >> 8) & (bbFrom << 8);
                    break;
                case Piece.QueenPromotion:
                    Pawns &= ~bbFrom;
                    Queens |= bbTo;
                    break;
                case Piece.RookPromotion:
                    Pawns &= ~bbFrom;
                    Rooks |= bbTo;
                    break;
                case Piece.BishopPromotion:
                    Pawns &= ~bbFrom;
                    Bishops |= bbTo;
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
                    Bishops ^= bbFrom | bbTo;
                    break;
                case Piece.Rook:
                    Rooks ^= bbFrom | bbTo;
                    CastleFlags &= ~bbFrom;
                    break;
                case Piece.Queen:
                    Queens ^= bbFrom | bbTo;
                    break;
                case Piece.King:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~WhiteCastling;
                    break;
                case Piece.CastleShort:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~WhiteCastling;
                    Rooks ^= 0x00000000000000A0UL;
                    White ^= 0x00000000000000A0UL;
                    break;
                case Piece.CastleLong:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~WhiteCastling;
                    Rooks ^= 0x0000000000000009UL;
                    White ^= 0x0000000000000009UL;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsChecked(Color color)
        {
            if (color == Color.White)
            {
                int kingSquare = (byte)Bitboard.LSB(Kings & White);
                return IsAttackedByBlack(kingSquare);
            }
            else
            {
                int kingSquare = (byte)Bitboard.LSB(Kings & Black);
                return IsAttackedByWhite(kingSquare);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAttackedByWhite(int square)
        {
            ulong pieces = White & Knights;
            if (pieces > 0 && (pieces & Bitboard.KnightTargets[square]) > 0)
                return true;

            pieces = White & Kings;
            if (pieces > 0 && (pieces & Bitboard.KingTargets[square]) > 0)
                return true;

            pieces = White & (Queens | Bishops);
            if (pieces > 0 && (pieces & Bitboard.GetBishopTargets(Black | White, square)) > 0)
                return true;

            pieces = White & (Queens | Rooks);
            if (pieces > 0 && (pieces & Bitboard.GetRookTargets(Black | White, square)) > 0)
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
            if (pieces > 0 && (pieces & Bitboard.KnightTargets[square]) > 0)
                return true;

            pieces = Black & Kings;
            if (pieces > 0 && (pieces & Bitboard.KingTargets[square]) > 0)
                return true;

            pieces = Black & (Queens | Bishops);
            if (pieces > 0 && (pieces & Bitboard.GetBishopTargets(Black | White, square)) > 0)
                return true;

            pieces = Black & (Queens | Rooks);
            if (pieces > 0 && (pieces & Bitboard.GetRookTargets(Black | White, square)) > 0)
                return true;

            //Warning: pawn attacks do not consider en-passent!
            pieces = Black & Pawns;
            ulong left = (pieces & 0xFEFEFEFEFEFEFEFEUL) >> 9;
            ulong right = (pieces & 0x7F7F7F7F7F7F7F7FUL) >> 7;
            return ((left | right) & 1UL << square) > 0;
        }
    }
}
