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
            Player ai = game.Player2;

            if (ai.MovesLeft.Count == 0)
            {
                TryNextTurn();
                return;
            }

            if (ai.OutedPieces > 0)
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
            List<int> moves = [.. game.Player2.MovesLeft];

            // Two passes: prefer hitting a blot, fall back to any open point
            foreach (bool hitsOnly in new[] { true, false })
            {
                var tried = new HashSet<int>();
                foreach (int d in moves)
                {
                    if (!tried.Add(d))
                    {
                        continue;
                    }

                    int col = 24 - d;
                    bool hits = game.TableValues[col] == 1;
                    bool open = game.TableValues[col] <= 0;

                    if (hitsOnly && !hits)
                    {
                        continue;
                    }

                    if (!hitsOnly && !open && !hits)
                    {
                        continue;
                    }

                    try { game.MoveOutedPiece(d); return true; } catch { }
                }
            }

            return false;
        }

        bool TryBearOff()
        {
            for (int i = 5; i >= 0; i--)
            {
                if (game.TableValues[i] >= 0)
                {
                    continue;
                }

                try { game.BearOffPiece(i); return true; } catch { }
            }
            return false;
        }

        bool TryNormalMove()
        {
            List<(int pos, int die, int score)> candidates = [];
            HashSet<(int, int)> triedCombos = [];

            foreach (int d in game.Player2.MovesLeft)
            {
                for (int i = 0; i < 24; i++)
                {
                    if (game.TableValues[i] >= 0)
                    {
                        continue;
                    }

                    if (!triedCombos.Add((i, d)))
                    {
                        continue;
                    }

                    int target = i - d;

                    if (target < 0)
                    {
                        continue;
                    }

                    if (game.TableValues[target] >= 2)
                    {
                        continue;
                    }

                    candidates.Add((i, d, ScoreMove(i, target)));
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            candidates.Sort((a, b) => b.score.CompareTo(a.score));

            foreach (var (pos, die, _) in candidates)
            {
                try
                {
                    game.MovePiece(pos, die);

                    return true;
                }
                catch { }
            }

            return false;
        }

        int ScoreMove(int from, int to)
        {
            int[] tv = game.TableValues;
            int score = 0;
            int targetVal = tv[to];

            // Don't move home-board pieces while there are stragglers in the far quadrant
            if (from < 6)
            {
                for (int i = 18; i < 24; i++)
                {
                    if (tv[i] < 0) { score -= 500; break; }
                }
            }

            if (targetVal == 1)
            {
                score += 150;
            }
            else if (targetVal <= -2)
            {
                score += to <= 5 ? 120 : 80;
            }
            else if (targetVal == -1)
            {
                score += to <= 5 ? 160 : 60;   // home-board gates are high priority
            }
            else
            {
                // Blot penalty proportional to actual threat; reduced for far-back pieces
                int blotPenalty = ThreatLevel(to) * 25;
                if (from >= 18)
                {
                    blotPenalty /= 2;
                }
                else if (from < 6)
                {
                    blotPenalty = (int)(blotPenalty * 1.5f);
                }

                score -= blotPenalty;
            }

            // Uncovering a source blot; proportional to threat, reduced for far-back pieces
            if (tv[from] == -2)
            {
                int sourcePenalty = ThreatLevel(from) * 20;
                if (from >= 18)
                {
                    sourcePenalty /= 2;
                }

                score -= sourcePenalty;
            }

            // Urgency for outer-board pieces, amplified by how close the human is to winning
            float pressure = HumanProgress();
            int pressureBonus = (int)(pressure * 300);

            if (from >= 18)
            {
                score += 250 + from * 5 + pressureBonus;
            }
            else if (from >= 12)
            {
                score += 80 + from * 3 + pressureBonus / 2;
            }
            else if (from >= 6)
            {
                score += 40 + from * 2;
            }

            if (from >= 6 && to <= 5)
            {
                score += 80;
            }

            score += from >= 6 ? (23 - to) * 2 : (23 - to);

            return score;
        }

        // Sum of opponent pieces within single-die striking range of col
        int ThreatLevel(int col)
        {
            int[] tv = game.TableValues;
            int threat = 0;
            for (int dist = 1; dist <= 6; dist++)
            {
                int attacker = col + dist;
                if (attacker < 24 && tv[attacker] > 0)
                {
                    threat += tv[attacker];
                }
            }
            return threat;
        }

        // 0..1: how close the human is to winning, based on pip count
        float HumanProgress()
        {
            int[] tv = game.TableValues;
            int pipsLeft = 0;
            for (int i = 0; i < 24; i++)
            {
                if (tv[i] > 0)
                {
                    pipsLeft += tv[i] * (24 - i);
                }
            }

            pipsLeft += game.Player1.OutedPieces * 25;
            const int maxPips = 167;

            return 1f - System.Math.Min(pipsLeft, maxPips) / (float)maxPips;
        }

        bool CanBearOff()
        {
            if (game.Player2.OutedPieces > 0)
            {
                return false;
            }

            for (int i = 6; i < 24; i++)
            {
                if (game.TableValues[i] < 0)
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
