﻿using MinimalChess;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MinimalChessEngine
{
    public static class Program
    {
        const string NAME_VERSION = "MinimalChess 0.3";
        const string UCI_DEFAULT_PST = "simple";

        static Engine _engine = new Engine();
        static string[] _pstFiles;

        static async Task Main(string[] args)
        {
            Console.WriteLine(NAME_VERSION);
            Start();
            while (_engine.Running)
            {
                string input = await Task.Run(Console.ReadLine);
                ParseUciCommand(input);
            }
        }

        private static void Start()
        {
            //Scan for PSTs...
            _pstFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.pst", SearchOption.AllDirectories);
            _engine.Start();
        }

        private static void ParseUciCommand(string input)
        {
            //remove leading & trailing whitecases, convert to lower case characters and split using ' ' as delimiter
            string[] tokens = input.Trim().Split();
            switch (tokens[0])
            {
                case "uci":
                    Console.WriteLine($"id name {NAME_VERSION}");
                    Console.WriteLine($"id author Thomas Jahn");
                    Uci.OptionPieceSquareTables(_pstFiles, UCI_DEFAULT_PST);
                    Console.WriteLine("uciok");
                    break;
                case "isready":
                    Console.WriteLine("readyok");
                    break;
                case "position":
                    UciPosition(tokens);
                    break;
                case "go":
                    UciGo(tokens);
                    break;
                case "ucinewgame":
                    break;
                case "stop":
                    _engine.Stop();
                    break;
                case "quit":
                    _engine.Quit();
                    break;
                case "setoption":
                    UciSetOption(tokens);
                    break;
                default:
                    Console.WriteLine("UNKNOWN INPUT " + input);
                    return;
            }
        }

        private static void UciSetOption(string[] tokens)
        {
            if (tokens[1] == "name" && tokens[2] == "PieceSquareTables" && tokens[3] == "value")
            {
                foreach (string pstFile in _pstFiles)
                    if (Path.GetFileNameWithoutExtension(pstFile) == tokens[4])
                    {
                        var stream = File.OpenText(pstFile);
                        PieceSquareTable.Load(stream);
                        return;
                    }
            }
            Console.WriteLine("setoption failed!");
        }

        private static void UciPosition(string[] tokens)
        {
            //position [fen <fenstring> | startpos ]  moves <move1> .... <movei>
            if (tokens[1] == "startpos")
                _engine.SetupPosition(new Board(Board.STARTING_POS_FEN));
            else if (tokens[1] == "fen") //rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
                _engine.SetupPosition(new Board($"{tokens[2]} {tokens[3]} {tokens[4]} {tokens[5]} {tokens[6]} {tokens[7]}"));
            else
            {
                Uci.Log("'position' parameters missing or not understood. Assuming 'startpos'.");
                _engine.SetupPosition(new Board(Board.STARTING_POS_FEN));
            }

            int firstMove = Array.IndexOf(tokens, "moves") + 1;
            if (firstMove == 0)
                return;

            for (int i = firstMove; i < tokens.Length; i++)
            {
                Move move = new Move(tokens[i]);
                _engine.Play(move);
            }
        }

        private static void UciGo(string[] tokens)
        {
            if (TryParse(tokens, "movetime", out int timePerMove))
            {
                //Fixed move time e.g. 5 Minutes per Move = go movetime 300000
                TryParse(tokens, "depth", out int searchDepth, int.MaxValue);
                _engine.Go(timePerMove, searchDepth);
            }
            else if (TryParse(tokens, "btime", out int blackTime) && TryParse(tokens, "wtime", out int whiteTime))
            {
                //Searching on a budget that may increase at certain intervals
                //40 Moves in 5 Minutes = go wtime 300000 btime 300000 movestogo 40
                //40 Moves in 5 Minutes, 1 second increment per Move =  go wtime 300000 btime 300000 movestogo 40 winc 1000 binc 1000 movestogo 40
                //5 Minutes total, no increment (sudden death) = go wtime 300000 btime 300000
                TryParse(tokens, "binc", out int blackIncrement);
                TryParse(tokens, "winc", out int whiteIncrement);
                TryParse(tokens, "movestogo", out int movesToGo, 40); //assuming 30 e.g. spend 1/30th of total budget on the move
                TryParse(tokens, "depth", out int searchDepth, int.MaxValue);
                _engine.Go(blackTime, whiteTime, blackIncrement, whiteIncrement, movesToGo, searchDepth);
            }
            else if (TryParse(tokens, "depth", out int searchDepth))
            {
                _engine.Go(searchDepth);
            }
            else if (IsDefined(tokens, "infinite"))
            {
                //Infinite = go infinite
                _engine.Go();
            }
            else
            {
                Uci.Log("'go' parameters missing or not understood. Stop the search using 'stop'.");
                _engine.Go();
            }
        }

        private static bool IsDefined(string[] tokens, string name)
        {
            return Array.IndexOf(tokens, name) >= 0;
        }

        private static bool TryParse(string[] tokens, string name, out int value, int defaultValue = 0)
        {
            value = defaultValue;
            int iParam = Array.IndexOf(tokens, name);
            if (iParam < 0)
                return false;
            int iValue = iParam + 1;
            if (iValue >= tokens.Length)
                return false;

            return int.TryParse(tokens[iValue], out value);
        }
    }
}