using System.Collections.Generic;
using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.AI.Search;
using BackgammonByHoratiu.GameLogic.GameManagers;
using BackgammonByHoratiu.Settings;
using Moq;
using NUnit.Framework;

namespace BackgammonByHoratiu.UnitTests.GameLogic.AI.Search
{
    [TestFixture]
    public class BoardSnapshotTests
    {
        private static BoardSnapshot CreateSnapshot(
            int[] columnValues,
            int aiOutedPieces = 0,
            int humanOutedPieces = 0,
            List<int> aiMovesLeft = null)
        {
            Mock<IGameManager> mockGame = new();
            Player player1 = new() { OutedPieces = humanOutedPieces };
            Player player2 = new() { OutedPieces = aiOutedPieces };

            if (aiMovesLeft is not null)
            {
                player2.MovesLeft = aiMovesLeft;
            }

            mockGame.Setup(m => m.TableValues).Returns(columnValues);
            mockGame.Setup(m => m.Player1).Returns(player1);
            mockGame.Setup(m => m.Player2).Returns(player2);

            return new BoardSnapshot(mockGame.Object);
        }

        // ---------------------------------------------------------------------------
        // Snapshot construction
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveGameManager_WhenSnapshotCreated_ThenColumnValuesAreCloned()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -3;
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            // Mutating the original should not affect the snapshot
            columnValues[5] = 0;

            Assert.That(snapshot.ColumnValues[5], Is.EqualTo(-3));
        }

