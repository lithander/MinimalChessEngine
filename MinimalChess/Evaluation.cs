namespace MinimalChess
{
    public static class Evaluation
    {
        public static int MinValue => -9999;
        public static int MaxValue => 9999;

        
        public static int[] PieceValues = new int[14]
        {
             0,     //Black = 0,
             0,     //White = 1,
            
            -100,   //BlackPawn = 2,
            +100,   //WhitePawn = 3,

            -300,   //BlackKnight = 4,
            +300,   //WhiteKnight = 5,
            
            -300,   //BlackBishop = 6,
            +300,   //WhiteBishop = 7,

            -500,   //BlackRook = 8,
            +500,   //WhiteRook = 9,
            
            -900,   //BlackQueen = 10,
            +900,   //WhiteQueen = 11,

           -9999,   //BlackKing = 12,
           +9999    //WhiteKing = 13,
        };

        public static int Evaluate(Board board)
        {
            int score = 0;
            for (int i = 0; i < 64; i++)
            {
                int j = (int)board[i] >> 1;
                score += PieceValues[j];
            }
            return score;
        }
    }
}
