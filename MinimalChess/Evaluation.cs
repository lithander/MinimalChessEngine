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
        Added dynamic mobility component to 'chillipepper003' creating 'mobility19' by evaluating 725000 positions from "quiet-labeled.epd" 
        MeanSquareError(k=102): 0.23835033815795936
        */

        const int Midgame = 5255;
        const int Endgame = 435;

        static readonly int[] PhaseValues = new int[6] { 0, 155, 305, 405, 1050, 0 };

        static readonly int[,] MidgameTables = new int[6, 64]{
        {  //PAWN MG
  100,  100,  100,  100,  100,  100,  100,  100,
  175,  212,  146,  197,  189,  216,  133,   77,
   82,   87,  106,  113,  150,  147,  108,   72,
   67,   94,   83,   96,   96,   92,  100,   62,
   55,   73,   80,   88,   94,   86,   90,   54,
   54,   69,   67,   68,   75,   81,  102,   65,
   52,   83,   66,   60,   69,   98,  117,   60,
  100,  100,  100,  100,  100,  100,  100,  100,
        },
        {  //KNIGHT MG
  124,  222,  267,  268,  342,  216,  278,  195,
  225,  248,  353,  332,  321,  361,  302,  283,
  259,  355,  342,  362,  390,  428,  377,  348,
  300,  331,  324,  361,  350,  377,  340,  334,
  298,  320,  327,  321,  338,  331,  334,  305,
  287,  297,  316,  321,  326,  322,  327,  295,
  279,  261,  301,  305,  309,  321,  296,  294,
  212,  290,  259,  275,  301,  283,  292,  287,
        },
        {  //BISHOP MG
  297,  342,  255,  284,  300,  296,  341,  324,
  317,  342,  318,  320,  357,  383,  345,  293,
  346,  376,  372,  375,  370,  391,  386,  365,
  333,  328,  357,  379,  366,  380,  332,  338,
  327,  356,  353,  366,  373,  347,  347,  339,
  334,  349,  351,  352,  355,  362,  351,  347,
  333,  355,  356,  341,  347,  357,  368,  332,
  310,  346,  343,  331,  338,  334,  303,  317,
        },
        {  //ROOK MG
  492,  510,  484,  517,  513,  485,  482,  494,
  492,  495,  529,  535,  545,  546,  481,  506,
  468,  492,  497,  495,  483,  517,  527,  477,
  445,  463,  474,  492,  483,  507,  465,  452,
  440,  452,  468,  469,  475,  468,  495,  454,
  440,  456,  466,  464,  475,  479,  477,  453,
  444,  469,  466,  476,  481,  498,  484,  425,
  459,  462,  469,  479,  479,  480,  446,  460,
        },
        {  //QUEEN MG
  867,  900,  920,  911,  967,  952,  935,  930,
  887,  866,  903,  921,  886,  951,  926,  943,
  907,  904,  905,  918,  932,  980,  966,  966,
  884,  886,  898,  892,  898,  930,  906,  913,
  909,  885,  899,  897,  903,  907,  915,  916,
  900,  917,  900,  903,  902,  913,  926,  922,
  876,  901,  919,  909,  915,  925,  912,  909,
  909,  900,  909,  918,  899,  894,  884,  867,
        },
        {  //KING MG
  -13,   76,   61,   34,  -44,  -15,   23,   26,
   41,   26,   28,   38,   16,    8,  -15,  -31,
   40,   31,   45,   12,   12,   40,   38,   -2,
    2,   -8,   -4,  -22,  -23,  -23,  -20,  -63,
  -24,   15,  -27,  -70,  -86,  -61,  -42,  -63,
    8,   -5,  -38,  -77,  -80,  -62,  -22,  -28,
   12,   15,  -15,  -74,  -57,  -28,   15,   17,
   -5,   42,   28,  -57,   10,  -24,   35,   28,
        }
        };

        static readonly int[,] EndgameTables = new int[6, 64]{
        {  //PAWN EG
  100,  100,  100,  100,  100,  100,  100,  100,
  279,  270,  254,  230,  241,  234,  267,  288,
  190,  196,  182,  166,  155,  150,  181,  181,
  128,  117,  107,  102,   94,   99,  109,  111,
  107,  101,   89,   85,   85,   83,   92,   91,
   97,   96,   86,   92,   88,   84,   85,   83,
  107,   98,   97,   95,   99,   89,   89,   84,
  100,  100,  100,  100,  100,  100,  100,  100,
        },
        {  //KNIGHT EG
  231,  241,  272,  253,  259,  253,  224,  190,
  255,  276,  264,  281,  274,  259,  260,  235,
  255,  265,  290,  289,  280,  277,  265,  244,
  268,  282,  299,  299,  298,  293,  283,  265,
  263,  274,  293,  301,  294,  293,  283,  262,
  259,  277,  278,  290,  286,  274,  260,  257,
  246,  262,  268,  275,  277,  263,  263,  237,
  256,  233,  259,  266,  259,  262,  235,  217,
        },
        {  //BISHOP EG
  286,  277,  290,  293,  294,  291,  288,  278,
  289,  293,  301,  287,  295,  289,  293,  282,
  291,  289,  295,  293,  296,  299,  294,  291,
  292,  305,  306,  304,  307,  302,  298,  297,
  289,  291,  304,  309,  298,  302,  291,  290,
  285,  291,  304,  303,  306,  295,  290,  280,
  285,  284,  291,  299,  300,  289,  284,  274,
  278,  293,  288,  294,  294,  288,  291,  285,
        },
        {  //ROOK EG
  505,  500,  507,  501,  505,  505,  504,  501,
  505,  505,  502,  499,  490,  495,  504,  500,
  503,  501,  498,  501,  499,  495,  495,  496,
  504,  502,  510,  500,  502,  503,  502,  505,
  505,  508,  507,  504,  502,  501,  495,  495,
  501,  503,  499,  505,  498,  498,  498,  490,
  496,  496,  502,  505,  498,  495,  490,  499,
  492,  497,  497,  495,  493,  494,  496,  482,
        },
        {  //QUEEN EG
  922,  940,  940,  945,  935,  928,  927,  942,
  909,  946,  950,  953,  985,  935,  930,  915,
  896,  921,  928,  966,  964,  937,  926,  919,
  932,  943,  941,  964,  985,  956,  983,  954,
  896,  951,  941,  970,  951,  960,  956,  940,
  914,  891,  933,  929,  934,  945,  936,  928,
  909,  898,  884,  907,  904,  895,  885,  890,
  891,  888,  891,  870,  919,  890,  912,  881,
        },
        {  //KING EG
  -77,  -45,  -24,  -26,   -9,   11,    2,  -15,
  -19,    7,    5,    8,    7,   27,   15,    9,
   -4,    6,   10,    6,    8,   25,   27,    4,
  -17,    7,   13,   20,   14,   20,   10,   -4,
  -25,  -14,   14,   21,   24,   16,    2,  -16,
  -26,  -10,    9,   20,   23,   14,    1,  -12,
  -33,  -17,    4,   14,   15,    5,  -10,  -22,
  -56,  -40,  -23,   -6,  -21,   -8,  -29,  -49,
        }};

        public static int[] MobilityValues = new int[(3 * 6 + 1) * 6]
        {
          // -      P   N   B   R   Q   K       P   N   B   R   Q   K     P   N   B   R   Q   K
             0,    10,  9, 10,  2, -1, -5,      0, 28, 37, 18, 31, 89,    0,  0,  0,  0,  0,  0,
             1,     1,  3,  0,  3,  4,  2,     -3,  0, 21, 19, 20, 35,    0,  0,  0,  0,  0,  0,
             2,    -8,  3, 53,  4, -2, -5,     -3, 21,  0, 14, 30, 68,   -2,  5,  0, 11, 19, 22,
             2,   -10,  5, -1,  4,  1, -1,     -1,  6, 15,  0, 31, 36,   -3, -3, -1,  0,  9,  9,
             3,    -4,  4,  5,  2,-43, -4,     -4, -5,  2,  1,  0, 80,    1, -1,  3,  4,  0,  8,
             0,     6,  5,  7, -8,  6,  0,     30,  3, 13,  5,-43,  0,    0,  0,  0,  0,  0,  0,
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
            int mobility = 0;
            Piece piece = board[square];
            switch (piece)
            {
                case Piece.BlackPawn:
                    return GetMobility(board, piece, Attacks.BlackPawn[square]);
                case Piece.WhitePawn:
                    return GetMobility(board, piece, Attacks.WhitePawn[square]);
                case Piece.BlackKing:
                case Piece.WhiteKing:
                    return GetMobility(board, piece, Attacks.King[square]);
                case Piece.BlackKnight:
                case Piece.WhiteKnight:
                    return GetMobility(board, piece, Attacks.Knight[square]);
                case Piece.BlackRook:
                case Piece.WhiteRook:
                    for (int dir = 0; dir < 4; dir++)
                        mobility += GetMobilitySlider(board, piece, Attacks.Straight[square, dir]);
                    return mobility;
                case Piece.BlackBishop:
                case Piece.WhiteBishop:
                    for (int dir = 0; dir < 4; dir++)
                        mobility += GetMobilitySlider(board, piece, Attacks.Diagonal[square, dir]);
                    return mobility;
                case Piece.BlackQueen:
                case Piece.WhiteQueen:
                    for (int dir = 0; dir < 4; dir++)
                    {
                        mobility += GetMobilitySlider(board, piece, Attacks.Straight[square, dir]);
                        mobility += GetMobilitySlider(board, piece, Attacks.Diagonal[square, dir]);
                    }
                    return mobility;
                default: //Piece == Piece.None
                    return 0;
            }
        }


        private static int GetMobility(Board board, Piece piece, byte[] targets)
        {
            int result = 0;
            int subjectIndex = PieceTableIndex(piece);
            foreach (int target in targets)
            {
                Piece targetPiece = board[target];
                if (targetPiece == Piece.None)
                {
                    result += MobilityValues[subjectIndex * 19];
                    continue;
                }

                //[0..5]
                int targetIndex = (int)targetPiece >> 2;
                if (Pieces.Color(targetPiece) != Pieces.Color(piece))
                    targetIndex += 6;

                //each piece has a value for friendly [0..5] and opponent pieces [6..11] it can attack
                result += MobilityValues[subjectIndex * 19 + targetIndex];
            }
            //but we return negative values if subject is black (black minimizes)
            return (int)Pieces.GetColor(piece) * result;
        }

        private static int GetMobilitySlider(Board board, Piece piece, byte[] targets)
        {
            int subjectIndex = PieceTableIndex(piece);
            int result = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                Piece targetPiece = board[targets[i]];
                if (targetPiece == Piece.None)
                {
                    result += MobilityValues[subjectIndex * 19];
                    continue;
                }

                //[0..5]
                int targetIndex = (int)targetPiece >> 2;
                if (Pieces.Color(targetPiece) != Pieces.Color(piece))
                    targetIndex += 6;

                //each piece has a value for friendly [0..5] and opponent pieces [6..11] it can attack
                result += MobilityValues[subjectIndex * 19 + targetIndex];

                //after the first piece only scan for enemy pieces that are target of a pin or discovered attack
                for (i++; i < targets.Length; i++)
                {
                    targetPiece = board[targets[i]];
                    if (targetPiece == Piece.None)
                        continue;

                    if (Pieces.Color(targetPiece) != Pieces.Color(piece))
                        result += MobilityValues[subjectIndex * 19 + ((int)targetPiece >> 2) + 12];

                    break;
                }

                break;
            }
            //but we return negative values if subject is black (black minimizes)
            return (int)Pieces.GetColor(piece) * result;
        }
    }
}
