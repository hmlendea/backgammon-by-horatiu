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
            else
            {
                // Blocking phase: build primes, hit blots, keep the human trapped
                score += pipLead * 3;
                score -= snapshot.AiOutedPieces * 60;
                score += snapshot.HumanOutedPieces * 60;

                int consecutiveOwnedPoints = 0;
                int longestPrime = 0;

                for (int column = 0; column < 24; column++)
                {
                    if (snapshot.ColumnValues[column] <= -2)
                    {
                        score += column <= 5 ? 60 : 30;
                        consecutiveOwnedPoints++;

                        if (consecutiveOwnedPoints > longestPrime)
                        {
                            longestPrime = consecutiveOwnedPoints;
                        }
                    }
                    else
                    {
                        consecutiveOwnedPoints = 0;
                    }

                    if (snapshot.ColumnValues[column] == -1)
                    {
                        int threatLevel = CalculateThreatLevel(snapshot, column);
                        score -= threatLevel * 20;
                    }
                }

                score += longestPrime * longestPrime * 8;
            }

            return score;
        }

        static GamePhase DetermineGamePhase(int aiPipCount, int humanPipCount)
        {
            int pipDifference = aiPipCount - humanPipCount;

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
