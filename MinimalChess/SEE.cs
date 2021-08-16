using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChess
{
    public static class SEE
    {
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

            -9999,   //BlackKing = 12,
            +9999    //WhiteKing = 13,
        };

        public static int PieceValue(Piece piece) => PieceValues[(int)piece >> 1];

        public static int EvaluateRecursive(Board position, Move move)
        {
            position = new Board(position);
            int material = 0;
            if (move.Promotion != Piece.None)
            {
                material -= PieceValue(position[move.FromSquare]);
                material += PieceValue(move.Promotion);
            }
            Piece victim = position.Play(move);
            material -= PieceValue(victim);
            return Eval(position, SearchWindow.Infinite, move.ToSquare, material);
        }

        private static int Eval(Board position, SearchWindow window, int toSquare, int material)
        {
            Color color = position.SideToMove;
            //raise alpha to standPatScore and perform beta cutoff when standPatScore is too good
            if (window.Cut(material, color))
                return window.GetScore(color);
        
            int fromSquare = position.GetLeastValuableAttacker(toSquare, position.SideToMove);
            //can't capture. We return the 'alpha' which may have been raised by "stand pat"
            if (fromSquare == -1)
                return window.GetScore(color);
        
            Piece attacker = position[fromSquare];
            Move move;
            if (attacker == Piece.WhitePawn && Board.Rank(toSquare) == 7)
                move = new Move(fromSquare, toSquare, Piece.WhiteQueen);
            else if (attacker == Piece.BlackPawn && Board.Rank(toSquare) == 0)
                move = new Move(fromSquare, toSquare, Piece.BlackQueen);
            else
                move = new Move(fromSquare, toSquare);
        
            if (move.Promotion != Piece.None)
            {
                material -= PieceValue(attacker);
                material += PieceValue(move.Promotion);
            }
        
            Piece victim = position.Play(move);
            material -= PieceValue(victim);
            return Eval(position, window, toSquare, material);
        }

        public static int EvalPST(Board position, Move move)
        {
            SearchWindow window = SearchWindow.Infinite;
            position = new Board(position);
            int seeSquare = move.ToSquare;
            int baseScore = Evaluation.Evaluate(position);
            while (true)
            {
                var victim = position.Play(move);
                if (victim == Piece.King.OfColor(position.SideToMove))
                    return window.GetScore(position.SideToMove);

                int score = Evaluation.Evaluate(position) - baseScore;
                //raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(score, position.SideToMove))
                    return window.GetScore(position.SideToMove);

                int fromSquare = position.GetLeastValuableAttacker(seeSquare, position.SideToMove);
                //can't capture. We return the 'alpha'
                if (fromSquare == -1)
                    return window.GetScore(position.SideToMove);

                if (position[fromSquare] == Piece.WhitePawn && Board.Rank(seeSquare) == 7)
                    move = new Move(fromSquare, seeSquare, Piece.WhiteQueen);
                else if (position[fromSquare] == Piece.BlackPawn && Board.Rank(seeSquare) == 0)
                    move = new Move(fromSquare, seeSquare, Piece.BlackQueen);
                else
                    move = new Move(fromSquare, seeSquare);
            }
        }

        //public static int EvaluateExchange(Board position, Move move)
        //{
        //    SearchWindow window = SearchWindow.Infinite;
        //    Board board = new Board(position);
        //    int seeSquare = move.ToSquare;
        //    int baseScore = board.Score;
        //    while (true)
        //    {
        //        var victim = board.Play(move);
        //
        //        //if we captured a KING the beta cutoff is guaranteed (but not automatic because the king is zero-valued in the PSTs)
        //        if (victim == Piece.King.OfColor(board.SideToMove))
        //            return window.GetScore(board.SideToMove);
        //
        //        //raise alpha and perform beta cutoff when standPatScore is too good
        //        if (window.Cut(board.Score - baseScore, board.SideToMove))
        //            return window.GetScore(board.SideToMove);
        //
        //        //can we capture again? if not we return 'alpha'
        //        int fromSquare = board.GetLeastValuableAttacker(seeSquare, board.SideToMove);
        //        if (fromSquare == -1)
        //            return window.GetScore(board.SideToMove);
        //
        //        //generate the next capture on seeSquare
        //        if (board[fromSquare] == Piece.WhitePawn && Board.Rank(seeSquare) == 7)
        //            move = new Move(fromSquare, seeSquare, Piece.WhiteQueen);
        //        else if (board[fromSquare] == Piece.BlackPawn && Board.Rank(seeSquare) == 0)
        //            move = new Move(fromSquare, seeSquare, Piece.BlackQueen);
        //        else
        //            move = new Move(fromSquare, seeSquare);
        //    }
        //}

        public static int Evaluate(Board position, Move move)
        {
            SearchWindow window = SearchWindow.Infinite;
            position = new Board(position);
            int seeSquare = move.ToSquare;
            int material = 0;
            while (true)
            {
                if (move.Promotion != Piece.None)
                {
                    material -= PieceValue(position[move.FromSquare]);
                    material += PieceValue(move.Promotion);
                }
                Piece victim = position.Play(move);
                material -= PieceValue(victim);
                Color color = position.SideToMove;

                //raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(material, color))
                    return window.GetScore(color);

                int fromSquare = position.GetLeastValuableAttacker(seeSquare, color);
                //can't capture. We return the 'alpha'
                if (fromSquare == -1)
                    return window.GetScore(color);

                if (position[fromSquare] == Piece.WhitePawn && Board.Rank(seeSquare) == 7)
                    move = new Move(fromSquare, seeSquare, Piece.WhiteQueen);
                else if (position[fromSquare] == Piece.BlackPawn && Board.Rank(seeSquare) == 0)
                    move = new Move(fromSquare, seeSquare, Piece.BlackQueen);
                else
                    move = new Move(fromSquare, seeSquare);
            }
        }

        public static int EvaluateSign(Board position, Move move)
        {
            position = new Board(position);
            int material = 0;
            if (move.Promotion != Piece.None)
            {
                material -= PieceValue(position[move.FromSquare]);
                material += PieceValue(move.Promotion);
            }
            int toSquare = move.ToSquare;
            Piece victim = position.Play(move);
            material -= PieceValue(victim);
            SearchWindow window = SearchWindow.Infinite;
            while (true)
            {
                Color color = position.SideToMove;
                //raise alpha and perform beta cutoff when standPatScore is too good
                if (window.Cut(material, color) || window.Ceiling < 0 || window.Floor > 0)
                    return Math.Sign(window.GetScore(color));

                int fromSquare = position.GetLeastValuableAttacker(toSquare, position.SideToMove);
                //can't capture. We return the 'alpha'
                if (fromSquare == -1)
                    return Math.Sign(window.GetScore(color));

                Piece attacker = position[fromSquare];
                Piece promotion = Piece.None;
                if (attacker == Piece.WhitePawn && Board.Rank(toSquare) == 7)
                {
                    promotion = Piece.WhiteQueen;
                    material -= PieceValue(attacker);
                    material += PieceValue(Piece.WhiteQueen);
                }
                else if (attacker == Piece.BlackPawn && Board.Rank(toSquare) == 0)
                {
                    promotion = Piece.BlackQueen;
                    material -= PieceValue(attacker);
                    material += PieceValue(Piece.BlackQueen);
                }
                victim = position.Play(new Move(fromSquare, toSquare, promotion));
                material -= PieceValue(victim);
            }
        }
    }
}
