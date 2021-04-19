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
        const string WHITE = "1-0";
        const string DRAW = "1/2-1/2";
        const string BLACK = "0-1";

        const double MSE_SCALING = 102; //found via 'tune_mse2' with PeSTO weights on the full quiet-labeled dataset

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
            "data data/quiet-labeled01.epd",
            "data data/quiet-labeled.v7.epd"
        };

        static readonly string[] COMMANDS =
        {
            "reset",
            "data 'file'",
            "dump 'file'",
            "restore 'file'",
            "compare 'fileA' 'fileB'",
            "mse",
            "tune mse",
            "tune mg|eg 'step'[1..N] 'minPhase'[0..1] 'maxPhase'[0..1] 'thresholdScale'[0..N] 'passes'[0..N]",
            "tune tables 'step'[1..N] 'thresholdScale'[0..N] 'passes'[0..N]",
            "tune phase 'step'[1..N] 'passes'[0..N]"
        };

        private static void PrintHeader()
        {
            Console.WriteLine("#Commands:");
            for (int i = 0; i < COMMANDS.Length; i++)
                Console.WriteLine("  " + COMMANDS[i]);
            Console.WriteLine();

            Console.WriteLine("#Shortcuts:");
            for (int i = 0; i < SHORTCUTS.Length; i++)
                Console.WriteLine($"  ({i + 1}) '{SHORTCUTS[i]}'");
            Console.WriteLine();
        }

        //Losely based on: https://www.chessprogramming.org/Texel%27s_Tuning_Method
        static void Main(string[] args)
        {
            PrintHeader();
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
                if (command == "reset")
                {
                    EVAL = new Evaluation();
                }
                else if (command == "data")
                {
                    string epdFile = tokens[1];
                    LoadData(epdFile);
                    PrintDataInfo();
                }
                else if (command == "dump")
                {
                    string pstFile = tokens.Length > 1 ? tokens[1] : "dump";
                    SaveWeights(pstFile);
                }
                else if (command == "restore")
                {
                    string pstFile = tokens.Length > 1 ? tokens[1] : "dump";
                    LoadWeights(pstFile);
                    double error = MeanSquareError();
                    Console.WriteLine($"MeanSquareError(k={MSE_SCALING}): {error}");
                }
                else if(command == "compare")
                {
                    string fileA = tokens[1];
                    string fileB = tokens[2];
                    CompareWeights(fileA, fileB);
                }
                else if (command == "mse")
                {
                    if (tokens.Length == 3)
                    {
                        double minPhase = double.Parse(tokens[1]);
                        double maxPhase = double.Parse(tokens[2]);
                        double error = MeanSquareError(minPhase, maxPhase, out int count);
                        Console.WriteLine($"{(count * 100) / DATA.Count}% of {DATA.Count} positions evaluated!");
                        Console.WriteLine($"MeanSquareError(k={MSE_SCALING}, minPhase={minPhase}, maxPhase={maxPhase}): {error}");
                    }
                    else
                    {
                        double error = MeanSquareError();
                        Console.WriteLine($"{DATA.Count} positions evaluated!");
                        Console.WriteLine($"MeanSquareError(k={MSE_SCALING}): {error}");
                    }
                }
                else if (command == "tune")
                {
                    SaveWeights("dump");
                    string mode = tokens[1];
                    if (mode == "mse")
                    {
                        double k = Minimize(MeanSquareError, 1, 1000);
                        Console.WriteLine($"min_k: {k}, MeanSquareError: {MeanSquareError(k)}");
                    }
                    else if(mode == "mg" || mode == "eg")
                    {
                        int step = int.Parse(tokens[2]);
                        double minPhase = double.Parse(tokens[3]);
                        double maxPhase = double.Parse(tokens[4]);
                        double thresholdScale = double.Parse(tokens[5]);
                        int passCount = int.Parse(tokens[6]);
                        if (mode == "mg")
                            TuneTables(EVAL.MidgameTables, step, minPhase, maxPhase, thresholdScale, passCount, "temp_mg");
                        else if (mode == "eg")
                            TuneTables(EVAL.EndgameTables, step, minPhase, maxPhase, thresholdScale, passCount, "temp_eg");
                    }
                    else if (mode == "phase")
                    {
                        int step = int.Parse(tokens[2]);
                        int passCount = int.Parse(tokens[3]);
                        TunePhase(step, passCount, "temp_phase");
                    }
                    else if(mode=="tables")
                    {
                        int step = int.Parse(tokens[2]);
                        double thresholdScale = double.Parse(tokens[3]);
                        int passCount = int.Parse(tokens[4]);
                        TuneTables(step, thresholdScale, passCount, "temp_tables");
                    }
                    else
                        Console.WriteLine($"Unknown mode {mode}!");
                }

                PrintTime(t0);
                Console.WriteLine();
            }
        }

        private static void TunePhase(int step, int passCount, string tempFileName)
        {
            double startError = MeanSquareError();
            Console.WriteLine($"Tuning step={step} passCap={passCount} on {DATA.Count} positions");
            Console.WriteLine($"#0: {startError} MSE, MG {EVAL.Midgame}..{EVAL.Endgame} EG");

            //Tunable are the phase values and the phase thresholds
            int pass = 0;
            TunePhaseThreshold(step);
            Console.WriteLine($"MG {EVAL.Midgame}..{EVAL.Endgame} EG!");
            while (true)
            {
                pass++;
                int up = 0;
                int down = 0;
                //Exclude Pawns and King from tuning!
                for (int piece = 1; piece < 5; piece++)
                {
                    Console.WriteLine($"PhaseValue[{piece}] = {EVAL.PhaseValues[piece]}, MG {EVAL.Midgame}..{EVAL.Endgame} EG!");
                    int oldMidgame = EVAL.Midgame;
                    int oldEndgame = EVAL.Endgame;

                    double baseError = MeanSquareError();
                    //try raising value
                    EVAL.PhaseValues[piece] += step;
                    TunePhaseThreshold(step);
                    if (MeanSquareError() < baseError)
                    {
                        Console.WriteLine($"PhaseValue[{piece}] += {step}, MG {EVAL.Midgame}..{EVAL.Endgame} EG!");
                        up++;
                        continue;
                    }

                    //try lowering value
                    EVAL.PhaseValues[piece] -= 2 * step;
                    TunePhaseThreshold(step);
                    if (MeanSquareError() < baseError)
                    {
                        Console.WriteLine($"PhaseValue[{piece}] -= {step}, MG {EVAL.Midgame}..{EVAL.Endgame} EG!");
                        down++;
                        continue;
                    }

                    //restore current
                    EVAL.PhaseValues[piece] += step;
                    EVAL.Midgame = oldMidgame;
                    EVAL.Endgame = oldEndgame;
                }
                double currentError = MeanSquareError();
                Console.WriteLine($"#{pass}: {MeanSquareError()} MSE, {up} inc, {down} dec!");
                SaveWeights(tempFileName);

                //DONE?
                if (pass >= passCount || (up == 0 && down == 0))
                {
                    Console.WriteLine($"Done! Eval improved by {startError - currentError}");
                    return;
                }
            }
        }

        private static void TunePhaseThreshold(int step)
        {
            while (true)
            {
                Console.Write('|');
                double baseError = MeanSquareError();
                EVAL.Midgame += step;
                if (MeanSquareError() < baseError)
                    continue;
                EVAL.Midgame -= 2 * step;
                if (MeanSquareError() < baseError)
                    continue;
                EVAL.Midgame += step;

                //try changing endgame
                EVAL.Endgame += step;
                if (MeanSquareError() < baseError)
                    continue;
                EVAL.Endgame -= 2 * step;
                if (MeanSquareError() < baseError)
                    continue;
                EVAL.Endgame += step;
                //both midgame and endgame have stabilized
                break;
            }
            Console.WriteLine();
        }

        private static double GetMeanSquareError(int[,] tables, List<Data> data)
        {
            //Losely based on: https://www.chessprogramming.org/Texel%27s_Tuning_Method
            double squaredErrorSum = 0;
            foreach (Data entry in data)
            {
                int score = Evaluation.Evaluate(entry.Position, tables);
                squaredErrorSum += SquareError(entry.Result, score);
            }
            double result = squaredErrorSum / data.Count;
            return result;
        }

        private static void TuneTables(int[,] tables, int step, double minPhase, double maxPhase, double thresholdScale, int passCap, string tempFileName)
        {
            List<Data> subset = new List<Data>();
            foreach (Data entry in DATA)
            {
                EVAL.Evaluate(entry.Position, out double phase);
                if (phase >= minPhase && phase <= maxPhase)
                    subset.Add(entry);
            }

            double startError = GetMeanSquareError(tables, subset);
            Console.WriteLine($"Tuning step={step} minPhase={minPhase} maxPhase={maxPhase} thresholdScale={thresholdScale} on {(subset.Count * 100) / DATA.Count}% of {DATA.Count} positions");
            Console.WriteLine($"#0: {startError} MSE");

            int pass = 0;
            double threshold = thresholdScale * step / (double)subset.Count;
            while (true)
            {
                pass++;
                int up = 0;
                int down = 0;
                for (int table = 0; table < 6; table++)
                {
                    Console.Write('.');
                    for (int i = 0; i < 64; i++)
                    {
                        double baseError = GetMeanSquareError(tables, subset);
                        //try raising value
                        tables[table, i] += step;
                        if (GetMeanSquareError(tables, subset) - baseError < -threshold)
                        {
                            up++;
                            continue;
                        }
                        //try lowering value
                        tables[table, i] -= 2 * step;
                        if (GetMeanSquareError(tables, subset) - baseError < -threshold)
                        {
                            down++;
                            continue;
                        }
                        //restore current
                        tables[table, i] += step;
                    }
                }

                double currentError = GetMeanSquareError(tables, subset);
                Console.WriteLine($"#{pass}: {currentError} MSE, {up} inc, {down} dec!");
                SaveWeights(tempFileName);

                //DONE?
                if (pass >= passCap || (up == 0 && down == 0))
                {
                    Console.WriteLine($"Done! Eval improved by {startError - currentError}");
                    return;
                }
            }
        }


        private static void TuneTables(int step, double thresholdScale, int passCap, string tempFileName)
        {
            double startError = MeanSquareError();
            Console.WriteLine($"Tuning step={step} thresholdScale={thresholdScale} on {DATA.Count} positions");
            Console.WriteLine($"#0: {startError} MSE");

            int pass = 0;
            double threshold = thresholdScale * step / (double)DATA.Count;
            while (true)
            {
                pass++;
                int up = 0;
                int down = 0;
                for(int phase = 0; phase < 2; phase++)
                {
                    var tables = phase == 0 ? EVAL.MidgameTables : EVAL.EndgameTables;
                    for (int table = 0; table < 6; table++)
                    {
                        Console.Write('.');
                        for (int i = 0; i < 64; i++)
                        {
                            double baseError = MeanSquareError();
                            //try raising value
                            tables[table, i] += step;
                            if (MeanSquareError() - baseError < -threshold)
                            {
                                up++;
                                continue;
                            }
                            //try lowering value
                            tables[table, i] -= 2 * step;
                            if (MeanSquareError() - baseError < -threshold)
                            {
                                down++;
                                continue;
                            }
                            //restore current
                            tables[table, i] += step;
                        }
                    }
                }

                double currentError = MeanSquareError();
                Console.WriteLine($"#{pass}: {currentError} MSE, {up} inc, {down} dec!");
                SaveWeights(tempFileName);

                //DONE?
                if (pass >= passCap || (up == 0 && down == 0))
                {
                    Console.WriteLine($"Done! Eval improved by {startError - currentError}");
                    return;
                }
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

        private static double MeanSquareError(double scalingConstant = MSE_SCALING)
        {
            double squaredErrorSum = 0;
            foreach (Data entry in DATA)
            {
                int score = EVAL.Evaluate(entry.Position, out _);
                squaredErrorSum += SquareError(entry.Result, score);
            }
            double result = squaredErrorSum / DATA.Count;
            return result;
        }

        private static double MeanSquareError(double minPhase, double maxPhase, out int count)
        {
            double squaredErrorSum = 0;
            count = 0;
            foreach (Data entry in DATA)
            {
                int score = EVAL.Evaluate(entry.Position, out double phase);
                if(phase >= minPhase && phase <= maxPhase)
                {
                    squaredErrorSum += SquareError(entry.Result, score);
                    count++;
                }
            }
            double result = squaredErrorSum / count;
            return result;
        }

        private static double SquareError(int reference, int value)
        {
            double sigmoid = 2 / (1 + Math.Exp(-(value / MSE_SCALING))) - 1;
            double error = reference - sigmoid;
            return (error * error);
        }

        private static void LoadData(string epdFile)
        {
            DATA.Clear();
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

        private static void CompareWeights(string fileA, string fileB)
        {
            Console.WriteLine($"Comparing '{fileA}' vs '{fileB}'");
            try
            {
                double[] squaredErrorSumsA = new double[10];
                int[] entryCountsA = new int[10];
                MseBuckets(fileA, squaredErrorSumsA, entryCountsA);

                double[] squaredErrorSumsB = new double[10];
                int[] entryCountsB = new int[10];
                MseBuckets(fileB, squaredErrorSumsB, entryCountsB);

                double totalMseA = 0, totalMseB = 0;
                for (int i = 0; i < 10; i++)
                {
                    double mseA = squaredErrorSumsA[i] / entryCountsA[i];
                    totalMseA += squaredErrorSumsA[i];

                    double mseB = squaredErrorSumsB[i] / entryCountsB[i];
                    totalMseB += squaredErrorSumsB[i];

                    double delta = mseA - mseB;
                    Console.WriteLine($"{entryCountsA[i],10}| {mseA:+0.000000;-0.000000} | {delta:+0.000000;-0.000000} | {mseB:+0.00000;-0.0000000} | {entryCountsB[i]}");
                }
                totalMseA /= DATA.Count;
                totalMseB /= DATA.Count;
                Console.WriteLine("------------------------------------------------------");
                Console.WriteLine($"            {totalMseA:+0.000000;-0.000000} | {(totalMseA- totalMseB):+0.000000;-0.000000} | {totalMseB:+0.00000;-0.0000000}");
            }
            catch (Exception error)
            {
                Console.WriteLine("ERROR: " + error.Message);
            }
        }

        private static void MseBuckets(string fileA, double[] squaredErrorSums, int[] entryCounts)
        {
            Evaluation eval = new Evaluation();
            var reader = File.OpenText(fileA);
            eval.Read(reader);
            reader.Close();

            foreach (Data entry in DATA)
            {
                int score = eval.Evaluate(entry.Position, out double phase);
                for (int i = 0; i < 10; i++)
                    if (phase <= (i + 1) / 10.0)
                    {
                        squaredErrorSums[i] += SquareError(entry.Result, score);
                        entryCounts[i]++;
                        break;
                    }
            }
        }
    }
}
