using System;
using System.Runtime.Serialization;

namespace BackgammonByHoratiu.Entities
{
    [Serializable]
    public class PieceMoveException : Exception
    {
        public PieceMoveException()
        {

        }

        public PieceMoveException(string message)
            : base(message)
        {

        }

        public PieceMoveException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected PieceMoveException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
