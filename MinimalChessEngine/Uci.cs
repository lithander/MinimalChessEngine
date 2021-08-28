using MinimalChess;
using System;

namespace MinimalChessEngine
{
    public static class Uci
    {
        public static void BestMove(Move move)
        {
            Console.WriteLine($"bestmove {move}");
        }

        public static void Info(int depth, int score, long nodes, int timeMs, Move[] pv)
        {
            double tS = Math.Max(1, timeMs) / 1000.0;
            int nps = (int)(nodes / tS);
            
            Console.WriteLine($"info depth {depth} score {ScoreToString(score, pv.Length)} nodes {nodes} nps {nps} time {timeMs} pv {string.Join(' ', pv)}");
        }

        private static string ScoreToString(int score, int plies)
        {
            if(!Evaluation.IsCheckmate(score))
                return $"cp {score}";

            //return mate in Y moves, not plies.
            int moves = (plies + 1) / 2;
            //if the engine is getting mated use negative values for Y.
            return $"mate {Math.Sign(score) * moves}";
        }

        public static void Log(string message)
        {
            Console.WriteLine($"info string {message}");
        }
    }
}
