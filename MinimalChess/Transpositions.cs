using System;

namespace MinimalChess
{
    public static class Transpositions
    {
        public enum ScoreType : byte
        {
            GreaterOrEqual,
            LessOrEqual,
            Exact
        }

        public struct HashEntry
        {
            public ulong Hash;       //8 Bytes
            public short Score;      //2 Bytes
            public byte Depth;       //1 Byte
            public byte Age;         //1 Byte
            public ScoreType Type;   //1 Byte
            public Move BestMove;    //3 Bytes
            //==================================
            //                        16 Bytes
        }

        public const short HISTORY = 255;
        public const int DEFAULT_SIZE_MB = 50;
        const int ENTRY_SIZE = 16; //BYTES
        static HashEntry[] _table;

        static bool Index(in ulong hash, out int index)
        {
            index = (int)(hash % (ulong)_table.Length);
            if (_table[index].Hash != hash)
                index ^= 1; //try other slot

            if (_table[index].Hash != hash)
                return false; //both slots missed

            //a table hit resets the age
            _table[index].Age = 0;
            return true;
        }

        static int Index(in ulong hash)
        {
            int index = (int)(hash % (ulong)_table.Length);
            ref HashEntry e0 = ref _table[index];
            ref HashEntry e1 = ref _table[index ^ 1];

            if (e0.Hash == hash)
                return index;

            if (e1.Hash == hash)
                return index ^ 1;

            //raise age of both and choose the older, shallower entry!
            return (++e0.Age - e0.Depth) > (++e1.Age - e1.Depth) ? index : index ^ 1;
        }

        static Transpositions()
        {
            Resize(DEFAULT_SIZE_MB);
        }

        public static void Resize(int hashSizeMBytes)
        {
            int length = (hashSizeMBytes * 1024 * 1024) / ENTRY_SIZE;
            _table = new HashEntry[length];
        }

        public static void Clear()
        {
            Array.Clear(_table, 0, _table.Length);
        }

        public static void Store(ulong zobristHash, int depth, int ply, SearchWindow window, int score, Move bestMove)
        {
            ref HashEntry entry = ref _table[Index(zobristHash)];

            //don't overwrite a bestmove with 'default' unless it's a new position
            if (entry.Hash != zobristHash || bestMove != default)
                entry.BestMove = bestMove;

            entry.Hash = zobristHash;
            entry.Depth = depth < 0 ? default : (byte)depth;
            entry.Age = 0;

            //a checkmate score is reduced by the number of plies from the root so that shorter mates are preferred
            //but when we talk about a position being 'mate in X' then X is independent of the root distance. So we store
            //the score relative to the position by adding the current ply to the encoded mate distance (from the root).
            if (Evaluation.IsCheckmate(score))
                score += Math.Sign(score) * ply;

            if (score >= window.Ceiling)
            {
                entry.Type = ScoreType.GreaterOrEqual;
                entry.Score = (short)window.Ceiling;
            }
            else if (score <= window.Floor)
            {
                entry.Type = ScoreType.LessOrEqual;
                entry.Score = (short)window.Floor;
            }
            else
            {
                entry.Type = ScoreType.Exact;
                entry.Score = (short)score;
            }
        }

        public static bool GetBestMove(Board position, out Move bestMove)
        {
            bestMove = Index(position.ZobristHash, out int index) ? _table[index].BestMove : default;
            return bestMove != default;
        }

        public static bool GetScore(ulong zobristHash, int depth, int ply, SearchWindow window, out int score)
        {
            score = 0;
            if (!Index(zobristHash, out int index))
                return false;

            ref HashEntry entry = ref _table[index];
            if (entry.Depth < depth)
                return false;

            score = entry.Score;

            //a checkmate score is reduced by the number of plies from the root so that shorter mates are preferred
            //but when we store it in the TT the score is made relative to the current position. So when we want to 
            //retrieve the score we have to subtract the current ply to make it relative to the root again.
            if (Evaluation.IsCheckmate(score))
                score -= Math.Sign(score) * ply;

            //1.) score is exact and within window
            if (entry.Type == ScoreType.Exact)
                return true;
            //2.) score is below floor
            if (entry.Type == ScoreType.LessOrEqual && score <= window.Floor)
                return true; //failLow
            //3.) score is above ceiling
            if (entry.Type == ScoreType.GreaterOrEqual && score >= window.Ceiling)
                return true; //failHigh

            return false;
        }
    }
}