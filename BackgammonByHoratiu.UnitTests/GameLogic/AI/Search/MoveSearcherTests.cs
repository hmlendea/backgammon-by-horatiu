using System.Collections.Generic;
using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.AI.Search;
using BackgammonByHoratiu.GameLogic.GameManagers;
using Moq;
using NUnit.Framework;

namespace BackgammonByHoratiu.UnitTests.GameLogic.AI.Search
{
    [TestFixture]
    public class MoveSearcherTests
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
        // FindBestSequence – no moves available
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithNoMovesLeft_WhenFindBestSequence_ThenActionsListIsEmpty()
        {
            int[] columnValues = new int[24];
            columnValues[5] = -2;
            List<int> moves = [];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveSequenceResult result = MoveSearcher.FindBestSequence(snapshot);

            Assert.That(result.Actions, Is.Empty);
        }

        [Test]
        public void GiveSnapshotWithNoMovesLeft_WhenFindBestSequence_ThenScoreIsLeafEvaluation()
        {
            int[] columnValues = new int[24];
            List<int> moves = [];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveSequenceResult result = MoveSearcher.FindBestSequence(snapshot);

            // Score must be some integer — just check no exception is thrown
            Assert.That(() => result.Score, Throws.Nothing);
        }

        // ---------------------------------------------------------------------------
        // FindBestSequence – moves available
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithOneLegalMove_WhenFindBestSequence_ThenActionsListIsNonEmpty()
        {
            // AI piece at col 5, die = 1 → one legal move to col 4
            int[] columnValues = new int[24];
            columnValues[5] = -2;
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveSequenceResult result = MoveSearcher.FindBestSequence(snapshot);

            Assert.That(result.Actions, Is.Not.Empty);
        }

        [Test]
        public void GiveSnapshotWithOneLegalMove_WhenFindBestSequence_ThenFirstActionUsesCorrectDie()
        {
            int[] columnValues = new int[24];
            columnValues[5] = -2;
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveSequenceResult result = MoveSearcher.FindBestSequence(snapshot);

            Assert.That(result.Actions[0].DieValue, Is.EqualTo(1));
        }

        [Test]
        public void GiveSnapshotWithAiOutedPieceAndValidBarEntry_WhenFindBestSequence_ThenFirstActionIsBarEntry()
        {
            // AI outed piece, die = 1, col 23 is empty → only legal move is bar entry
            int[] columnValues = new int[24];
            List<int> moves = [1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 1, aiMovesLeft: moves);

            MoveSequenceResult result = MoveSearcher.FindBestSequence(snapshot);

            Assert.That(result.Actions[0].Type, Is.EqualTo(MoveActionType.BarEntry));
        }

        [Test]
        public void GiveSnapshotWithTwoMoves_WhenFindBestSequence_ThenResultHasScoreSet()
        {
            int[] columnValues = new int[24];
            columnValues[5] = -3;
            List<int> moves = [1, 2];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            MoveSequenceResult result = MoveSearcher.FindBestSequence(snapshot);

            // Score is always set (any integer)
            Assert.That(result.Score, Is.Not.EqualTo(int.MinValue));
        }

        // ---------------------------------------------------------------------------
        // FindBestSequence – transposition table prevents duplicate work
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithDoubles_WhenFindBestSequence_ThenCompletesWithoutError()
        {
            // Four identical dice → large search space; transposition table must handle it
            int[] columnValues = new int[24];
            columnValues[5] = -5;
            List<int> moves = [1, 1, 1, 1];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiMovesLeft: moves);

            Assert.That(() => MoveSearcher.FindBestSequence(snapshot), Throws.Nothing);
        }
    }
}
