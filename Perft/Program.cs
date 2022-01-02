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
            Console.WriteLine("Leorik Perft v16");
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
                Positions[0].Copy(Notation.ToBoardState(fen));

                long t0 = Stopwatch.GetTimestamp();
                long result = Perft(depth);
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
        static Move[] Moves;

        static Program()
        {
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Moves = new Move[MAX_PLY * MAX_MOVES];
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
                BoardState next = Positions[depth + 1];
                next.Copy(Positions[depth]);

                if (remaining > 1)
                    sum += BenchCopy(depth + 1, remaining - 1);
                else
                    sum++;
            }
            return sum;
        }

        private static long Perft(int depth)
        {
            return Perft(0, depth, new MoveGen(Moves, 0));
        }

        private static long Perft(int depth, int remaining, MoveGen moves)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];
            int i = moves.Next;
            moves.Collect(current);
            long sum = 0;
            for (; i < moves.Next; i++)
            {
                next.Play(current, ref Moves[i]);
                if (next.IsChecked(current.SideToMove))
                    continue;

                if (remaining > 1)
                    sum += Perft(depth + 1, remaining - 1, moves);
                else
                    sum++;
            }
            return sum;
        }

        /***********************/
        /*** MOVE GENERATION ***/
        /***********************/

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
                ulong sideToMove = board.SideToMove == Color.Black ? board.Black : board.White;
                ulong occupied = board.Black | board.White;
                Piece color = (Piece)(board.SideToMove + 2);

                //Kings
                int square = Bitboard.LSB(board.Kings & sideToMove);
                //can't move on squares occupied by side to move
                ulong targets = Bitboard.KingTargets[square] & ~sideToMove;
                for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                    Add(Piece.King | color, square, Bitboard.LSB(targets));

                //Knights
                for (ulong knights = board.Knights & sideToMove; knights != 0; knights = Bitboard.ClearLSB(knights))
                {
                    square = Bitboard.LSB(knights);
                    targets = Bitboard.KnightTargets[square] & ~sideToMove;
                    for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                        Add(Piece.Knight | color, square, Bitboard.LSB(targets));
                }

                //Bishops
                for (ulong bishops = board.Bishops & sideToMove; bishops != 0; bishops = Bitboard.ClearLSB(bishops))
                {
                    square = Bitboard.LSB(bishops);
                    targets = Bitboard.GetDiagonalTargets(occupied, square) & ~sideToMove;
                    for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                        Add(Piece.Bishop | color, square, Bitboard.LSB(targets));
                }

                //Rooks
                for (ulong rooks = board.Rooks & sideToMove; rooks != 0; rooks = Bitboard.ClearLSB(rooks))
                {
                    square = Bitboard.LSB(rooks);
                    targets = Bitboard.GetOrthogonalTargets(occupied, square) & ~sideToMove;
                    for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                        Add(Piece.Rook | color, square, Bitboard.LSB(targets));
                }

                //Queens
                for (ulong queens = board.Queens & sideToMove; queens != 0; queens = Bitboard.ClearLSB(queens))
                {
                    square = Bitboard.LSB(queens);
                    targets = Bitboard.GetQueenTargets(occupied, square) & ~sideToMove;
                    for (; targets != 0; targets = Bitboard.ClearLSB(targets))
                        Add(Piece.Queen | color, square, Bitboard.LSB(targets));
                }

                //Pawns & Castling
                if (board.SideToMove == Color.White)
                {
                    CollectWhitePawnMoves(board);
                    CollectWhiteCastlingMoves(board);
                }
                else
                {
                    CollectBlackPawnMoves(board);
                    CollectBlackCastlingMoves(board);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CollectWhiteCastlingMoves(BoardState board)
            {
                if (board.CanWhiteCastleLong() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(3) /*&& !board.IsAttackedByBlack(2)*/)
                    Add(Move.WhiteCastlingLong);

                if (board.CanWhiteCastleShort() && !board.IsAttackedByBlack(4) && !board.IsAttackedByBlack(5) /*&& !board.IsAttackedByBlack(6)*/)
                    Add(Move.WhiteCastlingShort);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CollectBlackCastlingMoves(BoardState board)
            {
                if (board.CanBlackCastleLong() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(59) /*&& !board.IsAttackedByWhite(58)*/)
                    Add(Move.BlackCastlingLong);

                if (board.CanBlackCastleShort() && !board.IsAttackedByWhite(60) && !board.IsAttackedByWhite(61) /*&& !board.IsAttackedByWhite(62)*/)
                    Add(Move.BlackCastlingShort);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CollectBlackPawnMoves(BoardState board)
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
            private void CollectWhitePawnMoves(BoardState board)
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
            private void PawnMove(Piece flags, ulong moveTargets, int offset)
            {
                int to = Bitboard.LSB(moveTargets);
                int from = to + offset;
                Add(flags, from, to);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WhitePawnPromotions(ulong moveTargets, int offset)
            {
                int to = Bitboard.LSB(moveTargets);
                int from = to + offset;
                Add(Piece.WhitePawn | Piece.QueenPromotion, from, to);
                Add(Piece.WhitePawn | Piece.RookPromotion, from, to);
                Add(Piece.WhitePawn | Piece.BishopPromotion, from, to);
                Add(Piece.WhitePawn | Piece.KnightPromotion, from, to);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void BlackPawnPromotions(ulong moveTargets, int offset)
            {
                int to = Bitboard.LSB(moveTargets);
                int from = to + offset;
                Add(Piece.BlackPawn | Piece.QueenPromotion, from, to);
                Add(Piece.BlackPawn | Piece.RookPromotion, from, to);
                Add(Piece.BlackPawn | Piece.BishopPromotion, from, to);
                Add(Piece.BlackPawn | Piece.KnightPromotion, from, to);
            }
        }
    }
}
