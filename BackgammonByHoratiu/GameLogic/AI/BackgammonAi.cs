using System;
using System.Collections.Generic;

using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.GameManagers;

namespace BackgammonByHoratiu.GameLogic.AI
{
    internal sealed class BackgammonAi
    {
        readonly IGameManager game;

        internal BackgammonAi(IGameManager game)
        {
            this.game = game;
        }

        internal void TryPlayMove()
        {
            Player aiPlayer = game.Player2;

            if (aiPlayer.MovesLeft.Count == 0)
            {
                TryNextTurn();

                return;
            }

            if (aiPlayer.OutedPieces > 0)
            {
                if (!TryBarEntry())
                {
                    TryNextTurn();
                }

                return;
            }

            if (CanBearOff())
            {
                if (!TryBearOff())
                {
                    TryNextTurn();
                }

                return;
            }

            if (!TryNormalMove())
            {
                TryNextTurn();
            }
        }

        bool TryBarEntry()
        {
            List<int> availableMoves = [.. game.Player2.MovesLeft];

            // Two passes: prefer hitting a blot, fall back to any open point
            foreach (bool hitsOnly in new[] { true, false })
            {
                HashSet<int> alreadyTried = [];

                foreach (int dieValue in availableMoves)
                {
                    if (!alreadyTried.Add(dieValue))
                    {
                        continue;
                    }

                    int destinationColumn = 24 - dieValue;
                    bool hitsOpponentBlot = game.TableValues[destinationColumn] == 1;
                    bool isOpen = game.TableValues[destinationColumn] <= 0;

                    if (hitsOnly && !hitsOpponentBlot)
                    {
                        continue;
                    }

                    if (!hitsOnly && !isOpen && !hitsOpponentBlot)
                    {
                        continue;
                    }

                    try
                    {
                        game.MoveOutedPiece(dieValue);

                        return true;
                    }
                    catch { }
                }
            }

            return false;
        }

        bool TryBearOff()
        {
            for (int column = 5; column >= 0; column--)
            {
                if (game.TableValues[column] >= 0)
                {
                    continue;
                }

                try
                {
                    game.BearOffPiece(column);

                    return true;
                }
                catch { }
            }

            return false;
        }

        bool TryNormalMove()
        {
            List<MoveCandidate> candidates = [];
            HashSet<MoveKey> alreadyConsidered = [];

            foreach (int dieValue in game.Player2.MovesLeft)
            {
                for (int column = 0; column < 24; column++)
                {
                    if (game.TableValues[column] >= 0)
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

                    if (game.TableValues[destinationColumn] >= 2)
                    {
                        continue;
                    }

                    candidates.Add(new MoveCandidate(column, dieValue, ScoreMove(column, destinationColumn)));
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            candidates.Sort((first, second) => second.Score.CompareTo(first.Score));

            foreach (MoveCandidate candidate in candidates)
            {
                try
                {
                    game.MovePiece(candidate.SourceColumn, candidate.DieValue);

                    return true;
                }
                catch { }
            }

            return false;
        }

        int ScoreMove(int sourceColumn, int destinationColumn)
        {
            int[] boardValues = game.TableValues;
            int score = 0;
            int destinationPieceCount = boardValues[destinationColumn];

            // Don't move home-board pieces while there are stragglers in the far quadrant
            if (sourceColumn < 6)
            {
                for (int farColumn = 18; farColumn < 24; farColumn++)
                {
                    if (boardValues[farColumn] < 0)
                    {
                        score -= 500;

                        break;
                    }
                }
            }

            if (destinationPieceCount == 1)
            {
                score += 150;
            }
            else if (destinationPieceCount <= -2)
            {
                score += destinationColumn <= 5 ? 120 : 80;
            }
            else if (destinationPieceCount == -1)
            {
                score += destinationColumn <= 5 ? 160 : 60;   // home-board gates are high priority
            }
            else
            {
                int blotPenalty = ThreatLevel(destinationColumn) * 25;

                if (sourceColumn >= 18)
                {
                    blotPenalty /= 2;
                }
                else if (sourceColumn < 6)
                {
                    blotPenalty = (int)(blotPenalty * 1.5f);
                }

                score -= blotPenalty;
            }

            if (boardValues[sourceColumn] == -2)
            {
                int sourceBecomesBlotPenalty = ThreatLevel(sourceColumn) * 20;

                if (sourceColumn >= 18)
                {
                    sourceBecomesBlotPenalty /= 2;
                }

                score -= sourceBecomesBlotPenalty;
            }

            float humanWinProgress = HumanProgress();
            int urgencyBonus = (int)(humanWinProgress * 300);

            if (sourceColumn >= 18)
            {
                score += 250 + sourceColumn * 5 + urgencyBonus;
            }
            else if (sourceColumn >= 12)
            {
                score += 80 + sourceColumn * 3 + urgencyBonus / 2;
            }
            else if (sourceColumn >= 6)
            {
                score += 40 + sourceColumn * 2;
            }

            if (sourceColumn >= 6 && destinationColumn <= 5)
            {
                score += 80;
            }

            if (sourceColumn >= 6)
            {
                score += (23 - destinationColumn) * 2;
            }
            else
            {
                score += 23 - destinationColumn;
            }

            return score;
        }

        // Sum of opponent pieces within single-die striking range of a column
        int ThreatLevel(int column)
        {
            int[] boardValues = game.TableValues;
            int totalThreat = 0;

            for (int distance = 1; distance <= 6; distance++)
            {
                int attackerColumn = column + distance;

                if (attackerColumn < 24 && boardValues[attackerColumn] > 0)
                {
                    totalThreat += boardValues[attackerColumn];
                }
            }

            return totalThreat;
        }

        // 0..1: how close the human is to winning, based on pip count
        float HumanProgress()
        {
            int[] boardValues = game.TableValues;
            int pipsRemaining = 0;

            for (int column = 0; column < 24; column++)
            {
                if (boardValues[column] > 0)
                {
                    pipsRemaining += boardValues[column] * (24 - column);
                }
            }

            pipsRemaining += game.Player1.OutedPieces * 25;

            const int maxPips = 167;

            return 1f - Math.Min(pipsRemaining, maxPips) / (float)maxPips;
        }

        bool CanBearOff()
        {
            if (game.Player2.OutedPieces > 0)
            {
                return false;
            }

            for (int column = 6; column < 24; column++)
            {
                if (game.TableValues[column] < 0)
                {
                    return false;
                }
            }

            return true;
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
