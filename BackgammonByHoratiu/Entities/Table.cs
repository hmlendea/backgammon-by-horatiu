using System;
using System.Collections.Generic;

using BackgammonByHoratiu.Settings;

namespace BackgammonByHoratiu.Entities
{
    public class Table
    {
        public int[] TableValues { get; set; }

        public Player Player1 { get; }

        public Player Player2 { get; }

        public int Dice1 { get; private set; }

        public int Dice2 { get; private set; }

        public int ActivePlayer { get; private set; }

        public Table()
        {
            TableValues = new int[24];

            Player1 = new();
            Player2 = new();

            TableValues[0] = 2;
            TableValues[5] = -5;
            TableValues[7] = -3;
            TableValues[11] = 5;
            TableValues[12] = -5;
            TableValues[16] = 3;
            TableValues[18] = 5;
            TableValues[23] = -2;

            Dice1 = 2;
            Dice2 = 4;

            ActivePlayer = 1;
            ThrowDice();
        }

        public void MoveOutedPiece(int distance)
        {
            if (ActivePlayer == 1)
            {
                if (Player1.OutedPieces == 0)
                {
                    throw new PieceMoveException("Player 1 has no outed pieces");
                }

                if (Player1.MovesLeft.Contains(distance))
                {
                    int col = distance - 1;

                    if (TableValues[col] < -1)
                    {
                        throw new PieceMoveException("Invalid destination");
                    }

                    ApplyBarEntry(col, 1);
                    Player1.OutedPieces -= 1;
                    Player1.MovesLeft.Remove(distance);

                    if (Player1.MovesLeft.Count == 0)
                    {
                        NextTurn();
                    }
                }
                else
                {
                    DicePair barEntryDicePair = FindBarEntryCombo(distance, 1);

                    if (barEntryDicePair.IsValid)
                    {
                        int intermediate = barEntryDicePair.FirstDie - 1;
                        int target = distance - 1;

                        ApplyBarEntry(intermediate, 1);
                        Player1.OutedPieces -= 1;
                        ApplyMoveStep(intermediate, target, 1);
                        Player1.MovesLeft.Remove(barEntryDicePair.FirstDie);
                        Player1.MovesLeft.Remove(barEntryDicePair.SecondDie);

                        if (Player1.MovesLeft.Count == 0)
                        {
                            NextTurn();
                        }
                    }
                    else
                    {
                        DiceTriple barEntryDiceTriple = FindBarEntryThreeDiceCombo(distance, 1);

                        if (barEntryDiceTriple.IsValid)
                        {
                            int int1 = barEntryDiceTriple.FirstDie - 1;
                            int int2 = int1 + barEntryDiceTriple.SecondDie;
                            int target = distance - 1;

                            ApplyBarEntry(int1, 1);
                            Player1.OutedPieces -= 1;
                            ApplyMoveStep(int1, int2, 1);
                            ApplyMoveStep(int2, target, 1);
                            Player1.MovesLeft.Remove(barEntryDiceTriple.FirstDie);
                            Player1.MovesLeft.Remove(barEntryDiceTriple.SecondDie);
                            Player1.MovesLeft.Remove(barEntryDiceTriple.ThirdDie);

                            if (Player1.MovesLeft.Count == 0)
                            {
                                NextTurn();
                            }
                        }
                        else
                        {
                            DiceQuadruple barEntryDiceQuadruple = FindBarEntryFourDiceCombo(distance, 1);

                            if (!barEntryDiceQuadruple.IsValid)
                            {
                                throw new PieceMoveException("Invalid destination");
                            }

                            int int1 = barEntryDiceQuadruple.FirstDie - 1;
                            int int2 = int1 + barEntryDiceQuadruple.SecondDie;
                            int int3 = int2 + barEntryDiceQuadruple.ThirdDie;
                            int target = distance - 1;

                            ApplyBarEntry(int1, 1);
                            Player1.OutedPieces -= 1;
                            ApplyMoveStep(int1, int2, 1);
                            ApplyMoveStep(int2, int3, 1);
                            ApplyMoveStep(int3, target, 1);
                            Player1.MovesLeft.Remove(barEntryDiceQuadruple.FirstDie);
                            Player1.MovesLeft.Remove(barEntryDiceQuadruple.SecondDie);
                            Player1.MovesLeft.Remove(barEntryDiceQuadruple.ThirdDie);
                            Player1.MovesLeft.Remove(barEntryDiceQuadruple.FourthDie);

                            if (Player1.MovesLeft.Count == 0)
                            {
                                NextTurn();
                            }
                        }
                    }
                }
            }
            else
            {
                if (Player2.OutedPieces == 0)
                {
                    throw new PieceMoveException("Player 2 has no outed pieces");
                }

                if (Player2.MovesLeft.Contains(distance))
                {
                    int col = 24 - distance;

                    if (TableValues[col] > 1)
                    {
                        throw new PieceMoveException("Invalid destination");
                    }

                    ApplyBarEntry(col, -1);
                    Player2.OutedPieces -= 1;
                    Player2.MovesLeft.Remove(distance);

                    if (Player2.MovesLeft.Count == 0)
                    {
                        NextTurn();
                    }
                }
                else
                {
                    DicePair barEntryDicePair = FindBarEntryCombo(distance, -1);

                    if (barEntryDicePair.IsValid)
                    {
                        int intermediate = 24 - barEntryDicePair.FirstDie;
                        int target = 24 - distance;

                        ApplyBarEntry(intermediate, -1);
                        Player2.OutedPieces -= 1;
                        ApplyMoveStep(intermediate, target, -1);
                        Player2.MovesLeft.Remove(barEntryDicePair.FirstDie);
                        Player2.MovesLeft.Remove(barEntryDicePair.SecondDie);

                        if (Player2.MovesLeft.Count == 0)
                        {
                            NextTurn();
                        }
                    }
                    else
                    {
                        DiceTriple barEntryDiceTriple = FindBarEntryThreeDiceCombo(distance, -1);

                        if (barEntryDiceTriple.IsValid)
                        {
                            int int1 = 24 - barEntryDiceTriple.FirstDie;
                            int int2 = int1 - barEntryDiceTriple.SecondDie;
                            int target = 24 - distance;

                            ApplyBarEntry(int1, -1);
                            Player2.OutedPieces -= 1;
                            ApplyMoveStep(int1, int2, -1);
                            ApplyMoveStep(int2, target, -1);
                            Player2.MovesLeft.Remove(barEntryDiceTriple.FirstDie);
                            Player2.MovesLeft.Remove(barEntryDiceTriple.SecondDie);
                            Player2.MovesLeft.Remove(barEntryDiceTriple.ThirdDie);

                            if (Player2.MovesLeft.Count == 0)
                            {
                                NextTurn();
                            }
                        }
                        else
                        {
                            DiceQuadruple barEntryDiceQuadruple = FindBarEntryFourDiceCombo(distance, -1);

                            if (!barEntryDiceQuadruple.IsValid)
                            {
                                throw new PieceMoveException("Invalid destination");
                            }

                            int int1 = 24 - barEntryDiceQuadruple.FirstDie;
                            int int2 = int1 - barEntryDiceQuadruple.SecondDie;
                            int int3 = int2 - barEntryDiceQuadruple.ThirdDie;
                            int target = 24 - distance;

                            ApplyBarEntry(int1, -1);
                            Player2.OutedPieces -= 1;
                            ApplyMoveStep(int1, int2, -1);
                            ApplyMoveStep(int2, int3, -1);
                            ApplyMoveStep(int3, target, -1);
                            Player2.MovesLeft.Remove(barEntryDiceQuadruple.FirstDie);
                            Player2.MovesLeft.Remove(barEntryDiceQuadruple.SecondDie);
                            Player2.MovesLeft.Remove(barEntryDiceQuadruple.ThirdDie);
                            Player2.MovesLeft.Remove(barEntryDiceQuadruple.FourthDie);

                            if (Player2.MovesLeft.Count == 0)
                            {
                                NextTurn();
                            }
                        }
                    }
                }
            }
        }

