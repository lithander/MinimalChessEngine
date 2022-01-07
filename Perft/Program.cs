using System;
using System.Diagnostics;
using System.IO;

namespace Perft
{
    class Program
    {
        const int MAX_PLY = 16;
        const int MAX_MOVES = MAX_PLY * 225; //https://www.stmintz.com/ccc/index.php?id=425058
        static BoardState[] Positions;
        static Move[] Moves;

        static Program()
        {
            Positions = new BoardState[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                Positions[i] = new BoardState();
            Moves = new Move[MAX_PLY * MAX_MOVES];
        }

        static void Main()
        {
            Console.WriteLine("Leorik Perft v21");
            Console.WriteLine();
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
                //Print(Positions[0]);
                PerftTable.Clear();

                long t0 = Stopwatch.GetTimestamp();
                long result = Perft2(depth);
                long t1 = Stopwatch.GetTimestamp();

                double dt = (t1 - t0) / (double)Stopwatch.Frequency;
                double ms = (1000 * dt);

                totalNodes += result;
                totalDuration += dt;

                if (result != refResult)
                    Console.WriteLine($"{line++} ERROR! perft({depth})={result}, expected {refResult} ({result - refResult:+#;-#})");
                else
                    Console.WriteLine($"{line++} OK! {(int)ms}ms, {(int)(result / ms)}K NPS");
            }
            file.Close();
            Console.WriteLine();
            Console.WriteLine($"Total: {totalNodes} Nodes, {(int)(1000 * totalDuration)}ms, {(int)(totalNodes / totalDuration / 1000)}M NPS");
        }

        private static void Print(BoardState board)
        {
            Console.WriteLine("   A B C D E F G H");
            Console.WriteLine(" .----------------.");
            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write($"{rank + 1}|"); //ranks aren't zero-indexed
                for (int file = 0; file < 8; file++)
                {
                    Piece piece = board.GetPiece(rank * 8 + file);
                    Console.Write(Notation.ToChar(piece));
                    Console.Write(' ');
                }
                Console.ResetColor();
                Console.WriteLine($"|{rank + 1}"); //ranks aren't zero-indexed
            }
            Console.WriteLine(" '----------------'");
        }

        private static void Benchmark()
        {
            const int M = 1000000;
            long t0 = Stopwatch.GetTimestamp();
            long result = BenchCopy(0, 6);
            long t1 = Stopwatch.GetTimestamp();
            double dt = (t1 - t0) / (double)Stopwatch.Frequency;
            Console.WriteLine($"Copying {result / M}M BoardStates at {(int)(result / M / dt)}M NPS");
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
                if (next.TryPlay(current, ref Moves[i]))
                {
                    if (remaining > 1)
                        sum += Perft(depth + 1, remaining - 1, moves);
                    else
                        sum++;
                }
            }

            return sum;
        }

        private static long Perft2(int depth)
        {
            ulong hash = Positions[0].ComputeZobristHash();
            return Perft2(0, depth, hash, new MoveGen(Moves, 0));
        }

        private static long Perft2(int depth, int remaining, ulong hash, MoveGen moves)
        {
            BoardState current = Positions[depth];
            BoardState next = Positions[depth + 1];

            //probe hash-tree
            if (PerftTable.Retrieve(hash, depth, out long childCount))
                return childCount;

            int i = moves.Next;
            moves.Collect(current);
            long sum = 0;
            for (; i < moves.Next; i++)
            {
                if (next.TryPlay(current, ref Moves[i]))
                {
                    if (remaining > 1)
                    {
                        ulong nextHash = BoardState.UpdateHash(hash, ref Moves[i], current, next);
                        sum += Perft2(depth + 1, remaining - 1, nextHash, moves);
                    }
                    else
                        sum++;
                }
            }

            PerftTable.Store(hash, depth, sum);
            return sum;
        }

        static class PerftTable
        {
            const int ENTRY_SIZE = 24; //BYTES
            const int HASH_MEMORY = 256; //Megabytes
            const int TT_SIZE = (HASH_MEMORY * 1024 * 1024) / ENTRY_SIZE;

            static PerftHashEntry[] _table = new PerftHashEntry[TT_SIZE];

            struct PerftHashEntry
            {
                public ulong ZobristHash;
                public long ChildCount;
                public int Depth;
            }

            public static void Store(ulong zobristHash, int depth, long childCount)
            {
                int slot = (int)(zobristHash % TT_SIZE);
                _table[slot].ZobristHash = zobristHash;
                _table[slot].ChildCount = childCount;
                _table[slot].Depth = depth;
            }

            public static bool Retrieve(ulong zobristHash, int depth, out long childCount)
            {
                int slot = (int)(zobristHash % TT_SIZE);
                if (_table[slot].Depth == depth && _table[slot].ZobristHash == zobristHash)
                {
                    childCount = _table[slot].ChildCount;
                    return true;
                }
                childCount = 0;
                return false;
            }

            public static void Clear()
            {
                Array.Clear(_table, 0, TT_SIZE);
            }
        }
    }
}
