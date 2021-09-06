using System;

namespace MinimalChess
{
    public struct SearchWindow
    {
        public static SearchWindow Infinite = new(short.MinValue, short.MaxValue);

        public int Floor;//Alpha
        public int Ceiling;//Beta

        public SearchWindow UpperBound => new(Ceiling - 1, Ceiling);
        public SearchWindow LowerBound => new(Floor, Floor + 1);
        //used to quickly determine that a move is not improving the score for color.
        public SearchWindow GetLowerBound(Color color) => color == Color.White ? LowerBound : UpperBound;
        //used to quickly determine that a move is too good and will not be allowed by the opponent .
        public SearchWindow GetUpperBound(Color color) => color == Color.White ? UpperBound : LowerBound;

        public SearchWindow(int floor, int ceiling)
        {
            Floor = floor;
            Ceiling = ceiling;
        }

        public bool Cut(int score, Color color)
        {
            if (color == Color.White) //Cut floor
            {
                if (score <= Floor)
                    return false; //outside search window

                Floor = score;
                return Floor >= Ceiling; //Cutoff?
            }
            else
            {
                if (score >= Ceiling) //Cut ceiling
                    return false; //outside search window

                Ceiling = score;
                return Ceiling <= Floor; //Cutoff?
            }
        }

        public bool FailLow(int score, Color color) => color == Color.White ? (score <= Floor) : (score >= Ceiling);

        public bool FailHigh(int score, Color color) => color == Color.White ? (score >= Ceiling) : (score <= Floor);

        public int GetScore(Color color) => color == Color.White ? Floor : Ceiling;

        public bool CanFailHigh(Color color) => color == Color.White ? (Ceiling < short.MaxValue) : (Floor > short.MinValue);
    }
}
