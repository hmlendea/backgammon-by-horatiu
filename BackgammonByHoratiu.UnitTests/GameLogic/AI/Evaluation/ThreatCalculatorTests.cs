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
    public class ThreatCalculatorTests
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
        // Direct threat
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithNoHumanPiecesInDirectRange_WhenCalculateThreat_ThenThreatIsZero()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[10] = -1; // AI blot at col 10; no human pieces within 6 columns
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int threat = ThreatCalculator.Calculate(snapshot, 10);

            Assert.That(threat, Is.EqualTo(0));
        }

        [Test]
        public void GiveSnapshotWithHumanPieceOneColumnAboveAiBlot_WhenCalculateThreat_ThenDirectThreatIsNonZero()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[10] = -1; // AI blot
            columnValues[11] = 2;  // 2 human pieces 1 step away
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int threat = ThreatCalculator.Calculate(snapshot, 10);

            Assert.That(threat, Is.GreaterThan(0));
        }

        [Test]
        public void GiveSnapshotWithHumanPiecesAtMaxDieDistance_WhenCalculateThreat_ThenDirectThreatIsNonZero()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[10] = -1; // AI blot
            columnValues[16] = 1;  // human piece exactly 6 away (max die value)
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int threat = ThreatCalculator.Calculate(snapshot, 10);

            Assert.That(threat, Is.GreaterThan(0));
        }

        [Test]
        public void GiveSnapshotWithHumanPiecesMoreThanSixColumnsAway_WhenCalculateThreat_ThenDirectThreatFromThosePiecesIsZero()
        {
            // Only a human piece > 6 columns away; no two-dice path covers it either
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[0] = -1; // AI blot
            columnValues[20] = 3; // human pieces 20 columns away — out of both direct and indirect range
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int threat = ThreatCalculator.Calculate(snapshot, 0);

            Assert.That(threat, Is.EqualTo(0));
        }

        [Test]
        public void GiveSnapshotWithMultipleHumanPiecesInDirectRange_WhenCalculateThreat_ThenThreatEqualsTheirSum()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[10] = -1; // AI blot
            columnValues[11] = 1;  // 1 human piece 1 step away
            columnValues[13] = 2;  // 2 human pieces 3 steps away
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int threat = ThreatCalculator.Calculate(snapshot, 10);

            // Direct threat = 1 + 2 = 3 (plus any indirect)
            Assert.That(threat, Is.GreaterThanOrEqualTo(3));
        }

        // ---------------------------------------------------------------------------
        // Indirect threat (two-dice sums: 7-12)
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveSnapshotWithHumanPiecesAtTwoDiceDistance_WhenCalculateThreat_ThenThreatIsNonZero()
        {
            int[] columnValues = new int[GameDefines.TotalColumns];
            columnValues[5] = -1;  // AI blot
            columnValues[12] = 2;  // human pieces 7 away (min two-dice sum)
            BoardSnapshot snapshot = CreateSnapshot(columnValues);

            int threat = ThreatCalculator.Calculate(snapshot, 5);

            Assert.That(threat, Is.GreaterThan(0));
        }
    }
}
