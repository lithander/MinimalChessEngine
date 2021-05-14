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
        Added dynamic mobility component to 'chillipepper003' by evaluating 725000 positions from "quiet-labeled.epd" 
        */

        const int Midgame = 5255;
        const int Endgame = 435;

        static readonly int[] PhaseValues = new int[6] { 0, 155, 305, 405, 1050, 0 };

        static readonly int[,] MidgameTables = new int[6, 64]{
        {  //PAWN MG
   100,   100,   100,   100,   100,   100,   100,   100,
   176,   217,   146,   194,   187,   217,   130,    78,
    82,    90,   106,   113,   148,   149,   111,    73,
    67,    95,    84,    97,    99,    93,   102,    63,
    53,    75,    82,    90,    96,    91,    91,    54,
    54,    71,    69,    69,    79,    81,   104,    67,
    45,    84,    62,    57,    66,    98,   118,    58,
   100,   100,   100,   100,   100,   100,   100,   100,
        },
        {  //KNIGHT MG
   118,   237,   271,   271,   333,   205,   279,   185,
   223,   248,   357,   327,   320,   355,   297,   282,
   255,   354,   340,   362,   388,   423,   375,   344,
   295,   329,   322,   356,   346,   378,   335,   328,
   292,   318,   323,   319,   335,   329,   328,   296,
   286,   295,   316,   318,   323,   322,   324,   295,
   276,   259,   295,   303,   306,   323,   298,   290,
   208,   287,   253,   273,   294,   284,   289,   282,
        },
        {  //BISHOP MG
   290,   334,   255,   285,   295,   289,   329,   318,
   312,   342,   320,   317,   358,   386,   345,   292,
   335,   375,   378,   376,   368,   388,   382,   354,
   327,   331,   360,   381,   376,   380,   332,   333,
   321,   349,   351,   369,   375,   352,   343,   334,
   330,   340,   346,   354,   356,   358,   347,   340,
   329,   347,   355,   339,   346,   355,   360,   329,
   297,   334,   329,   319,   329,   324,   297,   307,
        },
        {  //ROOK MG
   496,   514,   489,   519,   514,   488,   491,   501,
   496,   499,   537,   536,   547,   545,   488,   507,
   471,   496,   504,   503,   488,   522,   531,   482,
   449,   469,   478,   497,   491,   508,   468,   456,
   444,   455,   473,   473,   480,   473,   497,   456,
   443,   460,   471,   466,   478,   481,   476,   451,
   456,   475,   468,   476,   484,   499,   484,   437,
   458,   463,   471,   481,   480,   480,   445,   455,
        },
        {  //QUEEN MG
   862,   903,   920,   912,   962,   949,   935,   928,
   882,   859,   903,   920,   891,   957,   928,   943,
   898,   898,   911,   919,   942,   975,   960,   962,
   879,   880,   898,   894,   904,   928,   902,   908,
   901,   883,   898,   902,   904,   908,   913,   908,
   896,   907,   901,   905,   904,   911,   920,   916,
   873,   900,   917,   909,   915,   926,   908,   908,
   907,   892,   900,   913,   893,   888,   878,   862,
        },
        {  //KING MG
   -10,    59,    50,    30,   -29,   -16,    23,    20,
    29,    18,    21,    33,    15,     6,   -12,   -32,
    30,    25,    39,    12,    13,    35,    34,    -2,
     0,    -5,     2,   -21,   -18,   -20,   -13,   -50,
   -21,    12,   -27,   -57,   -70,   -45,   -36,   -56,
    10,     0,   -35,   -77,   -79,   -59,   -23,   -25,
    12,    17,   -13,   -71,   -56,   -27,    15,    18,
    -4,    43,    24,   -59,     9,   -29,    32,    29,
        }
        };

        static readonly int[,] EndgameTables = new int[6, 64]{
        {  //PAWN EG
   100,   100,   100,   100,   100,   100,   100,   100,
   272,   262,   250,   225,   236,   227,   261,   280,
   186,   192,   178,   163,   151,   146,   176,   179,
   126,   116,   106,   100,    93,    98,   109,   110,
   106,   101,    88,    85,    85,    83,    92,    91,
    96,    95,    85,    91,    88,    84,    85,    82,
   107,    98,    97,    97,   100,    89,    89,    85,
   100,   100,   100,   100,   100,   100,   100,   100,
        },
        {  //KNIGHT EG
   227,   234,   264,   247,   256,   248,   220,   187,
   248,   270,   258,   277,   271,   256,   255,   227,
   251,   261,   288,   288,   277,   275,   261,   240,
   262,   278,   297,   299,   298,   291,   284,   260,
   260,   270,   293,   301,   295,   296,   283,   261,
   254,   275,   279,   291,   287,   277,   260,   251,
   241,   257,   267,   273,   277,   262,   259,   237,
   251,   231,   254,   263,   258,   259,   234,   218,
        },
        {  //BISHOP EG
   287,   279,   287,   293,   295,   290,   287,   276,
   288,   297,   305,   291,   298,   289,   293,   281,
   291,   291,   300,   300,   303,   306,   297,   294,
   292,   304,   311,   311,   312,   305,   298,   296,
   288,   295,   307,   316,   305,   306,   293,   290,
   282,   294,   306,   306,   311,   298,   291,   280,
   282,   285,   293,   301,   303,   293,   286,   270,
   275,   290,   283,   296,   295,   286,   292,   282,
        },
        {  //ROOK EG
   508,   503,   512,   507,   509,   509,   506,   504,
   506,   507,   503,   503,   494,   499,   506,   502,
   503,   503,   501,   503,   502,   496,   496,   497,
   503,   501,   511,   500,   501,   504,   502,   503,
   505,   508,   508,   506,   503,   501,   496,   494,
   500,   504,   500,   504,   499,   496,   498,   489,
   496,   497,   503,   507,   498,   497,   491,   496,
   491,   497,   497,   496,   493,   493,   495,   479,
        },
        {  //QUEEN EG
   919,   936,   942,   946,   936,   929,   926,   940,
   901,   938,   942,   946,   975,   933,   929,   907,
   894,   918,   923,   964,   963,   935,   923,   911,
   924,   945,   936,   964,   979,   956,   977,   945,
   891,   948,   941,   970,   955,   956,   947,   933,
   905,   888,   931,   924,   931,   936,   926,   923,
   901,   898,   882,   902,   901,   893,   883,   883,
   884,   887,   886,   869,   914,   887,   905,   881,
        },
        {  //KING EG
   -73,   -40,   -22,   -24,   -10,    10,     1,   -13,
   -15,     8,     5,     9,     8,    28,    15,     8,
    -2,     7,    11,     7,     8,    25,    27,     2,
   -15,     7,    13,    20,    13,    18,    10,    -4,
   -24,   -12,    13,    20,    23,    15,     1,   -16,
   -26,   -10,     9,    21,    24,    15,     2,   -12,
   -30,   -15,     5,    14,    15,     5,    -9,   -21,
   -52,   -38,   -22,    -6,   -23,    -9,   -29,   -47,
        }};

        static readonly int[] MobilityValues = new int[6 * 12]
        {
          // P   N   B   R   Q   K       P   N   B   R   Q   K
     9,     7,    13,     3,    -2,    -6,     0,    27,    36,    15,    32,    84,
    -1,     0,    -2,     0,     3,    -1,    -5,     0,    18,    15,    14,    30,
   -11,    -3,    34,     3,    -6,    -5,    -4,    11,     0,     8,    19,    20,
   -10,    -2,     2,    -2,    -1,    -3,    -4,    -2,     1,     0,    10,     5,
    -3,     1,     4,    -4,   -34,    -3,    -2,    -3,     0,     3,     0,     6,
     6,     0,     7,    -8,    -3,     0,    30,     5,     9,     3,   -34,     0,
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
                            mobility += GetMobilityValue(piece, board[target]);
                    break;
                case Piece.BlackBishop:
                case Piece.WhiteBishop:
                    for (int dir = 0; dir < 4; dir++)
                        foreach (int target in Attacks.Diagonal[square, dir])
                            mobility += GetMobilityValue(piece, board[target]);
                    break;
                case Piece.BlackQueen:
                case Piece.WhiteQueen:
                    for (int dir = 0; dir < 4; dir++)
                    {
                        foreach (int target in Attacks.Diagonal[square, dir])
                            mobility += GetMobilityValue(piece, board[target]);
                        foreach (int target in Attacks.Straight[square, dir])
                            mobility += GetMobilityValue(piece, board[target]);
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
