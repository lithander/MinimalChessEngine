using System.Runtime.CompilerServices;

namespace Leorik
{
    public struct MoveGen2
    {
        private readonly Move[] _moves;
        public int Next;

        public MoveGen2(Move[] moves, int nextIndex)
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
            _moves[Next++] = new Move(flags, from, to, Piece.None);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddAll(Piece piece, int square, ulong targets)
        {
            for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                Add(piece, square, Bitboard.LSB(targets));
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddAllCaptures(Piece piece, int square, ulong targets, BoardState board)
        {
            for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                AddCapture(piece, square, Bitboard.LSB(targets), board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCapture(Piece flags, int from, int to, BoardState board)
        {
            _moves[Next++] = new Move(flags, from, to, board.GetPiece(to));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PawnCapture(Piece flags, ulong moveTargets, int offset, BoardState board)
        {
            int to = Bitboard.LSB(moveTargets);
            int from = to + offset;
            AddCapture(flags, from, to, board);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PawnCapturePromotions(Piece flags, ulong moveTargets, int offset, BoardState board)
        {
            int to = Bitboard.LSB(moveTargets);
            int from = to + offset;
            Piece target = board.GetPiece(to);
            _moves[Next++] = new Move(flags | Piece.QueenPromotion, from, to, target);
            _moves[Next++] = new Move(flags | Piece.RookPromotion, from, to, target);
            _moves[Next++] = new Move(flags | Piece.BishopPromotion, from, to, target);
            _moves[Next++] = new Move(flags | Piece.KnightPromotion, from, to, target);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Collect(BoardState board)
        {
            if (board.SideToMove == Color.White)
            {
                CollectWhiteCaptures(board);
                CollectWhiteQuiets(board);
            }
            else
            {
                CollectBlackCaptures(board);
                CollectBlackQuiets(board);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectBlackCaptures(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.Black);
            //can't move on squares occupied by side to move
            AddAllCaptures(Piece.BlackKing, square, Bitboard.KingTargets[square] & board.White, board);

            //Knights
            for (ulong knights = board.Knights & board.Black; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                AddAllCaptures(Piece.BlackKnight, square, Bitboard.KnightTargets[square] & board.White, board);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                AddAllCaptures(Piece.BlackBishop, square, Bitboard.GetBishopTargets(occupied, square) & board.White, board);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                AddAllCaptures(Piece.BlackRook, square, Bitboard.GetRookTargets(occupied, square) & board.White, board);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                AddAllCaptures(Piece.BlackQueen, square, Bitboard.GetQueenTargets(occupied, square) & board.White, board);
            }

            //Pawns & Castling
            ulong targets;
            ulong blackPawns = board.Pawns & board.Black;

            //capture left
            ulong captureLeft = ((blackPawns & 0xFEFEFEFEFEFEFEFEUL) >> 9) & board.White;
            for (targets = captureLeft & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapture(Piece.BlackPawn, targets, +9, board);

            //capture left to first rank and promote
            for (targets = captureLeft & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapturePromotions(Piece.BlackPawn, targets, +9, board);

            //capture right
            ulong captureRight = ((blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7) & board.White;
            for (targets = captureRight & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapture(Piece.BlackPawn, targets, +7, board);

            //capture right to first rank and promote
            for (targets = captureRight & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapturePromotions(Piece.BlackPawn, targets, +7, board);

            //is en-passent possible?
            captureLeft = ((blackPawns & 0x00000000FE000000UL) >> 9) & board.EnPassant;
            if (captureLeft != 0)
                PawnMove(Piece.BlackPawn | Piece.EnPassant, captureLeft, +9);

            captureRight = ((blackPawns & 0x000000007F000000UL) >> 7) & board.EnPassant;
            if (captureRight != 0)
                PawnMove(Piece.BlackPawn | Piece.EnPassant, captureRight, +7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectBlackQuiets(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.Black);
            //can't move on squares occupied by side to move
            AddAll(Piece.BlackKing, square, Bitboard.KingTargets[square] & ~occupied);

            //Knights
            for (ulong knights = board.Knights & board.Black; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                AddAll(Piece.BlackKnight, square, Bitboard.KnightTargets[square] & ~occupied);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.Black; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                AddAll(Piece.BlackBishop, square, Bitboard.GetBishopTargets(occupied, square) & ~occupied);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.Black; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                AddAll(Piece.BlackRook, square, Bitboard.GetRookTargets(occupied, square) & ~occupied);
            }

            //Queens
            for (ulong queens = board.Queens & board.Black; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                AddAll(Piece.BlackQueen, square, Bitboard.GetQueenTargets(occupied, square) & ~occupied);
            }

            //Pawns & Castling
            ulong targets;
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

            //Castling
            if (board.CanBlackCastleLong() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(59) /*&& !board.IsAttackedByWhite(58)*/)
                Add(Move.BlackCastlingLong);

            if (board.CanBlackCastleShort() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(61) /*&& !board.IsAttackedByWhite(62)*/)
                Add(Move.BlackCastlingShort);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectWhiteCaptures(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.White);
            //can't move on squares occupied by side to move
            AddAllCaptures(Piece.WhiteKing, square, Bitboard.KingTargets[square] & board.Black, board);

            //Knights
            for (ulong knights = board.Knights & board.White; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                AddAllCaptures(Piece.WhiteKnight, square, Bitboard.KnightTargets[square] & board.Black, board);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                AddAllCaptures(Piece.WhiteBishop, square, Bitboard.GetBishopTargets(occupied, square) & board.Black, board);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                AddAllCaptures(Piece.WhiteRook, square, Bitboard.GetRookTargets(occupied, square) & board.Black, board);
            }

            //Queens
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                AddAllCaptures(Piece.WhiteQueen, square, Bitboard.GetQueenTargets(occupied, square) & board.Black, board);
            }

            //Pawns                
            ulong targets;
            ulong whitePawns = board.Pawns & board.White;

            //capture left
            ulong captureLeft = ((whitePawns & 0xFEFEFEFEFEFEFEFEUL) << 7) & board.Black;
            for (targets = captureLeft & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapture(Piece.WhitePawn, targets, -7, board);

            //capture left to last rank and promote
            for (targets = captureLeft & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapturePromotions(Piece.WhitePawn, targets, -7, board);

            //capture right
            ulong captureRight = ((whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9) & board.Black;
            for (targets = captureRight & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapture(Piece.WhitePawn, targets, -9, board);

            //capture right to last rank and promote
            for (targets = captureRight & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnCapturePromotions(Piece.WhitePawn, targets, -9, board);

            //is en-passent possible?
            captureLeft = ((whitePawns & 0x000000FE00000000UL) << 7) & board.EnPassant;
            if (captureLeft != 0)
                PawnMove(Piece.WhitePawn | Piece.EnPassant, captureLeft, -7);

            captureRight = ((whitePawns & 0x000007F00000000UL) << 9) & board.EnPassant;
            if (captureRight != 0)
                PawnMove(Piece.WhitePawn | Piece.EnPassant, captureRight, -9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CollectWhiteQuiets(BoardState board)
        {
            ulong occupied = board.Black | board.White;

            //Kings
            int square = Bitboard.LSB(board.Kings & board.White);
            //can't move on squares occupied by side to move
            AddAll(Piece.WhiteKing, square, Bitboard.KingTargets[square] & ~occupied);

            //Knights
            for (ulong knights = board.Knights & board.White; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = Bitboard.LSB(knights);
                AddAll(Piece.WhiteKnight, square, Bitboard.KnightTargets[square] & ~occupied);
            }

            //Bishops
            for (ulong bishops = board.Bishops & board.White; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = Bitboard.LSB(bishops);
                AddAll(Piece.WhiteBishop, square, Bitboard.GetBishopTargets(occupied, square) & ~occupied);
            }

            //Rooks
            for (ulong rooks = board.Rooks & board.White; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = Bitboard.LSB(rooks);
                AddAll(Piece.WhiteRook, square, Bitboard.GetRookTargets(occupied, square) & ~occupied);
            }

            //Queens
            for (ulong queens = board.Queens & board.White; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = Bitboard.LSB(queens);
                AddAll(Piece.WhiteQueen, square, Bitboard.GetQueenTargets(occupied, square) & ~occupied);
            }

            //Pawns                
            ulong targets;
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

            //Castling
            if (board.CanWhiteCastleLong() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(3) /*&& !board.IsAttackedByBlack(2)*/)
                Add(Move.WhiteCastlingLong);

            if (board.CanWhiteCastleShort() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(5) /*&& !board.IsAttackedByBlack(6)*/)
                Add(Move.WhiteCastlingShort);
        }
    }
}
