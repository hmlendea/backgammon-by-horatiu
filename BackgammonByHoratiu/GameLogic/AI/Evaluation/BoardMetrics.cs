using System;
using BackgammonByHoratiu.GameLogic.AI.Search;

namespace BackgammonByHoratiu.GameLogic.AI.Evaluation
{
    internal static class BoardMetrics
    {
        internal static int AiPipCount(BoardSnapshot snapshot)
        {
            int pipCount = snapshot.AiOutedPieces * AiWeights.BarPipEquivalent;

            for (int column = 0; column < BoardLayout.ColumnCount; column++)
            {
                if (snapshot.ColumnValues[column] < 0)
                {
                    pipCount += Math.Abs(snapshot.ColumnValues[column]) * (column + 1);
                }
            }

            return pipCount;
        }

        internal static int HumanPipCount(BoardSnapshot snapshot)
        {
            int pipCount = snapshot.HumanOutedPieces * AiWeights.BarPipEquivalent;

            for (int column = 0; column < BoardLayout.ColumnCount; column++)
            {
                if (snapshot.ColumnValues[column] > 0)
                {
                    pipCount += snapshot.ColumnValues[column] * (BoardLayout.ColumnCount - column);
                }
            }

            return pipCount;
        }

        internal static GamePhase DeterminePhase(int aiPipCount, int humanPipCount)
        {
            int pipDifference = aiPipCount - humanPipCount;

            if (pipDifference > AiWeights.BackGamePipThreshold)
            {
                return GamePhase.BackGame;
            }

            if (pipDifference < -AiWeights.RacingPipThreshold)
            {
                return GamePhase.Racing;
            }

            return GamePhase.Blocking;
        }
    }
}
