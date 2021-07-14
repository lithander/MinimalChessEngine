using System.Collections.Generic;

namespace MinimalChess
{
    public static class Attacks
    {
        public static byte[][][] Bishop = new byte[64][][];
        public static byte[][][] Rook = new byte[64][][];
        public static byte[][][] Queen = new byte[64][][];
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
            for (int index = 0; index < 64; index++)
            {
                int rank = index / 8;
                int file = index % 8;

                Bishop[index] = new byte[4][];
                Rook[index] = new byte[4][];
                Queen[index] = new byte[8][];

                //Add 4 diagonal lines
                for (int dir = 0; dir < 4; dir++)
                    Queen[index][dir] = Bishop[index][dir] = WalkTheLine(rank, file, DIAGONALS_RANK[dir], DIAGONALS_FILE[dir]);

                //Add 4 straight lines
                for (int dir = 0; dir < 4; dir++)
                    Queen[index][dir+4] = Rook[index][dir] = WalkTheLine(rank, file, STRAIGHTS_RANK[dir], STRAIGHTS_FILE[dir]);

                //Add Knight&King attack patterns
                King[index] = ApplyPattern(rank, file, KING_RANK, KING_FILE);
                Knight[index] = ApplyPattern(rank, file, KNIGHT_RANK, KNIGHT_FILE);

                //Add size-2 arrays for pawn attacks
                BlackPawn[index] = PawnAttacks(rank, file, -1);
                WhitePawn[index] = PawnAttacks(rank, file, +1);
            }
        }

        private static byte[] PawnAttacks(int rank, int file, int dRank)
        {
            IndexBuffer.Clear();
            IndexBuffer.TryAddSquare(rank + dRank, file - 1);
            IndexBuffer.TryAddSquare(rank + dRank, file + 1);
            return IndexBuffer.ToArray();
        }

        private static byte[] ApplyPattern(int rank, int file, int[] patternRank, int[] patternFile)
        {
            IndexBuffer.Clear();
            for (int i = 0; i < 8; i++)
                IndexBuffer.TryAddSquare(rank + patternRank[i], file + patternFile[i]);
            return IndexBuffer.ToArray();
        }

        private static byte[] WalkTheLine(int rank, int file, int dRank, int dFile)
        {
            IndexBuffer.Clear();
            while(true)
            {
                //inc i as long as the resulting index is still on the board
                rank += dRank;
                file += dFile;
                if (!IsLegalSquare(rank, file))
                    break;
                IndexBuffer.AddSquare(rank, file);
            }
            return IndexBuffer.ToArray();
        }

        private static void TryAddSquare(this List<byte> buffer, int rank, int file)
        {
            if (IsLegalSquare(rank, file))
                buffer.AddSquare(rank, file);
        }

        private static void AddSquare(this List<byte> buffer, int rank, int file) => buffer.Add((byte)(rank * 8 + file));

        private static bool IsLegalSquare(int rank, int file) => rank >= 0 && rank <= 7 && file >= 0 && file <= 7;
    }
}
