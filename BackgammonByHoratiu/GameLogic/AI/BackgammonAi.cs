using System.Collections.Generic;

using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.GameManagers;

namespace BackgammonByHoratiu.GameLogic.AI
{
    /// <summary>Greedy AI that controls Player 2 (brown pieces).</summary>
    internal sealed class BackgammonAi
    {
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
            int score = 0;
            int targetVal = game.TableValues[to];

            if (targetVal == 1)
                score += 100;       // hit opponent blot — great
            else if (targetVal <= -1)
                score += 30;        // build on our own point — safe
            else
                score -= 20;        // leaving an isolated blot — risky

            score += 23 - to;       // pieces closer to home are better
            score += from;          // prefer moving farthest-back pieces first

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
