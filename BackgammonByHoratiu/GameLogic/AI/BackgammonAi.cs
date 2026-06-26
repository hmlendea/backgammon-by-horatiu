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

            if (targetVal == 1)
                score += 150;           // hit a blot
            else if (targetVal <= -2)
                score += 80;            // reinforce an owned point
            else if (targetVal == -1)
                score += 60;            // pair up to make a point
            else
            {
                score -= 200;           // landing alone — exposed blot

                // Extra penalty based on opponent pieces within striking distance
                for (int threat = to + 1; threat <= to + 6 && threat < 24; threat++)
                    if (tv[threat] > 0)
                        score -= tv[threat] * 20;
            }

            if (tv[from] == -2)
            {
                score -= 150;           // uncovering a blot on the source

                for (int threat = from + 1; threat <= from + 6 && threat < 24; threat++)
                    if (tv[threat] > 0)
                        score -= tv[threat] * 20;
            }

            score += 23 - to;
            score += from;

            return score;
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

        readonly IGameManager game;

        internal BackgammonAi(IGameManager game)
        {
            this.game = game;
        }

        /// <summary>Attempts to make one move for the AI. Called once per AI timer tick.</summary>
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

        // ---------------------------------------------------------------- //

        bool TryBarEntry()
        {
            var moves = new List<int>(game.Player2.MovesLeft);

            // Two passes: first try to hit a blot, then accept any open point
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
            // Bear off farthest piece first (highest index in home board 0-5)
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
                    if (game.TableValues[i] >= 0) continue;     // not our piece
                    if (!triedCombos.Add((i, d))) continue;      // deduplicate

                    int target = i - d;
                    if (target < 0) continue;
                    if (game.TableValues[target] >= 2) continue; // blocked by opponent

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

            // --- destination quality ---
            if (targetVal == 1)
            {
                score += 150;       // hit an opponent blot — best outcome
            }
            else if (targetVal <= -2)
            {
                score += 80;        // land on a point we already own (3+ or 2 pieces) — very safe
            }
            else if (targetVal == -1)
            {
                score += 60;        // make a new point (pair up a blot) — good
            }
            else // targetVal == 0 — landing alone, creating an exposed blot
            {
                score -= 200;       // strongly penalise creating a blot

                // Extra penalty if the exposed point can easily be hit:
                // count opponent pieces within 6 squares in front of the target
                for (int threat = to + 1; threat <= to + 6 && threat < 24; threat++)
                    if (tv[threat] > 0)
                        score -= tv[threat] * 20;
            }

            // --- source quality after moving ---
            // If we leave exactly one piece behind on `from`, that becomes a blot
            if (tv[from] == -1)     // we are the only piece; moving leaves it empty — fine
            {
                // no extra penalty — column becomes vacant
            }
            else if (tv[from] == -2)
            {
                // Moving one of two pieces leaves a blot — bad
                score -= 150;

                // Extra threat assessment for the remaining blot
                for (int threat = from + 1; threat <= from + 6 && threat < 24; threat++)
                    if (tv[threat] > 0)
                        score -= tv[threat] * 20;
            }
            // If tv[from] <= -3 we still have ≥2 pieces behind — safe, no penalty

            // --- positional bonuses ---
            score += 23 - to;       // pieces closer to home are better
            score += from;          // prefer moving the farthest-back pieces first

            return score;
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
