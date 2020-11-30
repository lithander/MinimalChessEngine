using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MinimalChessEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            bool running = true;
            while (running)
            {
                string input = Console.ReadLine();
                string[] tokens = input.Split();
                switch (tokens[0])
                {
                    case "uci":
                        Console.WriteLine("id name MinimalChessEngine");
                        Console.WriteLine("uciok");
                        break;
                    case "isready":
                        Console.WriteLine("readyok");
                        break;
                    case "position":
                        break;
                    case "go":
                        Console.WriteLine("bestmove e2e4");
                        break;
                    case "ucinewgame":
                        break;
                    case "stop":
                        break;
                    case "quit":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("UNKNOWN INPUT " + input);
                        running = false;
                        break;
                }
            }
        }
    }
}
