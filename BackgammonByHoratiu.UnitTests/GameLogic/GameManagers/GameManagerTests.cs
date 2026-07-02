using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.GameManagers;
using BackgammonByHoratiu.Settings;
using NUnit.Framework;

namespace BackgammonByHoratiu.UnitTests.GameLogic.GameManagers
{
    [TestFixture]
    public class GameManagerTests
    {
        // ---------------------------------------------------------------------------
        // LoadContent
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveGameManager_WhenLoadContentCalled_ThenIsRunningIsTrue()
        {
            GameManager manager = new();
            manager.LoadContent();

            Assert.That(manager.IsRunning, Is.True);
        }

        [Test]
        public void GiveGameManager_WhenLoadContentCalled_ThenActivePlayerIs1()
        {
            GameManager manager = new();
            manager.LoadContent();

            Assert.That(manager.ActivePlayer, Is.EqualTo(1));
        }

        [Test]
        public void GiveGameManager_WhenLoadContentCalled_ThenPlayer1IsNotNull()
        {
            GameManager manager = new();
            manager.LoadContent();

            Assert.That(manager.Player1, Is.Not.Null);
        }

        [Test]
        public void GiveGameManager_WhenLoadContentCalled_ThenPlayer2IsNotNull()
        {
            GameManager manager = new();
            manager.LoadContent();

            Assert.That(manager.Player2, Is.Not.Null);
        }

        [Test]
        public void GiveGameManager_WhenLoadContentCalled_ThenTableValuesHas24Columns()
        {
            GameManager manager = new();
            manager.LoadContent();

            Assert.That(manager.TableValues, Has.Length.EqualTo(GameDefines.TotalColumns));
        }

        [Test]
        public void GiveGameManager_WhenLoadContentCalled_ThenDice1IsInRange1To6()
        {
            GameManager manager = new();
            manager.LoadContent();

            Assert.That(manager.Dice1, Is.InRange(1, 6));
        }

        [Test]
        public void GiveGameManager_WhenLoadContentCalled_ThenDice2IsInRange1To6()
        {
            GameManager manager = new();
            manager.LoadContent();

            Assert.That(manager.Dice2, Is.InRange(1, 6));
        }

        // ---------------------------------------------------------------------------
        // NewGame
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveGameManager_WhenNewGameCalled_ThenIsRunningIsTrue()
        {
            GameManager manager = new();
            manager.LoadContent();
            manager.NewGame();

            Assert.That(manager.IsRunning, Is.True);
        }

        [Test]
        public void GiveGameManager_WhenNewGameCalled_ThenActivePlayerIs1()
        {
            GameManager manager = new();
            manager.LoadContent();
            manager.Player1.MovesLeft.Clear();
            manager.NextTurn();
            manager.NewGame();

            Assert.That(manager.ActivePlayer, Is.EqualTo(1));
        }

        [Test]
        public void GiveGameManager_WhenNewGameCalled_ThenBoardResets()
        {
            GameManager manager = new();
            manager.LoadContent();
            manager.NewGame();

            // col 11 should be back to its initial value of 5
            Assert.That(manager.TableValues[11], Is.EqualTo(5));
        }

        // ---------------------------------------------------------------------------
        // Delegation to Table
        // ---------------------------------------------------------------------------

        [Test]
        public void GiveGameManager_WhenMovePieceCalled_ThenTableValuesAreUpdated()
        {
            GameManager manager = new();
            manager.LoadContent();
            manager.Player1.MovesLeft.Clear();
            manager.Player1.MovesLeft.Add(6);

            manager.MovePiece(11, 6); // col 11 → col 17 (empty)

            Assert.That(manager.TableValues[17], Is.EqualTo(1));
        }

        [Test]
        public void GiveGameManager_WhenBearOffCalledWithPieceOutsideHomeBoard_ThenThrowsPieceMoveException()
        {
            GameManager manager = new();
            manager.LoadContent();
            manager.Player1.MovesLeft.Clear();
            manager.Player1.MovesLeft.Add(6);

            // col 11 is not in the P1 home board (18-23), so bear-off must throw
            Assert.Throws<PieceMoveException>(() => manager.BearOffPiece(11));
        }

        [Test]
        public void GiveGameManager_WhenThrowDiceCalled_ThenDiceValuesAreInValidRange()
        {
            GameManager manager = new();
            manager.LoadContent();
            manager.ThrowDice();

            Assert.That(manager.Dice1, Is.InRange(1, 6));
            Assert.That(manager.Dice2, Is.InRange(1, 6));
        }
    }
}
