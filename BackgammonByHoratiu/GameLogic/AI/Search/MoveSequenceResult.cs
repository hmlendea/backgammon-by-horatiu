using System.Collections.Generic;

namespace BackgammonByHoratiu.GameLogic.AI.Search
{
    internal sealed class MoveSequenceResult
    {
        internal int Score { get; }
        internal List<MoveAction> Actions { get; }

        internal MoveSequenceResult(int score, List<MoveAction> actions)
        {
            Score = score;
            Actions = actions;
        }
    }
}
