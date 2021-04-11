using MinimalChess;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace MinimalChessTuning
{
    class Program
    {
        const string NAME_VERSION = "MinimalChessTuning 0.0.1";

        static readonly string[] SHORTCUTS = 
        {
            "error data/quiet-labeled.epd"
        };

        //http://www.talkchess.com/forum3/viewtopic.php?t=63408
        static void Main(string[] args)
        {
            Console.WriteLine(NAME_VERSION);
            Console.WriteLine();

            //Offer Shortcut
            Console.WriteLine("<<< CMD Shortcuts >>>");
            for (int i = 0; i < SHORTCUTS.Length; i++)
                Console.WriteLine($"{i+1}) '{SHORTCUTS[i]}'");
                
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            while (true)
            {
                string input = Console.ReadLine().Trim();
                
                //Handle Shortcuts
                if (int.TryParse(input, out int i) && i > 0 && i <= SHORTCUTS.Length)
                    input = SHORTCUTS[i-1];

                long t0 = Stopwatch.GetTimestamp();
                string[] tokens = input.Split();
                string command = tokens[0];
                if(command == "error")
                {
                    string epdFile = tokens[1];
                    double error = GetAverageEvalError(epdFile);
                }
                long t1 = Stopwatch.GetTimestamp();
                double dt = (t1 - t0) / (double)Stopwatch.Frequency;
                if (dt > 0.01)
                    Console.WriteLine($"  Operation took {dt:0.####}s");
            }
        }

        const string WHITE = "1-0";
        const string DRAW = "1/2-1/2";
        const string BLACK = "0-1";

        private static double GetAverageEvalError(string epdFile)
        {
            var file = File.OpenText(epdFile);
            int entryCount = 0;
            int whiteWin = 0, blackWin = 0, draw = 0;
            Board board = new Board();
            double errorSum = 0;
            double squaredErrorSum = 0;
            while (!file.EndOfStream)
            {
                //Expected Format:
                //rnb1kbnr/pp1pppp1/7p/2q5/5P2/N1P1P3/P2P2PP/R1BQKBNR w KQkq - c9 "1/2-1/2";
                //Labels: "1/2-1/2", "1-0", "0-1"
                string entry = file.ReadLine();
                int iLabel = entry.IndexOf('"');
                string fen = entry.Substring(0, iLabel-1);
                string label = entry.Substring(iLabel+1, entry.Length-iLabel-3);
                entryCount++;
                int result = 0;
                if (label == WHITE)
                {
                    whiteWin++;
                    result = 1;
                }
                else if (label == DRAW)
                {
                    draw++;
                    result = 0;
                }
                else if (label == BLACK)
                {
                    blackWin++;
                    result = -1;
                }

                board.SetupPosition(fen);
                int score = board.Score;
                //sigmoid
                double sigmoid = 2 / (1 + Math.Pow(10, -(score / 400.0))) - 1;
                double error = result - sigmoid;
                errorSum += error;
                squaredErrorSum += Math.Sign(error) * (error * error);
                //Console.WriteLine($"Result {result}, Score {score}, Error {error}");
            }
            float winRate = (whiteWin + 0.5f * draw) / entryCount;
            //Print stats like in CuteChess CLI
            Console.WriteLine($"{whiteWin} - {blackWin} - {draw} [{winRate:0.###}] {entryCount}");
            Console.WriteLine($"MSE: {squaredErrorSum / entryCount} ME:{errorSum / entryCount}");
            return 0;
        }
    }
}
