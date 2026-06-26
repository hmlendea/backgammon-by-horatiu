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
                    TryNextTurn();
                return;
            }

            if (CanBearOff())
            {
                if (!TryBearOff())
                    TryNextTurn();
                return;
            }

            if (!TryNormalMove())
                TryNextTurn();
        }

        bool TryBarEntry()
        {
            var moves = new List<int>(game.Player2.MovesLeft);

            // Prefer hitting a blot; fall back to any open point
            foreach (bool hitsOnly in new[] { true, false })
            {
                var tried = new HashSet<int>();
                foreach (int d in moves)
                {
                    if (!tried.Add(d)) continue;
                    int col = 24 - d;
                    bool hits = game.TableValues[col] == 1;
                    bool open = game.TableValues[col] <= 0;

                    if (hitsOnly && !hits) continue;
                    if (!hitsOnly && !open && !hits) continue;

                    try { game.MoveOutedPiece(d); return true; } catch { }
                }
            }

            return false;
        }

        bool TryBearOff()
        {
            for (int i = 5; i >= 0; i--)
            {
                if (game.TableValues[i] >= 0) continue;
                try { game.BearOffPiece(i); return true; } catch { }
            }
            return false;
        }

        bool TryNormalMove()
        {
            var candidates = new List<(int pos, int die, int score)>();
            var triedCombos = new HashSet<(int, int)>();

            foreach (int d in game.Player2.MovesLeft)
            {
                for (int i = 0; i < 24; i++)
                {
                    if (game.TableValues[i] >= 0) continue;
                    if (!triedCombos.Add((i, d))) continue;

                    int target = i - d;
                    if (target < 0) continue;
                    if (game.TableValues[target] >= 2) continue;

                    candidates.Add((i, d, ScoreMove(i, target)));
                }
            }

            if (candidates.Count == 0) return false;

            candidates.Sort((a, b) => b.score.CompareTo(a.score));

            foreach (var (pos, die, _) in candidates)
            {
                try { game.MovePiece(pos, die); return true; } catch { }
            }

            return false;
        }

        int ScoreMove(int from, int to)
        {
            int[] tv = game.TableValues;
            int score = 0;
            int targetVal = tv[to];

            // Never move a home-board piece while stragglers are still in the far quadrant
            if (from < 6)
            {
                for (int i = 18; i < 24; i++)
                    if (tv[i] < 0) { score -= 500; break; }
            }

            // Destination quality
            if (targetVal == 1)
                score += 150;           // hit a blot
            else if (targetVal <= -2)
                score += 80;            // reinforce an owned point
            else if (targetVal == -1)
                score += 60;            // pair up to make a point
            else
            {
                // Far-back pieces must accept blot risk to make progress
                int blotPenalty = from >= 18 ? 60 : (from >= 12 ? 140 : 220);
                score -= blotPenalty;

                for (int threat = to + 1; threat <= to + 6 && threat < 24; threat++)
                    if (tv[threat] > 0)
                        score -= tv[threat] * 20;
            }

            // Penalty for uncovering a source blot; lighter for far-back pieces
            if (tv[from] == -2)
            {
                int sourcePenalty = from >= 18 ? 40 : 150;
                score -= sourcePenalty;

                for (int threat = from + 1; threat <= from + 6 && threat < 24; threat++)
                    if (tv[threat] > 0)
                        score -= tv[threat] * 20;
            }

            // Urgency tiers — far stragglers (cols 18-23) dominate everything;
            // scale up with how close the human is to winning (0..1)
            float pressure = HumanProgress();
            int pressureBonus = (int)(pressure * 300);
            if (from >= 18)
                score += 250 + from * 5 + pressureBonus;
            else if (from >= 12)
                score += 80 + from * 3 + pressureBonus / 2;
            else if (from >= 6)
                score += 40 + from * 2;

            // Bonus for crossing into the home board
            if (from >= 6 && to <= 5)
                score += 80;

            // Positional bonus, doubled for outer-board pieces
            score += from >= 6 ? (23 - to) * 2 : (23 - to);

            return score;
        }

        // Returns a 0..1 value representing how close the human (Player 1) is to winning.
        float HumanProgress()
        {
            int[] tv = game.TableValues;
            int pipsLeft = 0;

            for (int i = 0; i < 24; i++)
                if (tv[i] > 0)
                    pipsLeft += tv[i] * (24 - i);

            pipsLeft += game.Player1.OutedPieces * 25;

            // Max possible pip count at game start is ~167; clamp to that range
            const int maxPips = 167;
            return 1f - System.Math.Min(pipsLeft, maxPips) / (float)maxPips;
        }

        bool CanBearOff()
        {
            if (game.Player2.OutedPieces > 0) return false;
            for (int i = 6; i < 24; i++)
                if (game.TableValues[i] < 0) return false;
            return true;
        }

        void TryNextTurn()
        {
            try { game.NextTurn(); } catch (PieceMoveException) { }
        }
    }
}
