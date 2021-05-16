using System;

namespace MinimalChess
{
    public class Evaluation
    {
        public const int Checkmate = -9999;

        private static int PieceTableIndex(Piece piece) => ((int)piece >> 2) - 1;

        //a black piece has 2nd bit set, white has not. Square ^ 56 flips the file, so that tables work for black
        private static int SquareTableIndex(int square, Piece piece) => square ^ (28 * ((int)piece & 2));

        public static int Evaluate(Board board)
        {
            int midGame = 0;
            int endGame = 0;
            int phaseValue = 0;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = board[i];
                if (piece == Piece.None)
                    continue;

                Color color = Pieces.GetColor(piece);
                int pieceIndex = PieceTableIndex(piece);
                int squareIndex = SquareTableIndex(i, piece);
                phaseValue += PhaseValues[pieceIndex];
                midGame += (int)color * MidgameTables[pieceIndex, squareIndex];
                endGame += (int)color * EndgameTables[pieceIndex, squareIndex];
            }

            //linearily interpolate between midGame and endGame score based on current phase (tapered eval)
            double phase = Linstep(Midgame, Endgame, phaseValue);
            double score = midGame + phase * (endGame - midGame);
            return (int)score;
        }

        public static double Linstep(double edge0, double edge1, double value)
        {
            return Math.Min(1, Math.Max(0, (value - edge0) / (edge1 - edge0)));
        }

        /*
        Added dynamic mobility component to 'chillipepper003' creating 'mobility_one' by evaluating 725000 positions from "quiet-labeled.epd" 
        MeanSquareError(k=102): 0.23988765735283205
        */

        const int Midgame = 5255;
        const int Endgame = 435;

        static readonly int[] PhaseValues = new int[6] { 0, 155, 305, 405, 1050, 0 };

        static readonly int[,] MidgameTables = new int[6, 64]{
        {  //PAWN MG
   100,   100,   100,   100,   100,   100,   100,   100,
   176,   215,   147,   195,   188,   215,   132,    78,
    82,    89,   106,   114,   148,   148,   110,    73,
    67,    94,    84,    97,    98,    93,   101,    63,
    55,    75,    80,    90,    95,    86,    91,    55,
    55,    70,    68,    67,    74,    81,   102,    66,
    49,    83,    63,    55,    65,    98,   116,    59,
   100,   100,   100,   100,   100,   100,   100,   100,
        },
        {  //KNIGHT MG
   116,   228,   271,   269,   338,   212,   278,   191,
   225,   247,   354,   331,   320,   360,   299,   282,
   258,   354,   343,   363,   390,   426,   375,   346,
   297,   330,   325,   362,   349,   378,   336,   332,
   295,   320,   326,   321,   336,   330,   329,   301,
   286,   296,   316,   318,   323,   322,   326,   293,
   274,   258,   297,   301,   305,   321,   295,   291,
   208,   286,   256,   271,   293,   284,   289,   284,
        },
        {  //BISHOP MG
   291,   338,   254,   283,   298,   294,   336,   322,
   315,   342,   320,   320,   360,   386,   344,   295,
   340,   377,   376,   377,   370,   390,   385,   361,
   331,   338,   361,   387,   373,   382,   338,   341,
   325,   355,   356,   369,   375,   352,   346,   339,
   332,   348,   352,   350,   353,   361,   349,   341,
   333,   352,   354,   338,   343,   353,   363,   330,
   309,   339,   336,   322,   333,   329,   301,   311,
        },
        {  //ROOK MG
   495,   512,   489,   517,   514,   484,   486,   496,
   493,   498,   531,   536,   547,   544,   486,   507,
   467,   493,   502,   498,   485,   521,   531,   480,
   448,   468,   478,   498,   487,   506,   467,   455,
   442,   453,   470,   472,   479,   473,   498,   455,
   442,   461,   470,   467,   480,   482,   479,   452,
   444,   473,   468,   477,   484,   500,   487,   423,
   459,   463,   470,   480,   480,   480,   446,   455,
        },
        {  //QUEEN MG
   862,   903,   921,   911,   963,   948,   934,   927,
   880,   862,   903,   921,   888,   953,   923,   940,
   897,   900,   911,   922,   941,   978,   965,   960,
   878,   885,   899,   902,   907,   931,   906,   909,
   900,   886,   905,   905,   910,   911,   914,   907,
   892,   914,   903,   908,   906,   913,   924,   912,
   868,   899,   918,   909,   916,   926,   911,   905,
   904,   892,   901,   913,   894,   888,   877,   858,
        },
        {  //KING MG
   -11,    70,    55,    31,   -37,   -16,    22,    22,
    36,    24,    25,    36,    16,     8,   -13,   -31,
    33,    26,    42,    11,    11,    39,    35,    -2,
     0,    -9,     1,   -21,   -20,   -20,   -15,   -60,
   -24,    15,   -26,   -65,   -79,   -56,   -38,   -60,
     8,    -1,   -37,   -77,   -79,   -60,   -23,   -25,
    13,    16,   -12,   -72,   -56,   -27,    15,    17,
    -5,    43,    29,   -60,     9,   -27,    32,    29,
        }
        };

