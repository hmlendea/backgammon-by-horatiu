using System;
using System.Drawing;

using BackgammonByHoratiu.Utils;

namespace BackgammonByHoratiu.Entities
{
    public class Table
    {
        Player player1, player2;
        Color clrBackground, clrColumnOut, clrColumnHouse, clrColumnOdd, clrColumnEven;
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

        public Color BackgroundColor
        {
            get { return clrBackground; }
            set { clrBackground = value; }
        }

        public Color OddColumnColor
        {
            get { return clrColumnOdd; }
            set { clrColumnOdd = value; }
        }

        public Color EvenColumnColor
        {
            get { return clrColumnEven; }
            set { clrColumnEven = value; }
        }

        public Color HouseColumnColor
        {
            get { return clrColumnHouse; }
            set { clrColumnHouse = value; }
        }

        public Color OutColumnColor
        {
            get { return clrColumnOut; }
            set { clrColumnOut = value; }
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

            player1 = new Player(Color.White);
            player2 = new Player(Color.Brown);

            clrBackground = Color.Gray;
            clrColumnOdd = Color.FromArgb(255, 255, 255, 127);
            clrColumnEven = Color.FromArgb(255, 0, 127, 0);
            clrColumnHouse = Color.FromArgb(255, 63, 63, 63);
            clrColumnOut = Color.Black;

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
            // Player 1
            if (activePlayer == 1)
            {
                if (player1.OutedPieces == 0)
                    throw new PieceMoveException("Player 1 has no outed pieces");

                if (table[distance - 1] >= -1)
                {
                    if (table[distance - 1] == -1)
                    {
                        table[distance - 1] = 1;
                        player2.OutedPieces += 1;
                    }
                    else
                        table[distance - 1] += 1;

                    player1.OutedPieces -= 1;
                    player1.MovesLeft.Remove(distance);

                    if (player1.MovesLeft.Count == 0)
                        NextTurn();
                }
                else
                    throw new PieceMoveException("Invalid destination");
            }
            // Player 2
            else
            {
                if (player2.OutedPieces == 0)
                    throw new PieceMoveException("Player 2 has no outed pieces");

                if (table[table.Length - distance] <= 1)
                {
                    if (table[table.Length - distance] == 1)
                    {
                        table[table.Length - distance] = -1;
                        player1.OutedPieces += 1;
                    }
                    else
                        table[table.Length - distance] -= 1;
                    
                    player2.OutedPieces -= 1;
                    player2.MovesLeft.Remove(distance);

                    if (player2.MovesLeft.Count == 0)
                        NextTurn();
                }
                else
                    throw new PieceMoveException("Invalid destination");
            }
        }

        public void MovePiece(int pos, int move)
        {
            if (table[pos] == 0)
                throw new PieceMoveException("There are no pieces on this column");

            if ((activePlayer == 1 && table[pos] < 0) || (activePlayer == 2 && table[pos] > 0))
                throw new PieceMoveException("Cannot move the other player's pieces");

            // Player 1
            if (activePlayer == 1)
            {
                if (player1.OutedPieces != 0)
                    throw new PieceMoveException("Player 1 has outed pieces");

                if (move > 0 && pos + move < 24 && table[pos + move] >= -1)
                {
                    if (table[pos + move] == -1)
                    {
                        table[pos + move] = 1;
                        player2.OutedPieces += 1;
                    }
                    else
                        table[pos + move] += 1;
                    
                    table[pos] -= 1;
                    player1.MovesLeft.Remove(move);

                    if (player1.MovesLeft.Count == 0)
                        NextTurn();
                }
                else
                    throw new PieceMoveException("Invalid destination");
            }
            // Player 2
            else if (activePlayer == 2)
            {
                if (player2.OutedPieces != 0)
                    throw new PieceMoveException("Player 2 has outed pieces");
                
                if (move > 0 && pos - move >= 0 && table[pos - move] <= 1)
                {
                    if (table[pos - move] == 1)
                    {
                        table[pos - move] = -1;
                        player1.OutedPieces += 1;
                    }
                    else
                        table[pos - move] -= 1;

                    table[pos] += 1;
                    player2.MovesLeft.Remove(move);

                    if (player2.MovesLeft.Count == 0)
                        NextTurn();
                }
                else
                    throw new PieceMoveException("Invalid destination");
            }
        }

        void NextTurn()
        {
            Player player;

            if (activePlayer == 1)
                activePlayer = 2;
            else
                activePlayer = 1;

            ThrowDice();
        }

        void ThrowDice()
        {
            Player player;
            Random rnd = new Random();
            dice1 = rnd.Next(1, 7);
            dice2 = rnd.Next(1, 7);

            if (activePlayer == 1)
                player = player1;
            else
                player = player2;
            
            if (dice1 == dice2)
                for (int i = 0; i < 4; i++)
                    player.MovesLeft.Add(dice1);
            else
            {
                player.MovesLeft.Add(dice1);
                player.MovesLeft.Add(dice2);
            }

            Logger.MainLog.WriteLine("Dice thrown for player " + activePlayer + ": [" + dice1 + ", " + dice2 + "]");
        }
    }
}
