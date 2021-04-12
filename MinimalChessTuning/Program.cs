using MinimalChess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace MinimalChessTuning
{
    class Program
    {
        const string NAME_VERSION = "MinimalChessTuning 0.0.1";

        const string WHITE = "1-0";
        const string DRAW = "1/2-1/2";
        const string BLACK = "0-1";

        const double MSE_SCALING = 235; //found via 'tune_mse' with PeSTO weights on the full quiet-labeled dataset
        const double MSE2_SCALING = 102; //found via 'tune_mse2' with PeSTO weights on the full quiet-labeled dataset

        struct Data
        {
            public Board Position;
            public sbyte Result;
        }

        static List<Data> DATA = new List<Data>(1000000);
        static Evaluation EVAL = new Evaluation();

        static readonly string[] SHORTCUTS = 
        {
            "data data/quiet-labeled.epd",
            "data data/quiet-labeled01.epd"
        };                

        //http://www.talkchess.com/forum3/viewtopic.php?t=63408
        static void Main(string[] args)
        {
            Console.WriteLine(NAME_VERSION);
            Console.WriteLine();

            //List available shortcuts
            Console.WriteLine("<<< CMD Shortcuts >>>");
            for (int i = 0; i < SHORTCUTS.Length; i++)
                Console.WriteLine($"{i+1}) '{SHORTCUTS[i]}'");
            Console.WriteLine();

            //REPL
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            while (true)
            {
                Console.Write(">>");
                string input = Console.ReadLine().Trim();

                //Handle Shortcuts
                if (int.TryParse(input, out int i) && i > 0 && i <= SHORTCUTS.Length)
                    input = SHORTCUTS[i - 1];

                long t0 = Stopwatch.GetTimestamp();
                string[] tokens = input.Split();
                string command = tokens[0];
                if (command == "data")
                {
                    string epdFile = tokens[1];
                    LoadData(epdFile);
                }
                else if (command == "info")
                {
                    PrintDataInfo();
                }
                else if (command == "mse")
                {
                    double k = tokens.Length > 1 ? double.Parse(tokens[1]) : MSE_SCALING;
                    double error = MeanSquareError(k);
                    Console.WriteLine($"MeanSquareError(k={k}): {error}");
                }
                else if (command == "mse2")
                {
                    double k = tokens.Length > 1 ? double.Parse(tokens[1]) : MSE2_SCALING;
                    double error = MeanSquareError2(k);
                    Console.WriteLine($"MeanSquareError2(k={k}): {error}");
                }
                else if (command == "tune_mse")
                {
                    double k = Minimize(MeanSquareError, 1, 1000);
                    Console.WriteLine($"min_k: {k}, MeanSquareError: {MeanSquareError(k)}");
                }
                else if (command == "tune_mse2")
                {
                    double k = Minimize(MeanSquareError2, 1, 1000);
                    Console.WriteLine($"min_k: {k}, MeanSquareError: {MeanSquareError2(k)}");
                }
                else if(command == "dump")
                {
                    string pstFile = tokens.Length > 1 ? tokens[1] : "dump.txt";
                    SaveWeights(pstFile);
                }
                else if (command == "weights")
                {
                    string pstFile = tokens.Length > 1 ? tokens[1] : "dump.txt";
                    LoadWeights(pstFile);
                }

                PrintTime(t0);
                Console.WriteLine();
            }
        }

        private static double Minimize(Func<double, double> func, double range0, double range1, double precision = 0.1)
        {
            Console.WriteLine($"[{range0}..{range1}]");
            double step = (range1 - range0) / 10.0;
            double min_k = range0;
            double min = func(min_k);
            for (double k = range0; k < range1; k += step)
            {
                double y = func(k);
                Console.WriteLine($"k: {k}, f(k): {y}");
                if(y < min)
                {
                    min = y;
                    min_k = k;
                }
            }
            if (step < 0.1)
                return min_k;
            
            return Minimize(func, min_k - step, min_k + step);
        }

        private static void PrintTime(long t0)
        {
            long t1 = Stopwatch.GetTimestamp();
            double dt = (t1 - t0) / (double)Stopwatch.Frequency;
            Console.WriteLine($"-- Operation took {dt:0.####}s");
        }

        private static void PrintDataInfo()
        {
            int whiteWin = 0, blackWin = 0, draw = 0;
            foreach (Data entry in DATA)
            {
                switch (entry.Result)
                {
                    case 1:
                        whiteWin++;
                        break;
                    case 0:
                        draw++;
                        break;
                    case -1:
                        blackWin++;
                        break;
                }
            }
            //Print stats like in CuteChess CLI
            float winRate = (whiteWin + 0.5f * draw) / DATA.Count;
            Console.WriteLine($"{whiteWin} - {blackWin} - {draw} [{winRate:0.###}] {DATA.Count}");
        }

        private static double MeanSquareError(double k = MSE_SCALING)
        {
            Random rnd = new Random();
            double squaredErrorSum = 0;
            foreach(Data entry in DATA)
            {
                int score = EVAL.Evaluate(entry.Position);
                Debug.Assert(score == entry.Position.Score);
                //int score = Evaluation.Material(entry.Position);
                //int score = rnd.Next(-1000, 1000);
                //sigmoid
                double sigmoid = 2 / (1 + Math.Pow(10, -(score / k))) - 1;
                double error = entry.Result - sigmoid;
                //Console.WriteLine($"Result {entry.Result}, Score {score}, Eval {sigmoid}, Error {error}");
                squaredErrorSum += (error * error);
            }
            double result = squaredErrorSum / DATA.Count;
            return result;
        }

        private static double MeanSquareError2(double k = MSE2_SCALING)
        {
            //Losely based on: https://www.chessprogramming.org/Texel%27s_Tuning_Method
            //but we're not using 10 as base e.g Math.Pow(10, ...) we'll use Math.Exp because it's faster.
            //...the scaling factor needs to adapted to compensate for that 10^-(x/400) ~= e^-(x/174)
            double squaredErrorSum = 0;
            foreach (Data entry in DATA)
            {
                int score = EVAL.Evaluate(entry.Position);
                Debug.Assert(score == entry.Position.Score);
                //sigmoid
                double sigmoid = 2 / (1 + Math.Exp(-(score / k))) - 1;
                double error = entry.Result - sigmoid;
                squaredErrorSum += (error * error);
            }
            double result = squaredErrorSum / DATA.Count;
            return result;
        }

        private static void LoadData(string epdFile)
        {
            Console.WriteLine($"Loading DATA from '{epdFile}'");
            var file = File.OpenText(epdFile);
            while (!file.EndOfStream)
            {
                //Expected Format:
                //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
                //Labels: "1/2-1/2", "1-0", "0-1"
                string line = file.ReadLine();
                int iLabel = line.IndexOf('"');
                string fen = line.Substring(0, iLabel - 1);
                string label = line.Substring(iLabel + 1, line.Length - iLabel - 3);
                Debug.Assert(label == BLACK || label == WHITE || label == DRAW);
                int result = (label == WHITE) ? 1 : (label == BLACK) ? -1 : 0;
                Data entry = new Data
                {
                    Position = new Board(fen),
                    Result = (sbyte)result
                };
                DATA.Add(entry);
            }
            Console.WriteLine($"{DATA.Count} labeled positions loaded!");
        }


        private static void LoadWeights(string pstFile)
        {
            Console.WriteLine($"Loading EVAL from '{pstFile}'");
            try
            {
                var reader = File.OpenText(pstFile);
                EVAL.Read(reader);
                reader.Close();
            }
            catch (Exception error)
            {
                Console.WriteLine("ERROR: " + error.Message);
            }
        }

        private static void SaveWeights(string pstFile)
        {
            Console.WriteLine($"Writing EVAL to '{pstFile}'");
            var writer = new StreamWriter(pstFile);
            EVAL.Write(writer);
            writer.Close();
        }
    }
}