        [Test]
        public void GiveGameManager_WhenSnapshotCreated_ThenAiOutedPiecesIsCopied()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 2);

            Assert.That(snapshot.AiOutedPieces, Is.EqualTo(2));
        }

        [Test]
        public void GiveGameManager_WhenSnapshotCreated_ThenHumanOutedPiecesIsCopied()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, humanOutedPieces: 1);

            Assert.That(snapshot.HumanOutedPieces, Is.EqualTo(1));
        }

        [Test]
        public void GiveGameManager_WhenSnapshotCreated_ThenMovesLeftAreCloned()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            List<int> moves = [3, 5];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            moves.Add(6); // mutate original

            Assert.That(snapshot.MovesLeft, Has.Count.EqualTo(2));
        }

        // ---------------------------------------------------------------------------
        // GetLegalMoves – bar entry
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithAiOutedPiece_WhenGetLegalMoves_ThenOnlyBarEntryMovesAreReturned()
        {
            int[] columnValues = new int[GameDefines.TotalColumns]; // all empty
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 1, aiMovesLeft: moves);

            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            Assert.That(legalMoves, Has.Count.GreaterThan(0));
            Assert.That(legalMoves, Has.All.Matches<MoveAction>(m => m.Type == MoveActionType.BarEntry));
        }

        [Test]
        public void GiveSnapshotWithAiOutedPieceAndDie1_WhenGetLegalMoves_ThenBarEntryTargetsColumn23()
        {
            // die = 1 → destination = 24 - 1 = 23
            int[] columnValues = new int[GameDefines.TotalColumns];
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 1, aiMovesLeft: moves);

            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            Assert.That(legalMoves[0].DestinationColumn, Is.EqualTo(23));
        }

        [Test]
        public void GiveSnapshotWithAiOutedPieceAndBlockedEntryColumn_WhenGetLegalMoves_ThenThatColumnIsNotOffered()
        {
            // die = 1, col 23 is blocked by 2 human pieces
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[23] = 2; // col 23 blocked
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 1, aiMovesLeft: moves);

            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            Assert.That(legalMoves, Is.Empty);
        }

        // ---------------------------------------------------------------------------
        // GetLegalMoves – normal moves
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithAiPieceAndDie1_WhenGetLegalMoves_ThenNormalMoveToAdjacentColumnIsOffered()
        {
            // AI piece at col 5, die=1 → destination col 4 (empty)
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -2;
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            Assert.That(legalMoves, Has.Some.Matches<MoveAction>(m =>
                m.Type == MoveActionType.Normal &&
                m.SourceColumn == 5 &&
                m.DestinationColumn == 4));
        }

        [Test]
        public void GiveSnapshotWithAiPieceAndDestinationBlockedByHuman_WhenGetLegalMoves_ThenBlockedColumnIsNotOffered()
        {
            // AI piece at col 5, die=1, but col 4 has 2 human pieces → blocked
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -2;
            columnValues[4] = 2; // human block
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            Assert.That(legalMoves, Has.None.Matches<MoveAction>(m =>
                m.DestinationColumn == 4));
        }

        [Test]
        public void GiveSnapshotWithNoMovesLeft_WhenGetLegalMoves_ThenReturnsEmptyList()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -2;
            List<int> moves = [];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            Assert.That(legalMoves, Is.Empty);
        }

        // ---------------------------------------------------------------------------
        // GetLegalMoves – bear off
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithAiPiecesOnlyInHomeBoardAndExactDie_WhenGetLegalMoves_ThenBearOffMoveIsOffered()
        {
            // AI piece at col 2; distance = 2+1 = 3; die = 3 → exact bear-off
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[2] = -1;
            List<int> moves = [3];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            Assert.That(legalMoves, Has.Some.Matches<MoveAction>(m =>
                m.Type == MoveActionType.BearOff &&
                m.SourceColumn == 2));
        }

        [Test]
        public void GiveSnapshotWithAiPiecesOutsideHomeBoard_WhenGetLegalMoves_ThenBearOffMovesAreNotOffered()
        {
            // AI piece at col 10 (outside home board 0-5) → cannot bear off
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[10] = -2;
            List<int> moves = [3];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            Assert.That(legalMoves, Has.None.Matches<MoveAction>(m => m.Type == MoveActionType.BearOff));
        }

        // ---------------------------------------------------------------------------
        // AfterMove – Normal
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshot_WhenAfterNormalMove_ThenSourceColumnValueIncremented()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -2;
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.Normal, 5, 4, 1);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.ColumnValues[5], Is.EqualTo(-1));
        }

        [Test]
        public void GiveSnapshot_WhenAfterNormalMove_ThenDestinationColumnValueDecremented()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -2;
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.Normal, 5, 4, 1);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.ColumnValues[4], Is.EqualTo(-1));
        }

        [Test]
        public void GiveSnapshot_WhenAfterNormalMove_ThenDieIsRemovedFromMovesLeft()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -2;
            List<int> moves = [1, 3];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.Normal, 5, 4, 1);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.MovesLeft, Does.Not.Contain(1));
        }

        [Test]
        public void GiveSnapshot_WhenAfterNormalMoveHitsHumanBlot_ThenHumanOutedPiecesIncremented()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -2;
            columnValues[4] = 1; // lone human piece
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.Normal, 5, 4, 1);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.HumanOutedPieces, Is.EqualTo(1));
        }

        [Test]
        public void GiveSnapshot_WhenAfterNormalMove_ThenOriginalSnapshotIsUnchanged()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -2;
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.Normal, 5, 4, 1);
            snapshot.AfterMove(action);

            Assert.That(snapshot.ColumnValues[5], Is.EqualTo(-2));
        }

        // ---------------------------------------------------------------------------
        // AfterMove – Bar entry
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshot_WhenAfterBarEntry_ThenAiOutedPiecesDecremented()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 1, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.BarEntry, -1, 23, 1);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.AiOutedPieces, Is.EqualTo(0));
        }

        [Test]
        public void GiveSnapshot_WhenAfterBarEntry_ThenDestinationColumnHasAiPiece()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 1, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.BarEntry, -1, 23, 1);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.ColumnValues[23], Is.EqualTo(-1));
        }

        [Test]
        public void GiveSnapshot_WhenAfterBarEntryToColumnWithHumanBlot_ThenHumanOutedPiecesIncremented()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[23] = 1; // lone human piece
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 1, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.BarEntry, -1, 23, 1);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.HumanOutedPieces, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------------------
        // AfterMove – Bear off
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshot_WhenAfterBearOff_ThenSourceColumnValueIncremented()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[2] = -2;
            List<int> moves = [3];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.BearOff, 2, -1, 3);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.ColumnValues[2], Is.EqualTo(-1));
        }

        [Test]
        public void GiveSnapshot_WhenAfterBearOff_ThenDieIsRemovedFromMovesLeft()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[2] = -2;
            List<int> moves = [3];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveAction action = new(MoveActionType.BearOff, 2, -1, 3);
            BoardSnapshot afterMove = snapshot.AfterMove(action);

            Assert.That(afterMove.MovesLeft, Does.Not.Contain(3));
        }
    }
}
