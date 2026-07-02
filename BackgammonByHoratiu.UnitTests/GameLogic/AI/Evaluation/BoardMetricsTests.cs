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
    public class BoardMetricsTests
    {
        private static BoardSnapshot CreateSnapshot(int[] columnValues, int aiOutedPieces = 0, int humanOutedPieces = 0)
        {
            Mock<IGameManager> mockGame = new();
            Player player1 = new() { OutedPieces = humanOutedPieces };
            Player player2 = new() { OutedPieces = aiOutedPieces };

            mockGame.Setup(m => m.TableValues).Returns(columnValues);
            mockGame.Setup(m => m.Player1).Returns(player1);
            mockGame.Setup(m => m.Player2).Returns(player2);

            return new BoardSnapshot(mockGame.Object);
        }

        // ---------------------------------------------------------------------------
        // AiPipCount
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithAiPiecesAtColumn2_WhenAiPipCountCalculated_ThenReturnsCorrectValue()
        {
            // col 2 = -3 → 3 AI pieces × (2+1) = 9 pips
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[2] = -3;
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int pipCount = BoardMetrics.AiPipCount(snapshot);

            Assert.That(pipCount, Is.EqualTo(9));
        }

        [Test]
        public void GiveSnapshotWithAiPiecesAtMultipleColumns_WhenAiPipCountCalculated_ThenReturnsSumOfAllPips()
        {
            // col 0: -1 → 1 pip; col 4: -2 → 10 pips; total = 11
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[0] = -1;
            columnValues[4] = -2;
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int pipCount = BoardMetrics.AiPipCount(snapshot);

            Assert.That(pipCount, Is.EqualTo(11));
        }

        [Test]
        public void GiveSnapshotWithAiOutedPiece_WhenAiPipCountCalculated_ThenBarPipsAreIncluded()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, aiOutedPieces: 1);

            int pipCount = BoardMetrics.AiPipCount(snapshot);

            Assert.That(pipCount, Is.EqualTo(AiWeights.BarPipEquivalent));
        }

        [Test]
        public void GiveEmptySnapshot_WhenAiPipCountCalculated_ThenReturnsZero()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int pipCount = BoardMetrics.AiPipCount(snapshot);

            Assert.That(pipCount, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------------------
        // HumanPipCount
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithHumanPiecesAtColumn21_WhenHumanPipCountCalculated_ThenReturnsCorrectValue()
        {
            // col 21 = +2 → 2 human pieces × (24-21) = 2 × 3 = 6 pips
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[21] = 2;
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int pipCount = BoardMetrics.HumanPipCount(snapshot);

            Assert.That(pipCount, Is.EqualTo(6));
        }

        [Test]
        public void GiveSnapshotWithHumanPiecesAtMultipleColumns_WhenHumanPipCountCalculated_ThenReturnsSumOfAllPips()
        {
            // col 23: +1 → 1 pip; col 0: +2 → 48 pips; total = 49
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[23] = 1;
            columnValues[0] = 2;
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int pipCount = BoardMetrics.HumanPipCount(snapshot);

            Assert.That(pipCount, Is.EqualTo(49));
        }

        [Test]
        public void GiveSnapshotWithHumanOutedPiece_WhenHumanPipCountCalculated_ThenBarPipsAreIncluded()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            BoardSnapshot snapshot = CreateSnapshot(columnValues, humanOutedPieces: 1);

            int pipCount = BoardMetrics.HumanPipCount(snapshot);

            Assert.That(pipCount, Is.EqualTo(AiWeights.BarPipEquivalent));
        }

        [Test]
        public void GiveEmptySnapshot_WhenHumanPipCountCalculated_ThenReturnsZero()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int pipCount = BoardMetrics.HumanPipCount(snapshot);

            Assert.That(pipCount, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------------------
        // DeterminePhase
        // ---------------------------------------------------------------------------

        [Test]
        public void GivePipCounts_WhenAiCountExceedsHumanByMoreThanThreshold_ThenPhaseIsBackGame()
        {
            // aiPipCount - humanPipCount > BackGamePipThreshold (40) → BackGame
            int aiPipCount = AiWeights.BackGamePipThreshold + 10;
            int humanPipCount = 0;

            GamePhase phase = BoardMetrics.DeterminePhase(aiPipCount, humanPipCount);

            Assert.That(phase, Is.EqualTo(GamePhase.BackGame));
        }

        [Test]
        public void GivePipCounts_WhenHumanCountExceedsAiByMoreThanThreshold_ThenPhaseIsRacing()
        {
            // aiPipCount - humanPipCount < -RacingPipThreshold (-15) → Racing
            int aiPipCount = 0;
            int humanPipCount = AiWeights.RacingPipThreshold + 10;

            GamePhase phase = BoardMetrics.DeterminePhase(aiPipCount, humanPipCount);

            Assert.That(phase, Is.EqualTo(GamePhase.Racing));
        }

        [Test]
        public void GivePipCounts_WhenDifferenceIsWithinBothThresholds_ThenPhaseIsBlocking()
        {
            int aiPipCount = 50;
            int humanPipCount = 50;

            GamePhase phase = BoardMetrics.DeterminePhase(aiPipCount, humanPipCount);

            Assert.That(phase, Is.EqualTo(GamePhase.Blocking));
        }

        [Test]
        public void GivePipCounts_WhenDifferenceEqualsBackGameThreshold_ThenPhaseIsNotBackGame()
        {
            // Exactly at threshold is not ">" so should NOT be BackGame
            int aiPipCount = AiWeights.BackGamePipThreshold;
            int humanPipCount = 0;

            GamePhase phase = BoardMetrics.DeterminePhase(aiPipCount, humanPipCount);

            Assert.That(phase, Is.Not.EqualTo(GamePhase.BackGame));
        }
    }
}
