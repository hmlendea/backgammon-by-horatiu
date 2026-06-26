using BackgammonByHoratiu.GameLogic.AI;
using BackgammonByHoratiu.GameLogic.AI.Search;

namespace BackgammonByHoratiu.GameLogic.AI.Evaluation
{
    internal static class PositionEvaluator
    {
        internal static int Evaluate(BoardSnapshot snapshot)
        {
            int aiPips = BoardMetrics.AiPipCount(snapshot);
            int humanPips = BoardMetrics.HumanPipCount(snapshot);
            int pipLead = humanPips - aiPips;
            GamePhase phase = BoardMetrics.DeterminePhase(aiPips, humanPips);

            int score = ScoreByPhase(snapshot, phase, pipLead);
            score += ScoreHomeBoardClosure(snapshot);
            score += ScoreReturnHitRisk(snapshot);

            return score;
        }

        static int ScoreByPhase(BoardSnapshot snapshot, GamePhase phase, int pipLead)
        {
            if (phase == GamePhase.Racing)
            {
                return ScoreRacing(snapshot, pipLead);
            }

            if (phase == GamePhase.BackGame)
            {
                return ScoreBackGame(snapshot);
            }

            return ScoreBlocking(snapshot, pipLead);
        }

        static int ScoreRacing(BoardSnapshot snapshot, int pipLead)
        {
            int score = 0;
            score += pipLead * AiWeights.PipLeadRacing;
            score -= snapshot.AiOutedPieces * AiWeights.AiBarPenaltyRacing;
            score += snapshot.HumanOutedPieces * AiWeights.HumanBarBonusRacing;
            score += ScoreRacingPoints(snapshot);
            score += ScoreHomeBoardDistribution(snapshot);

            return score;
        }

        static int ScoreRacingPoints(BoardSnapshot snapshot)
        {
            int score = 0;

            for (int column = 0; column < BoardLayout.ColumnCount; column++)
            {
                int columnValue = snapshot.ColumnValues[column];

                if (columnValue <= -2)
                {
                    score += column <= BoardLayout.AiHomeBoardLastColumn ? AiWeights.RacingHomePointBonus : AiWeights.RacingOuterPointBonus;
                }
                else if (columnValue == -1)
                {
                    score -= ThreatCalculator.Calculate(snapshot, column) * AiWeights.BlotThreatRacing;
                }
            }

            return score;
        }

        static int ScoreBlocking(BoardSnapshot snapshot, int pipLead)
        {
            int score = 0;
            score += pipLead * AiWeights.PipLeadBlocking;
            score -= snapshot.AiOutedPieces * AiWeights.AiBarPenaltyBlocking;
            score += snapshot.HumanOutedPieces * AiWeights.HumanBarBonusBlocking;
            score += ScoreBlockingPoints(snapshot);
            score += ScoreAnchorPoints(snapshot);
            score += ScorePrimes(snapshot, maxColumn: BoardLayout.HumanHomeBoardLastColumn, multiplier: AiWeights.PrimeMultiplierBlocking);

            return score;
        }

        static int ScoreBlockingPoints(BoardSnapshot snapshot)
        {
            int score = 0;

            for (int column = 0; column < BoardLayout.ColumnCount; column++)
            {
                int columnValue = snapshot.ColumnValues[column];

                if (columnValue <= -2)
                {
                    score += BlockingPointBonus(column);
                }
                else if (columnValue == -1)
                {
                    score -= ThreatCalculator.Calculate(snapshot, column) * AiWeights.BlotThreatBlocking;
                }
            }

            return score;
        }

        static int BlockingPointBonus(int column)
        {
            if (column <= BoardLayout.AiHomeBoardLastColumn)
            {
                return AiWeights.BlockingHomePointBonus;
            }

            if (column >= BoardLayout.HumanHomeBoardFirstColumn)
            {
                return AiWeights.BlockingAnchorBonus;
            }

            return AiWeights.BlockingOuterPointBonus;
        }

        static int ScoreBackGame(BoardSnapshot snapshot)
        {
            int score = 0;

            score -= snapshot.AiOutedPieces * AiWeights.AiBarPenaltyBackGame;
            score += snapshot.HumanOutedPieces * AiWeights.HumanBarBonusBackGame;
            score += ScoreBackGamePoints(snapshot);
            score += ScorePrimes(snapshot, maxColumn: BoardLayout.AiHomeBoardLastColumn, multiplier: AiWeights.PrimeMultiplierBackGame);

            return score;
        }

        static int ScoreBackGamePoints(BoardSnapshot snapshot)
        {
            int score = 0;

            for (int column = 0; column < BoardLayout.ColumnCount; column++)
            {
                int columnValue = snapshot.ColumnValues[column];

                if (columnValue <= -2)
                {
                    score += BackGamePointBonus(column);
                }
                else if (columnValue == -1)
                {
                    score -= ThreatCalculator.Calculate(snapshot, column) * BackGameBlotMultiplier(column);
                }
            }

            return score;
        }

        static int BackGamePointBonus(int column)
        {
            if (column >= BoardLayout.HumanHomeBoardFirstColumn)
            {
                return AiWeights.BackGameAnchorBonus;
            }

            if (column <= BoardLayout.AiHomeBoardLastColumn)
            {
                return AiWeights.BackGameHomePointBonus;
            }

            return AiWeights.BackGameOuterPointBonus;
        }

