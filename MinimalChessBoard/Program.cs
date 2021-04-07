using MinimalChess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Net.Http.Headers;

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
                //try
                {
                    Console.WriteLine();
                    Print(board, move);
                    if (board.IsChecked(Color.Black))
                        Console.WriteLine(" <!> Black is in check");
                    if (board.IsChecked(Color.White))
                        Console.WriteLine(" <!> White is in check");
                }
                //catch (Exception error)
                //{
                //    Console.WriteLine("ERROR: " + error.Message);
                //}

                Console.WriteLine();
                Console.Write($"{board.ActiveColor} >> ");
                string input = Console.ReadLine().Trim();
                string[] tokens = input.Split();
                string command = tokens[0];

                long t0 = Stopwatch.GetTimestamp();
                //try
                {
                    if (command == "reset")
                    {
                        board = new Board(Board.STARTING_POS_FEN);
                        move = default;
                    }
                    else if (command.Count(c => c == '/') == 7) //Fen-string detection
                    {
                        board.SetupPosition(input);
                        move = default;
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
                        move = Search.GetBestMoveMinMax(board, depth);
                        Console.WriteLine($"{board.ActiveColor} >> {move}");
                        board.Play(move);
                    }
                    else if (command == "?")
                    {
                        int depth = tokens.Length > 1 ? int.Parse(tokens[1]) : 0;
                        bool quiesce = tokens.Length > 2 ? tokens[2] == "q" : false;
                        ListBestMove(board, depth, quiesce);
                    }
                    else if (command == "??")
                    {
                        int depth = tokens.Length > 1 ? int.Parse(tokens[1]) : 0;
                        bool quiesce = tokens.Length > 2 ? tokens[2] == "q" : false;
                        ListMoves(board, depth, quiesce);
                    }
                    else if (command == "#")
                    {
                        PrintMoves(board);
                    }
                    else if(command == "see")
                    {
                        Move start = new Move(tokens[1]);
                        Console.WriteLine(Evaluation.SEE(board, start));
                    }
                    else if (command == "pst")
                    {
                        string file = $"pst/{tokens[1]}.pst";
                        PieceSquareTable.Load(File.OpenText(file));
                    }
                    else if (command == "bench")
                    {
                        string mode = tokens[1];                       
                        string file = $"test/{tokens[2]}.txt";
                        switch(mode)
                        {
                            case "mm":
                                RunBenchmark(file, BenchMode.MinMax);
                                break;
                            case "ab":
                                RunBenchmark(file, BenchMode.AlphaBeta);
                                break;
                            case "it":
                                RunBenchmark(file, BenchMode.Iterative);
                                break;
                            case "q":
                                RunBenchmark(file, BenchMode.QSearch);
                                break;
                            case "debug":
                                RunBenchmark(file, BenchMode.Debug);
                                break;
                            default:
                                Console.WriteLine($"Mode {mode} not supported!");
                                break;
                        }
                    }
                    else if (command == "make_bench")
                    {
                        //make_bench 4 benchmark.epd benchMinMaxD4.txt
                        int depth = int.Parse(tokens[1]);
                        string positionSrc = tokens[2];
                        string benchOutput = tokens[3];
                        MakeBenchmark(positionSrc, depth, benchOutput);
                    }
                    else
                    {
                        ApplyMoves(board, tokens);
                    }

                    long t1 = Stopwatch.GetTimestamp();
                    double dt = (t1 - t0) / (double)Stopwatch.Frequency;
                    if(dt > 0.01)
                        Console.WriteLine($"  Operation took {dt:0.####}s");
                }
                //catch (Exception error)
                //{
                //    Console.WriteLine("ERROR: " + error.Message);
                //}
            }
        }

        private static int QEval(Board position)
        {
            return Evaluation.QEval(position, SearchWindow.Infinite);
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
            Console.WriteLine($"  A B C D E F G H");
            Console.WriteLine();
            Console.WriteLine($"  PSTs     {board.Score:+0;-0}");
            Console.WriteLine($"  Quiet    {QEval(board):+0; -0}");
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
            var moves = new LegalMoves(board);
            MoveOrdering.SortMvvLva(moves, board);
            foreach (var move in moves)
            {
                Console.WriteLine($"{i++}. {board.ActiveColor} >> {move}");
                var copy = new Board(board, move);
                Print(copy, move);
                Console.WriteLine();
            }
        }

        private static void ListMoves(Board board, int depth, bool qSearch)
        {
            int i = 1;
            foreach (var move in new LegalMoves(board))
            {
                Board next = new Board(board, move);
                ISearch search;
                if (qSearch)
                    search = new IterativeQSearch(next);
                else
                    search = new FastIterativeSearch(next);

                search.Search(depth-1);
                Move[] line = search.PrincipalVariation;
                if (line != null)
                {
                    string pvString = move + " " + string.Join(' ', line);
                    Console.WriteLine($"{i++,4}. {pvString} = {search.Score:+0.00;-0.00}");
                }
                else
                {
                    int score = qSearch ? QEval(next) : Evaluation.Evaluate(next);
                    Console.WriteLine($"{i++,4}. {move} = {score:+0.00;-0.00}");
                }
            }
        }

        private static void ListBestMove(Board board, int depth, bool qSearch)
        {
            ISearch search;
            if (qSearch)
                search = new DebugSearch(board);
            else
                search = new FastIterativeSearch(board);
            
            search.Search(depth);
            Move[] line = search.PrincipalVariation;

            int i = 1;
            foreach (var move in new LegalMoves(board))
            {
                if (line != null && line[0] == move)
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

        enum BenchMode
        {
            AlphaBeta,
            MinMax,
            Iterative,
            QSearch,
            Debug
        }

        private static void RunBenchmark(string filePath, BenchMode mode)
        {
            var file = File.OpenText(filePath);
            int error = 0;
            int success = 0;
            int line = 1;
            double timeRatioSum = 0;
            double evalRatioSum = 0;
            double moveRatioSum = 0;
            double posRatioSum = 0;
            long moveGenSum = 0;
            long movePlayedSum = 0;
            long tStart = Stopwatch.GetTimestamp();
            long totalEvals = 0;
            while (!file.EndOfStream)
            {
                //The parser expects a fen-string followed by
                //ce = evaluation of best moves
                //bm = best moves
                //acd = search depth
                //acn = nodes evaluated
                //acs = search duration in seconds
                //Example: 1R6/1brk2p1/4p2p/p1P1Pp2/P7/6P1/1P4P1/2R3K1 w - - 0 1;ce 300;bm b8g8;acd 4;acn 144581;acs 0.072

                string entry = file.ReadLine();
                string[] data = entry.Split(';');
                string fen = data[0];
                Dictionary<string, string> props = new Dictionary<string, string>();
                foreach(string prop in data[1..^0])
                {
                    int split = prop.IndexOf(' ');
                    string key = prop.Substring(0, split);
                    string value = prop.Substring(split);
                    props[key] = value;
                }
                if (props.Count != 7)
                {
                    Console.WriteLine($"{line++} SKIPPED! Properties missing. Line: {entry}");
                    continue;
                }

                int depth = int.Parse(props["acd"]);
                int refScore = int.Parse(props["ce"]);
                long refEvalCount = long.Parse(props["evals"]);
                long refMoveCount = long.Parse(props["moves"]);
                long refPosCount = long.Parse(props["positions"]);
                float refTime = Math.Max(0.001f, float.Parse(props["acs"], CultureInfo.InvariantCulture));
                List<Move> refMoves = props["bm"].Split()[1..^0].Select(s => new Move(s)).ToList();

                Move[] moves = null;
                int score = 0;
                long evals = 0;
                long movegens = 0;
                long positions = 0;

                long t0 = Stopwatch.GetTimestamp();
                Board board = new Board(fen);
                switch (mode)
                {
                    case BenchMode.MinMax:
                        BenchMinMax(board, depth, out moves, out score, out evals);
                        break;
                    case BenchMode.AlphaBeta:
                        BenchSearch(new AlphaBetaSearch(board), depth, out moves, out score, out evals, out movegens, out positions);
                        break;
                    case BenchMode.Iterative:
                        BenchSearch(new FastIterativeSearch(board), depth, out moves, out score, out evals, out movegens, out positions);
                        break;
                    case BenchMode.QSearch:
                        BenchSearch(new IterativeQSearch(board), depth, out moves, out score, out evals, out movegens, out positions);
                        break;
                    case BenchMode.Debug:
                        BenchSearch(new DebugSearch(board), depth, out moves, out score, out evals, out movegens, out positions);
                        break;
                    default:
                        return; //Unsupported mode!
                }
                long t1 = Stopwatch.GetTimestamp();
                double dt = (t1 - t0) / (double)Stopwatch.Frequency;

                totalEvals += evals;

                if (!ValidatePV(board, score, moves))
                {
                    error++;
                    Console.WriteLine($"{line++} ERROR! PV validation failed. FEN: {fen}");
                }                
                else if (score != refScore)
                {
                    error++;
                    Console.WriteLine($"{line++} ERROR! ce={score}, expected {refScore}. ");
                }                
                else if(!refMoves.Contains(moves[0]))
                {
                    error++;
                    Console.WriteLine($"{line++} WARNING! bm={string.Join(' ', moves[0])}, expected {string.Join(' ', refMoves)}. FEN: {fen}");
                }
                else //GOOD! but how good?
                {
                    success++;

                    moveGenSum = movegens;
                    movePlayedSum = positions;

                    double timeRatio = dt / refTime;
                    timeRatioSum += timeRatio;

                    double evalRatio = evals / (double)refEvalCount;
                    evalRatioSum += evalRatio;

                    double moveRatio = movegens / (double)refMoveCount;
                    moveRatioSum += moveRatio;

                    double posRatio = positions / (double)refPosCount; 
                    posRatioSum += posRatio;
                    Console.WriteLine($"{line++} OK! eval {evalRatio:0.###}, moves {moveRatio:0.###}, positions {posRatio:0.###} acs {dt:0.###} / {refTime:0.###} = {timeRatio:0.###}.");
                }
                Console.WriteLine();
            }
            double elapsed = (Stopwatch.GetTimestamp() - tStart) / (double)Stopwatch.Frequency;
            moveGenSum = Math.Max(moveGenSum, 1);

            int knps = (int)(totalEvals / elapsed / 1000);
            Console.WriteLine($"Test finished with {error} wrong results! Time: {timeRatioSum/success:0.###}, Eval: {evalRatioSum/success:0.###}, Moves generated: {moveRatioSum / success:0.###}, Moves played: {posRatioSum / success:0.###}, Performance: {knps}kN/sec");
            Console.WriteLine($"A total of {moveGenSum} moves were generated. {movePlayedSum} were played. {(movePlayedSum * 100) / moveGenSum}%");
        }

        private static void BenchMinMax(Board board, int depth, out Move[] moves, out int score, out long nodes)
        {
            Search.ClearStats();
            moves = Search.GetBestMovesMinMax(board, depth, out score).ToArray();
            nodes = Search.PositionsEvaluated;
        }

        private static void BenchSearch(ISearch search, int depth, out Move[] moves, out int score, out long evals, out long movegen, out long positions)
        {
            search.Search(depth);
            moves = search.PrincipalVariation;
            score = search.Score;
            evals = search.NodesVisited;
            positions = search.MovesPlayed;
            movegen = search.MovesGenerated;
        }

        private static bool ValidatePV(Board board, int score, Move[] line)
        {
            Board copy = new Board(board);
            foreach (var move in line)
                copy.Play(move);

            int playedScore = Evaluation.QEval(copy, SearchWindow.Infinite);
            string pvString = string.Join(' ', line);
            if (playedScore != score)
            {
                Console.WriteLine($"PV {pvString} = {playedScore:+0.00;-0.00}, expected {score}");
                Print(copy, line.Last());
                return false;
            }
            else
                Console.WriteLine($"PV {pvString} = {playedScore:+0.00;-0.00}, OK!");

            return true;
        }

        private static void MakeBenchmark(string filePath, int depth, string outputFilePath)
        {
            var source = File.OpenText(filePath);
            var target = new StreamWriter(outputFilePath);
            int line = 1;

            while (!source.EndOfStream)
            {
                //The parser expects a fen-string
                //Startpos in FEN looks like this: "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
                string entry = source.ReadLine();
                string fen = string.Join(' ', entry.Split()[0..4]);
                Console.WriteLine($"{line++}. {fen}");
                Board board = new Board(fen);
                int color = (int)board.ActiveColor;

                long t0 = Stopwatch.GetTimestamp();
                IterativeQSearch search = new IterativeQSearch(board);
                search.Search(depth);
                Move bestMove = search.PrincipalVariation[0];
   
                long pvEval = search.Score;
                long t1 = Stopwatch.GetTimestamp();
                double dt = (t1 - t0) / (double)Stopwatch.Frequency;
                string result = $"ce {pvEval};bm {bestMove};acd {depth};evals {search.NodesVisited};moves {search.MovesGenerated};positions {search.MovesPlayed};acs {dt:0.###}";
                target.WriteLine($"{fen};{result}");
                target.Flush();

                Console.WriteLine();
                Console.WriteLine(result);
                Console.WriteLine();
            }
            target.Close();
            source.Close();
        }
    }
}
