using MinimalChess;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalChessEngine
{
    public class SearchInstance
    {
        UciGui _output = new UciGui();
        IterativeSearch2 _search = null;
        Move _best = default;
        long _t0 = -1;

        public int ElapsedMilliseconds 
        { 
            get
            {
                long t1 = Stopwatch.GetTimestamp();
                double dt = (t1 - _t0) / (double)Stopwatch.Frequency;
                return (int)(1000 * dt);
            }
        }

        public void Update()
        {
            if (_search == null)
            {
                Thread.Sleep(5);
                return;
            }

            if (ElapsedMilliseconds > 500)
            {
                Stop();
                return;
            }

            _best = SearchDeeper();
        }

        private Move SearchDeeper()
        {
            _search.SearchDeeper();

            int score = (int)_search.Position.ActiveColor * _search.Score;
            _output.UciInfo(_search.Depth, score, _search.EvalCount, ElapsedMilliseconds, _search.PrincipalVariation);

            return _search.PrincipalVariation[0];
        }

        internal void Stop()
        {
            if (_search == null)
                return;

            _search = null;
            _output.UciBestMove(_best);
        }

        internal void Init(Board board, List<Board> history)
        {
            _t0 = Stopwatch.GetTimestamp();
            _search = new IterativeSearch2(board, moves => AvoidRepetitionAndRandomize(board, moves, history));
            _best = SearchDeeper();
        }

        private void AvoidRepetitionAndRandomize(Board root, LegalMoves moves, List<Board> history)
        {
            //while there are more then 1 moves iterate backwards and remove those that lead to a repetition
            for(int i = moves.Count - 1; i >= 0 && moves.Count > 1; i--)
            {
                Move move = moves[i];
                Board test = new Board(root, move);
                //is the board in the history? skip the move!
                if (history.Contains(test))
                {
                    Console.WriteLine($"Move {move} would repeat a position. Skipped!");
                    moves.RemoveAt(i);
                }
            }
            moves.Randomize();
        }
    }

    public class UciGui
    {
        public void UciBestMove(Move move)
        {
            Console.WriteLine($"bestmove {move}");
        }

        internal void UciInfo(int depth, int score, long nodes, int timeMs, Move[] pv)
        {
            int nps = (int)(nodes / (timeMs / 1000.0));
            Console.WriteLine($"info depth {depth} score cp {score} nodes {nodes} nps {nps} time {timeMs} pv {string.Join(' ', pv)}");
        }
    }

    public static class Program
    {
        static bool _running = true;
        static Board _board = new Board(Board.STARTING_POS_FEN);
        static SearchInstance _search = new SearchInstance();
        static List<Board> _history = new List<Board>();

        static void Main(string[] args)
        {
            ConcurrentQueue<string> input = new ConcurrentQueue<string>();
            var readConsole = new Thread(() => ReadConsole(input));
            readConsole.Start();

            Console.WriteLine("MinimalChess 0.2");
            while (_running)
            {
                while (input.Count > 0)
                    if (input.TryDequeue(out string cmd))
                        ParseCommand(cmd);

                _search.Update();
            }
            readConsole.Abort();
        }

        private static Action ReadConsole(ConcurrentQueue<string> input)
        {
            while(true)
            {
                input.Enqueue(Console.ReadLine());
                Thread.Sleep(100);
            }
        }

        private static void ParseCommand(string input)
        {
            string[] tokens = input.Split();
            switch (tokens[0])
            {
                case "uci":
                    Console.WriteLine("id name MinimalChess");
                    Console.WriteLine("id author Thomas Jahn");
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
                    UciStop();
                    break;
                case "quit":
                    _running = false;
                    break;
                default:
                    Console.WriteLine("UNKNOWN INPUT " + input);
                    return;
            }
        }

        private static void UciStop()
        {
            _search.Stop();
        }

        private static void UciGo(string[] tokens)
        {
            _search.Init(_board, _history);
        }

        private static void UciPosition(string[] tokens)
        {
            //position [fen <fenstring> | startpos ]  moves <move1> .... <movei>
            if (tokens[1] == "startpos")
                _board = new Board(Board.STARTING_POS_FEN);
            else if (tokens[1] == "fen") //rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
                _board = new Board($"{tokens[2]} {tokens[3]} {tokens[4]} {tokens[5]} {tokens[6]} {tokens[7]}");

            int firstMove = Array.IndexOf(tokens, "moves") + 1;
            if (firstMove == 0)
                return;

            _history.Clear();
            _history.Add(new Board(_board));
            for (int i = firstMove; i < tokens.Length; i++)
            {
                Move move = new Move(tokens[i]);
                Piece captured = _board.Play(move);
                if (captured != Piece.None)
                    _history.Clear();//after capture the previous positions can't be replicated anyway so we don't need to remember them

                _history.Add(new Board(_board));
            }
        }
    }
}
