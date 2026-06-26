using BackgammonByHoratiu.Entities;
using NUnit.Framework;

namespace BackgammonByHoratiu.UnitTests.Entities
{
    [TestFixture]
    public class TableTests
    {
        // ---------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Creates a Table with a fixed initial layout and clears the random dice so
        /// individual tests can set exactly the moves they need.
        /// </summary>
        private static Table CreateTableForPlayer1()
        {
            Table table = new();
            table.Player1.MovesLeft.Clear();

            return table;
        }

        /// <summary>
        /// Creates a Table, ends Player 1's turn without moves, and clears Player 2's
        /// random dice so tests can inject specific moves.
        /// </summary>
        private static Table CreateTableForPlayer2()
        {
            Table table = new();
            table.Player1.MovesLeft.Clear();
            table.NextTurn();
            table.Player2.MovesLeft.Clear();

            return table;
        }

        // ---------------------------------------------------------------------------
        // Initial board layout
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveNewTable_WhenCreated_ThenColumn0HasTwoPlayer1Pieces()
        {
            Table table = new();

            Assert.That(table.TableValues[0], Is.EqualTo(2));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenColumn5HasFivePlayer2Pieces()
        {
            Table table = new();

            Assert.That(table.TableValues[5], Is.EqualTo(-5));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenColumn7HasThreePlayer2Pieces()
        {
            Table table = new();

            Assert.That(table.TableValues[7], Is.EqualTo(-3));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenColumn11HasFivePlayer1Pieces()
        {
            Table table = new();

            Assert.That(table.TableValues[11], Is.EqualTo(5));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenColumn12HasFivePlayer2Pieces()
        {
            Table table = new();

            Assert.That(table.TableValues[12], Is.EqualTo(-5));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenColumn16HasThreePlayer1Pieces()
        {
            Table table = new();

            Assert.That(table.TableValues[16], Is.EqualTo(3));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenColumn18HasFivePlayer1Pieces()
        {
            Table table = new();

            Assert.That(table.TableValues[18], Is.EqualTo(5));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenColumn23HasTwoPlayer2Pieces()
        {
            Table table = new();

            Assert.That(table.TableValues[23], Is.EqualTo(-2));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenActivePlayerIs1()
        {
            Table table = new();

            Assert.That(table.ActivePlayer, Is.EqualTo(1));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenBothPlayersHaveZeroOutedPieces()
        {
            Table table = new();

            Assert.That(table.Player1.OutedPieces, Is.EqualTo(0));
            Assert.That(table.Player2.OutedPieces, Is.EqualTo(0));
        }

        [Test]
        public void GiveNewTable_WhenCreated_ThenBothPlayersHaveZeroCompletedPieces()
        {
            Table table = new();

            Assert.That(table.Player1.CompletedPieces, Is.EqualTo(0));
            Assert.That(table.Player2.CompletedPieces, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------------------
        // ThrowDice
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveTable_WhenThrowDice_ThenDice1IsInRange1To6()
        {
            Table table = new();
            table.Player1.MovesLeft.Clear();
            table.ThrowDice();

            Assert.That(table.Dice1, Is.InRange(1, 6));
        }

        [Test]
        public void GiveTable_WhenThrowDice_ThenDice2IsInRange1To6()
        {
            Table table = new();
            table.Player1.MovesLeft.Clear();
            table.ThrowDice();

            Assert.That(table.Dice2, Is.InRange(1, 6));
        }

        [Test]
        public void GiveTable_WhenThrowDice_ThenMovesLeftIsNotEmpty()
        {
            Table table = new();
            table.ThrowDice();

            Assert.That(table.Player1.MovesLeft, Is.Not.Empty);
        }

        // ---------------------------------------------------------------------------
        // MovePiece – Player 1
        // ---------------------------------------------------------------------------

        [Test]
        public void GivePlayer1Piece_WhenMovingToEmptyColumn_ThenSourceColumnDecremented()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(6);

            table.MovePiece(11, 6); // col 11 → col 17 (empty)

            Assert.That(table.TableValues[11], Is.EqualTo(4));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovingToEmptyColumn_ThenDestinationColumnIs1()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(6);

            table.MovePiece(11, 6); // col 11 → col 17 (empty)

            Assert.That(table.TableValues[17], Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovingToColumnWithOwnPieces_ThenPiecesStack()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(5);

            table.MovePiece(11, 5); // col 11 → col 16 (has +3)

            Assert.That(table.TableValues[16], Is.EqualTo(4));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovingToOpponentBlot_ThenOpponentPieceGoesToBar()
        {
            Table table = CreateTableForPlayer1();
            table.TableValues[14] = -1; // lone opponent piece
            table.Player1.MovesLeft.Add(3);

            table.MovePiece(11, 3); // col 11 → col 14 (opponent blot)

            Assert.That(table.Player2.OutedPieces, Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovingToOpponentBlot_ThenPlayer1PieceOccupiesColumn()
        {
            Table table = CreateTableForPlayer1();
            table.TableValues[14] = -1;
            table.Player1.MovesLeft.Add(3);

            table.MovePiece(11, 3);

            Assert.That(table.TableValues[14], Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovingToBlockedOpponentColumn_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(1);

            // col 12 has -5 (blocked by player 2)
            Assert.Throws<PieceMoveException>(() => table.MovePiece(11, 1));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovingOpponentPiece_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(1);

            // col 5 has -5 (player 2 pieces)
            Assert.Throws<PieceMoveException>(() => table.MovePiece(5, 1));
        }

        [Test]
        public void GivePlayer1PieceWithOutedPieces_WhenMovingNormalPiece_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.OutedPieces = 1;
            table.Player1.MovesLeft.Add(6);

            Assert.Throws<PieceMoveException>(() => table.MovePiece(11, 6));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovingFromEmptyColumn_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(1);

            // col 1 is empty in the initial layout
            Assert.Throws<PieceMoveException>(() => table.MovePiece(1, 1));
        }

        [Test]
        public void GivePlayer1LastMove_WhenMoveConsumesLastDie_ThenActivePlayerBecomesPlayer2()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(6);

            table.MovePiece(11, 6); // last die consumed → NextTurn called automatically

            Assert.That(table.ActivePlayer, Is.EqualTo(2));
        }

        [Test]
        public void GivePlayer1Piece_WhenMoveIsValid_ThenDieIsRemovedFromMovesLeft()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(6);
            table.Player1.MovesLeft.Add(4);

            table.MovePiece(11, 6);

            Assert.That(table.Player1.MovesLeft, Does.Not.Contain(6));
        }

        // ---------------------------------------------------------------------------
        // MovePiece – Player 2
        // ---------------------------------------------------------------------------

        [Test]
        public void GivePlayer2Piece_WhenMovingToEmptyColumn_ThenSourceColumnIncremented()
        {
            Table table = CreateTableForPlayer2();
            table.Player2.MovesLeft.Add(3);

            table.MovePiece(23, 3); // col 23 → col 20 (empty), player 2 moves toward lower cols

            Assert.That(table.TableValues[23], Is.EqualTo(-1));
        }

        [Test]
        public void GivePlayer2Piece_WhenMovingToEmptyColumn_ThenDestinationColumnIsNegative1()
        {
            Table table = CreateTableForPlayer2();
            table.Player2.MovesLeft.Add(3);

            table.MovePiece(23, 3);

            Assert.That(table.TableValues[20], Is.EqualTo(-1));
        }

        [Test]
        public void GivePlayer2Piece_WhenMovingToOpponentBlot_ThenOpponentPieceGoesToBar()
        {
            Table table = CreateTableForPlayer2();
            table.TableValues[20] = 1; // lone player 1 piece
            table.Player2.MovesLeft.Add(3);

            table.MovePiece(23, 3); // col 23 → col 20 (player 1 blot)

            Assert.That(table.Player1.OutedPieces, Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer2Piece_WhenMovingToBlockedOpponentColumn_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer2();
            table.Player2.MovesLeft.Add(5);

            // col 18 has +5 (player 1 pieces), col 23-5=18 — blocked
            Assert.Throws<PieceMoveException>(() => table.MovePiece(23, 5));
        }

        [Test]
        public void GivePlayer2PieceWithOutedPieces_WhenMovingNormalPiece_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer2();
            table.Player2.OutedPieces = 1;
            table.Player2.MovesLeft.Add(3);

            Assert.Throws<PieceMoveException>(() => table.MovePiece(23, 3));
        }

        // ---------------------------------------------------------------------------
        // MoveOutedPiece – Player 1
        // ---------------------------------------------------------------------------

        [Test]
        public void GivePlayer1OutedPiece_WhenReEnteringToEmptyColumn_ThenOutedPiecesDecremented()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(1);
            table.Player1.OutedPieces = 1;

            table.MoveOutedPiece(1); // die=1 → col = 1-1 = 0

            Assert.That(table.Player1.OutedPieces, Is.EqualTo(0));
        }

        [Test]
        public void GivePlayer1OutedPiece_WhenReEnteringToEmptyColumn_ThenPieceAppearsOnBoard()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(1);
            table.Player1.OutedPieces = 1;

            table.MoveOutedPiece(1);

            Assert.That(table.TableValues[0], Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1OutedPiece_WhenReEnteringToOpponentBlot_ThenOpponentSentToBar()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.TableValues[0] = -1; // lone player 2 piece
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(1);
            table.Player1.MovesLeft.Add(4); // extra die so NextTurn is not triggered
            table.Player1.OutedPieces = 1;

            table.MoveOutedPiece(1);

            Assert.That(table.Player2.OutedPieces, Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1OutedPiece_WhenReEnteringToBlockedColumn_ThenThrowsPieceMoveException()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.TableValues[0] = -2; // blocked by player 2
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(1);
            table.Player1.OutedPieces = 1;

            Assert.Throws<PieceMoveException>(() => table.MoveOutedPiece(1));
        }

        [Test]
        public void GivePlayer1WithNoOutedPieces_WhenMoveOutedPieceCalled_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(1);
            // OutedPieces defaults to 0

            Assert.Throws<PieceMoveException>(() => table.MoveOutedPiece(1));
        }

        // ---------------------------------------------------------------------------
        // MoveOutedPiece – Player 2
        // ---------------------------------------------------------------------------

        [Test]
        public void GivePlayer2OutedPiece_WhenReEnteringToEmptyColumn_ThenOutedPiecesDecremented()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.Player1.MovesLeft.Clear();
            table.NextTurn();
            table.Player2.MovesLeft.Clear();
            table.Player2.MovesLeft.Add(1);
            table.Player2.OutedPieces = 1;

            table.MoveOutedPiece(1); // die=1 → col = 24-1 = 23

            Assert.That(table.Player2.OutedPieces, Is.EqualTo(0));
        }

        [Test]
        public void GivePlayer2OutedPiece_WhenReEnteringToEmptyColumn_ThenPieceAppearsOnBoard()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.Player1.MovesLeft.Clear();
            table.NextTurn();
            table.Player2.MovesLeft.Clear();
            table.Player2.MovesLeft.Add(1);
            table.Player2.OutedPieces = 1;

            table.MoveOutedPiece(1);

            Assert.That(table.TableValues[23], Is.EqualTo(-1));
        }

        [Test]
        public void GivePlayer2OutedPiece_WhenReEnteringToOpponentBlot_ThenOpponentSentToBar()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.TableValues[23] = 1; // lone player 1 piece
            table.Player1.MovesLeft.Clear();
            table.NextTurn();
            table.Player2.MovesLeft.Clear();
            table.Player2.MovesLeft.Add(1);
            table.Player2.MovesLeft.Add(4); // extra die so NextTurn is not triggered
            table.Player2.OutedPieces = 1;

            table.MoveOutedPiece(1);

            Assert.That(table.Player1.OutedPieces, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------------------
        // BearOffPiece – Player 1
        // ---------------------------------------------------------------------------

        [Test]
        public void GivePlayer1InBearOffPosition_WhenBearingOffPiece_ThenCompletedPiecesIncremented()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.TableValues[23] = 1; // single piece in P1 home board
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(1); // distance 24-23 = 1

            table.BearOffPiece(23);

            Assert.That(table.Player1.CompletedPieces, Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1InBearOffPosition_WhenBearingOffPiece_ThenColumnBecomesEmpty()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.TableValues[23] = 1;
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(1);

            table.BearOffPiece(23);

            Assert.That(table.TableValues[23], Is.EqualTo(0));
        }

        [Test]
        public void GivePlayer1InBearOffPosition_WhenBearingOffWithLargerDie_ThenBearOffSucceeds()
        {
            // Die is larger than exact distance but the piece is the farthest
            Table table = new();
            table.TableValues = new int[24];
            table.TableValues[23] = 1; // distance = 1; use die = 3 (> 1)
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(3);

            table.BearOffPiece(23);

            Assert.That(table.Player1.CompletedPieces, Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1_WhenBearOffButPieceOutsideHomeBoard_ThenThrowsPieceMoveException()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.TableValues[17] = 1; // col 17 is outside P1 home board (18-23)
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(1);

            Assert.Throws<PieceMoveException>(() => table.BearOffPiece(17));
        }

        [Test]
        public void GivePlayer1_WhenBearOffFromEmptyColumn_ThenThrowsPieceMoveException()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.Player1.MovesLeft.Clear();
            table.Player1.MovesLeft.Add(1);

            Assert.Throws<PieceMoveException>(() => table.BearOffPiece(23));
        }

        [Test]
        public void GivePlayer2InBearOffPosition_WhenBearingOffPiece_ThenCompletedPiecesIncremented()
        {
            Table table = new();
            table.TableValues = new int[24];
            table.TableValues[0] = -1; // single piece in P2 home board (cols 0-5)
            table.Player1.MovesLeft.Clear();
            table.NextTurn();
            table.Player2.MovesLeft.Clear();
            table.Player2.MovesLeft.Add(1); // distance 0+1 = 1

            table.BearOffPiece(0);

            Assert.That(table.Player2.CompletedPieces, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------------------
        // NextTurn
        // ---------------------------------------------------------------------------

        [Test]
        public void GivePlayer1WithNoMovesLeft_WhenNextTurn_ThenActivePlayerBecomesPlayer2()
        {
            Table table = CreateTableForPlayer1();
            // MovesLeft is already cleared

            table.NextTurn();

            Assert.That(table.ActivePlayer, Is.EqualTo(2));
        }

        [Test]
        public void GivePlayer2WithNoMovesLeft_WhenNextTurn_ThenActivePlayerBecomesPlayer1()
        {
            Table table = CreateTableForPlayer2();
            // MovesLeft is already cleared

            table.NextTurn();

            Assert.That(table.ActivePlayer, Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1WithMovesLeftAndValidMoves_WhenNextTurn_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(6); // col 11 + 6 = col 17 (empty) is a valid move

            Assert.Throws<PieceMoveException>(() => table.NextTurn());
        }

        [Test]
        public void GivePlayer1_WhenNextTurnCalled_ThenPlayer2ReceivesDice()
        {
            Table table = CreateTableForPlayer1();

            table.NextTurn();

            Assert.That(table.Player2.MovesLeft, Is.Not.Empty);
        }

        // ---------------------------------------------------------------------------
        // MovePieceDirect
        // ---------------------------------------------------------------------------

        [Test]
        public void GivePlayer1Piece_WhenMovedDirectlyToEmptyColumn_ThenSourceAndDestinationUpdated()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(6);

            table.MovePieceDirect(11, 17); // col 11 → col 17 using die = 6

            Assert.That(table.TableValues[11], Is.EqualTo(4));
            Assert.That(table.TableValues[17], Is.EqualTo(1));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovedDirectlyBackward_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(6);

            // Player 1 moves toward higher columns, so from > to is backward
            Assert.Throws<PieceMoveException>(() => table.MovePieceDirect(18, 12));
        }

        [Test]
        public void GivePlayer1Piece_WhenMovedDirectlyToBlockedColumn_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.MovesLeft.Add(1);

            // col 12 has -5 (blocked)
            Assert.Throws<PieceMoveException>(() => table.MovePieceDirect(11, 12));
        }

        [Test]
        public void GivePlayer1PieceWithOutedPieces_WhenMovedDirectly_ThenThrowsPieceMoveException()
        {
            Table table = CreateTableForPlayer1();
            table.Player1.OutedPieces = 1;
            table.Player1.MovesLeft.Add(6);

            Assert.Throws<PieceMoveException>(() => table.MovePieceDirect(11, 17));
        }
    }
}
