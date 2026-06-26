using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.AI;

namespace BackgammonByHoratiu.GameLogic.GameManagers
{
    /// <summary>
    /// Game manager that drives Player 2 (brown) automatically with the AI.
    /// Player 1 (white) is controlled by the human.
    /// </summary>
    public class AiGameManager : IGameManager
    {
        readonly GameManager inner = new();
        readonly BackgammonAi ai;

        double aiTimer = 0;
        const double AiMoveDelayMs = 600;

        public AiGameManager()
        {
            ai = new BackgammonAi(this);
        }

        public bool IsRunning    => inner.IsRunning;
        public int[] TableValues => inner.TableValues;
        public int Dice1         => inner.Dice1;
        public int Dice2         => inner.Dice2;
        public int ActivePlayer  => inner.ActivePlayer;
        public Player Player1    => inner.Player1;
        public Player Player2    => inner.Player2;

        public void LoadContent()   => inner.LoadContent();
        public void UnloadContent() => inner.UnloadContent();

        public void Update(double elapsedMs)
        {
            inner.Update(elapsedMs);

            if (ActivePlayer != 2)
            {
                aiTimer = AiMoveDelayMs; // reset delay so AI doesn't move immediately on its turn
                return;
            }

            aiTimer -= elapsedMs;
            if (aiTimer > 0) return;
            aiTimer = AiMoveDelayMs;

            ai.TryPlayMove();
        }

        public void MoveOutedPiece(int distance)  => inner.MoveOutedPiece(distance);
        public void MovePiece(int pos, int move)   => inner.MovePiece(pos, move);
        public void MovePieceDirect(int from, int to) => inner.MovePieceDirect(from, to);
        public void BearOffPiece(int from)         => inner.BearOffPiece(from);
        public void ThrowDice()                    => inner.ThrowDice();
        public void NextTurn()                     => inner.NextTurn();

        public void NewGame()
        {
            inner.NewGame();
            aiTimer = AiMoveDelayMs;
        }
    }
}
