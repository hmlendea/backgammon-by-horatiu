using System;
using BackgammonByHoratiu.Entities;
using NUnit.Framework;

namespace BackgammonByHoratiu.UnitTests.Entities
{
    [TestFixture]
    public class PieceMoveExceptionTests
    {
        [Test]
        public void GivePieceMoveException_WhenCreatedWithDefaultConstructor_ThenNoExceptionThrown()
        {
            Assert.That(() => new PieceMoveException(), Throws.Nothing);
        }

        [Test]
        public void GivePieceMoveException_WhenCreatedWithMessage_ThenMessageIsStored()
        {
            string message = "Invalid move";
            PieceMoveException exception = new(message);

            Assert.That(exception.Message, Is.EqualTo(message));
        }

        [Test]
        public void GivePieceMoveException_WhenCreatedWithMessageAndInnerException_ThenBothAreStored()
        {
            InvalidOperationException innerException = new("inner error");
            PieceMoveException exception = new("outer error", innerException);

            Assert.That(exception.Message, Is.EqualTo("outer error"));
            Assert.That(exception.InnerException, Is.SameAs(innerException));
        }

        [Test]
        public void GivePieceMoveException_WhenThrown_ThenIsInstanceOfException()
        {
            PieceMoveException exception = new("msg");

            Assert.That(exception, Is.InstanceOf<Exception>());
        }

        [Test]
        public void GivePieceMoveException_WhenThrown_ThenCanBeCaughtAsException()
        {
            bool caught = false;

            try
            {
                throw new PieceMoveException("test");
            }
            catch (Exception)
            {
                caught = true;
            }

            Assert.That(caught, Is.True);
        }
    }
}
