using BackgammonByHoratiu.Entities;

namespace BackgammonByHoratiu.GameLogic.GameManagers
{
    public class GameManager : IGameManager
    {
        Table table;

        public bool IsRunning { get; private set; }

        public int[] TableValues => table.TableValues;

        public int Dice1 => table.Dice1;

        public int Dice2 => table.Dice2;

        public int ActivePlayer => table.ActivePlayer;

        public Player Player1 => table.Player1;

        public Player Player2 => table.Player2;

        public void LoadContent()
        {
            table = new Table();
            IsRunning = true;
        }

        public void UnloadContent() { }

        public void Update(double elapsedMilliseconds) { }

        public void MoveOutedPiece(int distance) => table.MoveOutedPiece(distance);

        public void MovePiece(int pos, int move) => table.MovePiece(pos, move);

        public void MovePieceDirect(int from, int to) => table.MovePieceDirect(from, to);

        public void BearOffPiece(int from) => table.BearOffPiece(from);

        public void ThrowDice() => table.ThrowDice();

        public void NextTurn() => table.NextTurn();

        public int FindMovePieceDirectIntermediate(int from, int to) => table.FindMovePieceDirectIntermediate(from, to);

        public int FindMoveOutedPieceIntermediate(int distance) => table.FindMoveOutedPieceIntermediate(distance);

        public void NewGame()
        {
            table = new Table();
            IsRunning = true;
        }
    }
}
