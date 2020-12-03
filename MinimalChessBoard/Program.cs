using MinimalChess;
using System;

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
                Console.Write(">> Move: ");
                string input = Console.ReadLine();
                string[] moves = input.Split();
                foreach(string move in moves)
                {
                    ApplyMove(board, move);
                    Print(board);
                }
            }
        }

        private static void ApplyMove(Board board, string moveNotation)
        {           
            Move move = new Move(moveNotation);
            board.Play(move);
        }

        private static void Print(Board board)
        {
            Console.WriteLine();
            Console.WriteLine("  A B C D E F G H");
            Console.WriteLine("  ---------------");
            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write($"{rank + 1}|"); //ranks aren't zero-indexed
                for (int file = 0; file < 8; file++)
                {
                    Piece piece = board[rank, file];
                    Print(piece);
                    Console.Write(' ');
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private static void Print(Piece piece)
        {
            Console.Write(Notation.ToChar(piece));
        }
    }
}
