using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Perft
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Leorik Perft v12");
            Benchmark();
            Benchmark();
            Benchmark();
            Console.WriteLine();
            var file = File.OpenText("qbb.txt");
            ComparePerft(file);
            Console.WriteLine();
            Console.WriteLine("Press any key to quit");//stop command prompt from closing automatically on windows
            Console.ReadKey();
        }

        static void ComparePerft(StreamReader file)
        {
            int line = 1;
            long totalNodes = 0;
            double totalDuration = 0;
            while (!file.EndOfStream)
            {
                //The parser expects a fen-string followed by a depth and a perft results at that depth
                //Example: 4k3 / 8 / 8 / 8 / 8 / 8 / 8 / 4K2R w K - 0 1; D1 15; D2 66; 6; 764643
                string[] data = file.ReadLine().Split(';');
                string fen = data[0];
                int depth = int.Parse(data[1]);
                long refResult = long.Parse(data[2]);
                Positions[0] = new BoardState(fen);

                long t0 = Stopwatch.GetTimestamp();
                long result = Perft(0, depth);
                long t1 = Stopwatch.GetTimestamp();

                double dt = (t1 - t0) / (double)Stopwatch.Frequency;
                double ms = (1000 * dt);

                totalNodes += result;
                totalDuration += dt;

                if (result != refResult)
                    Console.WriteLine($"{line++} ERROR! perft({depth})={result}, expected {refResult} ({result - refResult:+#;-#}) FEN: {fen}");
                else
                    Console.WriteLine($"OK! {(int)ms}ms, {(int)(result / ms)}K NPS");
            }
            Console.WriteLine();
            Console.WriteLine($"Total: {totalNodes} Nodes, {(int)(1000 * totalDuration)}ms, {(int)(totalNodes / totalDuration / 1000)}K NPS");
        }

        const int MAX_PLY = 32;
        const int MAX_MOVES = 225; //https://www.stmintz.com/ccc/index.php?id=425058
        static BoardState[] Positions;
        static Move[][] Moves;

        static Program()
        {
            Positions = new BoardState[MAX_PLY];
            Moves = new Move[MAX_PLY][];
            for (int i = 0; i < MAX_PLY; i++)
                Moves[i] = new Move[MAX_MOVES];
        }

        private static void Benchmark()
        {
            long t0 = Stopwatch.GetTimestamp();
            long result = BenchCopy(0, 6);
            long t1 = Stopwatch.GetTimestamp();
            double dt = (t1 - t0) / (double)Stopwatch.Frequency;
            double ms = (1000 * dt);
            Console.WriteLine($"BenchCopy took {(int)ms}ms, {(int)(result / ms)}K Ops");
        }
        private static long BenchCopy(int depth, int remaining)
        {
            long sum = 0;
            for (int i = 0; i < 20; i++)
            {
                //224M Ops
                //ref BoardState current = ref Positions[depth];
                //ref BoardState next = ref Positions[depth + 1];
                //next = current;

                //109M Ops
                //Positions[depth + 1] = Positions[depth];

                //185M Ops
                //Positions[depth + 1].Copy(ref Positions[depth]);

                //195M Ops //Fixed with field offset
                //Positions[depth + 1].Copy(ref Positions[depth]);

                //180M Ops //struct with explicit layout and a fixed unsafe ulong[10] array
                //Positions[depth + 1].CopySpan(ref Positions[depth]);

                //251M Ops //Inlined
                //Positions[depth + 1].Copy(ref Positions[depth]);
                //removing EnPassant 282M (+31M) Ops
                //adding 1 field 239M (-12M) Ops
                //adding 2 fields 230M (-21M) Ops

                //251M Ops
                ref BoardState next = ref Positions[depth + 1];
                next.Copy(ref Positions[depth]);

                if (remaining > 1)
                    sum += BenchCopy(depth + 1, remaining - 1);
                else
                    sum++;
            }
            return sum;
        }

        private static long Perft(int depth, int remaining)
        {
            long sum = 0;
            int numMoves = GenerateMoves(depth);
            for (int i = 0; i < numMoves; i++)
            {
                if (TryMake(depth, ref Moves[depth][i]))
                {
                    if (remaining > 1)
                        sum += Perft(depth + 1, remaining - 1);
                    else
                        sum++;
                }
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryMake(int depth, ref Move move)
        {
            ref BoardState current = ref Positions[depth];
            ref BoardState next = ref Positions[depth + 1];
            next.Copy(ref current);

            next.Play(ref move);
            bool legal = !next.IsChecked(current.SideToMove);
            return legal;
        }

        /***********************/
        /*** MOVE GENERATION ***/
        /***********************/

        static Move[] _moves;
        static int _nextMove;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GenerateMoves(int position)
        {
            _moves = Moves[position];
            _nextMove = 0;
            GenerateMoves(ref Positions[position]);
            return _nextMove;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateMoves(ref BoardState board)
        {
            ulong sideToMove = board.SideToMove == Color.Black ? board.Black : board.White;
            ulong occupied = board.Black | board.White;
            Piece color = (Piece)(board.SideToMove + 2);

            //Kings
            byte square = (byte)Bitboard.LSB(board.Kings & sideToMove);
            //can't move on squares occupied by side to move
            ulong targets = Bitboard.KingTargets[square] & ~sideToMove;
            for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                NewMove(Piece.King | color, square, targets);

            //Knights
            for (ulong knights = board.Knights & sideToMove; knights != 0; knights = Bitboard.ClearLSB(knights))
            {
                square = (byte)Bitboard.LSB(knights);
                //can't move on squares occupied by side to move
                targets = Bitboard.KnightTargets[square] & ~sideToMove;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    NewMove(Piece.Knight | color, square, targets);
            }

            //Bishops
            for (ulong bishops = board.Bishops & sideToMove; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
            {
                square = (byte)Bitboard.LSB(bishops);
                //can't move on squares occupied by side to move
                targets = Bitboard.GetBishopTargets(occupied, square) & ~sideToMove;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    NewMove(Piece.Bishop | color, square, targets);
            }

            //Rooks
            for (ulong rooks = board.Rooks & sideToMove; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
            {
                square = (byte)Bitboard.LSB(rooks);
                //can't move on squares occupied by side to move
                targets = Bitboard.GetRookTargets(occupied, square) & ~sideToMove;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    NewMove(Piece.Rook | color, square, targets);
            }

            //Queens
            for (ulong queens = board.Queens & sideToMove; queens != 0; queens = Bitboard.ClearLSB(queens))
            {
                square = (byte)Bitboard.LSB(queens);
                //can't move on squares occupied by side to move
                targets = (Bitboard.GetBishopTargets(occupied, square) | Bitboard.GetRookTargets(occupied, square)) & ~sideToMove;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    NewMove(Piece.Queen | color, square, targets);
            }

            //Pawns & Castling
            if (board.SideToMove == Color.White)
            {
                CollectWhitePawnMoves(ref board);
                CollectWhiteCastlingMoves(ref board);
            }
            else
            {
                CollectBlackPawnMoves(ref board);
                CollectBlackCastlingMoves(ref board);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CollectWhiteCastlingMoves(ref BoardState board)
        {
            if (board.CanWhiteCastleLong() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(3) /*&& !board.IsAttackedByBlack(2)*/)
                _moves[_nextMove++] = Move.WhiteCastlingLong;

            if (board.CanWhiteCastleShort() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(5) /*&& !board.IsAttackedByBlack(6)*/)
                _moves[_nextMove++] = Move.WhiteCastlingShort;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CollectBlackCastlingMoves(ref BoardState board)
        {
            if (board.CanBlackCastleLong() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(59) /*&& !board.IsAttackedByWhite(58)*/)
                _moves[_nextMove++] = Move.BlackCastlingLong;

            if (board.CanBlackCastleShort() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(61) /*&& !board.IsAttackedByWhite(62)*/)
                _moves[_nextMove++] = Move.BlackCastlingShort;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CollectBlackPawnMoves(ref BoardState board)
        {
            ulong targets;
            ulong occupied = board.Black | board.White;
            ulong blackPawns = board.Pawns & board.Black;
            ulong oneStep = (blackPawns >> 8) & ~occupied;
            //move one square down
            for (targets = oneStep & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.BlackPawn, targets, +8);

            //move to first rank and promote
            for (targets = oneStep & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                BlackPawnPromotions(targets, +8);

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
                BlackPawnPromotions(targets, +9);

            //capture right
            ulong captureRight = ((blackPawns & 0x7F7F7F7F7F7F7F7FUL) >> 7) & board.White;
            for (targets = captureRight & 0xFFFFFFFFFFFFFF00UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.BlackPawn, targets, +7);

            //capture right to first rank and promote
            for (targets = captureRight & 0x00000000000000FFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                BlackPawnPromotions(targets, +7);

            //is en-passent possible?
            captureLeft = ((blackPawns & 0x00000000FE000000UL) >> 9) & board.EnPassant;
            if (captureLeft != 0)
                PawnMove(Piece.BlackPawn | Piece.EnPassant, captureLeft, +9);

            captureRight = ((blackPawns & 0x000000007F000000UL) >> 7) & board.EnPassant;
            if (captureRight != 0)
                PawnMove(Piece.BlackPawn | Piece.EnPassant, captureRight, +7);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CollectWhitePawnMoves(ref BoardState board)
        {
            ulong targets;
            ulong whitePawns = board.Pawns & board.White;
            ulong occupied = board.Black | board.White;
            ulong oneStep = (whitePawns << 8) & ~occupied;
            //move one square up
            for (targets = oneStep & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.WhitePawn, targets, -8);

            //move to last rank and promote
            for (targets = oneStep & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                WhitePawnPromotions(targets, -8);

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
                WhitePawnPromotions(targets, -7);

            //capture right
            ulong captureRight = ((whitePawns & 0x7F7F7F7F7F7F7F7FUL) << 9) & board.Black;
            for (targets = captureRight & 0x00FFFFFFFFFFFFFFUL; targets != 0; targets = Bitboard.ClearLSB(targets))
                PawnMove(Piece.WhitePawn, targets, -9);

            //capture right to last rank and promote
            for (targets = captureRight & 0xFF00000000000000UL; targets != 0; targets = Bitboard.ClearLSB(targets))
                WhitePawnPromotions(targets, -9);

            //is en-passent possible?
            captureLeft = ((whitePawns & 0x000000FE00000000UL) << 7) & board.EnPassant;
            if (captureLeft != 0)
                PawnMove(Piece.WhitePawn | Piece.EnPassant, captureLeft, -7);

            captureRight = ((whitePawns & 0x000007F00000000UL) << 9) & board.EnPassant;
            if (captureRight != 0)
                PawnMove(Piece.WhitePawn | Piece.EnPassant, captureRight, -9);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void NewMove(Piece attacker, byte from, ulong moveTargets)
        {
            byte to = (byte)Bitboard.LSB(moveTargets);
            _moves[_nextMove++] = new Move(attacker, from, to); //TODO: don't forget that this was a bishop! Assign the flags here were they are readily available!
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PawnMove(Piece flags, ulong moveTargets, int offset)
        {
            byte to = (byte)Bitboard.LSB(moveTargets);
            byte from = (byte)(to + offset);
            _moves[_nextMove++] = new Move(flags, from, to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WhitePawnPromotions(ulong moveTargets, int offset)
        {
            byte to = (byte)Bitboard.LSB(moveTargets);
            byte from = (byte)(to + offset);
            _moves[_nextMove++] = new Move(Piece.WhitePawn | Piece.QueenPromotion, from, to);
            _moves[_nextMove++] = new Move(Piece.WhitePawn | Piece.RookPromotion, from, to);
            _moves[_nextMove++] = new Move(Piece.WhitePawn | Piece.BishopPromotion, from, to);
            _moves[_nextMove++] = new Move(Piece.WhitePawn | Piece.KnightPromotion, from, to);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlackPawnPromotions(ulong moveTargets, int offset)
        {
            byte to = (byte)Bitboard.LSB(moveTargets);
            byte from = (byte)(to + offset);
            _moves[_nextMove++] = new Move(Piece.BlackPawn | Piece.QueenPromotion, from, to);
            _moves[_nextMove++] = new Move(Piece.BlackPawn | Piece.RookPromotion, from, to);
            _moves[_nextMove++] = new Move(Piece.BlackPawn | Piece.BishopPromotion, from, to);
            _moves[_nextMove++] = new Move(Piece.BlackPawn | Piece.KnightPromotion, from, to);
        }
    }
}
