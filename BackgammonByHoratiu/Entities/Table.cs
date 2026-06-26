using System;

namespace BackgammonByHoratiu.Entities
{
    public class Table
    {
        Player player1, player2;
        int[] table;
        int dice1, dice2;
        int activePlayer;

        public int[] TableValues
        {
            get { return table; }
            set { table = value; }
        }

        public Player Player1
        {
            get { return player1; }
        }

        public Player Player2
        {
            get { return player2; }
        }

        public int Dice1
        {
            get { return dice1; }
        }

        public int Dice2
        {
            get { return dice2; }
        }

        public int ActivePlayer
        {
            get { return activePlayer; }
        }

        public Table()
        {
            table = new int[24];

            player1 = new Player();
            player2 = new Player();

            table[0] = 2;
            table[5] = -5;
            table[7] = -3;
            table[11] = 5;

            table[12] = -5;
            table[16] = 3;
            table[18] = 5;
            table[23] = -2;

            dice1 = 2;
            dice2 = 4;

            activePlayer = 1;
            ThrowDice();
        }

        public void MoveOutedPiece(int distance)
        {
            if (activePlayer == 1)
            {
                if (player1.OutedPieces == 0)
                {
                    throw new PieceMoveException("Player 1 has no outed pieces");
                }

                if (table[distance - 1] >= -1)
                {
                    if (table[distance - 1] == -1)
                    {
                        table[distance - 1] = 1;
                        player2.OutedPieces += 1;
                    }
                    else
                    {
                        table[distance - 1] += 1;
                    }

                    player1.OutedPieces -= 1;
                    player1.MovesLeft.Remove(distance);

                    if (player1.MovesLeft.Count == 0)
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
                if (player2.OutedPieces == 0)
                {
                    throw new PieceMoveException("Player 2 has no outed pieces");
                }

                if (table[table.Length - distance] <= 1)
                {
                    if (table[table.Length - distance] == 1)
                    {
                        table[table.Length - distance] = -1;
                        player1.OutedPieces += 1;
                    }
                    else
                    {
                        table[table.Length - distance] -= 1;
                    }

                    player2.OutedPieces -= 1;
                    player2.MovesLeft.Remove(distance);

                    if (player2.MovesLeft.Count == 0)
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
            if (table[pos] == 0)
            {
                throw new PieceMoveException("There are no pieces on this column");
            }

            if ((activePlayer == 1 && table[pos] < 0) || (activePlayer == 2 && table[pos] > 0))
            {
                throw new PieceMoveException("Cannot move the other player s pieces");
            }

            if (activePlayer == 1)
            {
                if (player1.OutedPieces != 0)
                {
                    throw new PieceMoveException("Player 1 has outed pieces");
                }

                if (move > 0 && pos + move < 24 && table[pos + move] >= -1)
                {
                    if (table[pos + move] == -1)
                    {
                        table[pos + move] = 1;
                        player2.OutedPieces += 1;
                    }
                    else
                    {
                        table[pos + move] += 1;
                    }

                    table[pos] -= 1;
                    player1.MovesLeft.Remove(move);

                    if (player1.MovesLeft.Count == 0)
                    {
                        NextTurn();
                    }
                }
                else
                {
                    throw new PieceMoveException("Invalid destination");
                }
            }
            else if (activePlayer == 2)
            {
                if (player2.OutedPieces != 0)
                {
                    throw new PieceMoveException("Player 2 has outed pieces");
                }

                if (move > 0 && pos - move >= 0 && table[pos - move] <= 1)
                {
                    if (table[pos - move] == 1)
                    {
                        table[pos - move] = -1;
                        player1.OutedPieces += 1;
                    }
                    else
                    {
                        table[pos - move] -= 1;
                    }

                    table[pos] += 1;
                    player2.MovesLeft.Remove(move);

                    if (player2.MovesLeft.Count == 0)
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
            if (table[from] == 0)
            {
                return;
            }

            int sign = table[from] > 0 ? 1 : -1;

            // Cross-half direction: brown moves top→bottom (0-11 → 12-23) is forbidden;
            // white moves bottom→top (12-23 → 0-11) is forbidden
            if (sign < 0 && from < 12 && to >= 12)
            {
                throw new PieceMoveException("Pieces cannot move backwards");
            }

            if (sign > 0 && from >= 12 && to < 12)
            {
                throw new PieceMoveException("Pieces cannot move backwards");
            }

            // Direction check within the same half of the board
            bool sameHalf = (from < 12 && to < 12) || (from >= 12 && to >= 12);
            if (sameHalf)
            {
                // Brown (-) always moves to a lower index; white (+) always moves to a higher index
                if (sign < 0 && to > from)
                {
                    throw new PieceMoveException("Pieces cannot move backwards");
                }

                if (sign > 0 && to < from)
                {
                    throw new PieceMoveException("Pieces cannot move backwards");
                }
            }

            // Target must not be held by 2 or more enemy pieces
            if (sign > 0 && table[to] <= -2)
            {
                throw new PieceMoveException("Column is blocked by the opponent");
            }

            if (sign < 0 && table[to] >= 2)
            {
                throw new PieceMoveException("Column is blocked by the opponent");
            }

            table[from] -= sign;
            table[to] += sign;
        }

        void NextTurn()
        {
            if (activePlayer == 1)
            {
                activePlayer = 2;
            }
            else
            {
                activePlayer = 1;
            }

            ThrowDice();
        }

        void ThrowDice()
        {
            Player player;
            Random rnd = new Random();
            dice1 = rnd.Next(1, 7);
            dice2 = rnd.Next(1, 7);

            if (activePlayer == 1)
            {
                player = player1;
            }
            else
            {
                player = player2;
            }

            if (dice1 == dice2)
            {
                for (int i = 0; i < 4; i++)
                {
                    player.MovesLeft.Add(dice1);
                }
            }
            else
            {
                player.MovesLeft.Add(dice1);
                player.MovesLeft.Add(dice2);
            }
        }
    }
}
