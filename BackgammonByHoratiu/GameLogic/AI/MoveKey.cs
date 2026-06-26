using System;

namespace BackgammonByHoratiu.GameLogic.AI
{
    internal sealed class MoveKey : IEquatable<MoveKey>
    {
        readonly int sourceColumn;
        readonly int dieValue;

        internal MoveKey(int sourceColumn, int dieValue)
        {
            this.sourceColumn = sourceColumn;
            this.dieValue = dieValue;
        }

        public bool Equals(MoveKey other)
        {
            if (other is null)
            {
                return false;
            }

            return sourceColumn == other.sourceColumn && dieValue == other.dieValue;
        }

        public override bool Equals(object obj) => Equals(obj as MoveKey);

        public override int GetHashCode() => HashCode.Combine(sourceColumn, dieValue);
    }
}
