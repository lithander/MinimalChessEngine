using MinimalChess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MinimalChessEngine
{
    public static class Program
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
                        UciPosition(tokens);
                        break;
                    case "go":
                        string move = UciBestMove(tokens);
                        Console.WriteLine($"bestmove {move}");
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

        static Board _board = null;

        private static void UciPosition(string[] tokens)
        {
            //position [fen <fenstring> | startpos ]  moves <move1> .... <movei>
            if (tokens[1] == "startpos")
                _board = new Board(Board.STARTING_POS_FEN);
            else if(tokens[1] == "fen") //rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
                _board = new Board($"{tokens[2]} {tokens[3]} {tokens[4]} {tokens[5]} {tokens[6]} {tokens[7]}");

            int firstMove = Array.IndexOf(tokens, "moves") + 1;
            if (firstMove == 0)
                return;

            for (int i = firstMove; i < tokens.Length; i++)
            {
                Move move = new Move(tokens[i]);
                _board.Play(move);
            }
        }

        private static string UciBestMove(string[] tokens)
        {
            //start calculating on the current position set up with the "position" command.
            //(ignoring the parameters)
            return Search.GetBestMove(_board).ToString();
            //return new LegalMoves(_board).GetRandom().ToString();
        }
    }
}
