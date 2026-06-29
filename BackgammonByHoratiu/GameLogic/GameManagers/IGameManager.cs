using System.Collections.Generic;

using BackgammonByHoratiu.Entities;

namespace BackgammonByHoratiu.GameLogic.GameManagers
{
    public interface IGameManager : IGameLogicManager
    {
        bool IsRunning { get; }

        int[] TableValues { get; }

        int Dice1 { get; }

        int Dice2 { get; }

        int ActivePlayer { get; }

        Player Player1 { get; }

        Player Player2 { get; }

        void MoveOutedPiece(int distance);

        void MovePiece(int pos, int move);

        void MovePieceDirect(int from, int to);

        void BearOffPiece(int from);

        void ThrowDice();

        void NextTurn();

        void NewGame();

        int FindMovePieceDirectIntermediate(int from, int to);

        int FindMoveOutedPieceIntermediate(int distance);

        IReadOnlyList<int> GetValidDestinations(int fromCol);
    }
}
