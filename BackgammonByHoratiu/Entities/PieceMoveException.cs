using System;

namespace BackgammonByHoratiu.Entities
{
    public class PieceMoveException : Exception
    {
        public PieceMoveException() { }

        public PieceMoveException(string message)
            : base(message) { }

        public PieceMoveException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
