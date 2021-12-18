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
            ClearBit(move.FromSquare, move.Flags);

            if (((Black | White) & (1UL << move.ToSquare)) > 0)
                ClearBits(move.ToSquare);

            EnPassantSquare = -1;
            switch (move.Flags & Piece.TypeMask)
            {
                case Piece.Pawn:
                    if ((move.Flags & Piece.Promotion) != 0)
                    {
                        SetBit(move.ToSquare, move.Promotion);
                    }
                    else if ((move.Flags & Piece.EnPassant) != 0)
                    {
                        SetBit(move.ToSquare, move.Flags);
                        //Delete the captured pawn
                        if ((move.Flags & Piece.ColorMask) == Piece.White)
                            ClearBit(move.ToSquare - 8, Piece.BlackPawn);
                        else
                            ClearBit(move.ToSquare + 8, Piece.WhitePawn);
                    }
                    else
                    {
                        SetBit(move.ToSquare, move.Flags);
                        //update enPassant
                        if (move.Flags == Piece.BlackPawn && move.ToSquare == move.FromSquare - 16)
                            EnPassantSquare = move.FromSquare - 8;
                        else if (move.Flags == Piece.WhitePawn && move.ToSquare == move.FromSquare + 16)
                            EnPassantSquare = move.FromSquare + 8;
                    }
                    break;
                case Piece.Knight:
                    SetBit(move.ToSquare, move.Flags);
                    break;
                case Piece.Bishop:
                    SetBit(move.ToSquare, move.Flags);
                    break;
                case Piece.Rook:
                    //any move from or to rook squares will effect castling right
                    //TODO: Mask
                    if (move.FromSquare == WhiteQueensideRookSquare || move.ToSquare == WhiteQueensideRookSquare)
                        CastleFlags &= ~CastlingRights.WhiteQueenside;
                    if (move.FromSquare == WhiteKingsideRookSquare || move.ToSquare == WhiteKingsideRookSquare)
                        CastleFlags &= ~CastlingRights.WhiteKingside;

                    if (move.FromSquare == BlackQueensideRookSquare || move.ToSquare == BlackQueensideRookSquare)
                        CastleFlags &= ~CastlingRights.BlackQueenside;
                    if (move.FromSquare == BlackKingsideRookSquare || move.ToSquare == BlackKingsideRookSquare)
                        CastleFlags &= ~CastlingRights.BlackKingside;

                    SetBit(move.ToSquare, move.Flags);
                    break;
                case Piece.Queen:
                    SetBit(move.ToSquare, move.Flags);
                    break;
                case Piece.King:
                    SetBit(move.ToSquare, move.Flags);

                    if (move.FromSquare == WhiteKingSquare)
                    {
                        CastleFlags &= ~CastlingRights.WhiteQueenside;
                        CastleFlags &= ~CastlingRights.WhiteKingside;
                    }
                    else if (move.FromSquare == BlackKingSquare)
                    {
                        CastleFlags &= ~CastlingRights.BlackQueenside;
                        CastleFlags &= ~CastlingRights.BlackKingside;
                    }
                    if ((move.Flags & Piece.Castle) != 0)
                    {
                        switch (move.ToSquare)
                        {
                            case 2: //white castling long/queenside
                                Rooks ^= 0x0000000000000009UL;
                                White ^= 0x0000000000000009UL;
                                break;
                            case 6: //white castling short/kingside
                                Rooks ^= 0x00000000000000A0UL;
                                White ^= 0x00000000000000A0UL;
                                break;
                            case 58: //black castling long/queenside
                                Rooks ^= 0x0900000000000000UL;
                                Black ^= 0x0900000000000000UL;
                                break;
                            case 62: //black castling short/kingside
                                Rooks ^= 0xA000000000000000UL;
                                Black ^= 0xA000000000000000UL;
                                break;
                        }
                    }
                    break;
            }

            //toggle active color!
            SideToMove = (Color)(-(int)SideToMove);
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
