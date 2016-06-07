using System.Collections.Generic;
using System.Drawing;

namespace BackgammonByHoratiu.Entities
{
    public class Player
    {
        Color color;
        int outedPieces, completedPieces;
        List<int> movesLeft;

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public int OutedPieces
        {
            get { return outedPieces; }
            set { outedPieces = value; }
        }

        public int CompletedPieces
        {
            get { return completedPieces; }
            set { completedPieces = value; }
        }

        public List<int> MovesLeft
        {
            get { return movesLeft; }
            set { movesLeft = value; }
        }

        public Player()
        {
            color = Color.Fuchsia;

            movesLeft = new List<int>();
        }

        public Player(Color color)
            : this()
        {
            this.color = color;
        }
    }
}
