using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public static class Evaluation
    {
        public static int MinValue => -9999;
        public static int MaxValue => 9999;

        public static int[] PieceValues = new int[13]
{
             000,   //None = 0,
            +100,   //WhitePawn = 1,
            +300,   //WhiteKnight = 2,
            +300,   //WhiteBishop = 3,
            +500,   //WhiteRook = 4,
            +900,   //WhiteQueen = 5,
           +9999,   //WhiteKing = 6,
            -100,   //BlackPawn = 7,
            -300,   //BlackKnight = 8,
            -300,   //BlackBishop = 9,
            -500,   //BlackRook = 10,
            -900,   //BlackQueen = 11,
           -9999    //BlackKing = 12,
};

        public static int SumPieceValues(Board board)
        {
            int score = 0;
            for (int i = 0; i < 64; i++)
            {
                int piece = (int)board[i];
                score += PieceValues[piece];
            }
            return score;
        }

        public static int Evaluate(Board board)
        {
            return SumPieceValues(board);
            /*
            var moves = new AnyLegalMoves(board);
            
            //if the game is not yet over just look who leads in material
            if (moves.CanMove)
                return SumPieceValues(board);

            //active color has lost the game?
            if (board.IsChecked(board.ActiveColor))
                return (int)board.ActiveColor * MinValue; 

            //No moves but king isn't checked -> it's a draw
            return 0;
            */
        }

        public static int EvaluateWithMate(Board board)
        {
            var moves = new AnyLegalMoves(board);
            
            //if the game is not yet over just look who leads in material
            if (moves.CanMove)
                return SumPieceValues(board);

            //active color has lost the game?
            if (board.IsChecked(board.ActiveColor))
                return (int)board.ActiveColor * MinValue; 

            //No moves but king isn't checked -> it's a draw
            return 0;
        }
    }
}
