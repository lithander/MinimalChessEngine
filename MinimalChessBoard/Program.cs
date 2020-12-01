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
            while (running)
            {
                Print(board);
                string cmd = Console.ReadLine();
            }
        }

        private static void Print(Board board)
        {
            Console.WriteLine("  A B C D E F G H");
            Console.WriteLine("  ---------------");
            for (int y = 0; y < 8; y++)
            {
                Console.Write($"{8-y}|");
                for (int x = 0; x < 8; x++)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Piece piece = board[y, x];
                    Print(piece);
                    Console.Write(' ');
                }
                Console.WriteLine();
            }
        }

        private static void Print(Piece piece)
        {
            Console.Write(Notation.ToChar(piece));
        }
    }
}
