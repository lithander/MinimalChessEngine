using System;

namespace MinimalChess
{
    public static class Zobrist
    {
        static ulong[][] BoardTable = new ulong[64][];
        static ulong[] EnPassantTable = new ulong[64];
        static ulong[] CastlingTable = new ulong[16]; //all permutations of castling rights, CastlingRights.All == 15
        static ulong Black;
        static ulong White;

        static Zobrist()
        {
            Random rnd = new Random(228126);
            for (int square = 0; square < 64; square++)
            {
                //6 black pieces + 6 white pieces
                BoardTable[square] = new ulong[12];
                for (int piece = 0; piece < 12; piece++)
                    BoardTable[square][piece] = RandomUInt64(rnd);
                //En passent
                EnPassantTable[square] = RandomUInt64(rnd);
            }
            //Side to Move
            Black = RandomUInt64(rnd);
            White = RandomUInt64(rnd);
            //Castling
            for (int i = 0; i < 16; i++)
                CastlingTable[i] = RandomUInt64(rnd);
        }

        public static ulong PieceSquare(Piece piece, int square)
        {
            return (piece != Piece.None) ? BoardTable[square][PieceIndex(piece)] : 0;
        }

        public static int PieceIndex(Piece piece)
        {
            return ((int)piece >> 1) - 2;
        }

        public static ulong Castling(Board.CastlingRights castlingRights)
        {
            return CastlingTable[(int)castlingRights];
        }

        public static ulong EnPassant(int square)
        {
            return (square >= 0) ? EnPassantTable[square] : 0;
        }

        public static ulong SideToMove(Color sideToMove)
        {
            return sideToMove == Color.White ? Zobrist.Black : Zobrist.White;
        }

        private static ulong RandomUInt64(Random rnd)
        {
            byte[] bytes = new byte[8];
            rnd.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
