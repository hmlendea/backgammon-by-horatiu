using BackgammonByHoratiu.GameLogic.AI.Search;

namespace BackgammonByHoratiu.GameLogic.AI.Evaluation
{
    internal static class ThreatCalculator
    {
        static readonly int[] TwoDiceSums = [7, 8, 9, 10, 11, 12];

        // Returns a threat score for a single AI blot at the given column.
        // Counts both direct (1-die) and indirect (2-dice) attackers from higher columns.
        internal static int Calculate(BoardSnapshot snapshot, int column)
        {
            return DirectThreat(snapshot, column) + IndirectThreat(snapshot, column);
        }

        static int DirectThreat(BoardSnapshot snapshot, int column)
        {
            int threat = 0;

            for (int distance = 1; distance <= BoardLayout.MaxDieValue; distance++)
            {
                int attackerColumn = column + distance;

                if (attackerColumn < BoardLayout.ColumnCount && snapshot.ColumnValues[attackerColumn] > 0)
                {
                    threat += snapshot.ColumnValues[attackerColumn];
                }
            }

            return threat;
        }

        static int IndirectThreat(BoardSnapshot snapshot, int column)
        {
            int threat = 0;

            foreach (int sum in TwoDiceSums)
            {
                int attackerColumn = column + sum;

                if (attackerColumn >= BoardLayout.ColumnCount || snapshot.ColumnValues[attackerColumn] <= 0)
                {
                    continue;
                }

                int attackerPieces = snapshot.ColumnValues[attackerColumn];
                int waysToCover = CountWaysToCover(sum);
                threat += attackerPieces * waysToCover / BoardLayout.MaxDieValue;
            }

            return threat;
        }

        static int CountWaysToCover(int sum)
        {
            int ways = 0;

            for (int die1 = 1; die1 <= BoardLayout.MaxDieValue; die1++)
            {
                int die2 = sum - die1;

                if (die2 >= 1 && die2 <= BoardLayout.MaxDieValue)
                {
                    ways++;
                }
            }

            return ways;
        }
    }
}
