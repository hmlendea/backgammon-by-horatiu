using System;
using System.Collections.Generic;

using BackgammonByHoratiu.Entities;
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
            Player aiPlayer = game.Player2;

            if (aiPlayer.MovesLeft.Count == 0)
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
            BoardSnapshot currentSnapshot = new(game);
            MoveSequenceResult bestResult = FindBestMoveSequence(currentSnapshot);

            foreach (MoveAction action in bestResult.Actions)
            {
                plannedMoves.Enqueue(action);
            }
        }

        MoveSequenceResult FindBestMoveSequence(BoardSnapshot snapshot)
        {
            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            if (legalMoves.Count == 0)
            {
                return new MoveSequenceResult(EvaluatePosition(snapshot), []);
            }

            int bestScore = int.MinValue;
            List<MoveAction> bestSequence = [];

            foreach (MoveAction action in legalMoves)
            {
                BoardSnapshot nextSnapshot = snapshot.AfterMove(action);
                MoveSequenceResult continuationResult = FindBestMoveSequence(nextSnapshot);

                if (continuationResult.Score > bestScore)
                {
                    bestScore = continuationResult.Score;
                    bestSequence = PrependAction(action, continuationResult.Actions);
                }
            }

            return new MoveSequenceResult(bestScore, bestSequence);
        }

        static List<MoveAction> PrependAction(MoveAction first, List<MoveAction> rest)
        {
            List<MoveAction> sequence = new(rest.Count + 1);
            sequence.Add(first);
            sequence.AddRange(rest);

            return sequence;
        }

        void ExecuteNextPlannedMove()
        {
            MoveAction action = plannedMoves.Dequeue();

            try
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
            catch (PieceMoveException)
            {
                plannedMoves.Clear();
            }
        }

        int EvaluatePosition(BoardSnapshot snapshot)
        {
            int score = 0;
            int aiPipCount = CalculateAiPipCount(snapshot);
            int humanPipCount = CalculateHumanPipCount(snapshot);
            int pipLead = humanPipCount - aiPipCount;

            GamePhase phase = DetermineGamePhase(aiPipCount, humanPipCount);

            if (phase == GamePhase.Racing)
            {
                score += pipLead * 10;
                score -= snapshot.AiOutedPieces * 80;
                score += snapshot.HumanOutedPieces * 30;

                for (int column = 0; column < 24; column++)
                {
                    if (snapshot.ColumnValues[column] <= -2)
                    {
                        score += column <= 5 ? 20 : 5;
                    }

                    if (snapshot.ColumnValues[column] == -1)
                    {
                        int threatLevel = CalculateThreatLevel(snapshot, column);
                        score -= threatLevel * 8;
                    }
                }
            }
            else if (phase == GamePhase.BackGame)
            {
                score -= snapshot.AiOutedPieces * 50;
                score += snapshot.HumanOutedPieces * 80;

                for (int column = 0; column < 24; column++)
                {
                    if (snapshot.ColumnValues[column] <= -2)
                    {
                        if (column >= 18)
                        {
                            score += 120;
                        }
                        else if (column <= 5)
                        {
                            score += 70;
                        }
                        else
                        {
                            score += 10;
                        }
                    }

                    if (snapshot.ColumnValues[column] == -1)
                    {
                        int threatLevel = CalculateThreatLevel(snapshot, column);
                        bool isInHumanHomeBoard = column >= 18;
                        score -= isInHumanHomeBoard ? threatLevel * 5 : threatLevel * 20;
                    }
                }

                score += ScorePrimes(snapshot, maxColumn: 5, primeBaseMultiplier: 10);
            }
            else
            {
                score += pipLead * 3;
                score -= snapshot.AiOutedPieces * 60;
                score += snapshot.HumanOutedPieces * 60;

                for (int column = 0; column < 24; column++)
                {
                    if (snapshot.ColumnValues[column] <= -2)
                    {
                        if (column <= 5)
                        {
                            score += 60;
                        }
                        else if (column >= 18)
                        {
                            score += 80;
                        }
                        else
                        {
                            score += 25;
                        }
                    }

                    if (snapshot.ColumnValues[column] == -1)
                    {
                        int threatLevel = CalculateThreatLevel(snapshot, column);
                        score -= threatLevel * 20;
                    }
                }

                score += ScoreAnchorPoints(snapshot);
                score += ScorePrimes(snapshot, maxColumn: 23, primeBaseMultiplier: 8);
            }

            score += ScoreHomeBoardClosure(snapshot);

            return score;
        }

        static int ScoreHomeBoardClosure(BoardSnapshot snapshot)
        {
            if (snapshot.HumanOutedPieces == 0)
            {
                return 0;
            }

            int closedPoints = 0;

            for (int column = 0; column <= 5; column++)
            {
                if (snapshot.ColumnValues[column] <= -2)
                {
                    closedPoints++;
                }
            }

            return closedPoints * snapshot.HumanOutedPieces * 80;
        }

        static int ScoreAnchorPoints(BoardSnapshot snapshot)
        {
            int totalScore = 0;
            int consecutiveAnchors = 0;

            for (int column = 18; column <= 23; column++)
            {
                if (snapshot.ColumnValues[column] <= -2)
                {
                    consecutiveAnchors++;
                    totalScore += consecutiveAnchors * 20;
                }
                else
                {
                    consecutiveAnchors = 0;
                }
            }

            return totalScore;
        }

        static int ScorePrimes(BoardSnapshot snapshot, int maxColumn, int primeBaseMultiplier)
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
                    int trappedHumanPieces = 0;

                    for (int behindColumn = 0; behindColumn < primeStart; behindColumn++)
                    {
                        if (snapshot.ColumnValues[behindColumn] > 0)
                        {
                            trappedHumanPieces += snapshot.ColumnValues[behindColumn];
                        }
                    }

                    trappedHumanPieces += snapshot.HumanOutedPieces;

                    int trapMultiplier = 1 + trappedHumanPieces;
                    totalScore += primeLength * primeLength * primeBaseMultiplier * trapMultiplier;

                    if (primeLength >= 6)
                    {
                        totalScore += 200 * trapMultiplier;
                    }
                    else if (primeLength >= 5)
                    {
                        totalScore += 100 * trapMultiplier;
                    }
                    else if (primeLength >= 4)
                    {
                        totalScore += 40 * trapMultiplier;
                    }

                    primeStart = -1;
                    primeLength = 0;
                }
            }

            return totalScore;
        }

        static GamePhase DetermineGamePhase(int aiPipCount, int humanPipCount)
        {
            int pipDifference = aiPipCount - humanPipCount;

            if (pipDifference > 40)
            {
                return GamePhase.BackGame;
            }

            if (pipDifference < -15)
            {
                return GamePhase.Racing;
            }

            return GamePhase.Blocking;
        }

        static int CalculateAiPipCount(BoardSnapshot snapshot)
        {
            int pipCount = snapshot.AiOutedPieces * 25;

            for (int column = 0; column < 24; column++)
            {
                if (snapshot.ColumnValues[column] < 0)
                {
                    pipCount += Math.Abs(snapshot.ColumnValues[column]) * (column + 1);
                }
            }

            return pipCount;
        }

        static int CalculateHumanPipCount(BoardSnapshot snapshot)
        {
            int pipCount = snapshot.HumanOutedPieces * 25;

            for (int column = 0; column < 24; column++)
            {
                if (snapshot.ColumnValues[column] > 0)
                {
                    pipCount += snapshot.ColumnValues[column] * (24 - column);
                }
            }

            return pipCount;
        }

        static int CalculateThreatLevel(BoardSnapshot snapshot, int column)
        {
            int totalThreat = 0;

            for (int distance = 1; distance <= 6; distance++)
            {
                int attackerColumn = column + distance;

                if (attackerColumn < 24 && snapshot.ColumnValues[attackerColumn] > 0)
                {
                    totalThreat += snapshot.ColumnValues[attackerColumn];
                }
            }

            int[] twodiceSums = [7, 8, 9, 10, 11, 12];

            foreach (int sum in twodiceSums)
            {
                int attackerColumn = column + sum;

                if (attackerColumn >= 24 || snapshot.ColumnValues[attackerColumn] <= 0)
                {
                    continue;
                }

                int attackerPieces = snapshot.ColumnValues[attackerColumn];
                int waysToCover = CountWaysToCover(sum);
                totalThreat += attackerPieces * waysToCover / 6;
            }

            return totalThreat;
        }

        static int CountWaysToCover(int sum)
        {
            int ways = 0;

            for (int die1 = 1; die1 <= 6; die1++)
            {
                int die2 = sum - die1;

                if (die2 >= 1 && die2 <= 6)
                {
                    ways++;
                }
            }

            return ways;
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
