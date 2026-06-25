using System.Collections.Generic;

namespace BackgammonByHoratiu.Entities
{
    public class Player
    {
        int outedPieces, completedPieces;
        List<int> movesLeft;

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
            movesLeft = new List<int>();
        }
    }
}
