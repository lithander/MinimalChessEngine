using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MinimalChess
{
    public static class PieceSquareTable
    {
        public static readonly int[,] Tables;

        static PieceSquareTable()
        {
            //14 = Black * [None..King] + White * [None..King]
            Tables = new int[14,64];
            Load(Default);
        }

        public static void Load(string pst)
        {
            Load(new StringReader(pst));
        }

        public static void Load(TextReader reader)
        {
            while(reader.ReadLine() is string line)
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
            for(int rank = 0; rank < 8; rank++)
            {
                string line = reader.ReadLine();
                string[] tokens = line.Split('|');
                if (tokens.Length != 8)
                    throw new Exception($"Exactly 8 numeral tokens expected. '{line}' not valid!");

                for(int file = 0; file < 8; file++)
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
        #PST Source: https://github.com/thomasahle/sunfish/blob/master/sunfish.py

        Pawn 100
          0|  0|  0|   0|   0|   0|  0|   0
         78| 83| 86|  73| 102|  82| 85|  90
          7| 29| 21|  44|  40|  31| 44|   7
        -17| 16| -2|  15|  14|   0| 15| -13
        -26|  3| 10|   9|   6|   1|  0| -23
        -22|  9|  5| -11| -10|  -2|  3| -19
        -31|  8| -7| -37| -36| -14|  3| -31
          0|  0|  0|   0|   0|   0|  0|   0

        Knight 280   
        -66| -53| -75| -75| -10| -55| -58| -70
         -3|  -6| 100| -36|   4|  62|  -4| -14
         10|  67|   1|  74|  73|  27|  62|  -2
         24|  24|  45|  37|  33|  41|  25|  17
         -1|   5|  31|  21|  22|  35|   2|   0
        -18|  10|  13|  22|  18|  15|  11| -14
        -23| -15|   2|   0|   2|   0| -23| -20
        -74| -23| -26| -24| -19| -35| -22| -69

        Bishop 320
        -59| -78| -82| -76| -23|-107| -37| -50
        -11|  20|  35| -42| -39|  31|   2| -22
         -9|  39| -32|  41|  52| -10|  28| -14
         25|  17|  20|  34|  26|  25|  15|  10
         13|  10|  17|  23|  17|  16|   0|   7
         14|  25|  24|  15|   8|  25|  20|  15
         19|  20|  11|   6|   7|   6|  20|  16
         -7|   2| -15| -12| -14| -15| -10| -10

        Rook 479
         35|  29|  33|   4|  37|  33|  56|  50
         55|  29|  56|  67|  55|  62|  34|  60
         19|  35|  28|  33|  45|  27|  25|  15
          0|   5|  16|  13|  18|  -4|  -9|  -6
        -28| -35| -16| -21| -13| -29| -46| -30
        -42| -28| -42| -25| -25| -35| -26| -46
        -53| -38| -31| -26| -29| -43| -44| -53
        -30| -24| -18|   5|  -2| -18| -31| -32

        Queen 929
          6|   1|  -8|-104|  69|  24|  88|  26
         14|  32|  60| -10|  20|  76|  57|  24
         -2|  43|  32|  60|  72|  63|  43|   2
          1| -16|  22|  17|  25|  20| -13|  -6
        -14| -15|  -2|  -5|  -1| -10| -20| -22
        -30|  -6| -13| -11| -16| -11| -16| -27
        -36| -18|   0| -19| -15| -15| -21| -38
        -39| -30| -31| -13| -31| -36| -34| -42

        King 66666
           4|  54|  47| -99| -99|  60|  83| -62
         -32|  10|  55|  56|  56|  55|  10|   3
         -62|  12| -57|  44| -67|  28|  37| -31
         -55|  50|  11|  -4| -19|  13|   0| -49
         -55| -43| -52| -28| -51| -47|  -8| -50
         -47| -42| -43| -79| -64| -32| -29| -32
          -4|   3| -14| -50| -57| -18|  13|   4
          17|  30|  -3| -14|   6|  -1|  40|  18";
    }
}
