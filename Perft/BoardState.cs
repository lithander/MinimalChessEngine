using System;
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
        public Piece CompleteFlags(Move move)
        {
            Piece flags = move.Flags;

            ulong bbTarget = 1UL << move.ToSquare;
            if (((Black | White) & bbTarget) > 0)
                flags |= Piece.Capture;

            ulong bbPiece = 1UL << move.FromSquare;
            if ((bbPiece & Black) > 0)
                flags |= Piece.Black;
            else if ((bbPiece & White) > 0)
                flags |= Piece.White;

            if ((bbPiece & Pawns) > 0)
                flags |= Piece.Pawn;
            else if ((bbPiece & Knights) > 0)
                flags |= Piece.Knight;
            else if ((bbPiece & Bishops) > 0)
                flags |= Piece.Bishop;
            else if ((bbPiece & Rooks) > 0)
                flags |= Piece.Rook;
            else if ((bbPiece & Queens) > 0)
                flags |= Piece.Queen;
            else if ((bbPiece & Kings) > 0)
                flags |= Piece.King;

            return flags;
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
            Piece flags = CompleteFlags(move);

            ClearBit(move.FromSquare, flags);

            if ((flags & Piece.Capture) != 0)
                ClearBits(move.ToSquare);

            if ((flags & Piece.Castle) != 0)
            {
                SetBit(move.ToSquare, flags);
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
            else if ((flags & Piece.Promotion) != 0)
            {
                SetBit(move.ToSquare, move.Promotion);
            }
            else if ((flags & Piece.EnPassant) != 0)
            {
                SetBit(move.ToSquare, flags);
                //Delete the captured pawn
                if ((flags & Piece.ColorMask) == Piece.White)
                    ClearBit(move.ToSquare - 8, Piece.BlackPawn);
                else
                    ClearBit(move.ToSquare + 8, Piece.WhitePawn);
            }
            else
            {
                SetBit(move.ToSquare, flags);
            }

            //update enPassant
            if (flags == Piece.BlackPawn && move.ToSquare == move.FromSquare - 16)
                EnPassantSquare = move.FromSquare - 8;
            else if (flags == Piece.WhitePawn && move.ToSquare == move.FromSquare + 16)
                EnPassantSquare = move.FromSquare + 8;
            else
                EnPassantSquare = -1;

            //update board state
            UpdateCastlingRights(move.FromSquare);
            UpdateCastlingRights(move.ToSquare);

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
            ulong occupied = Black | White;

            ulong pieces = White & Knights;
            if (pieces > 0 && (pieces & Bitboard.KnightTargets[square]) > 0)
                return true;

            pieces = White & Kings;
            if (pieces > 0 && (pieces & Bitboard.KingTargets[square]) > 0)
                return true;

            pieces = White & (Queens | Bishops);
            if (pieces > 0 && (pieces & Bitboard.GetBishopTargets(occupied, square)) > 0)
                return true;

            pieces = White & (Queens | Rooks);
            if (pieces > 0 && (pieces & Bitboard.GetRookTargets(occupied, square)) > 0)
                return true;

            pieces = White & Pawns;
            ulong left = (pieces & 0xFEFEFEFEFEFEFEFEUL) << 7;
            if ((left & 1UL << square) > 0)
                return true;

            ulong right = (pieces & 0x7F7F7F7F7F7F7F7FUL) << 9;
            if ((right & 1UL << square) > 0)
                return true;

            //Warning: does not consider en-passent!
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAttackedByBlack(int square)
        {
            ulong occupied = Black | White;

            ulong pieces = Black & Knights;
            if (pieces > 0 && (pieces & Bitboard.KnightTargets[square]) > 0)
                return true;

            pieces = Black & Kings;
            if (pieces > 0 && (pieces & Bitboard.KingTargets[square]) > 0)
                return true;

            pieces = Black & (Queens | Bishops);
            if (pieces > 0 && (pieces & Bitboard.GetBishopTargets(occupied, square)) > 0)
                return true;

            pieces = Black & (Queens | Rooks);
            if (pieces > 0 && (pieces & Bitboard.GetRookTargets(occupied, square)) > 0)
                return true;

            pieces = Black & Pawns;
            ulong left = (pieces & 0xFEFEFEFEFEFEFEFEUL) >> 9;
            if ((left & 1UL << square) > 0)
                return true;

            ulong right = (pieces & 0x7F7F7F7F7F7F7F7FUL) >> 7;
            if ((right & 1UL << square) > 0)
                return true;

            //Warning: does not consider en-passent!
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateCastlingRights(int square)
        {
            //TODO: early out against bitmask. if it's none of the squares don't bother

            //any move from or to king or rook squares will effect castling right
            if (square == WhiteKingSquare || square == WhiteQueensideRookSquare)
                CastleFlags &= ~CastlingRights.WhiteQueenside;
            if (square == WhiteKingSquare || square == WhiteKingsideRookSquare)
                CastleFlags &= ~CastlingRights.WhiteKingside;

            if (square == BlackKingSquare || square == BlackQueensideRookSquare)
                CastleFlags &= ~CastlingRights.BlackQueenside;
            if (square == BlackKingSquare || square == BlackKingsideRookSquare)
                CastleFlags &= ~CastlingRights.BlackKingside;
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
