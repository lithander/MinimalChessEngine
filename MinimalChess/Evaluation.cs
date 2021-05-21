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
  176,  211,  146,  197,  188,  215,  132,   77,
   82,   87,  107,  112,  149,  146,  110,   72,
   67,   93,   83,   94,   95,   93,   99,   63,
   55,   74,   79,   88,   94,   87,   91,   54,
   55,   69,   67,   69,   76,   82,  101,   65,
   52,   83,   65,   59,   68,  100,  116,   60,
  100,  100,  100,  100,  100,  100,  100,  100,
        },
        {  //KNIGHT MG
  119,  228,  269,  272,  345,  214,  279,  192,
  226,  250,  353,  332,  322,  362,  303,  281,
  262,  354,  344,  361,  391,  429,  378,  351,
  302,  332,  325,  361,  351,  378,  339,  333,
  299,  322,  326,  321,  338,  333,  333,  304,
  287,  297,  315,  320,  326,  320,  327,  295,
  280,  262,  300,  304,  309,  323,  297,  295,
  212,  290,  259,  276,  297,  284,  292,  286,
        },
        {  //BISHOP MG
  297,  343,  258,  284,  297,  296,  340,  326,
  317,  341,  319,  320,  360,  384,  341,  295,
  342,  375,  372,  370,  369,  394,  385,  363,
  334,  336,  359,  383,  366,  378,  337,  340,
  328,  354,  352,  364,  370,  346,  346,  343,
  336,  352,  350,  348,  352,  360,  350,  345,
  336,  355,  355,  339,  345,  354,  367,  335,
  312,  343,  342,  329,  336,  332,  303,  316,
        },
        {  //ROOK MG
  492,  511,  486,  518,  513,  481,  484,  492,
  490,  497,  527,  533,  546,  541,  484,  507,
  463,  492,  497,  494,  479,  519,  531,  477,
  448,  465,  473,  493,  483,  504,  465,  455,
  441,  452,  468,  469,  478,  472,  497,  452,
  442,  457,  468,  466,  477,  481,  478,  451,
  445,  469,  464,  476,  483,  500,  487,  423,
  459,  462,  470,  480,  479,  480,  446,  459,
        },
        {  //QUEEN MG
  868,  902,  922,  910,  967,  952,  937,  930,
  888,  868,  904,  920,  886,  949,  924,  944,
  905,  903,  906,  915,  931,  980,  968,  966,
  886,  886,  896,  894,  898,  927,  906,  915,
  907,  883,  898,  895,  902,  906,  912,  913,
  898,  916,  899,  901,  901,  910,  923,  919,
  878,  899,  918,  906,  915,  922,  910,  914,
  910,  902,  908,  918,  899,  893,  880,  866,
        },
        {  //KING MG
  -15,   73,   62,   36,  -44,  -16,   23,   22,
   42,   24,   24,   39,   18,    9,  -14,  -36,
   35,   34,   42,   11,   10,   45,   35,    0,
    1,   -9,   -2,  -22,  -20,  -20,  -17,  -63,
  -28,   16,  -27,  -73,  -84,  -60,  -40,  -62,
    6,   -4,  -37,  -77,  -79,  -61,  -24,  -27,
   12,   14,  -15,  -75,  -56,  -28,   14,   17,
   -6,   41,   28,  -60,    8,  -25,   34,   29,
        }
        };

        static readonly int[,] EndgameTables = new int[6, 64]{
        {  //PAWN EG
  100,  100,  100,  100,  100,  100,  100,  100,
  279,  272,  255,  231,  244,  235,  269,  289,
  191,  197,  183,  168,  155,  151,  181,  181,
  128,  117,  108,  102,   94,  100,  110,  111,
  107,  101,   89,   86,   86,   83,   92,   92,
   97,   97,   86,   92,   88,   84,   85,   83,
  108,   99,   97,   96,  101,   89,   90,   84,
  100,  100,  100,  100,  100,  100,  100,  100
        },
        {  //KNIGHT EG
  231,  239,  272,  253,  259,  252,  223,  189,
  255,  276,  266,  282,  276,  261,  262,  233,
  257,  265,  291,  292,  279,  277,  265,  245,
  269,  282,  301,  300,  299,  294,  285,  267,
  263,  275,  295,  302,  296,  293,  285,  261,
  259,  279,  278,  291,  287,  275,  260,  255,
  245,  261,  270,  277,  278,  262,  261,  238,
  257,  235,  259,  267,  261,  263,  235,  218,
        },
        {  //BISHOP EG
  286,  278,  286,  291,  294,  289,  285,  277,
  289,  293,  300,  286,  293,  289,  293,  281,
  292,  288,  294,  292,  294,  298,  294,  293,
  292,  301,  303,  302,  303,  299,  296,  297,
  288,  292,  303,  308,  297,  302,  290,  288,
  284,  291,  303,  303,  305,  294,  290,  279,
  284,  285,  291,  298,  300,  288,  284,  271,
  277,  293,  286,  293,  293,  288,  291,  284,
        },
        {  //ROOK EG
  506,  501,  508,  503,  504,  507,  504,  503,
  506,  506,  502,  500,  491,  497,  505,  501,
  503,  502,  499,  500,  500,  494,  495,  497,
  503,  502,  510,  499,  502,  504,  501,  503,
  506,  509,  510,  506,  503,  501,  496,  495,
  500,  504,  500,  505,  499,  499,  499,  489,
  497,  496,  505,  507,  498,  497,  491,  502,
  492,  497,  497,  496,  493,  493,  497,  482,
        },
        {  //QUEEN EG
  920,  940,  943,  946,  933,  925,  928,  946,
  914,  944,  949,  953,  982,  934,  929,  918,
  897,  921,  925,  968,  961,  937,  925,  918,
  932,  944,  936,  961,  982,  955,  986,  954,
  895,  950,  940,  969,  949,  958,  956,  939,
  910,  889,  933,  927,  935,  944,  933,  925,
  910,  898,  884,  905,  905,  894,  886,  891,
  889,  888,  892,  870,  917,  890,  910,  882,
        },
        {  //KING EG
  -76,  -43,  -23,  -25,  -10,   10,   -1,  -15,
  -18,    8,    4,    9,    7,   26,   13,    8,
   -4,    6,   10,    6,    9,   24,   27,    3,
  -17,    7,   13,   19,   14,   19,    9,   -3,
  -26,  -14,   14,   21,   24,   15,    1,  -17,
  -27,  -11,    9,   20,   23,   14,    1,  -12,
  -33,  -17,    5,   14,   15,    5,  -10,  -22,
  -57,  -40,  -23,   -6,  -22,   -8,  -29,  -48,
        }};

        public static int[] MobilityValues = new int[13 * 6]
        {
         // -    P      N     B     R     Q     K     P     N     B     R     Q     K
            0,   10,    9,   12,    2,    0,   -5,    0,   28,   36,   17,   31,   89,
            1,    1,    3,    0,    3,    5,    3,   -4,    0,   19,   19,   18,   34,
            2,   -8,    4,   53,    4,   -1,   -3,    0,   23,    0,   15,   30,   68,
            2,  -11,    4,   -1,    4,    2,   -1,    0,    6,   15,    0,   32,   36,
            3,   -2,    6,    6,    2,  -99,   -4,   -3,   -3,    4,    2,    0,   75,
            0,    6,    3,    7,   -8,    6,    0,   30,    3,   12,    5,  -99,    0,
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
                    return GetMobilityStraightSlider(board, square, piece);
                case Piece.BlackBishop:
                case Piece.WhiteBishop:
                    return GetMobilityDiagonalSlider(board, square, piece);
                case Piece.BlackQueen:
                case Piece.WhiteQueen:
                    return GetMobilityDiagonalSlider(board, square, piece) +
                           GetMobilityStraightSlider(board, square, piece);
                default: //Piece == Piece.None
                    return 0;
            }
        }

        private static int GetMobilityStraightSlider(Board board, int square, Piece piece) =>
            GetMobilitySlider(board, piece, Attacks.Straight[square, 0]) +
            GetMobilitySlider(board, piece, Attacks.Straight[square, 1]) +
            GetMobilitySlider(board, piece, Attacks.Straight[square, 2]) +
            GetMobilitySlider(board, piece, Attacks.Straight[square, 3]);

        private static int GetMobilityDiagonalSlider(Board board, int square, Piece piece) =>
            GetMobilitySlider(board, piece, Attacks.Diagonal[square, 0]) +
            GetMobilitySlider(board, piece, Attacks.Diagonal[square, 1]) +
            GetMobilitySlider(board, piece, Attacks.Diagonal[square, 2]) +
            GetMobilitySlider(board, piece, Attacks.Diagonal[square, 3]);

        private static int GetMobility(Board board, Piece piece, byte[] targets)
        {
            int result = 0;
            int index = 13 * PieceTableIndex(piece);
            foreach (int target in targets)
                result += MobilityValues[index + GetOffset(piece, board[target])];
            //we return negative values if piece is black (black minimizes)
            return (int)Pieces.GetColor(piece) * result;
        }

        private static int GetMobilitySlider(Board board, Piece piece, byte[] targets)
        {
            int result = 0;
            int index = 13 * PieceTableIndex(piece);
            for (int i = 0; i < targets.Length; i++)
            {
                Piece targetPiece = board[targets[i]];
                if (targetPiece != Piece.None)
                {
                    result += MobilityValues[index + GetOffset(piece, targetPiece)];
                    break;
                }
                result += MobilityValues[index];
            }
            //we return negative values if piece is black (black minimizes)
            return (int)Pieces.GetColor(piece) * result;
        }

        private static int GetOffset(Piece piece, Piece targetPiece)
        {
            if (targetPiece == Piece.None)
                return 0;

            int offset = (int)targetPiece >> 2; //[1..6]
            if (Pieces.Color(targetPiece) == Pieces.Color(piece))
                return offset;

            return offset + 6;
        }
    }
}
