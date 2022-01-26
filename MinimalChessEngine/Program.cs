using MinimalChess;
using System;
using System.Runtime;
using System.Threading.Tasks;

namespace MinimalChessEngine
{
    public static class Program
    {
        const string NAME_VERSION = "MinimalChess 0.6.2";
        const string AUTHOR = "Thomas Jahn";

        static Engine _engine = new Engine();

        private static async Task Main()
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
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            _engine.Start();
        }

        private static void ParseUciCommand(string input)
        {
            //remove leading & trailing whitecases and split using ' ' as delimiter
            string[] tokens = input.Trim().Split();
            switch (tokens[0])
            {
                case "uci":
                    Console.WriteLine($"id name {NAME_VERSION}");
                    Console.WriteLine($"id author {AUTHOR}");
                    Console.WriteLine($"option name Hash type spin default {Transpositions.DEFAULT_SIZE_MB} min 1 max 2047");//consider gcAllowVeryLargeObjects if larger TT is needed
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
                    Transpositions.Clear();
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
            if (tokens[1] == "name" && tokens[2] == "Hash" && tokens[3] == "value" && int.TryParse(tokens[4], out int hashSizeMBytes))
                Transpositions.Resize(hashSizeMBytes);
        }

        private static void UciPosition(string[] tokens)
        {
            //position [fen <fenstring> | startpos ]  moves <move1> .... <movei>
            if (tokens[1] == "startpos")
                _engine.SetupPosition(new Board(Board.STARTING_POS_FEN));
            else if (tokens[1] == "fen") //rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
            {
                string fen = string.Join(' ', tokens[2..]);
                _engine.SetupPosition(new Board(fen));
            }
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
            //Searching on a budget that may increase at certain intervals
            //40 Moves in 5 Minutes = go wtime 300000 btime 300000 movestogo 40
            //40 Moves in 5 Minutes, 1 second increment per Move =  go wtime 300000 btime 300000 movestogo 40 winc 1000 binc 1000 movestogo 40
            //5 Minutes total, no increment (sudden death) = go wtime 300000 btime 300000

            TryParse(tokens, "depth", out int maxDepth, 99);
            TryParse(tokens, "movetime", out int maxTime, int.MaxValue);
            TryParse(tokens, "nodes", out long maxNodes, long.MaxValue);
            TryParse(tokens, "movestogo", out int movesToGo, 40); //assuming 30 e.g. spend 1/30th of total budget on the move

            if (_engine.SideToMove == Color.White && TryParse(tokens, "wtime", out int whiteTime))
            {
                TryParse(tokens, "winc", out int whiteIncrement);
                _engine.Go(whiteTime, whiteIncrement, movesToGo, maxDepth, maxNodes);
            }
            else if (_engine.SideToMove == Color.Black && TryParse(tokens, "btime", out int blackTime))
            {
                TryParse(tokens, "binc", out int blackIncrement);
                _engine.Go(blackTime, blackIncrement, movesToGo, maxDepth, maxNodes);
            }
            else
            {
                //Searching infinite within optional constraints
                _engine.Go(maxDepth, maxTime, maxNodes);
            }
        }

        private static bool TryParse(string[] tokens, string name, out int value, int defaultValue = 0)
        {
            if (int.TryParse(Token(tokens, name), out value))
                return true;
            //token couldn't be parsed. use default value
            value = defaultValue;
            return false;
        }

        private static bool TryParse(string[] tokens, string name, out long value, long defaultValue = 0)
        {
            if (long.TryParse(Token(tokens, name), out value))
                return true;
            //token couldn't be parsed. use default value
            value = defaultValue;
            return false;
        }

        private static string Token(string[] tokens, string name)
        {
            int iParam = Array.IndexOf(tokens, name);
            if (iParam < 0) return null;

            int iValue = iParam + 1;
            return (iValue < tokens.Length) ? tokens[iValue] : null;
        }
    }
}