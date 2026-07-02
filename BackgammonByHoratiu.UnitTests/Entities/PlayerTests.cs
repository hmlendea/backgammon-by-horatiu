using BackgammonByHoratiu.Entities;
using NUnit.Framework;

namespace BackgammonByHoratiu.UnitTests.Entities
{
    [TestFixture]
    public class PlayerTests
    {
        [Test]
        public void GiveNewPlayer_WhenCreated_ThenOutedPiecesIsZero()
        {
            Player player = new();

            Assert.That(player.OutedPieces, Is.EqualTo(0));
        }

        [Test]
        public void GiveNewPlayer_WhenCreated_ThenCompletedPiecesIsZero()
        {
            Player player = new();

            Assert.That(player.CompletedPieces, Is.EqualTo(0));
        }

        [Test]
        public void GiveNewPlayer_WhenCreated_ThenMovesLeftIsEmpty()
        {
            Player player = new();

            Assert.That(player.MovesLeft, Is.Empty);
        }

        [Test]
        public void GivePlayer_WhenOutedPiecesSet_ThenOutedPiecesReturnsNewValue()
        {
            Player player = new()
            {
                OutedPieces = 3
            };

            Assert.That(player.OutedPieces, Is.EqualTo(3));
        }

        [Test]
        public void GivePlayer_WhenCompletedPiecesSet_ThenCompletedPiecesReturnsNewValue()
        {
            Player player = new()
            {
                CompletedPieces = 7
            };

            Assert.That(player.CompletedPieces, Is.EqualTo(7));
        }

        [Test]
        public void GivePlayer_WhenMovesAddedToMovesLeft_ThenMovesLeftContainsThem()
        {
            Player player = new();
            player.MovesLeft.Add(3);
            player.MovesLeft.Add(5);

            Assert.That(player.MovesLeft, Is.EquivalentTo(new[] { 3, 5 }));
        }
    }
}
