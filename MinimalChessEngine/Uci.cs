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
            
            Console.WriteLine($"info depth {depth} score {ScoreToString(score)} nodes {nodes} nps {nps} time {timeMs} pv {string.Join(' ', pv)}");
        }

        private static string ScoreToString(int score)
        {
            if(Evaluation.IsCheckmate(score))
            {
                int sign = Math.Sign(score);
                int moves = Evaluation.GetMateDistance(score);               
                return $"mate {sign * moves}";
            }

            return $"cp {score}";
        }

        public static void Log(string message)
        {
            Console.WriteLine($"info string {message}");
        }
    }
}
