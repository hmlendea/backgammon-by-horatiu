using System.Collections.Generic;

using BackgammonByHoratiu.GameLogic.AI;
using BackgammonByHoratiu.GameLogic.AI.Evaluation;

namespace BackgammonByHoratiu.GameLogic.AI.Search
{
    internal static class MoveSearcher
    {
        // Maximum dice values per turn (doubles = 4 moves)
        const int MaxDiceSlots = 4;

        // Offset added to column values when encoding the state key (range: -15..+15)
        const int ColumnValueOffset = 15;

        internal static MoveSequenceResult FindBestSequence(BoardSnapshot snapshot)
        {
            Dictionary<string, MoveSequenceResult> transpositionTable = [];

            return SearchFrom(snapshot, transpositionTable);
        }

        static MoveSequenceResult SearchFrom(
            BoardSnapshot snapshot,
            Dictionary<string, MoveSequenceResult> transpositionTable)
        {
            List<MoveAction> legalMoves = snapshot.GetLegalMoves();

            if (legalMoves.Count == 0)
            {
                return new MoveSequenceResult(PositionEvaluator.Evaluate(snapshot), []);
            }

            string stateKey = ComputeStateKey(snapshot);

            if (transpositionTable.TryGetValue(stateKey, out MoveSequenceResult cached))
            {
                return cached;
            }

            MoveSequenceResult result = BestAmong(legalMoves, snapshot, transpositionTable);
            transpositionTable[stateKey] = result;

            return result;
        }

        static MoveSequenceResult BestAmong(
            List<MoveAction> legalMoves,
            BoardSnapshot snapshot,
            Dictionary<string, MoveSequenceResult> transpositionTable)
        {
            int bestScore = int.MinValue;
            List<MoveAction> bestSequence = [];

            foreach (MoveAction action in legalMoves)
            {
                BoardSnapshot nextSnapshot = snapshot.AfterMove(action);
                MoveSequenceResult continuation = SearchFrom(nextSnapshot, transpositionTable);

                if (continuation.Score > bestScore)
                {
                    bestScore = continuation.Score;
                    bestSequence = PrependAction(action, continuation.Actions);
                }
            }

            return new MoveSequenceResult(bestScore, bestSequence);
        }

        static List<MoveAction> PrependAction(MoveAction first, List<MoveAction> rest)
        {
            List<MoveAction> sequence = [first, .. rest];

            return sequence;
        }

        // Encodes the full board state as a compact string for transposition table lookup.
        // Dice are sorted so different orderings of the same dice produce the same key.
        static string ComputeStateKey(BoardSnapshot snapshot)
        {
            char[] key = new char[BoardLayout.ColumnCount + MaxDiceSlots + 2];

            for (int i = 0; i < BoardLayout.ColumnCount; i++)
            {
                key[i] = (char)(snapshot.ColumnValues[i] + ColumnValueOffset);
            }

            List<int> sortedDice = [.. snapshot.MovesLeft];
            sortedDice.Sort();

            for (int i = 0; i < MaxDiceSlots; i++)
            {
                key[BoardLayout.ColumnCount + i] = i < sortedDice.Count ? (char)sortedDice[i] : (char)0;
            }

            key[BoardLayout.ColumnCount + MaxDiceSlots] = (char)snapshot.AiOutedPieces;
            key[BoardLayout.ColumnCount + MaxDiceSlots + 1] = (char)snapshot.HumanOutedPieces;

            return new string(key);
        }
    }
}
