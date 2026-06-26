using System.Collections.Generic;

using BackgammonByHoratiu.GameLogic.GameManagers;

namespace BackgammonByHoratiu.GameLogic.AI
{
    internal sealed class BoardSnapshot
    {
        internal int[] ColumnValues { get; }
        internal int AiOutedPieces { get; private set; }
        internal int HumanOutedPieces { get; private set; }
        internal List<int> MovesLeft { get; }

        internal BoardSnapshot(IGameManager game)
        {
            ColumnValues = (int[])game.TableValues.Clone();
            AiOutedPieces = game.Player2.OutedPieces;
            HumanOutedPieces = game.Player1.OutedPieces;
            MovesLeft = [.. game.Player2.MovesLeft];
        }

        BoardSnapshot(BoardSnapshot source)
        {
            ColumnValues = (int[])source.ColumnValues.Clone();
            AiOutedPieces = source.AiOutedPieces;
            HumanOutedPieces = source.HumanOutedPieces;
            MovesLeft = [.. source.MovesLeft];
        }

        internal List<MoveAction> GetLegalMoves()
        {
            List<MoveAction> legalMoves = [];

            if (AiOutedPieces > 0)
            {
                CollectBarEntryMoves(legalMoves);
            }
            else if (CanBearOff())
            {
                CollectBearOffMoves(legalMoves);
            }
            else
            {
                CollectNormalMoves(legalMoves);
            }

            return legalMoves;
        }

        void CollectBarEntryMoves(List<MoveAction> legalMoves)
        {
            HashSet<int> triedDiceValues = [];

            foreach (int dieValue in MovesLeft)
            {
                if (!triedDiceValues.Add(dieValue))
                {
                    continue;
                }

                int destinationColumn = 24 - dieValue;

                if (ColumnValues[destinationColumn] <= 1)
                {
                    legalMoves.Add(new MoveAction(MoveActionType.BarEntry, -1, destinationColumn, dieValue));
                }
            }
        }

        void CollectBearOffMoves(List<MoveAction> legalMoves)
        {
            HashSet<MoveKey> alreadyConsidered = [];

            foreach (int dieValue in MovesLeft)
            {
                for (int column = 0; column <= 5; column++)
                {
                    if (ColumnValues[column] >= 0)
                    {
                        continue;
                    }

                    if (!alreadyConsidered.Add(new MoveKey(column, dieValue)))
                    {
                        continue;
                    }

                    int distance = column + 1;

                    if (distance == dieValue || (dieValue > distance && IsFarthestAiPiece(column)))
                    {
                        legalMoves.Add(new MoveAction(MoveActionType.BearOff, column, -1, dieValue));
                    }
                }
            }
        }

        void CollectNormalMoves(List<MoveAction> legalMoves)
        {
            HashSet<MoveKey> alreadyConsidered = [];

            foreach (int dieValue in MovesLeft)
            {
                for (int column = 0; column < 24; column++)
                {
                    if (ColumnValues[column] >= 0)
                    {
                        continue;
                    }

                    if (!alreadyConsidered.Add(new MoveKey(column, dieValue)))
                    {
                        continue;
                    }

                    int destinationColumn = column - dieValue;

                    if (destinationColumn < 0)
                    {
                        continue;
                    }

                    if (ColumnValues[destinationColumn] <= 1)
                    {
                        legalMoves.Add(new MoveAction(MoveActionType.Normal, column, destinationColumn, dieValue));
                    }
                }
            }
        }

        internal BoardSnapshot AfterMove(MoveAction action)
        {
            BoardSnapshot nextState = new(this);

            switch (action.Type)
            {
                case MoveActionType.BarEntry:
                    nextState.ApplyBarEntry(action.DestinationColumn, action.DieValue);
                    break;

                case MoveActionType.BearOff:
                    nextState.ApplyBearOff(action.SourceColumn, action.DieValue);
                    break;

                case MoveActionType.Normal:
                    nextState.ApplyNormalMove(action.SourceColumn, action.DestinationColumn, action.DieValue);
                    break;
            }

            return nextState;
        }

        void ApplyBarEntry(int destinationColumn, int dieValue)
        {
            if (ColumnValues[destinationColumn] == 1)
            {
                HumanOutedPieces++;
                ColumnValues[destinationColumn] = 0;
            }

            ColumnValues[destinationColumn]--;
            AiOutedPieces--;
            MovesLeft.Remove(dieValue);
        }

        void ApplyBearOff(int sourceColumn, int dieValue)
        {
            ColumnValues[sourceColumn]++;
            MovesLeft.Remove(dieValue);
        }

        void ApplyNormalMove(int sourceColumn, int destinationColumn, int dieValue)
        {
            if (ColumnValues[destinationColumn] == 1)
            {
                HumanOutedPieces++;
                ColumnValues[destinationColumn] = 0;
            }

            ColumnValues[sourceColumn]++;
            ColumnValues[destinationColumn]--;
            MovesLeft.Remove(dieValue);
        }

        bool CanBearOff()
        {
            if (AiOutedPieces > 0)
            {
                return false;
            }

            for (int column = 6; column < 24; column++)
            {
                if (ColumnValues[column] < 0)
                {
                    return false;
                }
            }

            return true;
        }

        bool IsFarthestAiPiece(int column)
        {
            for (int checkColumn = column + 1; checkColumn <= 5; checkColumn++)
            {
                if (ColumnValues[checkColumn] < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
