using System;
using System.Runtime.CompilerServices;

namespace Leorik
{
    public class Evaluation
    {
        public struct Eval
        {
            private short _midgameScore;
            private short _endgameScore;
            private short _phaseValue;

            public short Score { get; private set; }

            public Eval(ref BoardState board) : this()
            {
                AddPieces(ref board);
                Score = (short)Interpolate(_midgameScore, _endgameScore, _phaseValue);
            }

            public void Evaluate(ref BoardState board)
            {
                _midgameScore = 0;
                _endgameScore = 0;
                _phaseValue = 0;
                AddPieces(ref board);
                Score = (short)Interpolate(_midgameScore, _endgameScore, _phaseValue);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AddPieces(ref BoardState board)
            {
                //ulong occupied = board.Black | board.White;
                //for (ulong bits = occupied; bits != 0; bits = Bitboard.ClearLSB(bits))
                //{
                //    int square = Bitboard.LSB(bits);
                //    Piece piece = board.GetPiece(square);
                //    AddPiece(piece, square);
                //}
                for (ulong bits = board.Black; bits != 0; bits = Bitboard.ClearLSB(bits))
                {
                    int square = Bitboard.LSB(bits);
                    Piece piece = board.GetPiece(square);
                    AddBlackPiece(piece, square);
                }
                for (ulong bits = board.White; bits != 0; bits = Bitboard.ClearLSB(bits))
                {
                    int square = Bitboard.LSB(bits);
                    Piece piece = board.GetPiece(square);
                    AddWhitePiece(piece, square);
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void Update(ref Move move)
            {
                RemovePiece(move.MovingPiece(), move.FromSquare);
                AddPiece(move.NewPiece(), move.ToSquare);

                if (move.CapturedPiece() != Piece.None)
                    RemovePiece(move.CapturedPiece(), move.ToSquare);

                switch (move.Flags)
                {
                    case Piece.EnPassant | Piece.BlackPawn:
                        RemoveWhitePiece(Piece.WhitePawn, move.ToSquare + 8);
                        break;
                    case Piece.EnPassant | Piece.WhitePawn:
                        RemoveBlackPiece(Piece.BlackPawn, move.ToSquare - 8);
                        break;
                    case Piece.CastleShort | Piece.Black:
                        RemoveBlackPiece(Piece.BlackRook, 63);
                        AddBlackPiece(Piece.BlackRook, 61);
                        break;
                    case Piece.CastleLong | Piece.Black:
                        RemoveBlackPiece(Piece.BlackRook, 56);
                        AddBlackPiece(Piece.BlackRook, 59);
                        break;
                    case Piece.CastleShort | Piece.White:
                        RemoveWhitePiece(Piece.WhiteRook, 7);
                        AddWhitePiece(Piece.WhiteRook, 5);
                        break;
                    case Piece.CastleLong | Piece.White:
                        RemoveWhitePiece(Piece.WhiteRook, 0);
                        AddWhitePiece(Piece.WhiteRook, 3);
                        break;
                }

                Score = (short)Interpolate(_midgameScore, _endgameScore, _phaseValue);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AddPiece(Piece piece, int squareIndex)
            {
                if ((piece & Piece.ColorMask) == Piece.White)
                    AddWhitePiece(piece, squareIndex);
                else
                    AddBlackPiece(piece, squareIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void RemovePiece(Piece piece, int squareIndex)
            {
                if ((piece & Piece.ColorMask) == Piece.White)
                    RemoveWhitePiece(piece, squareIndex);
                else
                    RemoveBlackPiece(piece, squareIndex);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void RemoveBlackPiece(Piece piece, int squareIndex)
            {
                int pieceIndex = PieceIndex(piece);
                _phaseValue -= PhaseValues[pieceIndex];
                int tableIndex = TableIndex(pieceIndex, squareIndex);
                _midgameScore += MidgameTables[tableIndex];
                _endgameScore += EndgameTables[tableIndex];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void RemoveWhitePiece(Piece piece, int squareIndex)
            {
                int pieceIndex = PieceIndex(piece);
                _phaseValue -= PhaseValues[pieceIndex];
                int tableIndex = TableIndex(pieceIndex, squareIndex ^ 56);
                _midgameScore -= MidgameTables[tableIndex];
                _endgameScore -= EndgameTables[tableIndex];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AddBlackPiece(Piece piece, int squareIndex)
            {
                int pieceIndex = PieceIndex(piece);
                _phaseValue += PhaseValues[pieceIndex];
                int tableIndex = TableIndex(pieceIndex, squareIndex);
                _midgameScore -= MidgameTables[tableIndex];
                _endgameScore -= EndgameTables[tableIndex];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AddWhitePiece(Piece piece, int squareIndex)
            {
                int pieceIndex = PieceIndex(piece);
                _phaseValue += PhaseValues[pieceIndex];
                int tableIndex = TableIndex(pieceIndex, squareIndex ^ 56);
                _midgameScore += MidgameTables[tableIndex];
                _endgameScore += EndgameTables[tableIndex];
            }
        }

        const int CheckmateBase = 9000;
        const int CheckmateScore = 9999;

        public static int GetMateDistance(int score)
        {
            int plies = CheckmateScore - Math.Abs(score);
            int moves = (plies + 1) / 2;
            return moves;
        }

        public static bool IsCheckmate(int score) => Math.Abs(score) > CheckmateBase;

        public static int Checkmate(Color color, int ply) => (int)color * (ply - CheckmateScore);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Interpolate(short midgameScore, short endgameScore, short phaseValue)
        {
            double phase = (double)(phaseValue - Midgame) / (Endgame - Midgame);
            return midgameScore + Math.Clamp(phase, 0, 1) * (endgameScore - midgameScore);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        private static int PieceIndex(Piece piece) => ((int)piece >> 2) - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TableIndex(int pieceIndex, int squareIndex) => (pieceIndex << 6) | squareIndex;

        /*
        Added dynamic mobility component to 'chillipepper003' from 'mobility13_7e', the same values as in 0.4.3d which have proven best in tests despite a rather high MSE
        MeanSquareError(k=102): 0.23835033815795936
        */

        const short Midgame = 5255;
        const short Endgame = 435;

        //Pawn = 0, Knight = 155, Bishop = 305; Rook = 405, Queen = 1050, King = 0
        static readonly short[] PhaseValues = new short[6] { 0, 155, 305, 405, 1050, 0 };

        static readonly short[] MidgameTables = new short[6 * 64]
        {  //PAWN MG
          100,  100,  100,  100,  100,  100,  100,  100,
          176,  214,  147,  194,  189,  214,  132,   77,
           82,   88,  106,  113,  150,  146,  110,   73,
           67,   93,   83,   95,   97,   92,   99,   63,
           55,   74,   80,   89,   94,   86,   90,   55,
           55,   70,   68,   69,   76,   81,  101,   66,
           52,   84,   66,   60,   69,   99,  117,   60,
          100,  100,  100,  100,  100,  100,  100,  100,
          //KNIGHT MG
          116,  228,  271,  270,  338,  213,  278,  191,
          225,  247,  353,  331,  321,  360,  300,  281,
          258,  354,  343,  362,  389,  428,  375,  347,
          300,  332,  325,  360,  349,  379,  339,  333,
          298,  322,  325,  321,  337,  332,  332,  303,
          287,  297,  316,  319,  327,  320,  327,  294,
          276,  259,  300,  304,  308,  322,  296,  292,
          208,  290,  257,  274,  296,  284,  293,  284,
          //BISHOP MG
          292,  338,  254,  283,  299,  294,  337,  323,
          316,  342,  319,  319,  360,  385,  343,  295,
          342,  377,  373,  374,  368,  392,  385,  363,
          332,  338,  356,  384,  370,  380,  337,  341,
          327,  354,  353,  366,  373,  346,  345,  341,
          335,  350,  351,  347,  352,  361,  350,  344,
          333,  354,  354,  339,  344,  353,  367,  333,
          309,  341,  342,  325,  334,  332,  302,  313,
          //ROOK MG
          493,  511,  487,  515,  514,  483,  485,  495,
          493,  498,  529,  534,  546,  544,  483,  508,
          465,  490,  499,  497,  483,  519,  531,  480,
          448,  464,  476,  495,  484,  506,  467,  455,
          442,  451,  468,  470,  476,  472,  498,  454,
          441,  461,  468,  465,  478,  481,  478,  452,
          443,  472,  467,  476,  483,  500,  487,  423,
          459,  463,  470,  479,  480,  480,  446,  458,
          //QUEEN MG
          865,  902,  922,  911,  964,  948,  933,  928,
          886,  865,  903,  921,  888,  951,  923,  940,
          902,  901,  907,  919,  936,  978,  965,  966,
          881,  885,  897,  894,  898,  929,  906,  915,
          907,  884,  899,  896,  904,  906,  912,  911,
          895,  916,  900,  902,  904,  912,  924,  917,
          874,  899,  918,  908,  915,  924,  911,  906,
          906,  899,  906,  918,  898,  890,  878,  858,
          //KING MG
          -11,   70,   55,   31,  -37,  -16,   22,   22,
           37,   24,   25,   36,   16,    8,  -12,  -31,
           33,   26,   42,   11,   11,   40,   35,   -2,
            0,   -9,    1,  -21,  -20,  -22,  -15,  -60,
          -25,   16,  -27,  -67,  -81,  -58,  -40,  -62,
            7,   -2,  -37,  -77,  -79,  -60,  -23,  -26,
           12,   15,  -13,  -72,  -56,  -28,   15,   17,
           -6,   44,   29,  -58,    8,  -25,   34,   28,
        };

        static readonly short[] EndgameTables = new short[6 * 64]
        {  //PAWN EG
          100,  100,  100,  100,  100,  100,  100,  100,
          277,  270,  252,  229,  240,  233,  264,  285,
          190,  197,  182,  168,  155,  150,  180,  181,
          128,  117,  108,  102,   93,  100,  110,  110,
          107,  101,   89,   85,   86,   83,   92,   91,
           96,   96,   85,   92,   88,   83,   85,   82,
          107,   99,   97,   97,  100,   89,   89,   84,
          100,  100,  100,  100,  100,  100,  100,  100,
          //KNIGHT EG
          229,  236,  269,  250,  257,  249,  219,  188,
          252,  274,  263,  281,  273,  258,  260,  229,
          253,  264,  290,  289,  278,  275,  263,  243,
          267,  280,  299,  301,  299,  293,  285,  264,
          263,  273,  293,  301,  296,  293,  284,  261,
          258,  276,  278,  290,  287,  274,  260,  255,
          241,  259,  270,  277,  276,  262,  260,  237,
          253,  233,  258,  264,  261,  260,  234,  215,
          //BISHOP EG
          288,  278,  287,  292,  293,  290,  287,  277,
          289,  294,  301,  288,  296,  289,  294,  281,
          292,  289,  296,  292,  296,  300,  296,  293,
          293,  302,  305,  305,  306,  302,  296,  297,
          289,  293,  304,  308,  298,  301,  291,  288,
          285,  294,  304,  303,  306,  294,  290,  280,
          285,  284,  291,  299,  300,  290,  284,  271,
          277,  292,  286,  295,  294,  288,  290,  285,
          //ROOK EG
          506,  500,  508,  502,  504,  507,  505,  503,
          505,  506,  502,  502,  491,  497,  506,  501,
          504,  503,  499,  500,  500,  495,  496,  496,
          503,  502,  510,  500,  502,  504,  500,  505,
          505,  509,  509,  506,  504,  503,  496,  495,
          500,  503,  500,  505,  498,  498,  499,  489,
          496,  495,  502,  505,  498,  498,  491,  499,
          492,  497,  498,  496,  493,  493,  497,  480,
          //QUEEN EG
          918,  937,  943,  945,  934,  926,  924,  942,
          907,  945,  946,  951,  982,  933,  928,  912,
          896,  921,  926,  967,  963,  937,  924,  915,
          926,  944,  939,  962,  983,  957,  981,  950,
          893,  949,  942,  970,  952,  956,  953,  936,
          911,  892,  933,  928,  934,  942,  934,  924,
          907,  898,  883,  903,  903,  893,  886,  888,
          886,  887,  890,  872,  916,  890,  906,  879,
          //KING EG
          -74,  -43,  -23,  -25,  -11,   10,    1,  -12,
          -18,    6,    4,    9,    7,   26,   14,    8,
           -3,    6,   10,    6,    8,   24,   27,    3,
          -16,    8,   13,   20,   14,   19,   10,   -3,
          -25,  -14,   13,   20,   24,   15,    1,  -15,
          -27,  -10,    9,   20,   23,   14,    2,  -12,
          -32,  -17,    4,   14,   15,    5,  -10,  -22,
          -55,  -40,  -23,   -6,  -20,   -8,  -28,  -47,
        };     
    }
}
