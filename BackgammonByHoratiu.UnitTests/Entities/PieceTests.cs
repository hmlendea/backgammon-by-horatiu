using BackgammonByHoratiu.Entities;
using NUnit.Framework;

namespace BackgammonByHoratiu.UnitTests.Entities
{
    [TestFixture]
    public class PieceTests
    {
        [Test]
        public void GivePiece_WhenPlayerIsSetToPlayer1_ThenPlayerPropertyReturnsPlayer1()
        {
            Piece piece = new()
            {
                Player = PiecePlayer.Player1
            };

            Assert.That(piece.Player, Is.EqualTo(PiecePlayer.Player1));
        }

        [Test]
        public void GivePiece_WhenPlayerIsSetToPlayer2_ThenPlayerPropertyReturnsPlayer2()
        {
            Piece piece = new()
            {
                Player = PiecePlayer.Player2
            };

            Assert.That(piece.Player, Is.EqualTo(PiecePlayer.Player2));
        }

        [Test]
        public void GivePiece_WhenPlayerChangedFromPlayer1ToPlayer2_ThenPlayerPropertyReturnsPlayer2()
        {
            Piece piece = new()
            {
                Player = PiecePlayer.Player1
            };

            piece.Player = PiecePlayer.Player2;

            Assert.That(piece.Player, Is.EqualTo(PiecePlayer.Player2));
        }
    }
}
