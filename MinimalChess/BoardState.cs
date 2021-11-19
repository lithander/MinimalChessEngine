using System;

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

        public static BoardState CopyFrom(Board board)
        {
            BoardState result = new BoardState();
            for (int i = 0; i < 64; i++)
                result.SetBit(i, board[i]);
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
                   Kings == other.Kings;
        }

        public static bool operator ==(BoardState a, BoardState b) => a.Equals(b);
        public static bool operator !=(BoardState a, BoardState b) => !a.Equals(b);
        public override bool Equals(object obj) => obj is BoardState bs && Equals(bs);

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
        }
    }
}
