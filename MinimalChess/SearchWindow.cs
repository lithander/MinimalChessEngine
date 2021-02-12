namespace MinimalChess
{
    public struct SearchWindow
    {
        public int Floor;//Alpha
        public int Ceiling;//Beta

        public static SearchWindow Infinite = new SearchWindow(short.MinValue, short.MaxValue);

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
            };
        }

        public bool Inside(int score, Color color)
        {
            if (color == Color.White)
                return score > Floor;
            else
                return score < Ceiling;
        }

        public int GetScore(Color color) => color == Color.White ? Floor : Ceiling;
    }
}
