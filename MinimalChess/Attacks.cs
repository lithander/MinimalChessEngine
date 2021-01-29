using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MinimalChess
{
    public static class Attacks
    {
        public static byte[,][] Diagonal = new byte[64, 4][];
        public static byte[,][] Straight = new byte[64, 4][];
        public static byte[][] King = new byte[64][];
        public static byte[][] Knight = new byte[64][];
        public static byte[][] BlackPawn = new byte[64][];
        public static byte[][] WhitePawn = new byte[64][];

        static readonly int[] DIAGONALS_FILE = new int[4] { -1, 1, 1, -1 };
        static readonly int[] DIAGONALS_RANK = new int[4] { -1, -1, 1, 1 };

        static readonly int[] STRAIGHTS_FILE = new int[4] { -1, 0, 1, 0 };
        static readonly int[] STRAIGHTS_RANK = new int[4] { -0, -1, 0, 1 };

        static readonly int[] KING_FILE = new int[8] { -1, 0, 1, 1, 1, 0, -1, -1 };
        static readonly int[] KING_RANK = new int[8] { -1, -1, -1, 0, 1, 1, 1, 0 };

        static readonly int[] KNIGHT_FILE = new int[8] { -1, -2, 1, 2, -1, -2, 1, 2 };
        static readonly int[] KNIGHT_RANK = new int[8] { -2, -1, -2, -1, 2, 1, 2, 1 };

        static readonly List<byte> IndexBuffer = new List<byte>();

        static Attacks()
        {
            long t0 = Stopwatch.GetTimestamp();

            for (int index = 0; index < 64; index++)
            {
                int rank = index / 8;
                int file = index % 8;

                //Add 4 diagonal lines
                for (int dir = 0; dir < 4; dir++)
                    Diagonal[index, dir] = WalkTheLine(rank, file, DIAGONALS_RANK[dir], DIAGONALS_FILE[dir]);

                //Add 4 straight lines
                for (int dir = 0; dir < 4; dir++)
                    Straight[index, dir] = WalkTheLine(rank, file, STRAIGHTS_RANK[dir], STRAIGHTS_FILE[dir]);                              

                //Add Knight&King attack patterns
                King[index] = ApplyPattern(rank, file, KING_RANK, KING_FILE);
                Knight[index] = ApplyPattern(rank, file, KNIGHT_RANK, KNIGHT_FILE);

                //Add size-2 arrays for pawn attacks
                BlackPawn[index] = PawnAttacks(rank, file, -1);
                WhitePawn[index] = PawnAttacks(rank, file, +1);
            }

            long t1 = Stopwatch.GetTimestamp();
            double dt = 1000.0 * (t1 - t0) / Stopwatch.Frequency;
            //Console.WriteLine($"Attack index buffers computed in {dt:0.####}ms");
        }

        private static byte[] PawnAttacks(int rank, int file, int dRank)
        {
            IndexBuffer.Clear();
            TryAddIndex(rank + dRank, file - 1);
            TryAddIndex(rank + dRank, file + 1);
            return IndexBuffer.ToArray();
        }

        private static byte[] ApplyPattern(int rank, int file, int[] patternRank, int[] patternFile)
        {
            IndexBuffer.Clear();
            for (int i = 0; i < 8; i++)
                TryAddIndex(rank + patternRank[i], file + patternFile[i]);
            return IndexBuffer.ToArray();
        }

        private static byte[] WalkTheLine(int rank, int file, int dRank, int dFile)
        {
            IndexBuffer.Clear();
            //inc i as long as the resulting index is still on the board
            for (int i = 1; TryAddIndex(rank + i * dRank, file + i * dFile); i++);
            return IndexBuffer.ToArray();
        }

        private static bool TryAddIndex(int rank, int file)
        {
            bool squareExists = rank >= 0 && rank <= 7 && file >= 0 && file <= 7;
            if(squareExists)
                IndexBuffer.Add((byte)(rank * 8 + file));
            return squareExists;
        }
    }
}
