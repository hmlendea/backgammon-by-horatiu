namespace BackgammonByHoratiu.GameLogic.AI
{
    internal static class BoardLayout
    {
        internal const int ColumnCount = 24;

        // Max face value on a single die
        internal const int MaxDieValue = 6;

        // Full prime length (covers an entire home board)
        internal const int MaxPrimeLength = 6;

        // AI home board: columns 0-5
        internal const int AiHomeBoardFirstColumn = 0;
        internal const int AiHomeBoardLastColumn = 5;
        internal const int AiFirstNonHomeColumn = AiHomeBoardLastColumn + 1;

        // Human home board (anchor zone): columns 18-23
        internal const int HumanHomeBoardFirstColumn = 18;
        internal const int HumanHomeBoardLastColumn = ColumnCount - 1;
    }
}
