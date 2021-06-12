﻿using MinimalChess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MinimalChessEngine
{
    public static class Uci
    {
        static public void BestMove(Move move)
        {
            Console.WriteLine($"bestmove {move}");
        }

        static internal void Info(int depth, int score, long nodes, int timeMs, Move[] pv)
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

        static public void Log(string message)
        {
            Console.WriteLine($"info string {message}");
        }

        static public void OptionPieceSquareTables(IEnumerable<string> pstFiles, string defaultNameIfPresent)
        {
            if (pstFiles == null || !pstFiles.Any())
                return;

            List<string> pstNames = pstFiles.Select(file => Path.GetFileNameWithoutExtension(file)).ToList();

            string defaultName = pstNames.Contains(defaultNameIfPresent) ? defaultNameIfPresent : pstNames[0];
            string uciOption = $"option name PieceSquareTables type combo default {defaultName}";
            foreach (string pstName in pstNames)
                uciOption += $" var {pstName}";

            Console.WriteLine(uciOption);
        }
    }
}
