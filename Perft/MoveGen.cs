using System.Runtime.CompilerServices;

namespace Perft
{
    public struct MoveGen
    {
        private readonly Move[] _moves;
        public int Next;

        public MoveGen(Move[] moves, int nextIndex)
        {
            _moves = moves;
            Next = nextIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Move move)
        {
            _moves[Next++] = move;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Piece flags, int from, int to)
        {
            _moves[Next++] = new Move(flags, from, to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Collect(BoardState board)
        {
            if (board.SideToMove == Color.White)
                CollectWhite(board);
            else
                CollectBlack(board);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectBlack(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.Black);
            //can't move on squares occupied by side to move
            ulong targets = Bitboard.KingTargets[square] & ~board.Black;
            for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                Add(Piece.BlackKing, square, Bitboard.LSB(targets));

            //Knights
            for (ulong knights = board.Knights & board.Black; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                targets = Bitboard.KnightTargets[square] & ~board.Black;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.BlackKnight, square, Bitboard.LSB(targets));
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                targets = Bitboard.GetDiagonalTargets(occupied, square) & ~board.Black;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.BlackBishop, square, Bitboard.LSB(targets));
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                targets = Bitboard.GetOrthogonalTargets(occupied, square) & ~board.Black;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.BlackRook, square, Bitboard.LSB(targets));
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                targets = Bitboard.GetQueenTargets(occupied, square) & ~board.Black;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.BlackQueen, square, Bitboard.LSB(targets));
            }

            //Pawns & Castling
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //move one square down
            for (targets = oneStep & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.BlackPawn, targets, +8);

            //move to first rank and promote
            for (targets = oneStep & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnPromotions(Piece.BlackPawn, targets, +8);

            //move two squares down
            ulong twoStep = (oneStep >> 8) & ~occupied;
            for (targets = twoStep & 0x000000FF00000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.BlackPawn, targets, +16);

            //capture left
            ulong captureLeft = ((blackPawns & 0xFEFEFEFEFEFEFEFEUL) >> 9) & board.White;
            for (targets = captureLeft & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.BlackPawn, targets, +9);

            //capture left to first rank and promote
            for (targets = captureLeft & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnPromotions(Piece.BlackPawn, targets, +9);

            //capture right
            ulong captureRight = ((blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7) & board.White;
            for (targets = captureRight & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.BlackPawn, targets, +7);

            //capture right to first rank and promote
            for (targets = captureRight & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnPromotions(Piece.BlackPawn, targets, +7);

            //is en-passent possible?
            captureLeft = ((blackPawns & 0x00000000FE000000UL) >> 9) & board.EnPassant;
            if (captureLeft != 0)
                PawnMove(Piece.BlackPawn | Piece.EnPassant, captureLeft, +9);

            captureRight = ((blackPawns & 0x000000007F000000UL) >> 7) & board.EnPassant;
            if (captureRight != 0)
                PawnMove(Piece.BlackPawn | Piece.EnPassant, captureRight, +7);

            //Castling
            if (board.CanBlackCastleLong() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(59) /*&& !board.IsAttackedByWhite(58)*/)
                Add(Move.BlackCastlingLong);

            if (board.CanBlackCastleShort() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(61) /*&& !board.IsAttackedByWhite(62)*/)
                Add(Move.BlackCastlingShort);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectWhite(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.White);
            //can't move on squares occupied by side to move
            ulong targets = Bitboard.KingTargets[square] & ~board.White;
            for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                Add(Piece.WhiteKing, square, Bitboard.LSB(targets));

            //Knights
            for (ulong knights = board.Knights & board.White; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                targets = Bitboard.KnightTargets[square] & ~board.White;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.WhiteKnight, square, Bitboard.LSB(targets));
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                targets = Bitboard.GetDiagonalTargets(occupied, square) & ~board.White;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.WhiteBishop, square, Bitboard.LSB(targets));
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                targets = Bitboard.GetOrthogonalTargets(occupied, square) & ~board.White;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.WhiteRook, square, Bitboard.LSB(targets));
            }

            //Queens
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                targets = Bitboard.GetQueenTargets(occupied, square) & ~board.White;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.WhiteQueen, square, Bitboard.LSB(targets));
            }

            //Pawns                
            ulong whitePawns = board.Pawns & board.White;
            ulong oneStep = (whitePawns << 8) & ~occupied;
            //move one square up
            for (targets = oneStep & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.WhitePawn, targets, -8);

            //move to last rank and promote
            for (targets = oneStep & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnPromotions(Piece.WhitePawn, targets, -8);

            //move two squares up
            ulong twoStep = (oneStep << 8) & ~occupied;
            for (targets = twoStep & 0x00000000FF000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.WhitePawn, targets, -16);

            //capture left
            ulong captureLeft = ((whitePawns & 0xFEFEFEFEFEFEFEFEUL) << 7) & board.Black;
            for (targets = captureLeft & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.WhitePawn, targets, -7);

            //capture left to last rank and promote
            for (targets = captureLeft & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnPromotions(Piece.WhitePawn, targets, -7);

            //capture right
            ulong captureRight = ((whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9) & board.Black;
            for (targets = captureRight & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.WhitePawn, targets, -9);

            //capture right to last rank and promote
            for (targets = captureRight & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnPromotions(Piece.WhitePawn, targets, -9);

            //is en-passent possible?
            captureLeft = ((whitePawns & 0x000000FE00000000UL) << 7) & board.EnPassant;
            if (captureLeft != 0)
                PawnMove(Piece.WhitePawn | Piece.EnPassant, captureLeft, -7);

            captureRight = ((whitePawns & 0x000007F00000000UL) << 9) & board.EnPassant;
            if (captureRight != 0)
                PawnMove(Piece.WhitePawn | Piece.EnPassant, captureRight, -9);

            //Castling
            if (board.CanWhiteCastleLong() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(3) /*&& !board.IsAttackedByBlack(2)*/)
                Add(Move.WhiteCastlingLong);

            if (board.CanWhiteCastleShort() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(5) /*&& !board.IsAttackedByBlack(6)*/)
                Add(Move.WhiteCastlingShort);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PawnMove(Piece flags, ulong moveTargets, int offset)
        {
            int to = Bitboard.LSB(moveTargets);
            int from = to + offset;
            Add(flags, from, to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PawnPromotions(Piece flags, ulong moveTargets, int offset)
        {
            int to = Bitboard.LSB(moveTargets);
            int from = to + offset;
            Add(flags | Piece.QueenPromotion, from, to);
            Add(flags | Piece.RookPromotion, from, to);
            Add(flags | Piece.BishopPromotion, from, to);
            Add(flags | Piece.KnightPromotion, from, to);
        }
    }
}
