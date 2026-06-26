namespace BackgammonByHoratiu.Entities
{
    public enum PiecePlayer
    {
        Player1,
        Player2
    }

    public class Piece
    {
        public PiecePlayer Player { get; set; }
    }
}
