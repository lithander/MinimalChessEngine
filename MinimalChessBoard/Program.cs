using MinimalChess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MinimalChessBoard
{
    class Program
    {
        static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Board board = new Board(Board.STARTING_POS_FEN);
            Move move = default;
            while (true)
            {
                try
                {
                    Console.WriteLine();
                    Print(board, move);
                    move = default;
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
                Console.Write($"{board.SideToMove} >> ");
                string input = Console.ReadLine();
                string[] tokens = input.Split();
                string command = tokens[0];

                long t0 = Stopwatch.GetTimestamp();
                try
                {
                    if (command == "reset")
                    {
                        board = new Board(Board.STARTING_POS_FEN);
                    }
                    if (command == "kiwi")
                    {
                        //Kiwipete position
                        board = new Board("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq -");
                    }
                    else if (command.Count(c => c == '/') == 7) //Fen-string detection
                    {
                        board.SetupPosition(input);
                    }
                    else if (command == "perft")
                    {
                        int depth = int.Parse(tokens[1]);
                        if (tokens.Length > 2)
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
                        IterativeSearch search = new IterativeSearch(depth, board);
                        move = search.PrincipalVariation[0];
                        Console.WriteLine($"{board.SideToMove} >> {move}");
                        board.Play(move);
                    }
                    else if (command == "?" && tokens.Length == 3)
                    {
                        int timeBudgetMs = int.Parse(tokens[2]);
                        CompareBestMove(tokens[1], timeBudgetMs);
                    }
                    else if (command == "?")
                    {
                        int depth = tokens.Length > 1 ? int.Parse(tokens[1]) : 0;
                        ListMoves(board, depth);
                    }
                    else if (command == "m")
                    {
                        PrintMobility(board);
                    }
                    else
                    {
                        ApplyMoves(board, tokens);
                    }

                    long t1 = Stopwatch.GetTimestamp();
                    double dt = (t1 - t0) / (double)Stopwatch.Frequency;
                    if (dt > 0.01)
                        Console.WriteLine($"  Operation took {dt:0.####}s");
                }
                catch (Exception error)
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
            int pstScore = board.Score;
            int mobScore = Evaluation.ComputeMobility(board);
            Console.WriteLine($"  A B C D E F G H {(pstScore + mobScore):+0.00;-0.00} (PST:{pstScore:+0.00;-0.00}, Mobility:{mobScore})");
        }

        private static void PrintMobility(Board board)
        {
            Console.WriteLine("   A   B   C   D   E   F   G   H");
            Console.WriteLine("  .------------------------------.");
            for (int rank = 7; rank >= 0; rank--)
            {
                Console.Write($"{rank + 1}|"); //ranks aren't zero-indexed
                for (int file = 0; file < 8; file++)
                    Console.Write($"{Evaluation.GetMobility(board, rank * 8 + file),3} ");

                Console.WriteLine();
            }
            Console.WriteLine("  '-------------------------------'");
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
                if (move.FromSquare == index || move.ToSquare == index)
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
            }

            Console.ForegroundColor = piece.IsWhite() ? ConsoleColor.White : ConsoleColor.Gray;
        }

        private static void ListMoves(Board board, int depth)
        {
            IterativeSearch search = new IterativeSearch(depth, board);
            Move[] line = search.PrincipalVariation;

            int i = 1;
            foreach (var move in new LegalMoves(board))
            {
                if (line != null && line.Length > 0 && line[0] == move)
                {
                    string pvString = string.Join(' ', line);
                    Console.WriteLine($"{i++,4}. {pvString} = {search.Score:+0.00;-0.00}");
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
                    Console.Write($"{i + 1}. {board.SideToMove} >> {move}");
                    Console.WriteLine();
                }
                board.Play(move);
            }
        }

        private static long Perft(Board board, int depth)
        {
            if (depth == 0)
                return 1;

            //probe hash-tree
            //if (PerftTable.Retrieve(board.ZobristHash, depth, out long childCount))
            //    return childCount;

            long sum = 0;
            foreach (var move in new LegalMoves(board))
                sum += Perft(new Board(board, move), depth - 1);

            //PerftTable.Store(board.ZobristHash, depth, sum);
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
            long sum = 0;
            Board next = new Board(board);
            foreach (var move in new LegalMoves(board))
            {
                next.Copy(board);
                next.Play(move);
                PerftTable.Clear();
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
                if (data.Length <= depth)
                {
                    Console.WriteLine($"{line++} SKIPPED! No reference available for perft({depth}) FEN: {fen}");
                    continue;
                }
                long refResult = long.Parse(data[depth].Substring(3));

                Board board = new Board(data[0]);
                //PerftTable.Clear();
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


        private static void CompareBestMove(string filePath, int timeBudgetMs)
        {
            var file = File.OpenText(filePath);
            double freq = Stopwatch.Frequency;
            long totalTime = 0;
            long totalNodes = 0;
            int count = 0;
            int foundBest = 0;
            List<Move> bestMoves = new List<Move>();
            while (!file.EndOfStream)
            {
                ParseEpd(file.ReadLine(), out Board board, bestMoves);
                Transpositions.Clear();
                IterativeSearch search = new IterativeSearch(board);
                Move pvMove = default;
                long t0 = Stopwatch.GetTimestamp();
                long tStop = t0 + (timeBudgetMs * Stopwatch.Frequency) / 1000;
                //search until running out of time
                while (true)
                {
                    search.SearchDeeper(() => Stopwatch.GetTimestamp() > tStop);
                    if (search.Aborted)
                        break;
                    pvMove = search.PrincipalVariation[0];
                }
                long t1 = Stopwatch.GetTimestamp();
                long dt = t1 - t0;
                totalTime += dt;
                totalNodes += search.NodesVisited;
                count++;
                string pvString = string.Join(' ', search.PrincipalVariation);
                bool foundBestMove = bestMoves.Contains(pvMove);
                if (foundBestMove)
                    foundBest++;
                Console.WriteLine($"{count,4}. {(foundBestMove ? "[X]" : "[ ]")} {pvString} = {search.Score:+0.00;-0.00}, {search.NodesVisited / 1000}K nodes, { 1000 * dt / freq}ms");
                Console.WriteLine($"{totalNodes,14} nodes, { (int)(totalTime / freq)} seconds, {foundBest} solved.");
            }
            Console.WriteLine();
            Console.WriteLine($"Searched {count} positions for {timeBudgetMs}ms each. {totalNodes/1000}K nodes visited. Took {totalTime/freq:0.###} seconds!");
            Console.WriteLine($"Best move found in {foundBest} / {count} positions!");
        }

        private static void ParseEpd(string epd, out Board board, List<Move> bestMoves)
        {
            //The parser expects a fen-string with bm delimited by a ';'
            //Example: 2q1r1k1/1ppb4/r2p1Pp1/p4n1p/2P1n3/5NPP/PP3Q1K/2BRRB2 w - - bm f7+; id "ECM.001";
            int bmStart = epd.IndexOf("bm") + 3;
            int bmEnd = epd.IndexOf(';', bmStart);

            string fen = epd.Substring(0, bmStart);
            string bmString = epd.Substring(bmStart, bmEnd - bmStart);

            board = new Board(fen);
            bestMoves.Clear();
            foreach (var token in bmString.Split())
            {
                Move bestMove = AlgebraicNotation.ToMove(board, token);
                //Console.WriteLine($"{bmString} => {bestMove}");
                bestMoves.Add(bestMove);
            }
        }
    }
}
