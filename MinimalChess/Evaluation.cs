﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MinimalChess
{
    public static class Evaluation
    {
        public static int MinValue => -9999;
        public static int MaxValue => 9999;

        public static readonly int[] PieceValues = new int[14]
        {
             0,     //Black = 0,
             0,     //White = 1,
            
            -100,   //BlackPawn = 2,
            +100,   //WhitePawn = 3,

            -300,   //BlackKnight = 4,
            +300,   //WhiteKnight = 5,
            
            -300,   //BlackBishop = 6,
            +300,   //WhiteBishop = 7,

            -500,   //BlackRook = 8,
            +500,   //WhiteRook = 9,
            
            -900,   //BlackQueen = 10,
            +900,   //WhiteQueen = 11,

             0,   //BlackKing = 12,
             0    //WhiteKing = 13,
        };

        public static int Evaluate(Board board)
        {
            return PeSTO.Evaluate(board);
            //int score = 0;
            //for (int i = 0; i < 64; i++)
            //    score += PieceSquareTable.Value(board[i], i);
            //return score;
        }

        public static int EvaluateWithMate(Board board)
        {
            var moves = new AnyLegalMoves(board);
            
            //if the game is not yet over just look who leads in material
            if (moves.CanMove)
                return Evaluate(board);

            //active color has lost the game?
            if (board.IsChecked(board.ActiveColor))
                return (int)board.ActiveColor * MinValue; 

            //No moves but king isn't checked -> it's a draw
            return 0;
        }

        public static int QEval(Board position, SearchWindow window)
        {
            //Eval just counts material! If the active color would be in check or stalemated this doesn't affect the score
            int standPatScore = Evaluation.Evaluate(position);

            Color color = position.ActiveColor;
            //if inCheck we can't use standPat, need to escape check!
            bool inCheck = position.IsChecked(color);

            //the assumption of QEval is that the active player can either 
            //1) pick a neutral move that doesn't change the standPatScore for the worse
            // - OR -
            //2) continue to capture material to try and improve his position

            //Cut will raise alpha and perform beta cutoff when standPatScore is too good
            if (!inCheck && window.Cut(standPatScore, color))
                return window.GetScore(color);

            //Getting *all* possible moves in that position for 'color'
            List<Move> captures = new PseudoLegalMoves(position);

            //removing the non-captures and sorting the rest
            if (inCheck)
                MoveOrdering.SortMvvLva(captures, position);
            else
                MoveOrdering.RemoveNonCapturesAndSortMvvLva(captures, position);

            bool hasMoved = false;
            //now we play each remaining capture if legal and evaluate using QEval recursively
            foreach (var move in captures)
            {
                //because the move was 'pseudolegal' we make sure it doesnt result in a position 
                //where our (color) king is in check - in that case we can't play it
                Board nextPosition = new Board(position, move);
                if (nextPosition.IsChecked(color))
                    continue;

                hasMoved = true;
                //recursively evaluate the resulting position (after the capture) with QEval
                int score = QEval(nextPosition, window);

                //Cut will raise alpha and perform beta cutoff when the move is too good
                if (window.Cut(score, color))
                    break;
            }

            //TODO: if inCheck and no move was played -->
            if (!hasMoved)
            {
                if(inCheck)
                    return (int)color * Evaluation.MinValue;

                //stalemate?
                var moves = new AnyLegalMoves(position);
                if (!moves.CanMove)
                    return 0;
            }

            //No beta-cutoff has happened! We return the 'alpha'
            return window.GetScore(color);
        }
    }
}
