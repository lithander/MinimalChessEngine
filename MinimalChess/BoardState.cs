using System;
using System.Diagnostics;

namespace MinimalChess
{
    public struct BoardState : IEquatable<BoardState>
    {
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

        public static BoardState CopyFrom(Board board)
        {
            BoardState result = new BoardState();
            for (int i = 0; i < 64; i++)
                result.SetBit(i, board[i]);

            result.CastleFlags = board.CastlingRights;
            result.SideToMove = board.SideToMove;
            result.EnPassantSquare = board.EnPassentSquare;
            return result;
        }

        private void SetBit(int square, Piece piece)
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

        public bool Equals(BoardState other)
        {
            return White == other.White &&
                   Black == other.Black &&
                   Pawns == other.Pawns &&
                   Knights == other.Knights &&
                   Bishops == other.Bishops &&
                   Rooks == other.Rooks &&
                   Queens == other.Queens &&
                   Kings == other.Kings &&
                   CastleFlags == other.CastleFlags &&
                   SideToMove == other.SideToMove &&
                   EnPassantSquare == other.EnPassantSquare;
        }

        public void AssertEquality(BoardState other)
        {
            Trace.Assert(White == other.White, "BB White not equal");
            Trace.Assert(Black == other.Black, "BB Black not equal");
            Trace.Assert(Pawns == other.Pawns, "BB Pawns not equal");
            Trace.Assert(Knights == other.Knights, "BB Knights not equal");
            Trace.Assert(Bishops == other.Bishops, "BB Bishops not equal");
            Trace.Assert(Rooks == other.Rooks, "BB Rooks not equal");
            Trace.Assert(Queens == other.Queens, "BB Queens not equal");
            Trace.Assert(Kings == other.Kings, "BB Kings not equal");
            Trace.Assert(CastleFlags == other.CastleFlags, "CastleFlags not equal");
            Trace.Assert(SideToMove == other.SideToMove, "SideToMove not equal");
            Trace.Assert(EnPassantSquare == other.EnPassantSquare, "EnPassantSquare not equal");
        }

        public static bool operator ==(BoardState a, BoardState b) => a.Equals(b);
        public static bool operator !=(BoardState a, BoardState b) => !a.Equals(b);
        public override bool Equals(object obj) => obj is BoardState bs && Equals(bs);

        //TODO: use zobrist
        public override int GetHashCode()
        {
            return HashCode.Combine(White, Black, Pawns, Knights, Bishops, Rooks, Queens, Kings);
        }

        public void Play(Move move)
        {
            ClearBit(move.FromSquare, move.Flags);

            if ((move.Flags & Piece.Capture) != 0)
                ClearBits(move.ToSquare);

            if ((move.Flags & Piece.Castle) != 0)
            {
                SetBit(move.ToSquare, move.Flags);
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
            else if ((move.Flags & Piece.Promotion) != 0)
            {
                SetBit(move.ToSquare, move.Promotion);
            }
            else if ((move.Flags & Piece.EnPassant) != 0)
            {
                SetBit(move.ToSquare, move.Flags);
                //Delete the captured pawn
                if(move.Flags.Color() == Color.White)
                    ClearBit(move.ToSquare - 8, Piece.BlackPawn);
                else
                    ClearBit(move.ToSquare + 8, Piece.WhitePawn);
            }
            else
            {
                SetBit(move.ToSquare, move.Flags);
            }

            //update board state
            UpdateEnPassent(move);
            UpdateCastlingRights(move.FromSquare);
            UpdateCastlingRights(move.ToSquare);

            //toggle active color!
            SideToMove = Pieces.Flip(SideToMove);
        }

        //TODO: use consts
        static readonly int BlackKingSquare = Notation.ToSquare("e8");
        static readonly int WhiteKingSquare = Notation.ToSquare("e1");
        static readonly int BlackQueensideRookSquare = Notation.ToSquare("a8");
        static readonly int BlackKingsideRookSquare = Notation.ToSquare("h8");
        static readonly int WhiteQueensideRookSquare = Notation.ToSquare("a1");
        static readonly int WhiteKingsideRookSquare = Notation.ToSquare("h1");

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

        private void UpdateEnPassent(Move move)
        {
            //TODO: early out against pawn. if it's none of the squares don't bother 

            //movingPiece needs to be either a BlackPawn...
            if (move.Flags == Piece.BlackPawn && move.ToSquare == move.FromSquare - 16)
                EnPassantSquare = move.FromSquare - 8;
            else if (move.Flags == Piece.WhitePawn && move.ToSquare == move.FromSquare + 16)
                EnPassantSquare = move.FromSquare + 8;
            else
                EnPassantSquare = -1;
        }
    }
}
