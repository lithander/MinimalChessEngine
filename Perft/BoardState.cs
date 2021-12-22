using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Perft
{
    [Flags]
    public enum CastlingRights
    {
        None = 0,
        WhiteKingside = 1,
        WhiteQueenside = 2,
        BlackKingside = 4,
        BlackQueenside = 8,
        White = WhiteKingside + WhiteQueenside,
        Black = BlackKingside + BlackQueenside,
        All = 15
    }

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

        public CastlingRights CastleFlags;
        public Color SideToMove;
        public int EnPassantSquare;

        //TODO: use consts
        static readonly int BlackKingSquare = Notation.ToSquare("e8");
        static readonly int WhiteKingSquare = Notation.ToSquare("e1");
        static readonly int BlackQueensideRookSquare = Notation.ToSquare("a8");
        static readonly int BlackKingsideRookSquare = Notation.ToSquare("h8");
        static readonly int WhiteQueensideRookSquare = Notation.ToSquare("a1");
        static readonly int WhiteKingsideRookSquare = Notation.ToSquare("h1");

        public BoardState(string fen)
        {
            this = Notation.ToBoardState(fen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanWhiteCastleLong()
        {
            return (CastleFlags & CastlingRights.WhiteQueenside) != 0 && ((Black | White) & 0x000000000000000EUL) == 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanWhiteCastleShort()
        {
            return (CastleFlags & CastlingRights.WhiteKingside) != 0 && ((Black | White) & 0x0000000000000060UL) == 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBlackCastleLong()
        {
            return (CastleFlags & CastlingRights.BlackQueenside) != 0 && ((Black | White) & 0x0E00000000000000UL) == 0;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBlackCastleShort()
        {
            return (CastleFlags & CastlingRights.BlackKingside) != 0 && ((Black | White) & 0x6000000000000000UL) == 0;
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
        public void Play(Move move)
        {
            EnPassantSquare = -1;
            switch (SideToMove)
            {
                case Color.White:
                PlayWhite(move);
                SideToMove = Color.Black;
                break;
                case Color.Black:
                PlayBlack(move);
                SideToMove = Color.White;
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PlayBlack(Move move)
        {
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

                if (move.ToSquare == WhiteQueensideRookSquare)
                    CastleFlags &= ~CastlingRights.WhiteQueenside;
                else if (move.FromSquare == WhiteKingsideRookSquare)
                    CastleFlags &= ~CastlingRights.WhiteKingside;
            }

            Black ^= bbFrom | bbTo;

            switch (move.Flags & ~Piece.ColorMask)
            {
                case Piece.Pawn:
                    Pawns ^= bbFrom | bbTo;
                    //update enPassant
                    if (move.ToSquare == move.FromSquare - 16)
                        EnPassantSquare = move.FromSquare - 8;
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
                    if (move.FromSquare == BlackQueensideRookSquare)
                        CastleFlags &= ~CastlingRights.BlackQueenside;
                    else if (move.FromSquare == BlackKingsideRookSquare)
                        CastleFlags &= ~CastlingRights.BlackKingside;
                    break;
                case Piece.Queen:
                    Queens ^= bbFrom | bbTo;
                    break;
                case Piece.King:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~CastlingRights.Black;
                    break;
                case Piece.CastleShort:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~CastlingRights.Black;
                    Rooks ^= 0xA000000000000000UL;
                    Black ^= 0xA000000000000000UL;
                    break;
                case Piece.CastleLong:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~CastlingRights.Black;
                    Rooks ^= 0x0900000000000000UL;
                    Black ^= 0x0900000000000000UL;
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PlayWhite(Move move)
        {
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

                if (move.ToSquare == BlackQueensideRookSquare)
                    CastleFlags &= ~CastlingRights.BlackQueenside;
                else if (move.ToSquare == BlackKingsideRookSquare)
                    CastleFlags &= ~CastlingRights.BlackKingside;
            }

            White ^= bbFrom | bbTo;
            switch (move.Flags & ~Piece.ColorMask)
            {
                case Piece.Pawn:
                    Pawns ^= bbFrom | bbTo;
                    if (move.ToSquare == move.FromSquare + 16)
                        EnPassantSquare = move.FromSquare + 8;
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
                    if (move.FromSquare == WhiteQueensideRookSquare)
                        CastleFlags &= ~CastlingRights.WhiteQueenside;
                    else if (move.FromSquare == WhiteKingsideRookSquare)
                        CastleFlags &= ~CastlingRights.WhiteKingside;
                    break;
                case Piece.Queen:
                    Queens ^= bbFrom | bbTo;
                    break;
                case Piece.King:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~CastlingRights.White;
                    break;
                case Piece.CastleShort:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~CastlingRights.White;
                    Rooks ^= 0x00000000000000A0UL;
                    White ^= 0x00000000000000A0UL;
                    break;
                case Piece.CastleLong:
                    Kings ^= bbFrom | bbTo;
                    CastleFlags &= ~CastlingRights.White;
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

        public void SetCastlingRights(CastlingRights flag, bool state)
        {
            //_zobristHash ^= Zobrist.Castling(_castlingRights);

            if (state)
                CastleFlags |= flag;
            else
                CastleFlags &= ~flag;

            //_zobristHash ^= Zobrist.Castling(_castlingRights);
        }
    }
}
