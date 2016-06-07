namespace BackgammonByHoratiu.Entities
{
    public enum PiecePlayer
    {
        Player1,
        Player2
    }

    public class Piece
    {
        PiecePlayer player;

        public PiecePlayer Player
        {
            get { return player; }
            set { player = value; }
        }
    }
}

