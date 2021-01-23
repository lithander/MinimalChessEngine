using MinimalChess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace MinimalChessBoard
{
    class Program
    {
        static void Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            bool running = true;
            Board board = new Board(Board.STARTING_POS_FEN);
            Move move = default;
            while (running)
            {
                try
                {
                    Console.WriteLine();
                    Print(board, move);
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
                string[] tokens = input.Split();
                string command = tokens[0];

                try
                {
                    if (command == "reset")
                    {
                        board = new Board(Board.STARTING_POS_FEN);
                    }
                    else if (command == "fen")
                    {
                        string fen = input.Substring(4);
                        board.SetupPosition(fen);
                    }
                    else if (command == "perft")
                    {
                        int depth = int.Parse(tokens[1]);
                        if(tokens.Length > 2)
                            ComparePerft(depth, tokens[2]);
                        else
                            RunPerft(board, depth);
                    }
                    else if (command == "divide")
                    {
                        int depth = int.Parse(tokens[1]);
                        RunDivide(board, depth);
                    }
                    else if (command == "!")
                    {
                        int depth = tokens.Length > 1 ? int.Parse(tokens[1]) : 4;
                        move = Search.GetBestMove(board, depth);
                        Console.WriteLine($"{board.ActiveColor} >> {move}");
                        board.Play(move);
                    }
                    else if (command == "?")
                    {
                        int depth = tokens.Length > 1 ? int.Parse(tokens[1]) : 0;
                        ListMoves(board, depth);
                    }
                    else if (command == "??")
                    {
                        PrintMoves(board);
                    }
                    else
                    {
                        ApplyMoves(board, tokens);
                    }
                }
                catch(Exception error)
                {
                    Console.WriteLine("ERROR: " + error.Message);
                }
            }
        }

        private static void Print(Board board, Move move = default)
        {
            Console.WriteLine("   A B C D E F G H");
            Console.WriteLine(" .----------------.");
            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write($"{rank + 1}|"); //ranks aren't zero-indexed
                for (int file = 0; file < 8; file++)
                {
                    Piece piece = board[rank, file];
                    SetColor(piece, rank, file, move);
                    Console.Write(Notation.ToChar(piece));
                    Console.Write(' ');
                }
                Console.ResetColor();
                Console.WriteLine($"|{rank + 1}"); //ranks aren't zero-indexed
            }
            Console.WriteLine(" '----------------'");
            Console.WriteLine($"  A B C D E F G H {Evaluation.Evaluate(board):+0.00;-0.00}");
        }

        private static void SetColor(Piece piece, int rank, int file, Move move)
        {
            if ((rank + file) % 2 == 1)
                Console.BackgroundColor = ConsoleColor.DarkGray;
            else
                Console.BackgroundColor = ConsoleColor.Black;

            if (move != default)
            {
                int index = rank * 8 + file;
                //highlight squares if they belong to the move
                if (move.FromIndex == index || move.ToIndex == index)
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
            }

            if (piece != Piece.None && Pieces.GetColor(piece) == Color.White)
                Console.ForegroundColor = ConsoleColor.White;
            else
                Console.ForegroundColor = ConsoleColor.Gray;
        }

        private static void PrintMoves(Board board)
        {
            int i = 1;
            foreach (var move in new LegalMoves(board))
            {
                Console.WriteLine($"{i++}. {board.ActiveColor} >> {move}");
                var copy = new Board(board, move);
                Print(copy, move);
                Console.WriteLine();
            }
        }

        private static void ListMoves(Board board, int depth)
        {
            int i = 1;
            foreach (var move in new LegalMoves(board))
            {
                if(depth >= 1)
                {
                    int score = Search.Evaluate(new Board(board, move), depth - 1);
                    Console.WriteLine($"{i++,4}. {move} {score:+0.00;-0.00}");
                }
                else
                    Console.WriteLine($"{i++,4}. {move}");
            }
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

            var moves = new LegalMoves(board);
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
            var moves = new LegalMoves(board);
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

        private static void ComparePerft(int depth, string filePath)
        {
            var file = File.OpenText(filePath);
            int error = 0;
            int line = 1;
            long t0 = Stopwatch.GetTimestamp();
            while (!file.EndOfStream)
            {
                //The parser expects a fen-string followed by a list of perft results for each depth (D1, D2...) starting with depth D1.
                //Example: 4k3 / 8 / 8 / 8 / 8 / 8 / 8 / 4K2R w K - 0 1; D1 15; D2 66; D3 1197; D4 7059; D5 133987; D6 764643

                string entry = file.ReadLine();
                string[] data = entry.Split(';');
                string fen = data[0];
                if(data.Length <= depth)
                {
                    Console.WriteLine($"{line++} SKIPPED! No reference available for perft({depth}) FEN: {fen}");
                    continue;
                }
                long refResult = long.Parse(data[depth].Substring(3));

                Board board = new Board(data[0]);
                long result = Perft(board, depth);
                if (result != refResult)
                {
                    error++;
                    Console.WriteLine($"{line++} ERROR! perft({depth})={result}, expected {refResult} ({result - refResult:+#;-#}) FEN: {fen}");
                }
                else
                    Console.WriteLine($"{line++} OK! perft({depth})={result} FEN: {fen}");
            }
            long t1 = Stopwatch.GetTimestamp();
            double dt = (t1 - t0) / (double)Stopwatch.Frequency;
            Console.WriteLine();
            Console.WriteLine($"Test finished with {error} wrong results after {dt:0.###} seconds!");
        }
    }
}
