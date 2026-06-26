using System.Collections.Generic;

using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.AI.Search;
using BackgammonByHoratiu.GameLogic.GameManagers;

namespace BackgammonByHoratiu.GameLogic.AI
{
    internal sealed class BackgammonAi
    {
        readonly IGameManager game;
        readonly Queue<MoveAction> plannedMoves;

        internal BackgammonAi(IGameManager game)
        {
            this.game = game;
            plannedMoves = new Queue<MoveAction>();
        }

        internal void Reset()
        {
            plannedMoves.Clear();
        }

        internal void TryPlayMove()
        {
            if (game.Player2.MovesLeft.Count == 0)
            {
                plannedMoves.Clear();
                TryNextTurn();

                return;
            }

            if (plannedMoves.Count == 0)
            {
                PlanTurn();
            }

            if (plannedMoves.Count == 0)
            {
                TryNextTurn();

                return;
            }

            ExecuteNextPlannedMove();
        }

        void PlanTurn()
        {
            BoardSnapshot snapshot = new(game);
            MoveSequenceResult bestSequence = MoveSearcher.FindBestSequence(snapshot);

            foreach (MoveAction action in bestSequence.Actions)
            {
                plannedMoves.Enqueue(action);
            }
        }

        void ExecuteNextPlannedMove()
        {
            MoveAction action = plannedMoves.Dequeue();

            try
            {
                DispatchMove(action);
            }
            catch (PieceMoveException)
            {
                plannedMoves.Clear();
            }
        }

        void DispatchMove(MoveAction action)
        {
            switch (action.Type)
            {
                case MoveActionType.Normal:
                    game.MovePiece(action.SourceColumn, action.DieValue);
                    break;

                case MoveActionType.BarEntry:
                    game.MoveOutedPiece(action.DieValue);
                    break;

                case MoveActionType.BearOff:
                    game.BearOffPiece(action.SourceColumn);
                    break;
            }
        }

        void TryNextTurn()
        {
            try
            {
                game.NextTurn();
            }
            catch (PieceMoveException)
            {
            }
        }
    }
}
