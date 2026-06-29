using System;
using System.Collections.Generic;

using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.AI;
using BackgammonByHoratiu.Settings;

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

        bool isInsideAiDispatch;
        bool isWaitingForAnimation;

        /// <summary>
        /// Raised when the AI wants to move a piece and animation is wired up.
        /// Parameters: fromCol, toCol, activePlayer (always 2), onComplete callback.
        /// The subscriber must call onComplete once the visual animation finishes.
        /// </summary>
        public event Action<int, int, int, Action> AnimateMoveRequested;

        public AiGameManager()
        {
            ai = new BackgammonAi(this);
        }

        public bool IsRunning => inner.IsRunning;
        public int[] TableValues => inner.TableValues;
        public int Dice1 => inner.Dice1;
        public int Dice2 => inner.Dice2;
        public int ActivePlayer => inner.ActivePlayer;
        public Player Player1 => inner.Player1;
        public Player Player2 => inner.Player2;

        public void LoadContent() => inner.LoadContent();
        public void UnloadContent() => inner.UnloadContent();

        public void Update(double elapsedMs)
        {
            inner.Update(elapsedMs);

            if (ActivePlayer != 2)
            {
                aiTimer = AiMoveDelayMs;
                return;
            }

            if (isWaitingForAnimation)
            {
                return;
            }

            aiTimer -= elapsedMs;

            if (aiTimer > 0)
            {
                return;
            }

            aiTimer = AiMoveDelayMs;
            isInsideAiDispatch = true;
            ai.TryPlayMove();
            isInsideAiDispatch = false;
        }

        public void MoveOutedPiece(int distance)
        {
            if (isInsideAiDispatch && AnimateMoveRequested is not null)
            {
                int toCol = 24 - distance;
                isWaitingForAnimation = true;
                isInsideAiDispatch = false;
                AnimateMoveRequested.Invoke(GameDefines.ColBarP2, toCol, 2, () =>
                {
                    inner.MoveOutedPiece(distance);
                    isWaitingForAnimation = false;
                });
            }
            else
            {
                inner.MoveOutedPiece(distance);
            }
        }

        public void MovePiece(int pos, int move)
        {
            if (isInsideAiDispatch && AnimateMoveRequested is not null)
            {
                int toCol = pos - move;  // Player 2 pieces move from high to low column
                isWaitingForAnimation = true;
                isInsideAiDispatch = false;
                AnimateMoveRequested.Invoke(pos, toCol, 2, () =>
                {
                    inner.MovePiece(pos, move);
                    isWaitingForAnimation = false;
                });
            }
            else
            {
                inner.MovePiece(pos, move);
            }
        }

        public void MovePieceDirect(int from, int to) => inner.MovePieceDirect(from, to);

        public int FindMovePieceDirectIntermediate(int from, int to) => inner.FindMovePieceDirectIntermediate(from, to);

        public int FindMoveOutedPieceIntermediate(int distance) => inner.FindMoveOutedPieceIntermediate(distance);

        public void BearOffPiece(int from)
        {
            if (isInsideAiDispatch && AnimateMoveRequested is not null)
            {
                isWaitingForAnimation = true;
                isInsideAiDispatch = false;
                AnimateMoveRequested.Invoke(from, GameDefines.ColHouseP2, 2, () =>
                {
                    inner.BearOffPiece(from);
                    isWaitingForAnimation = false;
                });
            }
            else
            {
                inner.BearOffPiece(from);
            }
        }

        public IReadOnlyList<int> GetValidDestinations(int fromCol) => inner.GetValidDestinations(fromCol);

        public void ThrowDice() => inner.ThrowDice();
        public void NextTurn() => inner.NextTurn();

        public void NewGame()
        {
            inner.NewGame();
            ai.Reset();
            aiTimer = AiMoveDelayMs;
            isWaitingForAnimation = false;
            isInsideAiDispatch = false;
        }
    }
}
