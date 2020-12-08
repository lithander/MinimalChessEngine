using MinimalChess;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinimalChessBoard
{
    class Program
    {
        static void Main(string[] args)
        {
            bool running = true;
            Board board = new Board(Board.STARTING_POS_FEN);
            Print(board);
            while (running)
            {
                string active = board.WhiteMoves ? "White" : "Black";
                Console.Write($"{active} >> ");
                string input = Console.ReadLine();

                if (input == "?")
                    ListMoves(board);
                else
                    ApplyMoves(board, input.Split());
            }
        }

        private static void Print(Board board)
        {
            Console.WriteLine();
            Console.WriteLine(" A B C D E F G H");
            Console.WriteLine(" _______________");
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    Piece piece = board[rank, file];
                    Print(piece);
                }
                Console.WriteLine($"| {rank + 1}"); //ranks aren't zero-indexed
            }
            Console.WriteLine();
        }

        private static void Print(Piece piece)
        {
            Console.Write(' ');
            Console.Write(Notation.ToChar(piece));
        }

        private static void ListMoves(Board board)
        {
            List<Move> legalMoves = board.GetLegalMoves();
            Console.Write($"({legalMoves.Count}) ");
            foreach (var move in legalMoves)
                Console.Write(move.ToString() + " ");
            Console.WriteLine();
        }

        private static void ApplyMoves(Board board, string[] moves)
        {
            for (int i = 0; i < moves.Length; i++)
            {
                Move move = new Move(moves[i]);
                Debug.Assert(move.ToString() == moves[i]);
                if (i > 0)
                {
                    Console.Write($"{i + 1}. {(board.WhiteMoves ? "White" : "Black")} >> {move}");
                    Console.WriteLine();
                }
                board.Play(move);
                Print(board);
            }
        }
    }
}
