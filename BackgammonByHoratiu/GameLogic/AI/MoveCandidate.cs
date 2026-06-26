namespace BackgammonByHoratiu.GameLogic.AI
{
    internal sealed class MoveCandidate
    {
        internal int SourceColumn { get; }
        internal int DieValue { get; }
        internal int Score { get; }

        internal MoveCandidate(int sourceColumn, int dieValue, int score)
        {
            SourceColumn = sourceColumn;
            DieValue = dieValue;
            Score = score;
        }
    }
}