        static readonly int[,] EndgameTables = new int[6, 64]{
        {  //PAWN EG
   100,   100,   100,   100,   100,   100,   100,   100,
   274,   263,   251,   227,   237,   229,   262,   280,
   188,   193,   179,   164,   153,   147,   177,   179,
   126,   116,   107,   100,    93,    98,   109,   110,
   106,   101,    88,    85,    85,    83,    92,    91,
    96,    95,    85,    91,    88,    84,    85,    82,
   107,    98,    97,    93,   100,    89,    89,    85,
   100,   100,   100,   100,   100,   100,   100,   100,
        },
        {  //KNIGHT EG
   228,   236,   267,   249,   254,   249,   219,   187,
   248,   270,   261,   280,   271,   257,   255,   227,
   250,   262,   290,   289,   277,   274,   262,   240,
   263,   279,   298,   299,   298,   292,   283,   260,
   260,   271,   293,   300,   294,   294,   282,   257,
   254,   274,   279,   290,   287,   275,   259,   251,
   240,   257,   267,   273,   276,   262,   259,   236,
   252,   230,   255,   262,   258,   258,   232,   215,
        },
        {  //BISHOP EG
   287,   278,   287,   292,   293,   289,   287,   279,
   289,   295,   304,   289,   297,   289,   294,   281,
   291,   290,   300,   298,   301,   305,   296,   293,
   293,   304,   309,   310,   312,   306,   298,   296,
   288,   296,   308,   315,   304,   307,   294,   289,
   285,   295,   306,   306,   310,   298,   292,   280,
   283,   285,   293,   300,   302,   292,   286,   271,
   276,   292,   283,   295,   294,   287,   290,   285,
        },
        {  //ROOK EG
   508,   503,   510,   505,   508,   507,   505,   503,
   505,   506,   503,   502,   493,   498,   505,   501,
   503,   503,   500,   502,   501,   495,   496,   496,
   503,   502,   511,   501,   503,   505,   500,   503,
   505,   509,   510,   509,   506,   503,   497,   495,
   500,   505,   502,   508,   501,   500,   501,   490,
   494,   497,   504,   507,   500,   498,   492,   499,
   491,   497,   497,   496,   493,   493,   497,   479,
        },
        {  //QUEEN EG
   918,   936,   943,   946,   934,   926,   924,   941,
   902,   943,   946,   950,   980,   933,   928,   908,
   892,   919,   927,   968,   963,   937,   925,   913,
   926,   944,   940,   967,   984,   957,   981,   948,
   893,   951,   943,   974,   956,   956,   952,   935,
   908,   889,   932,   930,   934,   942,   932,   923,
   904,   898,   882,   902,   903,   893,   886,   886,
   885,   886,   889,   869,   915,   888,   906,   879,
        },
        {  //KING EG
   -74,   -43,   -23,   -25,   -11,    10,     1,   -12,
   -17,     6,     4,    10,     7,    26,    14,     8,
    -3,     6,    10,     6,     8,    24,    27,     3,
   -16,     8,    12,    19,    13,    19,     9,    -3,
   -25,   -14,    13,    20,    23,    15,     1,   -15,
   -27,   -10,     9,    20,    23,    14,     2,   -12,
   -31,   -16,     4,    14,    15,     5,    -9,   -21,
   -53,   -38,   -23,    -6,   -23,    -8,   -29,   -47,
        }};