        void ApplyBarEntry(int col, int sign)
        {
            if (sign > 0 && TableValues[col] == -1)
            {
                Player2.OutedPieces += 1;
                TableValues[col] = 0;
            }
            else if (sign < 0 && TableValues[col] == 1)
            {
                Player1.OutedPieces += 1;
                TableValues[col] = 0;
            }

            TableValues[col] += sign;
        }

        DicePair FindBarEntryCombo(int distance, int sign)
        {
            Player movingPlayer = sign > 0 ? Player1 : Player2;
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                int die2 = distance - die1;

                if (die2 < 1)
                {
                    continue;
                }

                bool die2Available = false;

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j != i && moves[j] == die2)
                    {
                        die2Available = true;
                        break;
                    }
                }

                if (!die2Available)
                {
                    continue;
                }

                int intermediate = sign > 0 ? die1 - 1 : 24 - die1;

                if (sign > 0 && TableValues[intermediate] < -1)
                {
                    continue;
                }

                if (sign < 0 && TableValues[intermediate] > 1)
                {
                    continue;
                }

                if (IsStepValid(intermediate, die2, sign))
                {
                    return new DicePair(die1, die2);
                }
            }

            return DicePair.None;
        }

        DiceTriple FindBarEntryThreeDiceCombo(int distance, int sign)
        {
            Player movingPlayer = sign > 0 ? Player1 : Player2;
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                int int1 = sign > 0 ? die1 - 1 : 24 - die1;
                if (int1 < 0 || int1 >= 24)
                {
                    continue;
                }

                bool blocked1 = sign > 0 ? TableValues[int1] < -1 : TableValues[int1] > 1;
                if (blocked1)
                {
                    continue;
                }

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    int die2 = moves[j];
                    int die3 = distance - die1 - die2;
                    if (die3 < 1)
                    {
                        continue;
                    }

                    if (!IsStepValid(int1, die2, sign))
                    {
                        continue;
                    }

                    int int2 = int1 + sign * die2;

                    for (int k = 0; k < moves.Count; k++)
                    {
                        if (k == i || k == j)
                        {
                            continue;
                        }

                        if (moves[k] != die3)
                        {
                            continue;
                        }

                        if (IsStepValid(int2, die3, sign))
                        {
                            return new DiceTriple(die1, die2, die3);
                        }
                    }
                }
            }

            return DiceTriple.None;
        }

        DiceQuadruple FindBarEntryFourDiceCombo(int distance, int sign)
        {
            Player movingPlayer = sign > 0 ? Player1 : Player2;
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                int int1 = sign > 0 ? die1 - 1 : 24 - die1;
                if (int1 < 0 || int1 >= 24)
                {
                    continue;
                }

                bool blocked1 = sign > 0 ? TableValues[int1] < -1 : TableValues[int1] > 1;
                if (blocked1)
                {
                    continue;
                }

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    int die2 = moves[j];
                    if (die1 + die2 >= distance)
                    {
                        continue;
                    }

                    if (!IsStepValid(int1, die2, sign))
                    {
                        continue;
                    }

                    int int2 = int1 + sign * die2;

                    for (int k = 0; k < moves.Count; k++)
                    {
                        if (k == i || k == j)
                        {
                            continue;
                        }

                        int die3 = moves[k];
                        int die4 = distance - die1 - die2 - die3;
                        if (die4 < 1)
                        {
                            continue;
                        }

                        if (!IsStepValid(int2, die3, sign))
                        {
                            continue;
                        }

                        int int3 = int2 + sign * die3;

                        for (int l = 0; l < moves.Count; l++)
                        {
                            if (l == i || l == j || l == k)
                            {
                                continue;
                            }

                            if (moves[l] != die4)
                            {
                                continue;
                            }

                            if (IsStepValid(int3, die4, sign))
                            {
                                return new DiceQuadruple(die1, die2, die3, die4);
                            }
                        }
                    }
                }
            }

            return DiceQuadruple.None;
        }

        public void MovePiece(int pos, int move)
        {
            if (TableValues[pos] == 0)
            {
                throw new PieceMoveException("There are no pieces on this column");
            }

            if ((ActivePlayer == 1 && TableValues[pos] < 0) || (ActivePlayer == 2 && TableValues[pos] > 0))
            {
                throw new PieceMoveException("Cannot move the other player s pieces");
            }

            if (ActivePlayer == 1)
            {
                if (Player1.OutedPieces != 0)
                {
                    throw new PieceMoveException("Player 1 has outed pieces");
                }

                if (move > 0 && pos + move < 24 && TableValues[pos + move] >= -1)
                {
                    if (TableValues[pos + move] == -1)
                    {
                        TableValues[pos + move] = 1;
                        Player2.OutedPieces += 1;
                    }
                    else
                    {
                        TableValues[pos + move] += 1;
                    }

                    TableValues[pos] -= 1;
                    Player1.MovesLeft.Remove(move);

                    if (Player1.MovesLeft.Count == 0)
                    {
                        NextTurn();
                    }
                }
                else
                {
                    throw new PieceMoveException("Invalid destination");
                }
            }
            else if (ActivePlayer == 2)
            {
                if (Player2.OutedPieces != 0)
                {
                    throw new PieceMoveException("Player 2 has outed pieces");
                }

                if (move > 0 && pos - move >= 0 && TableValues[pos - move] <= 1)
                {
                    if (TableValues[pos - move] == 1)
                    {
                        TableValues[pos - move] = -1;
                        Player1.OutedPieces += 1;
                    }
                    else
                    {
                        TableValues[pos - move] -= 1;
                    }

                    TableValues[pos] += 1;
                    Player2.MovesLeft.Remove(move);

                    if (Player2.MovesLeft.Count == 0)
                    {
                        NextTurn();
                    }
                }
                else
                {
                    throw new PieceMoveException("Invalid destination");
                }
            }
        }

        public void MovePieceDirect(int from, int to)
        {
            if (TableValues[from] == 0)
            {
                return;
            }

            int sign = TableValues[from] > 0 ? 1 : -1;

            if (sign > 0 && Player1.OutedPieces > 0)
            {
                throw new PieceMoveException("You have pieces on the bar that must be re-entered first");
            }

            if (sign < 0 && Player2.OutedPieces > 0)
            {
                throw new PieceMoveException("You have pieces on the bar that must be re-entered first");
            }

            if (sign < 0 && from < 12 && to >= 12)
            {
                throw new PieceMoveException("Pieces cannot move backwards");
            }

            if (sign > 0 && from >= 12 && to < 12)
            {
                throw new PieceMoveException("Pieces cannot move backwards");
            }

            bool sameHalf = (from < 12 && to < 12) || (from >= 12 && to >= 12);
            if (sameHalf)
            {
                if (sign < 0 && to > from)
                {
                    throw new PieceMoveException("Pieces cannot move backwards");
                }

                if (sign > 0 && to < from)
                {
                    throw new PieceMoveException("Pieces cannot move backwards");
                }
            }

            if (sign > 0 && TableValues[to] <= -2)
            {
                throw new PieceMoveException("Column is blocked by the opponent");
            }

            if (sign < 0 && TableValues[to] >= 2)
            {
                throw new PieceMoveException("Column is blocked by the opponent");
            }

            int distance = sign > 0 ? to - from : from - to;
            Player movingPlayer = sign > 0 ? Player1 : Player2;

            if (movingPlayer.MovesLeft.Contains(distance))
            {
                movingPlayer.MovesLeft.Remove(distance);
                ApplyMoveStep(from, to, sign);
            }
            else
            {
                DicePair twoDiceCombo = FindTwoDiceCombo(from, distance, sign, movingPlayer);

                if (twoDiceCombo.IsValid)
                {
                    int intermediate = from + sign * twoDiceCombo.FirstDie;
                    movingPlayer.MovesLeft.Remove(twoDiceCombo.FirstDie);
                    ApplyMoveStep(from, intermediate, sign);
                    movingPlayer.MovesLeft.Remove(twoDiceCombo.SecondDie);
                    ApplyMoveStep(intermediate, to, sign);
                }
                else
                {
                    DiceTriple threeDiceCombo = FindThreeDiceCombo(from, distance, sign, movingPlayer);

                    if (threeDiceCombo.IsValid)
                    {
                        int int1 = from + sign * threeDiceCombo.FirstDie;
                        int int2 = int1 + sign * threeDiceCombo.SecondDie;
                        movingPlayer.MovesLeft.Remove(threeDiceCombo.FirstDie);
                        ApplyMoveStep(from, int1, sign);
                        movingPlayer.MovesLeft.Remove(threeDiceCombo.SecondDie);
                        ApplyMoveStep(int1, int2, sign);
                        movingPlayer.MovesLeft.Remove(threeDiceCombo.ThirdDie);
                        ApplyMoveStep(int2, to, sign);
                    }
                    else
                    {
                        DiceQuadruple fourDiceCombo = FindFourDiceCombo(from, distance, sign, movingPlayer);

                        if (!fourDiceCombo.IsValid)
                        {
                            throw new PieceMoveException("No valid move to that column");
                        }

                        int int1 = from + sign * fourDiceCombo.FirstDie;
                        int int2 = int1 + sign * fourDiceCombo.SecondDie;
                        int int3 = int2 + sign * fourDiceCombo.ThirdDie;
                        movingPlayer.MovesLeft.Remove(fourDiceCombo.FirstDie);
                        ApplyMoveStep(from, int1, sign);
                        movingPlayer.MovesLeft.Remove(fourDiceCombo.SecondDie);
                        ApplyMoveStep(int1, int2, sign);
                        movingPlayer.MovesLeft.Remove(fourDiceCombo.ThirdDie);
                        ApplyMoveStep(int2, int3, sign);
                        movingPlayer.MovesLeft.Remove(fourDiceCombo.FourthDie);
                        ApplyMoveStep(int3, to, sign);
                    }
                }
            }
        }

        void ApplyMoveStep(int from, int to, int sign)
        {
            if (sign > 0 && TableValues[to] == -1)
            {
                Player2.OutedPieces += 1;
                TableValues[to] = 0;
            }
            else if (sign < 0 && TableValues[to] == 1)
            {
                Player1.OutedPieces += 1;
                TableValues[to] = 0;
            }

            TableValues[from] -= sign;
            TableValues[to] += sign;
        }

        bool IsStepValid(int from, int die, int sign)
        {
            int to = from + sign * die;

            if (to < 0 || to >= 24)
            {
                return false;
            }

            bool sameHalf = (from < 12) == (to < 12);

            if (sameHalf)
            {
                if (sign > 0 && to <= from)
                {
                    return false;
                }

                if (sign < 0 && to >= from)
                {
                    return false;
                }
            }
            else
            {
                if (sign < 0 && from < 12)
                {
                    return false;
                }

                if (sign > 0 && from >= 12)
                {
                    return false;
                }
            }

            if (sign > 0 && TableValues[to] <= -2)
            {
                return false;
            }

            if (sign < 0 && TableValues[to] >= 2)
            {
                return false;
            }

            return true;
        }

        DicePair FindTwoDiceCombo(int from, int distance, int sign, Player movingPlayer)
        {
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                int die2 = distance - die1;

                if (die2 < 1)
                {
                    continue;
                }

                bool die2Available = false;

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j != i && moves[j] == die2)
                    {
                        die2Available = true;
                        break;
                    }
                }

                if (!die2Available)
                {
                    continue;
                }

                int intermediate = from + sign * die1;

                if (IsStepValid(from, die1, sign) && IsStepValid(intermediate, die2, sign))
                {
                    return new DicePair(die1, die2);
                }
            }

            return DicePair.None;
        }

        DiceTriple FindThreeDiceCombo(int from, int distance, int sign, Player movingPlayer)
        {
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                if (die1 >= distance)
                {
                    continue;
                }

                if (!IsStepValid(from, die1, sign))
                {
                    continue;
                }

                int int1 = from + sign * die1;

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    int die2 = moves[j];
                    int die3 = distance - die1 - die2;
                    if (die3 < 1)
                    {
                        continue;
                    }

                    if (!IsStepValid(int1, die2, sign))
                    {
                        continue;
                    }

                    int int2 = int1 + sign * die2;

                    for (int k = 0; k < moves.Count; k++)
                    {
                        if (k == i || k == j)
                        {
                            continue;
                        }

                        if (moves[k] != die3)
                        {
                            continue;
                        }

                        if (IsStepValid(int2, die3, sign))
                        {
                            return new DiceTriple(die1, die2, die3);
                        }
                    }
                }
            }

            return DiceTriple.None;
        }

        DiceQuadruple FindFourDiceCombo(int from, int distance, int sign, Player movingPlayer)
        {
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                if (die1 >= distance)
                {
                    continue;
                }

                if (!IsStepValid(from, die1, sign))
                {
                    continue;
                }

                int int1 = from + sign * die1;

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    int die2 = moves[j];
                    if (die1 + die2 >= distance)
                    {
                        continue;
                    }

                    if (!IsStepValid(int1, die2, sign))
                    {
                        continue;
                    }

                    int int2 = int1 + sign * die2;

                    for (int k = 0; k < moves.Count; k++)
                    {
                        if (k == i || k == j)
                        {
                            continue;
                        }

                        int die3 = moves[k];
                        int die4 = distance - die1 - die2 - die3;
                        if (die4 < 1)
                        {
                            continue;
                        }

                        if (!IsStepValid(int2, die3, sign))
                        {
                            continue;
                        }

                        int int3 = int2 + sign * die3;

                        for (int l = 0; l < moves.Count; l++)
                        {
                            if (l == i || l == j || l == k)
                            {
                                continue;
                            }

                            if (moves[l] != die4)
                            {
                                continue;
                            }

                            if (IsStepValid(int3, die4, sign))
                            {
                                return new DiceQuadruple(die1, die2, die3, die4);
                            }
                        }
                    }
                }
            }

            return DiceQuadruple.None;
        }

        DiceTriple FindThreeDiceComboForBearOff(int from, int distance, int sign, Player movingPlayer)
        {
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                if (die1 >= distance)
                {
                    continue;
                }

                int int1 = from + sign * die1;
                if (int1 < 0 || int1 >= 24)
                {
                    continue;
                }

                if (!IsStepValid(from, die1, sign))
                {
                    continue;
                }

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    int die2 = moves[j];
                    int die3 = distance - die1 - die2;
                    if (die3 < 1)
                    {
                        continue;
                    }

                    int int2 = int1 + sign * die2;
                    if (int2 < 0 || int2 >= 24)
                    {
                        continue;
                    }

                    if (!IsStepValid(int1, die2, sign))
                    {
                        continue;
                    }

                    for (int k = 0; k < moves.Count; k++)
                    {
                        if (k == i || k == j)
                        {
                            continue;
                        }

                        if (moves[k] != die3)
                        {
                            continue;
                        }

                        return new DiceTriple(die1, die2, die3);
                    }
                }
            }

            return DiceTriple.None;
        }

        DiceQuadruple FindFourDiceComboForBearOff(int from, int distance, int sign, Player movingPlayer)
        {
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                if (die1 >= distance)
                {
                    continue;
                }

                int int1 = from + sign * die1;
                if (int1 < 0 || int1 >= 24)
                {
                    continue;
                }

                if (!IsStepValid(from, die1, sign))
                {
                    continue;
                }

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    int die2 = moves[j];
                    if (die1 + die2 >= distance)
                    {
                        continue;
                    }

                    int int2 = int1 + sign * die2;
                    if (int2 < 0 || int2 >= 24)
                    {
                        continue;
                    }

                    if (!IsStepValid(int1, die2, sign))
                    {
                        continue;
                    }

                    for (int k = 0; k < moves.Count; k++)
                    {
                        if (k == i || k == j)
                        {
                            continue;
                        }

                        int die3 = moves[k];
                        int die4 = distance - die1 - die2 - die3;
                        if (die4 < 1)
                        {
                            continue;
                        }

                        int int3 = int2 + sign * die3;
                        if (int3 < 0 || int3 >= 24)
                        {
                            continue;
                        }

                        if (!IsStepValid(int2, die3, sign))
                        {
                            continue;
                        }

                        for (int l = 0; l < moves.Count; l++)
                        {
                            if (l == i || l == j || l == k)
                            {
                                continue;
                            }

                            if (moves[l] != die4)
                            {
                                continue;
                            }

                            return new DiceQuadruple(die1, die2, die3, die4);
                        }
                    }
                }
            }

            return DiceQuadruple.None;
        }

        DicePair FindTwoDiceComboForBearOff(int from, int distance, int sign, Player movingPlayer)
        {
            var moves = movingPlayer.MovesLeft;

            for (int i = 0; i < moves.Count; i++)
            {
                int die1 = moves[i];
                int die2 = distance - die1;

                if (die2 < 1)
                {
                    continue;
                }

                bool die2Available = false;

                for (int j = 0; j < moves.Count; j++)
                {
                    if (j != i && moves[j] == die2)
                    {
                        die2Available = true;
                        break;
                    }
                }

                if (!die2Available)
                {
                    continue;
                }

                int intermediate = from + sign * die1;

                if (intermediate < 0 || intermediate >= 24)
                {
                    continue;
                }

                if (IsStepValid(from, die1, sign) && IsStepValid(intermediate, die2, sign))
                {
                    return new DicePair(die1, die2);
                }
            }

            return DicePair.None;
        }

        public void BearOffPiece(int from)
        {
            if (TableValues[from] == 0)
            {
                throw new PieceMoveException("No piece on that column");
            }

            int sign = TableValues[from] > 0 ? 1 : -1;

            if (sign > 0 && ActivePlayer != 1)
            {
                throw new PieceMoveException("Not your turn");
            }

            if (sign < 0 && ActivePlayer != 2)
            {
                throw new PieceMoveException("Not your turn");
            }

            if (!CanBearOff(sign))
            {
                throw new PieceMoveException("Not all pieces are in the home board");
            }

            if (sign > 0 && from < 18)
            {
                throw new PieceMoveException("Piece is not in the home board");
            }

            if (sign < 0 && from > 5)
            {
                throw new PieceMoveException("Piece is not in the home board");
            }

            int distance = sign > 0 ? 24 - from : from + 1;
            Player movingPlayer = sign > 0 ? Player1 : Player2;

            int usedDie = -1;

            if (movingPlayer.MovesLeft.Contains(distance))
            {
                usedDie = distance;
            }
            else
            {
                foreach (int die in movingPlayer.MovesLeft)
                {
                    if (die > distance && IsFarthestPiece(from, sign))
                    {
                        usedDie = die;
                        break;
                    }
                }
            }

            if (usedDie != -1)
            {
                movingPlayer.MovesLeft.Remove(usedDie);
            }
            else
            {
                DicePair bearOffDicePair = FindTwoDiceComboForBearOff(from, distance, sign, movingPlayer);

                if (!bearOffDicePair.IsValid)
                {
                    throw new PieceMoveException("No valid die for this move");
                }

                movingPlayer.MovesLeft.Remove(bearOffDicePair.FirstDie);
                movingPlayer.MovesLeft.Remove(bearOffDicePair.SecondDie);
            }
            TableValues[from] -= sign;

            if (sign > 0)
            {
                Player1.CompletedPieces++;

                if (Player1.CompletedPieces >= 15)
                {
                    GameOver();
                }
            }
            else
            {
                Player2.CompletedPieces++;

                if (Player2.CompletedPieces >= 15)
                {
                    GameOver();
                }
            }
        }

        void GameOver()
        {
            TableValues = new int[24];

            Player1.OutedPieces = 0;
            Player1.CompletedPieces = 0;
            Player1.MovesLeft.Clear();

            Player2.OutedPieces = 0;
            Player2.CompletedPieces = 0;
            Player2.MovesLeft.Clear();

            TableValues[0] = 2;
            TableValues[5] = -5;
            TableValues[7] = -3;
            TableValues[11] = 5;
            TableValues[12] = -5;
            TableValues[16] = 3;
            TableValues[18] = 5;
            TableValues[23] = -2;

            ActivePlayer = 1;
            ThrowDice();
        }

        bool IsFarthestPiece(int col, int sign)
        {
            if (sign > 0)
            {
                for (int i = 18; i < col; i++)
                {
                    if (TableValues[i] > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                for (int i = col + 1; i <= 5; i++)
                {
                    if (TableValues[i] < 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        bool CanBearOff(int sign)
        {
            if (sign > 0)
            {
                if (Player1.OutedPieces > 0)
                {
                    return false;
                }

                for (int i = 0; i < 18; i++)
                {
                    if (TableValues[i] > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                if (Player2.OutedPieces > 0)
                {
                    return false;
                }

                for (int i = 6; i < 24; i++)
                {
                    if (TableValues[i] < 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void NextTurn()
        {
            Player currentPlayer = ActivePlayer == 1 ? Player1 : Player2;

            if (currentPlayer.MovesLeft.Count > 0 && HasAnyValidMove())
            {
                throw new PieceMoveException("You still have moves left");
            }

            if (ActivePlayer == 1)
            {
                ActivePlayer = 2;
            }
            else
            {
                ActivePlayer = 1;
            }

            ThrowDice();
        }

        bool HasAnyValidMove()
        {
            int sign = ActivePlayer == 1 ? 1 : -1;
            Player currentPlayer = ActivePlayer == 1 ? Player1 : Player2;

            foreach (int die in currentPlayer.MovesLeft)
            {
                if (currentPlayer.OutedPieces > 0)
                {
                    int entryCol = sign > 0 ? die - 1 : 24 - die;

                    if (sign > 0 && TableValues[entryCol] >= -1)
                    {
                        return true;
                    }

                    if (sign < 0 && TableValues[entryCol] <= 1)
                    {
                        return true;
                    }
                }
                else if (CanBearOff(sign))
                {
                    for (int i = 0; i < 24; i++)
                    {
                        if (sign > 0 && TableValues[i] > 0 && i >= 18)
                        {
                            int distance = 24 - i;

                            if (distance == die || (die > distance && IsFarthestPiece(i, sign)))
                            {
                                return true;
                            }
                        }

                        if (sign < 0 && TableValues[i] < 0 && i <= 5)
                        {
                            int distance = i + 1;

                            if (distance == die || (die > distance && IsFarthestPiece(i, sign)))
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 24; i++)
                    {
                        if (sign > 0 && TableValues[i] <= 0)
                        {
                            continue;
                        }

                        if (sign < 0 && TableValues[i] >= 0)
                        {
                            continue;
                        }

                        int target = sign > 0 ? i + die : i - die;

                        if (target < 0 || target >= 24)
                        {
                            continue;
                        }

                        if (sign > 0 && TableValues[target] >= -1)
                        {
                            return true;
                        }

                        if (sign < 0 && TableValues[target] <= 1)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public int FindMovePieceDirectIntermediate(int from, int to)
        {
            if (TableValues[from] == 0)
            {
                return -1;
            }

            int sign = TableValues[from] > 0 ? 1 : -1;
            int distance = sign > 0 ? to - from : from - to;
            Player movingPlayer = sign > 0 ? Player1 : Player2;

            if (movingPlayer.MovesLeft.Contains(distance))
            {
                return -1;
            }

            DicePair twoDiceCombo = FindTwoDiceCombo(from, distance, sign, movingPlayer);

            if (twoDiceCombo.IsValid)
            {
                return from + sign * twoDiceCombo.FirstDie;
            }

            DiceTriple threeDiceCombo = FindThreeDiceCombo(from, distance, sign, movingPlayer);

            if (threeDiceCombo.IsValid)
            {
                return from + sign * threeDiceCombo.FirstDie;
            }

            DiceQuadruple fourDiceCombo = FindFourDiceCombo(from, distance, sign, movingPlayer);

            if (fourDiceCombo.IsValid)
            {
                return from + sign * fourDiceCombo.FirstDie;
            }

            return -1;
        }

        public int FindMoveOutedPieceIntermediate(int distance)
        {
            int sign = ActivePlayer == 1 ? 1 : -1;
            Player movingPlayer = ActivePlayer == 1 ? Player1 : Player2;

            if (movingPlayer.MovesLeft.Contains(distance))
            {
                return -1;
            }

            DicePair barEntryDicePair = FindBarEntryCombo(distance, sign);

            if (barEntryDicePair.IsValid)
            {
                if (ActivePlayer == 1)
                {
                    return barEntryDicePair.FirstDie - 1;
                }
                else
                {
                    return 24 - barEntryDicePair.FirstDie;
                }
            }

            DiceTriple barEntryDiceTriple = FindBarEntryThreeDiceCombo(distance, sign);

            if (barEntryDiceTriple.IsValid)
            {
                return ActivePlayer == 1 ? barEntryDiceTriple.FirstDie - 1 : 24 - barEntryDiceTriple.FirstDie;
            }

            DiceQuadruple barEntryDiceQuadruple = FindBarEntryFourDiceCombo(distance, sign);

            if (barEntryDiceQuadruple.IsValid)
            {
                return ActivePlayer == 1 ? barEntryDiceQuadruple.FirstDie - 1 : 24 - barEntryDiceQuadruple.FirstDie;
            }

            return -1;
        }

        public List<int> FindMovePieceDirectIntermediates(int from, int to)
        {
            if (TableValues[from] == 0)
            {
                return [];
            }

            int sign = -1;

            if (TableValues[from] > 0)
            {
                sign = 1;
            }

            int distance = from - to;

            if (sign > 0)
            {
                distance = to - from;
            }

            Player movingPlayer = Player2;

            if (sign > 0)
            {
                movingPlayer = Player1;
            }

            if (movingPlayer.MovesLeft.Contains(distance))
            {
                return [];
            }

            DicePair twoDiceCombo = FindTwoDiceCombo(from, distance, sign, movingPlayer);

            if (twoDiceCombo.IsValid)
            {
                return [from + sign * twoDiceCombo.FirstDie];
            }

            DiceTriple threeDiceCombo = FindThreeDiceCombo(from, distance, sign, movingPlayer);

            if (threeDiceCombo.IsValid)
            {
                int int1 = from + sign * threeDiceCombo.FirstDie;

                return [int1, int1 + sign * threeDiceCombo.SecondDie];
            }

            DiceQuadruple fourDiceCombo = FindFourDiceCombo(from, distance, sign, movingPlayer);

            if (fourDiceCombo.IsValid)
            {
                int int1 = from + sign * fourDiceCombo.FirstDie;
                int int2 = int1 + sign * fourDiceCombo.SecondDie;

                return [int1, int2, int2 + sign * fourDiceCombo.ThirdDie];
            }

            return [];
        }

        public List<int> FindMoveOutedPieceIntermediates(int distance)
        {
            int sign = ActivePlayer == 1 ? 1 : -1;
            Player movingPlayer = ActivePlayer == 1 ? Player1 : Player2;

            if (movingPlayer.MovesLeft.Contains(distance))
            {
                return [];
            }

            DicePair barEntryDicePair = FindBarEntryCombo(distance, sign);

            if (barEntryDicePair.IsValid)
            {
                int int1 = ActivePlayer == 1 ? barEntryDicePair.FirstDie - 1 : 24 - barEntryDicePair.FirstDie;

                return [int1];
            }

            DiceTriple barEntryDiceTriple = FindBarEntryThreeDiceCombo(distance, sign);

            if (barEntryDiceTriple.IsValid)
            {
                int int1 = ActivePlayer == 1 ? barEntryDiceTriple.FirstDie - 1 : 24 - barEntryDiceTriple.FirstDie;

                return [int1, int1 + sign * barEntryDiceTriple.SecondDie];
            }

            DiceQuadruple barEntryDiceQuadruple = FindBarEntryFourDiceCombo(distance, sign);

            if (barEntryDiceQuadruple.IsValid)
            {
                int int1 = ActivePlayer == 1 ? barEntryDiceQuadruple.FirstDie - 1 : 24 - barEntryDiceQuadruple.FirstDie;
                int int2 = int1 + sign * barEntryDiceQuadruple.SecondDie;

                return [int1, int2, int2 + sign * barEntryDiceQuadruple.ThirdDie];
            }

            return [];
        }

        /// <summary>
        /// Returns the valid destination column indices (0-23) for the piece at fromCol.
        /// fromCol may also be GameDefines.ColBarP1 / ColBarP2 for bar re-entry.
        /// GameDefines.ColHouseP1 / ColHouseP2 are appended when bear-off is possible.
        /// </summary>
        public List<int> GetValidDestinations(int fromCol)
        {
            var result = new List<int>();
            int sign = ActivePlayer == 1 ? 1 : -1;
            Player movingPlayer = sign > 0 ? Player1 : Player2;

            // --- Bar re-entry ---
            if (fromCol == GameDefines.ColBarP1 || fromCol == GameDefines.ColBarP2)
            {
                if (fromCol == GameDefines.ColBarP1 && sign < 0)
                {
                    return result;
                }

                if (fromCol == GameDefines.ColBarP2 && sign > 0)
                {
                    return result;
                }

                foreach (int die in movingPlayer.MovesLeft)
                {
                    int entryCol = sign > 0 ? die - 1 : 24 - die;
                    if (entryCol < 0 || entryCol >= 24)
                    {
                        continue;
                    }

                    if (result.Contains(entryCol))
                    {
                        continue;
                    }

                    if (sign > 0 && TableValues[entryCol] >= -1)
                    {
                        result.Add(entryCol);
                    }
                    else if (sign < 0 && TableValues[entryCol] <= 1)
                    {
                        result.Add(entryCol);
                    }
                }

                // Two-die combos from bar
                for (int i = 0; i < movingPlayer.MovesLeft.Count; i++)
                {
                    int die1 = movingPlayer.MovesLeft[i];
                    int intermediate = sign > 0 ? die1 - 1 : 24 - die1;
                    if (intermediate < 0 || intermediate >= 24)
                    {
                        continue;
                    }

                    bool blocked = sign > 0 ? TableValues[intermediate] < -1 : TableValues[intermediate] > 1;
                    if (blocked)
                    {
                        continue;
                    }

                    for (int j = 0; j < movingPlayer.MovesLeft.Count; j++)
                    {
                        if (i == j)
                        {
                            continue;
                        }

                        int die2 = movingPlayer.MovesLeft[j];
                        int finalCol = intermediate + sign * die2;
                        if (finalCol < 0 || finalCol >= 24)
                        {
                            continue;
                        }

                        if (result.Contains(finalCol))
                        {
                            continue;
                        }

                        if (IsStepValid(intermediate, die2, sign))
                        {
                            result.Add(finalCol);
                        }
                    }
                }

                // Three-die combos from bar
                for (int i = 0; i < movingPlayer.MovesLeft.Count; i++)
                {
                    int die1 = movingPlayer.MovesLeft[i];
                    int int1 = sign > 0 ? die1 - 1 : 24 - die1;
                    if (int1 < 0 || int1 >= 24)
                    {
                        continue;
                    }

                    bool blocked1 = sign > 0 ? TableValues[int1] < -1 : TableValues[int1] > 1;
                    if (blocked1)
                    {
                        continue;
                    }

                    for (int j = 0; j < movingPlayer.MovesLeft.Count; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }

                        if (!IsStepValid(int1, movingPlayer.MovesLeft[j], sign))
                        {
                            continue;
                        }

                        int int2 = int1 + sign * movingPlayer.MovesLeft[j];

                        for (int k = 0; k < movingPlayer.MovesLeft.Count; k++)
                        {
                            if (k == i || k == j)
                            {
                                continue;
                            }

                            int finalCol = int2 + sign * movingPlayer.MovesLeft[k];
                            if (finalCol < 0 || finalCol >= 24)
                            {
                                continue;
                            }

                            if (result.Contains(finalCol))
                            {
                                continue;
                            }

                            if (IsStepValid(int2, movingPlayer.MovesLeft[k], sign))
                            {
                                result.Add(finalCol);
                            }
                        }
                    }
                }

                // Four-die combos from bar
                for (int i = 0; i < movingPlayer.MovesLeft.Count; i++)
                {
                    int die1 = movingPlayer.MovesLeft[i];
                    int int1 = sign > 0 ? die1 - 1 : 24 - die1;
                    if (int1 < 0 || int1 >= 24)
                    {
                        continue;
                    }

                    bool blocked1 = sign > 0 ? TableValues[int1] < -1 : TableValues[int1] > 1;
                    if (blocked1)
                    {
                        continue;
                    }

                    for (int j = 0; j < movingPlayer.MovesLeft.Count; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }

                        if (!IsStepValid(int1, movingPlayer.MovesLeft[j], sign))
                        {
                            continue;
                        }

                        int int2 = int1 + sign * movingPlayer.MovesLeft[j];

                        for (int k = 0; k < movingPlayer.MovesLeft.Count; k++)
                        {
                            if (k == i || k == j)
                            {
                                continue;
                            }

                            if (!IsStepValid(int2, movingPlayer.MovesLeft[k], sign))
                            {
                                continue;
                            }

                            int int3 = int2 + sign * movingPlayer.MovesLeft[k];

                            for (int l = 0; l < movingPlayer.MovesLeft.Count; l++)
                            {
                                if (l == i || l == j || l == k)
                                {
                                    continue;
                                }

                                int finalCol = int3 + sign * movingPlayer.MovesLeft[l];
                                if (finalCol < 0 || finalCol >= 24)
                                {
                                    continue;
                                }

                                if (result.Contains(finalCol))
                                {
                                    continue;
                                }

                                if (IsStepValid(int3, movingPlayer.MovesLeft[l], sign))
                                {
                                    result.Add(finalCol);
                                }
                            }
                        }
                    }
                }

                return result;
            }

            // --- Regular column ---
            if (fromCol < 0 || fromCol >= 24)
            {
                return result;
            }

            if (sign > 0 && TableValues[fromCol] <= 0)
            {
                return result;
            }

            if (sign < 0 && TableValues[fromCol] >= 0)
            {
                return result;
            }

            if (movingPlayer.OutedPieces > 0)
            {
                return result;
            }

            // Single-die and two-die board moves
            for (int to = 0; to < 24; to++)
            {
                if (to == fromCol)
                {
                    continue;
                }

                if (CanMovePieceDirect(fromCol, to, sign, movingPlayer))
                {
                    result.Add(to);
                }
            }

            // Bear off
            if (CanBearOff(sign))
            {
                int homeStart = sign > 0 ? 18 : 0;
                int homeEnd = sign > 0 ? 23 : 5;
                if (fromCol >= homeStart && fromCol <= homeEnd)
                {
                    int distance = sign > 0 ? 24 - fromCol : fromCol + 1;
                    bool canBearOffHere = movingPlayer.MovesLeft.Contains(distance);

                    if (!canBearOffHere)
                    {
                        foreach (int die in movingPlayer.MovesLeft)
                        {
                            if (die > distance && IsFarthestPiece(fromCol, sign))
                            {
                                canBearOffHere = true;
                                break;
                            }
                        }
                    }

                    if (!canBearOffHere)
                    {
                        DicePair bearOffDicePair = FindTwoDiceComboForBearOff(fromCol, distance, sign, movingPlayer);
                        canBearOffHere = bearOffDicePair.IsValid;
                    }

                    if (canBearOffHere)
                    {
                        result.Add(sign > 0 ? GameDefines.ColHouseP1 : GameDefines.ColHouseP2);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the dice values consumed to move from fromCol to toCol.
        /// Returns one value for a single-die move, two values for a two-die move.
        /// </summary>
        public List<int> GetDiceForDestination(int fromCol, int toCol)
        {
            int sign = ActivePlayer == 1 ? 1 : -1;
            Player movingPlayer = sign > 0 ? Player1 : Player2;

            // --- Bar re-entry ---
            if (fromCol == GameDefines.ColBarP1 || fromCol == GameDefines.ColBarP2)
            {
                int singleDieValue = sign > 0 ? toCol + 1 : 24 - toCol;

                if (movingPlayer.MovesLeft.Contains(singleDieValue))
                {
                    return [singleDieValue];
                }

                int totalBarDistance = singleDieValue;
                DicePair barEntryDicePair = FindBarEntryCombo(totalBarDistance, sign);

                if (barEntryDicePair.IsValid)
                {
                    return [barEntryDicePair.FirstDie, barEntryDicePair.SecondDie];
                }

                DiceTriple barEntryDiceTriple = FindBarEntryThreeDiceCombo(totalBarDistance, sign);

                if (barEntryDiceTriple.IsValid)
                {
                    return [barEntryDiceTriple.FirstDie, barEntryDiceTriple.SecondDie, barEntryDiceTriple.ThirdDie];
                }

                DiceQuadruple barEntryDiceQuadruple = FindBarEntryFourDiceCombo(totalBarDistance, sign);

                if (barEntryDiceQuadruple.IsValid)
                {
                    return [barEntryDiceQuadruple.FirstDie, barEntryDiceQuadruple.SecondDie, barEntryDiceQuadruple.ThirdDie, barEntryDiceQuadruple.FourthDie];
                }

                return [];
            }

            // --- Bear-off ---
            if (toCol == GameDefines.ColHouseP1 || toCol == GameDefines.ColHouseP2)
            {
                int bearOffDistance = sign > 0 ? 24 - fromCol : fromCol + 1;

                if (movingPlayer.MovesLeft.Contains(bearOffDistance))
                {
                    return [bearOffDistance];
                }

                foreach (int die in movingPlayer.MovesLeft)
                {
                    if (die > bearOffDistance && IsFarthestPiece(fromCol, sign))
                    {
                        return [die];
                    }
                }

                DicePair bearOffDicePair = FindTwoDiceComboForBearOff(fromCol, bearOffDistance, sign, movingPlayer);

                if (bearOffDicePair.IsValid)
                {
                    return [bearOffDicePair.FirstDie, bearOffDicePair.SecondDie];
                }

                return [];
            }

            // --- Regular move ---
            int distance = sign > 0 ? toCol - fromCol : fromCol - toCol;

            if (movingPlayer.MovesLeft.Contains(distance))
            {
                return [distance];
            }

            DicePair twoDiceCombo = FindTwoDiceCombo(fromCol, distance, sign, movingPlayer);

            if (twoDiceCombo.IsValid)
            {
                return [twoDiceCombo.FirstDie, twoDiceCombo.SecondDie];
            }

            DiceTriple threeDiceCombo = FindThreeDiceCombo(fromCol, distance, sign, movingPlayer);

            if (threeDiceCombo.IsValid)
            {
                return [threeDiceCombo.FirstDie, threeDiceCombo.SecondDie, threeDiceCombo.ThirdDie];
            }

            DiceQuadruple fourDiceCombo = FindFourDiceCombo(fromCol, distance, sign, movingPlayer);

            if (fourDiceCombo.IsValid)
            {
                return [fourDiceCombo.FirstDie, fourDiceCombo.SecondDie, fourDiceCombo.ThirdDie, fourDiceCombo.FourthDie];
            }

            return [];
        }

        bool CanMovePieceDirect(int from, int to, int sign, Player movingPlayer)        {
            if (sign < 0 && from < 12 && to >= 12)
            {
                return false;
            }

            if (sign > 0 && from >= 12 && to < 12)
            {
                return false;
            }

            bool sameHalf = (from < 12 && to < 12) || (from >= 12 && to >= 12);
            if (sameHalf)
            {
                if (sign < 0 && to > from)
                {
                    return false;
                }

                if (sign > 0 && to < from)
                {
                    return false;
                }
            }

            if (sign > 0 && TableValues[to] <= -2)
            {
                return false;
            }

            if (sign < 0 && TableValues[to] >= 2)
            {
                return false;
            }

            int distance = sign > 0 ? to - from : from - to;
            if (movingPlayer.MovesLeft.Contains(distance))
            {
                return true;
            }

            DicePair twoDiceCombo = FindTwoDiceCombo(from, distance, sign, movingPlayer);

            if (twoDiceCombo.IsValid)
            {
                return true;
            }

            DiceTriple threeDiceCombo = FindThreeDiceCombo(from, distance, sign, movingPlayer);

            if (threeDiceCombo.IsValid)
            {
                return true;
            }

            DiceQuadruple fourDiceCombo = FindFourDiceCombo(from, distance, sign, movingPlayer);

            return fourDiceCombo.IsValid;
        }

        public void ThrowDice()
        {
            Player player;
            Random rnd = new();

            Dice1 = rnd.Next(1, 7);
            Dice2 = rnd.Next(1, 7);

            if (ActivePlayer == 1)
            {
                player = Player1;
            }
            else
            {
                player = Player2;
            }

            player.MovesLeft.Clear();

            if (Dice1 == Dice2)
            {
                for (int i = 0; i < 4; i++)
                {
                    player.MovesLeft.Add(Dice1);
                }
            }
            else
            {
                player.MovesLeft.Add(Dice1);
                player.MovesLeft.Add(Dice2);
            }
        }
    }
}
