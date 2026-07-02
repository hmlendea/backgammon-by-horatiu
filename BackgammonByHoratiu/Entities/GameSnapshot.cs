using System.Collections.Generic;

namespace BackgammonByHoratiu.Entities
{
    public class GameSnapshot(
        int[] tableValues,
        int player1OutedPieces, int player1CompletedPieces, IReadOnlyList<int> player1MovesLeft,
        int player2OutedPieces, int player2CompletedPieces, IReadOnlyList<int> player2MovesLeft,
        int dice1, int dice2, int activePlayer)
    {
        public int[] TableValues { get; } = (int[])tableValues.Clone();

        public int Player1OutedPieces { get; } = player1OutedPieces;

        public int Player1CompletedPieces { get; } = player1CompletedPieces;

        public IReadOnlyList<int> Player1MovesLeft { get; } = [.. player1MovesLeft];

        public int Player2OutedPieces { get; } = player2OutedPieces;

        public int Player2CompletedPieces { get; } = player2CompletedPieces;

        public IReadOnlyList<int> Player2MovesLeft { get; } = [.. player2MovesLeft];

        public int Dice1 { get; } = dice1;

        public int Dice2 { get; } = dice2;

        public int ActivePlayer { get; } = activePlayer;
    }
}
