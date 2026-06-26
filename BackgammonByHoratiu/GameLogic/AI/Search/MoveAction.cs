namespace BackgammonByHoratiu.GameLogic.AI.Search
{
    internal sealed class MoveAction
    {
        internal MoveActionType Type { get; }
        internal int SourceColumn { get; }
        internal int DestinationColumn { get; }
        internal int DieValue { get; }

        internal MoveAction(MoveActionType type, int sourceColumn, int destinationColumn, int dieValue)
        {
            Type = type;
            SourceColumn = sourceColumn;
            DestinationColumn = destinationColumn;
            DieValue = dieValue;
        }
    }
}