        static int BackGameBlotMultiplier(int column)
        {
            if (column >= BoardLayout.HumanHomeBoardFirstColumn)
            {
                return AiWeights.BlotThreatBackGameHome;
            }

            return AiWeights.BlotThreatBackGameOuter;
        }

        static int ScoreAnchorPoints(BoardSnapshot snapshot)
        {
            int totalScore = 0;
            int consecutiveAnchors = 0;

            for (int column = BoardLayout.HumanHomeBoardFirstColumn; column <= BoardLayout.HumanHomeBoardLastColumn; column++)
            {
                if (snapshot.ColumnValues[column] <= -2)
                {
                    consecutiveAnchors++;
                    totalScore += consecutiveAnchors * AiWeights.ConsecutiveAnchorBonus;
                }
                else
                {
                    consecutiveAnchors = 0;
                }
            }

            return totalScore;
        }

        static int ScorePrimes(BoardSnapshot snapshot, int maxColumn, int multiplier)
        {
            int totalScore = 0;
            int primeStart = -1;
            int primeLength = 0;

            for (int column = 0; column <= maxColumn + 1; column++)
            {
                bool isOwnedPoint = column <= maxColumn && snapshot.ColumnValues[column] <= -2;

                if (isOwnedPoint)
                {
                    if (primeStart == -1)
                    {
                        primeStart = column;
                    }

                    primeLength++;
                }
                else if (primeLength > 0)
                {
                    totalScore += ScorePrime(snapshot, primeStart, primeLength, multiplier);
                    primeStart = -1;
                    primeLength = 0;
                }
            }

            return totalScore;
        }

        static int ScorePrime(BoardSnapshot snapshot, int primeStart, int primeLength, int multiplier)
        {
            int trappedPieces = CountTrappedHumanPieces(snapshot, primeStart);
            int trapMultiplier = 1 + trappedPieces;

            int score = primeLength * primeLength * multiplier * trapMultiplier;
            score += PrimeLengthMilestoneBonus(primeLength) * trapMultiplier;

            return score;
        }

        static int CountTrappedHumanPieces(BoardSnapshot snapshot, int primeStart)
        {
            int count = snapshot.HumanOutedPieces;

            for (int column = 0; column < primeStart; column++)
            {
                if (snapshot.ColumnValues[column] > 0)
                {
                    count += snapshot.ColumnValues[column];
                }
            }

            return count;
        }

        static int PrimeLengthMilestoneBonus(int primeLength)
        {
            if (primeLength >= BoardLayout.MaxPrimeLength)
            {
                return AiWeights.PrimeMilestone6Bonus;
            }

            if (primeLength >= BoardLayout.MaxPrimeLength - 1)
            {
                return AiWeights.PrimeMilestone5Bonus;
            }

            if (primeLength >= 4)
            {
                return AiWeights.PrimeMilestone4Bonus;
            }

            return 0;
        }

        // Rewards closing AI home-board points when human pieces are on the bar.
        static int ScoreHomeBoardClosure(BoardSnapshot snapshot)
        {
            if (snapshot.HumanOutedPieces == 0)
            {
                return 0;
            }

            int closedPoints = 0;

            for (int column = BoardLayout.AiHomeBoardFirstColumn; column <= BoardLayout.AiHomeBoardLastColumn; column++)
            {
                if (snapshot.ColumnValues[column] <= -2)
                {
                    closedPoints++;
                }
            }

            return closedPoints * snapshot.HumanOutedPieces * AiWeights.ClosedPointBarBonus;
        }

        // Penalises AI blots that can be hit back after hitting a human piece.
        static int ScoreReturnHitRisk(BoardSnapshot snapshot)
        {
            if (snapshot.HumanOutedPieces == 0)
            {
                return 0;
            }

            int maxReturnThreat = 0;

            for (int column = 0; column < BoardLayout.ColumnCount; column++)
            {
                if (snapshot.ColumnValues[column] == -1)
                {
                    int threat = ThreatCalculator.Calculate(snapshot, column);

                    if (threat > maxReturnThreat)
                    {
                        maxReturnThreat = threat;
                    }
                }
            }

            return -(maxReturnThreat * snapshot.HumanOutedPieces * AiWeights.ReturnHitRiskFactor);
        }

        // Penalises stacking pieces on a single home-board point during bear-off preparation.
        static int ScoreHomeBoardDistribution(BoardSnapshot snapshot)
        {
            int totalPieces = 0;
            int occupiedPoints = 0;

            for (int column = BoardLayout.AiHomeBoardFirstColumn; column <= BoardLayout.AiHomeBoardLastColumn; column++)
            {
                int count = -snapshot.ColumnValues[column];

                if (count > 0)
                {
                    totalPieces += count;
                    occupiedPoints++;
                }
            }

            if (occupiedPoints == 0)
            {
                return 0;
            }

            int averagePerPoint = totalPieces / occupiedPoints;
            int penalty = 0;

            for (int column = BoardLayout.AiHomeBoardFirstColumn; column <= BoardLayout.AiHomeBoardLastColumn; column++)
            {
                int count = -snapshot.ColumnValues[column];

                if (count > averagePerPoint)
                {
                    int excess = count - averagePerPoint;
                    penalty += excess * excess * AiWeights.DistributionPenaltyFactor;
                }
            }

            return -penalty;
        }
    }
}
