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
                // In a race, pip lead is everything; structure rewards are muted
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
                // Far behind in pips: anchor in the human's home board and build a home prime
                score -= snapshot.AiOutedPieces * 50;
                score += snapshot.HumanOutedPieces * 80;

                for (int column = 0; column < 24; column++)
                {
                    if (snapshot.ColumnValues[column] <= -2)
                    {
                        if (column >= 18)
                        {
                            // Anchor point in the human's home board — very high value
                            score += 120;
                        }
                        else if (column <= 5)
                        {
                            // Home-board point — valuable for trapping hit pieces
                            score += 70;
                        }
                        else
                        {
                            score += 10;
                        }
                    }

                    // Blot exposure still matters, but less so in the human's board
                    if (snapshot.ColumnValues[column] == -1)
                    {
                        int threatLevel = CalculateThreatLevel(snapshot, column);
                        bool isInHumanHomeBoard = column >= 18;
                        score -= isInHumanHomeBoard ? threatLevel * 5 : threatLevel * 20;
                    }
                }

                // Home-board prime for trapping hit pieces
                score += ScorePrimes(snapshot, maxColumn: 5, primeBaseMultiplier: 10);
            }
            else
            {
                // Blocking phase: build primes, hit blots, keep the human trapped
                score += pipLead * 3;
                score -= snapshot.AiOutedPieces * 60;
                score += snapshot.HumanOutedPieces * 60;

                for (int column = 0; column < 24; column++)
                {
                    if (snapshot.ColumnValues[column] <= -2)
                    {
                        score += column <= 5 ? 60 : 30;
                    }

                    if (snapshot.ColumnValues[column] == -1)
                    {
                        int threatLevel = CalculateThreatLevel(snapshot, column);
                        score -= threatLevel * 20;
                    }
                }

                score += ScorePrimes(snapshot, maxColumn: 23, primeBaseMultiplier: 8);
            }

            return score;
        }

        // Scores all prime sequences on the board (columns 0..maxColumn).
        // Each prime is weighted by its length² × multiplier and by how many human
        // pieces it actually traps behind it. Milestones at length 4, 5, 6 add extra flat bonuses.
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
                    // Count human pieces trapped behind (at lower columns) this prime
                    int trappedHumanPieces = 0;

                    for (int behindColumn = 0; behindColumn < primeStart; behindColumn++)
                    {
                        if (snapshot.ColumnValues[behindColumn] > 0)
                        {
                            trappedHumanPieces += snapshot.ColumnValues[behindColumn];
                        }
                    }

                    // Also count human pieces on the bar (they must re-enter from the far end)
                    trappedHumanPieces += snapshot.HumanOutedPieces;

                    int trapMultiplier = 1 + trappedHumanPieces;
                    totalScore += primeLength * primeLength * primeBaseMultiplier * trapMultiplier;

                    // Milestone bonuses for powerful primes
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

            return totalThreat;
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
