using System.Collections.Generic;

namespace BackgammonByHoratiu.Entities
{
    public class Player
    {
        public int OutedPieces { get; set; }

        public int CompletedPieces { get; set; }

        public List<int> MovesLeft { get; set; }

        public Player()
        {
            MovesLeft = [];
        }
    }
}
