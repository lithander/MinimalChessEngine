using System;
using System.Collections.Generic;
using System.Text;

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

        public bool Limit(int score, Color color)
        {
            if (color == Color.White && score - 1 > Floor) //Cut floor
            {
                Floor = score - 1;
                return true;
            }

            if (color == Color.Black && score + 1 < Ceiling) //Cut ceiling
            {
                Ceiling = score + 1;
                return true;
            }

            return false;
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

        public bool Outside(int score, Color color)
        {
            if (color == Color.White)
                return score <= Floor;
            else
                return score >= Ceiling; //outside search window
        }

        public int GetScore(Color color) => color == Color.White ? Floor : Ceiling;

        public SearchWindow GetNullWindow(Color color)
        {
            //used to quickly determine that a move is not improving the score for color.
            if (color == Color.White)
                return new SearchWindow(Floor, Floor + 1);
            else
                return new SearchWindow(Ceiling - 1, Ceiling);
        }

        internal bool IsWorseOrEqual(Color color, int nullScore)
        {
            if (color == Color.White)
                return nullScore <= Floor;
            else
                return nullScore >= Ceiling;
        }
    }
}
