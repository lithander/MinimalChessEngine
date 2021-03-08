using System;
using System.IO;

namespace MinimalChess
{
    public static class PieceSquareTable
    {
        public static int LostValue => -9999;

        public static readonly int[,] Tables;

        static PieceSquareTable()
        {
            //14 = Black * [None..King] + White * [None..King]
            Tables = new int[14, 64];
            Load(Default);
        }

        public static void Load(string pst)
        {
            Load(new StringReader(pst));
        }

        public static void Load(TextReader reader)
        {
            while (reader.ReadLine() is string line)
            {
                line = line.Trim();
                if (line == "" || line.StartsWith('#'))
                    continue;
                string[] tokens = line.Split();
                if (tokens.Length != 2)
                    throw new Exception($"Exactly 2 tokens 'PieceType Value' expected. '{line}' not valid!");
                string pieceType = tokens[0].ToUpperInvariant();
                int value = int.Parse(tokens[1]);
                switch (pieceType)
                {
                    case "PAWN":
                        ParseTable(Piece.Pawn, value, reader); break;
                    case "KNIGHT":
                        ParseTable(Piece.Knight, value, reader); break;
                    case "BISHOP":
                        ParseTable(Piece.Bishop, value, reader); break;
                    case "ROOK":
                        ParseTable(Piece.Rook, value, reader); break;
                    case "QUEEN":
                        ParseTable(Piece.Queen, value, reader); break;
                    case "KING":
                        ParseTable(Piece.King, value, reader); break;
                    default:
                        throw new Exception($"PieceType {pieceType} not recognized!");
                }
            }
        }

        private static void ParseTable(Piece piece, int pieceValue, TextReader reader)
        {
            //read 8 lines for the 8 ranks of the board
            for (int rank = 0; rank < 8; rank++)
            {
                string line = reader.ReadLine();
                string[] tokens = line.Split('|');
                if (tokens.Length != 8)
                    throw new Exception($"Exactly 8 numeral tokens expected. '{line}' not valid!");

                for (int file = 0; file < 8; file++)
                {
                    int squareValueOffset = int.Parse(tokens[file]);
                    int pieceSquareValue = pieceValue + squareValueOffset;
                    //square indices in the piece table
                    int iBlackSquare = rank * 8 + file;
                    int iWhiteSquare = (7 - rank) * 8 + file;
                    Tables[PieceTableIndex(piece | Piece.Black), iBlackSquare] = -pieceSquareValue;
                    Tables[PieceTableIndex(piece | Piece.White), iWhiteSquare] = (pieceValue + squareValueOffset);
                }
            }
        }

        //strip the first bit and
        private static int PieceTableIndex(Piece piece) => ((int)piece >> 1);

        public static int Value(Piece piece, int squareIndex)
        {
            return Tables[PieceTableIndex(piece), squareIndex];
        }

        public static int Evaluate(Board board)
        {
            int score = 0;
            for (int i = 0; i < 64; i++)
                score += Tables[PieceTableIndex(board[i]), i];
            return score;
        }

        const string Default = @"
        #Source: https://www.chessprogramming.org/Simplified_Evaluation_Function
        #But I rewrote the King's table because I don't support mg/eg distinction

        Pawn 100
         0|  0|  0|  0|  0|  0|  0|  0
        50| 50| 50| 50| 50| 50| 50| 50
        10| 10| 20| 30| 30| 20| 10| 10
         5|  5| 10| 25| 25| 10|  5|  5
         0|  0|  0| 20| 20|  0|  0|  0
         5| -5|-10|  0|  0|-10| -5|  5
         5| 10| 10|-20|-20| 10| 10|  5
         0|  0|  0|  0|  0|  0|  0|  0

        Knight 320   
        -50|-40|-30|-30|-30|-30|-40|-50
        -40|-20|  0|  0|  0|  0|-20|-40
        -30|  0| 10| 15| 15| 10|  0|-30
        -30|  5| 15| 20| 20| 15|  5|-30
        -30|  0| 15| 20| 20| 15|  0|-30
        -30|  5| 10| 15| 15| 10|  5|-30
        -40|-20|  0|  5|  5|  0|-20|-40
        -50|-40|-30|-30|-30|-30|-40|-50

        Bishop 330
        -20|-10|-10|-10|-10|-10|-10|-20
        -10|  0|  0|  0|  0|  0|  0|-10
        -10|  0|  5| 10| 10|  5|  0|-10
        -10|  5|  5| 10| 10|  5|  5|-10
        -10|  0| 10| 10| 10| 10|  0|-10
        -10| 10| 10| 10| 10| 10| 10|-10
        -10|  5|  0|  0|  0|  0|  5|-10
        -20|-10|-10|-10|-10|-10|-10|-20

        Rook 500
          0|  0|  0|  0|  0|  0|  0|  0
          5| 10| 10| 10| 10| 10| 10|  5
         -5|  0|  0|  0|  0|  0|  0| -5
         -5|  0|  0|  0|  0|  0|  0| -5
         -5|  0|  0|  0|  0|  0|  0| -5
         -5|  0|  0|  0|  0|  0|  0| -5
         -5|  0|  0|  0|  0|  0|  0| -5
          0|  0|  0|  5|  5|  0|  0|  0

        Queen 900
        -20|-10|-10| -5| -5|-10|-10|-20
        -10|  0|  0|  0|  0|  0|  0|-10
        -10|  0|  5|  5|  5|  5|  0|-10
         -5|  0|  5|  5|  5|  5|  0| -5
          0|  0|  5|  5|  5|  5|  0| -5
        -10|  5|  5|  5|  5|  5|  0|-10
        -10|  0|  5|  0|  0|  0|  0|-10
        -20|-10|-10| -5| -5|-10|-10|-20

        King 66666
        -10| 10|  10| -20| -20|  10| 10| -10
        -20|  0|  20|  20|  20|  20|  0| -20
        -30|  0|   0|  10|   0|   0|  0| -30
        -30|  0|   0|   0|   0|   0|  0| -30
        -30|-10|   0|   0|   0|   0|-10| -30
        -20|-20| -20| -30| -30| -20|-20| -20
          0|  0| -10| -30| -30| -10|  0|   0
         10| 20|  10| -10| -10|   0| 20|  10";
    }
}
