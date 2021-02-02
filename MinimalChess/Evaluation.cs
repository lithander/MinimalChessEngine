﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MinimalChess
{
    public static class Evaluation
    {
        public static int MinValue => -333;
        public static int MaxValue => 333;

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
