using System;
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

        /*
        public static int Evaluate(Board board)
        {
            int score = 0;
            for (int i = 0; i < 64; i++)
            {
                int j = (int)board[i] >> 1;
                score += PieceValues[j];
            }
            return score;
        }*/

        public static int Evaluate(Board board)
        {
            return PieceSquareTable.Evaluate(board);
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

        public static int QEval(Board position, SearchWindow window, out Move best)
        {
            best = default;

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
                int score = QEval(nextPosition, window, out _);

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


        public static int QEval2(Board position, SearchWindow window)
        {
            Color color = position.ActiveColor;
            //if inCheck we can't use standPat, need to escape check!
            bool inCheck = position.IsChecked(color);

            //the assumption of QEval is that the active player can either 
            //1) pick a neutral move that doesn't change the standPatScore for the worse
            // - OR -
            //2) continue to capture material to try and improve his position

            //Cut will raise alpha and perform beta cutoff when standPatScore is too good
            if (!inCheck)
            {
                int standPatScore = Evaluate(position);
                if(window.Cut(standPatScore, color))
                    return window.GetScore(color);
            }

            ChildNodes4 childNodes = new ChildNodes4(position, inCheck);
            bool canMove = false;
            //now we play each remaining capture if legal and evaluate using QEval recursively
            foreach (var childNode in childNodes)
            {
                canMove = true;
                //recursively evaluate the resulting position (after the capture) with QEval
                int score = QEval2(childNode, window);

                //int score = QEval2(childNode, window, out _, qDepth + 1);

                //Cut will raise alpha and perform beta cutoff when the move is too good
                if (window.Cut(score, color))
                    break;
            }

            if (!canMove)
            {
                //checkmate?
                if (inCheck)
                    return (int)color * Evaluation.MinValue;

                //stalemate?
                if (!childNodes.AnyLegalMoves)
                    return 0;
            }

            //can't capture. We return the 'alpha' which may have been raised by stand pat
            return window.GetScore(color);
        }


        public static int QEval3(Board position, SearchWindow window, out Move best)
        {
            best = default;

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
            {
                Color otherColor = Pieces.Flip(color);
                if (position.IsChecked(otherColor))
                    return (int)otherColor * Evaluation.MinValue; //We could also kill their king

                return window.GetScore(color);
            }

            //Getting *all* possible moves in that position for 'color'
            List<Move> moves = new PseudoLegalMoves(position);
            //removing the non-captures and sorting the rest
            //removing the non-captures and sorting the rest
            if (inCheck)
                MoveOrdering.SortMvvLva(moves, position);
            else
                MoveOrdering.RemoveNonCapturesAndSortMvvLva(moves, position);

            bool escapeCheck = false;
            //now we play each remaining capture if legal and evaluate using QEval recursively
            foreach (var move in moves)
            {
                //because the move was 'pseudolegal' we make sure it doesnt result in a position 
                //where our (color) king is in check - in that case we can't play it
                Board nextPosition = new Board(position);
                Piece victim = nextPosition.Play(move);
                if (nextPosition.IsChecked(color))
                    continue;

                //we killed a king!!! (qsearch doesn't do the in check test)
                if (Pieces.Type(victim) == Piece.King)
                    return (int)victim.GetColor() * Evaluation.MinValue;

                //recursively evaluate the resulting position (after the capture) with QEval
                int score = QEval3(nextPosition, window, out _);
                if (score == (int)color * Evaluation.MinValue)
                    continue;

                escapeCheck = true;

                //Cut will raise alpha and perform beta cutoff when the move is too good
                if (window.Cut(score, color))
                    break;
            }

            if (inCheck && !escapeCheck)
                return (int)color * Evaluation.MinValue;

            //No beta-cutoff has happened! We return the 'alpha'
            return window.GetScore(color);
        }
    }
}
