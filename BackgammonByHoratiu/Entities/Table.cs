using System;

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

                if (TableValues[distance - 1] >= -1)
                {
                    if (TableValues[distance - 1] == -1)
                    {
                        TableValues[distance - 1] = 1;
                        Player2.OutedPieces += 1;
                    }
                    else
                    {
                        TableValues[distance - 1] += 1;
                    }

                    Player1.OutedPieces -= 1;
                    Player1.MovesLeft.Remove(distance);

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
            else
            {
                if (Player2.OutedPieces == 0)
                {
                    throw new PieceMoveException("Player 2 has no outed pieces");
                }

                if (TableValues[^distance] <= 1)
                {
                    if (TableValues[^distance] == 1)
                    {
                        TableValues[^distance] = -1;
                        Player1.OutedPieces += 1;
                    }
                    else
                    {
                        TableValues[^distance] -= 1;
                    }

                    Player2.OutedPieces -= 1;
                    Player2.MovesLeft.Remove(distance);

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

            int distance;

            if (sign > 0)
            {
                distance = to - from;
            }
            else
            {
                distance = from - to;
            }

            Player movingPlayer = sign > 0 ? Player1 : Player2;

            if (!movingPlayer.MovesLeft.Contains(distance))
            {
                throw new PieceMoveException($"No die showing {distance}");
            }

            movingPlayer.MovesLeft.Remove(distance);

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

            if (usedDie == -1)
            {
                throw new PieceMoveException($"No valid die for this move");
            }

            movingPlayer.MovesLeft.Remove(usedDie);
            TableValues[from] -= sign;

            if (sign > 0)
            {
                Player1.CompletedPieces++;
            }
            else
            {
                Player2.CompletedPieces++;
            }
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

            if (currentPlayer.MovesLeft.Count > 0)
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
