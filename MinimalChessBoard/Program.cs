using MinimalChess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace MinimalChessBoard
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            bool running = true;
            Board board = new Board(Board.STARTING_POS_FEN);
            while (running)
            {
                try
                {
                    Print(board);
                    if (board.IsChecked(Color.Black))
                        Console.WriteLine(" <!> Black is in check");
                    if (board.IsChecked(Color.White))
                        Console.WriteLine(" <!> White is in check");
                }
                catch (Exception error)
                {
                    Console.WriteLine("ERROR: " + error.Message);
                }

                Console.WriteLine();
                Console.Write($"{board.ActiveColor} >> ");
                string input = Console.ReadLine();

                try
                {
                    if (input.StartsWith("reset"))
                    {
                        board = new Board(Board.STARTING_POS_FEN);
                    }
                    else if (input.StartsWith("fen "))
                    {
                        string fen = input.Substring(4);
                        board.SetupPosition(fen);
                    }
                    else if (input.StartsWith("perft "))
                    {
                        int depth = int.Parse(input.Substring(6));
                        RunPerft(board, depth);
                    }
                    else if (input.StartsWith("divide "))
                    {
                        int depth = int.Parse(input.Substring(6));
                        RunDivide(board, depth);
                    }
                    else if (input == "?")
                    {
                        ListMoves(board);
                    }
                    else
                    {
                        ApplyMoves(board, input.Split());
                    }
                }
                catch(Exception error)
                {
                    Console.WriteLine("ERROR: " + error.Message);
                }
            }
        }

        private static void Print(Board board)
        {
            Console.WriteLine();
            Console.WriteLine("   A B C D E F G H");
            Console.WriteLine(" +----------------+");
            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write($"{rank + 1}|"); //ranks aren't zero-indexed
                for (int file = 0; file < 8; file++)
                {
                    Piece piece = board[rank, file];
                    SetColor(piece, rank, file);
                    Console.Write(Notation.ToChar(piece));
                    Console.Write(' ');
                }
                Console.ResetColor();
                Console.WriteLine($"|{rank + 1}"); //ranks aren't zero-indexed
            }
            Console.WriteLine(" +----------------+");
            Console.WriteLine("  A B C D E F G H");
        }

        private static void SetColor(Piece piece, int rank, int file)
        {
            if ((rank + file) % 2 == 1)
                Console.BackgroundColor = ConsoleColor.DarkGray;
            else
                Console.BackgroundColor = ConsoleColor.Black;

            if (piece != Piece.None && Pieces.GetColor(piece) == Color.White)
                Console.ForegroundColor = ConsoleColor.White;
            else
                Console.ForegroundColor = ConsoleColor.Gray;
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
                if (move.ToString() != moves[i])
                    throw new ArgumentException($"Move notation {moves[i]} not understood!");

                Debug.Assert(move.ToString() == moves[i]);
                if (i > 0)
                {
                    Console.Write($"{i + 1}. {board.ActiveColor} >> {move}");
                    Console.WriteLine();
                }
                board.Play(move);
            }
        }

        private static long Perft(Board board, int depth)
        {
            if (depth <= 0)
                return 1;

            var moves = board.GetLegalMoves();
            if (depth == 1) //no need to apply the moves before counting them
                return moves.Count;

            long sum = 0;
            Board next = new Board(board);
            foreach (var move in moves)
            {
                next.Copy(board);
                next.Play(move);
                sum += Perft(next, depth - 1);
            }
            return sum;
        }

        private static void RunPerft(Board board, int depth)
        {
            long t0 = Stopwatch.GetTimestamp();
            long result = Perft(board, depth);
            long t1 = Stopwatch.GetTimestamp();
            double dt = (t1 - t0) / (double)Stopwatch.Frequency;
            Console.WriteLine($"  Moves:    {result:N0}");
            Console.WriteLine($"  Seconds:  {dt:0.####}");
            Console.WriteLine($"  Moves/s:  {(result / dt):N0}");
        }

        private static void RunDivide(Board board, int depth)
        {
            var moves = board.GetLegalMoves();
            long sum = 0;
            Board next = new Board(board);
            foreach (var move in moves)
            {
                next.Copy(board);
                next.Play(move);
                long nodes = Perft(next, depth - 1);
                sum += nodes;
                Console.WriteLine($"  {move}:    {nodes:N0}");
            }
            Console.WriteLine();
            Console.WriteLine($"  Total:   {sum:N0}");
        }

    }
}
