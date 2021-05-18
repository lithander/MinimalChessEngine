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
  100,  100,  100,  100,  100,  100,  100,  100,
  176,  213,  145,  196,  188,  214,  132,   76,
   82,   89,  106,  114,  150,  148,  110,   73,
   67,   94,   84,   97,   98,   93,  101,   63,
   55,   75,   80,   90,   95,   86,   91,   55,
   55,   70,   68,   67,   74,   81,  102,   66,
   49,   83,   63,   55,   65,   98,  116,   59,
  100,  100,  100,  100,  100,  100,  100,  100,
        },
        {  //KNIGHT MG
  118,  228,  271,  268,  341,  213,  277,  192,
  224,  247,  353,  330,  320,  360,  300,  282,
  258,  354,  342,  363,  390,  426,  375,  346,
  297,  330,  325,  363,  350,  378,  337,  332,
  295,  320,  326,  321,  336,  331,  330,  301,
  285,  297,  316,  318,  323,  322,  326,  293,
  273,  257,  296,  300,  305,  321,  295,  291,
  207,  286,  258,  272,  294,  283,  289,  285,
        },
        {  //BISHOP MG
  295,  342,  255,  284,  299,  294,  337,  323,
  315,  343,  321,  320,  360,  386,  344,  296,
  342,  377,  376,  377,  370,  391,  385,  362,
  331,  330,  361,  385,  373,  382,  335,  338,
  324,  356,  356,  369,  375,  352,  346,  338,
  332,  348,  352,  353,  355,  361,  351,  342,
  332,  353,  356,  339,  346,  353,  365,  331,
  309,  340,  337,  323,  333,  330,  302,  312,
        },
        {  //ROOK MG
  494,  512,  487,  518,  513,  483,  487,  496,
  493,  498,  531,  536,  546,  544,  486,  506,
  467,  493,  502,  498,  485,  521,  531,  478,
  448,  468,  478,  498,  487,  506,  466,  453,
  442,  453,  470,  472,  478,  473,  498,  455,
  442,  461,  471,  466,  480,  482,  479,  452,
  445,  472,  467,  477,  484,  501,  487,  422,
  459,  463,  470,  480,  480,  480,  446,  456,
        },
        {  //QUEEN MG
  862,  901,  919,  911,  965,  949,  933,  926,
  880,  862,  905,  921,  887,  951,  923,  939,
  900,  901,  911,  922,  937,  979,  965,  959,
  878,  885,  899,  902,  907,  933,  906,  908,
  902,  886,  905,  906,  911,  913,  915,  909,
  895,  916,  905,  910,  908,  916,  924,  915,
  867,  899,  920,  909,  916,  928,  912,  905,
  904,  892,  901,  913,  894,  889,  878,  858,
        },
        {  //KING MG
  -11,   72,   58,   32,  -38,  -17,   22,   22,
   39,   24,   25,   37,   17,    8,  -11,  -31,
   34,   27,   43,   11,   11,   39,   35,   -2,
    0,   -9,   -2,  -21,  -20,  -20,  -16,  -61,
  -25,   15,  -26,  -65,  -80,  -56,  -38,  -60,
    8,   -1,  -37,  -77,  -79,  -60,  -23,  -26,
   13,   16,  -12,  -72,  -56,  -27,   15,   17,
   -5,   43,   29,  -59,    9,  -27,   32,   29,
        }
        };

        static readonly int[,] EndgameTables = new int[6, 64]{
        {  //PAWN EG
  100,  100,  100,  100,  100,  100,  100,  100,
  274,  263,  251,  227,  237,  229,  262,  281,
  188,  193,  179,  164,  153,  147,  177,  179,
  126,  116,  107,  100,   93,   98,  109,  110,
  106,  101,   88,   85,   85,   83,   92,   91,
   96,   95,   85,   91,   88,   84,   85,   82,
  107,   98,   97,   93,   99,   89,   89,   85,
  100,  100,  100,  100,  100,  100,  100,  100,
        },
        {  //KNIGHT EG
  227,  236,  267,  248,  254,  249,  219,  188,
  250,  270,  260,  280,  271,  257,  256,  227,
  250,  262,  290,  289,  277,  274,  262,  240,
  263,  279,  297,  298,  298,  292,  283,  259,
  260,  271,  292,  300,  294,  292,  281,  257,
  253,  274,  279,  290,  287,  275,  259,  251,
  241,  257,  267,  273,  276,  261,  259,  235,
  252,  230,  254,  262,  258,  258,  232,  215,
        },
        {  //BISHOP EG
  288,  278,  288,  292,  293,  289,  286,  277,
  289,  295,  304,  290,  297,  289,  294,  281,
  292,  290,  300,  298,  301,  304,  296,  291,
  292,  305,  309,  309,  312,  306,  298,  295,
  288,  295,  308,  315,  304,  306,  294,  286,
  284,  294,  306,  307,  310,  298,  292,  280,
  283,  285,  293,  301,  302,  292,  287,  271,
  276,  292,  283,  295,  293,  287,  291,  284,
        },
        {  //ROOK EG
  507,  503,  511,  505,  508,  506,  505,  503,
  505,  506,  503,  502,  492,  498,  505,  501,
  503,  503,  500,  502,  501,  495,  496,  496,
  503,  502,  511,  501,  503,  505,  501,  504,
  505,  509,  510,  509,  506,  503,  497,  495,
  500,  505,  502,  508,  501,  500,  501,  490,
  495,  497,  505,  507,  500,  498,  492,  499,
  491,  497,  497,  496,  493,  493,  497,  481,
        },
        {  //QUEEN EG
  917,  936,  941,  946,  933,  924,  924,  941,
  903,  944,  948,  950,  981,  934,  929,  909,
  893,  920,  927,  968,  963,  937,  925,  914,
  927,  944,  940,  967,  986,  959,  981,  950,
  895,  951,  943,  975,  957,  960,  954,  937,
  910,  890,  935,  932,  937,  944,  933,  923,
  904,  898,  884,  904,  904,  894,  886,  888,
  885,  886,  889,  869,  915,  888,  907,  879,
        },
        {  //KING EG
  -74,  -43,  -24,  -25,  -10,   11,    1,  -14,
  -18,    7,    3,    9,    7,   26,   14,    8,
   -3,    5,   10,    6,    7,   24,   27,    3,
  -16,    8,   12,   19,   13,   19,    9,   -3,
  -25,  -14,   13,   20,   23,   15,    1,  -15,
  -27,  -10,    9,   20,   23,   14,    2,  -12,
  -31,  -16,    4,   14,   15,    5,   -9,  -21,
  -53,  -39,  -23,   -6,  -23,   -8,  -29,  -47,
        }};

        public static readonly int[] MobilityValues = new int[6 * 18]
        {
          // Friend.                                Foes                                  2nd Foe
          // P     N     B     R     Q     K        P     N     B     R     Q     K       P     N     B     R     Q     K
             9,    7,   12,    3,    1,   -5,       0,   28,   38,   15,   31,   84,      0,    0,    0,    0,    0,    0,
             0,    1,   -2,    0,    4,    0,      -4,    0,   18,   16,   14,   30,      0,    0,    0,    0,    0,    0,
           -11,   -1,   49,    3,   -4,   -7,      -2,   20,    0,   13,   29,   68,     -3,    5,    0,   12,   16,   21,
           -15,   -4,   -6,    1,   -6,   -5,      -3,    3,   10,    0,   26,   34,     -5,   -3,   -3,    0,   11,    8,
            -7,    0,    0,    0,  -28,   -6,      -4,   -5,    0,   -2,    0,   79,     -1,   -2,    0,    5,    0,    7,
             5,    1,    8,   -8,    0,    0,      30,    4,   10,    4,  -28,    0,      0,    0,    0,    0,    0,    0,
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
                    continue;

                //[0..5]
                int targetIndex = PieceTableIndex(targetPiece);
                if (Pieces.Color(targetPiece) != Pieces.Color(piece))
                    targetIndex += 6;

                //each piece has a value for friendly [0..5] and opponent pieces [6..11] it can attack
                result += MobilityValues[subjectIndex * 18 + targetIndex];
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
                    continue;

                //[0..5]
                int targetIndex = PieceTableIndex(targetPiece);
                if (Pieces.Color(targetPiece) != Pieces.Color(piece))
                    targetIndex += 6;

                //each piece has a value for friendly [0..5] and opponent pieces [6..11] it can attack
                result += MobilityValues[subjectIndex * 18 + targetIndex];

                //after the first piece only scan for enemy pieces that are target of a pin or discovered attack
                for (i++; i < targets.Length; i++)
                {
                    targetPiece = board[targets[i]];
                    if (targetPiece == Piece.None)
                        continue;

                    if (Pieces.Color(targetPiece) != Pieces.Color(piece))
                        result += MobilityValues[subjectIndex * 18 + PieceTableIndex(targetPiece) + 12];

                    break;
                }

                break;
            }
            //but we return negative values if subject is black (black minimizes)
            return (int)Pieces.GetColor(piece) * result;
        }
    }
}
