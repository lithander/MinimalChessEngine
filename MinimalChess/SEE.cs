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

        public static int Evaluate(Board position, Move move)
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
    }
}