        static readonly int[] MobilityValues = new int[6 * 12]
        {
          // Friends                                      Foes
          // P      N      B      R      Q      K         P      N      B      R      Q      K
             9,     7,    12,     3,     1,    -5,        0,    28,    37,    15,    31,    86,
             0,     1,    -1,     0,     4,     0,       -4,     0,    18,    15,    13,    30,
           -11,    -1,    49,     4,    -3,    -6,       -1,    20,     0,    13,    27,    65,
           -16,    -4,    -6,     1,    -6,    -5,       -3,     3,    10,     0,    26,    33,
            -6,     0,     0,     0,   -79,    -6,       -4,    -4,     1,    -1,     0,    51,
             5,     0,     7,    -8,     0,     0,       30,     4,    10,     4,   -79,     0,
            };

        public static int DynamicScore;

        public static int ComputeMobility(Board board)
        {
            int mobility = 0;
            for (int i = 0; i < 64; i++)
                mobility += GetMobility(board, i);

            return mobility;
        }

        public static int GetMobility(Board board, int square)
        {
            Piece piece = board[square];
            if (piece == Piece.None)
                return 0;

            int mobility = 0;
            switch (piece)
            {
                case Piece.BlackPawn:
                    foreach (int target in Attacks.BlackPawn[square])
                        mobility += GetMobilityValue(piece, board[target]);
                    break;
                case Piece.WhitePawn:
                    foreach (int target in Attacks.WhitePawn[square])
                        mobility += GetMobilityValue(piece, board[target]);
                    break;
                case Piece.BlackKing:
                case Piece.WhiteKing:
                    foreach (int target in Attacks.King[square])
                        mobility += GetMobilityValue(piece, board[target]);
                    break;
                case Piece.BlackKnight:
                case Piece.WhiteKnight:
                    foreach (int target in Attacks.Knight[square])
                        mobility += GetMobilityValue(piece, board[target]);
                    break;
                case Piece.BlackRook:
                case Piece.WhiteRook:
                    for (int dir = 0; dir < 4; dir++)
                        foreach (int target in Attacks.Straight[square, dir])
                            if (board[target] != Piece.None)
                            {
                                mobility += GetMobilityValue(piece, board[target]);
                                break;
                            }
                    break;
                case Piece.BlackBishop:
                case Piece.WhiteBishop:
                    for (int dir = 0; dir < 4; dir++)
                        foreach (int target in Attacks.Diagonal[square, dir])
                            if (board[target] != Piece.None)
                            {
                                mobility += GetMobilityValue(piece, board[target]);
                                break;
                            }
                    break;
                case Piece.BlackQueen:
                case Piece.WhiteQueen:
                    for (int dir = 0; dir < 4; dir++)
                    {
                        foreach (int target in Attacks.Diagonal[square, dir])
                            if (board[target] != Piece.None)
                            {
                                mobility += GetMobilityValue(piece, board[target]);
                                break;
                            }
                        foreach (int target in Attacks.Straight[square, dir])
                            if (board[target] != Piece.None)
                            {
                                mobility += GetMobilityValue(piece, board[target]);
                                break;
                            }
                    }
                    break;
            }
            return mobility;
        }

        private static int GetMobilityValue(Piece subject, Piece target)
        {
            if (target == Piece.None)
                return 0;

            //[0..5]
            int subjectIndex = PieceTableIndex(subject);
            int targetIndex = PieceTableIndex(target);
            if (Pieces.Color(target) != Pieces.Color(subject))
                targetIndex += 6;

            //each piece has a value for friendly [0..5] and opponent pieces [6..11] it can attack
            int value = MobilityValues[subjectIndex * 12 + targetIndex];
            //but we return negative values if subject is black (black minimizes)
            return (int)Pieces.GetColor(subject) * value;
        }
    }
}
