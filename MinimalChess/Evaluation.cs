using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public static class Evaluation
    {
        public static int MinValue => short.MinValue;

        public static int[] PieceValues = new int[13]
{
             0,   //None = 0,
            +1,   //WhitePawn = 1,
            +3,   //WhiteKnight = 2,
            +3,   //WhiteBishop = 3,
            +5,   //WhiteRook = 4,
            +9,   //WhiteQueen = 5,
            +200, //WhiteKing = 6,
            -1,   //BlackPawn = 7,
            -3,   //BlackKnight = 8,
            -3,   //BlackBishop = 9,
            -5,   //BlackRook = 10,
            -9,   //BlackQueen = 11,
            -200  //BlackKing = 12,
};

        public static int Evaluate(Board board)
        {
            int score = 0;
            for (int i = 0; i < 64; i++)
            {
                int piece = (int)board[i];
                score += PieceValues[piece];
            }
            return score;
        }
    }
}
