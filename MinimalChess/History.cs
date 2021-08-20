using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChess
{
    public static class History
    {
        private const int Squares = 64;
        private const int Pieces = 12;
        private static int[,] Positive = new int[Squares, Pieces];
        private static int[,] Negative = new int[Squares, Pieces];

        public static int Max
        {
            get
            {
                int max = 0;
                for (int square = 0; square < Squares; square++)
                for (int piece = 0; piece < Pieces; piece++)
                {
                    max = Math.Max(max, Positive[square, piece]);
                    max = Math.Max(max, Negative[square, piece]);
                }

                return max;
            }
        }

        public static void Clear()
        {
            for (int square = 0; square < Squares; square++)
            for (int piece = 0; piece < Pieces; piece++)
            {
                Positive[square, piece] = 0;
                Negative[square, piece] = 0;
            }
        }


        public static void Shrink()
        {
            for (int square = 0; square < Squares; square++)
                for(int piece = 0; piece < Pieces; piece++)
                {
                    Positive[square, piece] /= 2;
                    Negative[square, piece] /= 2;
                }
        }

        public static void Good(Piece piece, int square, int depth)
        {
            int iPiece = ((byte)piece >> 1) - 2; //BlackPawn = 0...
            Positive[square, iPiece] += depth * depth;
        }

        public static void Bad(Piece piece, int square, int depth)
        {
            int iPiece = ((byte)piece >> 1) - 2; //BlackPawn = 0...
            Negative[square, iPiece] += depth * depth;
        }

        public static float Value(Piece piece, int square)
        {
            int iPiece = ((byte)piece >> 1) - 2; //BlackPawn = 0...
            float a = Positive[square, iPiece] + 1;
            float b = Negative[square, iPiece] + 2;
            return a / b;
        }
    }
}
