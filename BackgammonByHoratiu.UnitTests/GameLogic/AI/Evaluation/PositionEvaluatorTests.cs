using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.AI.Evaluation;
using BackgammonByHoratiu.GameLogic.AI.Search;
using BackgammonByHoratiu.GameLogic.GameManagers;
using BackgammonByHoratiu.Settings;
using Moq;
using NUnit.Framework;

namespace BackgammonByHoratiu.UnitTests.GameLogic.AI.Evaluation
{
    [TestFixture]
    public class PositionEvaluatorTests
    {
        /// <summary>Racing position: AI pieces close to home (low pips), human pieces far from home (high pips).</summary>
        private static BoardSnapshot CreateRacingSnapshot(int humanOutedPieces = 0)
        {
            Mock<IGameManager> mockGame = new();
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[0] = -2; // AI pieces at col 0: 2 × 1 = 2 AI pips
            columnValues[1] = 2;  // Human pieces at col 1: 2 × (24-1) = 46 human pips
            // aiPips (2) - humanPips (46) = -44 < -15 → Racing phase

            Player player1 = new() { OutedPieces = humanOutedPieces };
            Player player2 = new() { OutedPieces = 0 };

            mockGame.Setup(m => m.TableValues).Returns(columnValues);
            mockGame.Setup(m => m.Player1).Returns(player1);
            mockGame.Setup(m => m.Player2).Returns(player2);

            return new BoardSnapshot(mockGame.Object);
        }

        private static BoardSnapshot CreateBlockingSnapshot()
        {
            Mock<IGameManager> mockGame = new();
            int[] columnValues = new int[GameDefines.TotalColumns];
            // Balanced pip counts → Blocking phase
            columnValues[11] = -3; // AI pips = 3 × 12 = 36
            columnValues[12] = 3;  // Human pips = 3 × (24-12) = 36

            Player player1 = new();
            Player player2 = new();

            mockGame.Setup(m => m.TableValues).Returns(columnValues);
            mockGame.Setup(m => m.Player1).Returns(player1);
            mockGame.Setup(m => m.Player2).Returns(player2);

            return new BoardSnapshot(mockGame.Object);
        }

        // ---------------------------------------------------------------------------
        // Evaluate returns a numeric score without throwing
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveEmptySnapshot_WhenEvaluated_ThenReturnsAScore()
        {
            Mock<IGameManager> mockGame = new();
            int[] columnValues = new int[GameDefines.TotalColumns];
            Player player1 = new();
            Player player2 = new();

            mockGame.Setup(m => m.TableValues).Returns(columnValues);
            mockGame.Setup(m => m.Player1).Returns(player1);
            mockGame.Setup(m => m.Player2).Returns(player2);

            BoardSnapshot snapshot = new(mockGame.Object);

            Assert.That(() => PositionEvaluator.Evaluate(snapshot), Throws.Nothing);
        }

        [Test]
        public void GiveRacingSnapshot_WhenEvaluated_ThenReturnsAScore()
        {
            BoardSnapshot snapshot = CreateRacingSnapshot();

            Assert.That(() => PositionEvaluator.Evaluate(snapshot), Throws.Nothing);
        }

        [Test]
        public void GiveBlockingSnapshot_WhenEvaluated_ThenReturnsAScore()
        {
            BoardSnapshot snapshot = CreateBlockingSnapshot();

            Assert.That(() => PositionEvaluator.Evaluate(snapshot), Throws.Nothing);
        }

        // ---------------------------------------------------------------------------
        // Human bar pieces improve AI score
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveRacingSnapshotWithHumanPieceOnBar_WhenEvaluated_ThenScoreHigherThanWithoutBarPiece()
        {
            BoardSnapshot snapshotWithoutBar = CreateRacingSnapshot(humanOutedPieces: 0);
            BoardSnapshot snapshotWithBar = CreateRacingSnapshot(humanOutedPieces: 1);

            int scoreWithoutBar = PositionEvaluator.Evaluate(snapshotWithoutBar);
            int scoreWithBar = PositionEvaluator.Evaluate(snapshotWithBar);

            Assert.That(scoreWithBar, Is.GreaterThan(scoreWithoutBar));
        }

        // ---------------------------------------------------------------------------
        // AI anchor in human home board scores better than two exposed blots
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveBlockingSnapshotWithAiAnchorInHumanHomeBoard_WhenEvaluated_ThenScoreHigherThanTwoBlots()
        {
            // Position A: 2 AI pieces at col 20 (anchor in human home board, cols 18-23).
            // AI pips = 2 × 21 = 42. Earns BlockingAnchorBonus + AnchorChainBonus.
            Mock<IGameManager> mockAnchor = new();
            int[] anchorValues = new int[GameDefines.TotalColumns];
            anchorValues[20] = -2;
            Player player1Anchor = new();
            Player player2Anchor = new();
            mockAnchor.Setup(m => m.TableValues).Returns(anchorValues);
            mockAnchor.Setup(m => m.Player1).Returns(player1Anchor);
            mockAnchor.Setup(m => m.Player2).Returns(player2Anchor);
            BoardSnapshot snapshotAnchor = new(mockAnchor.Object);

            // Position B: 2 AI blots spread across col 19 and col 21, same total pips (42).
            // No owned points, no chain bonuses.
            Mock<IGameManager> mockBlots = new();
            int[] blotsValues = new int[GameDefines.TotalColumns];
            blotsValues[19] = -1;
            blotsValues[21] = -1;
            Player player1Blots = new();
            Player player2Blots = new();
            mockBlots.Setup(m => m.TableValues).Returns(blotsValues);
            mockBlots.Setup(m => m.Player1).Returns(player1Blots);
            mockBlots.Setup(m => m.Player2).Returns(player2Blots);
            BoardSnapshot snapshotBlots = new(mockBlots.Object);

            int scoreAnchor = PositionEvaluator.Evaluate(snapshotAnchor);
            int scoreBlots = PositionEvaluator.Evaluate(snapshotBlots);

            Assert.That(scoreAnchor, Is.GreaterThan(scoreBlots));
        }
    }
}
