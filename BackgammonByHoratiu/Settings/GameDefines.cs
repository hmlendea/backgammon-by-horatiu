namespace BackgammonByHoratiu.Settings
{
    public static class GameDefines
    {
        public const int PieceSize = 48;

        // Special column identifiers used by both the animation system and AI dispatch
        public const int ColBarP1 = 100;
        public const int ColBarP2 = 101;
        public const int ColHouseP1 = 200;
        public const int ColHouseP2 = 201;

        public const int Padding = 8;

        // Column triangles are 5 pieces tall
        public const int ColumnHeight = PieceSize * 5;

        // Total board height: 10 piece rows for columns + 3 piece rows for the middle strip
        public const int BoardHeight = PieceSize * 13;   // 624

        // The left and right halves each have 6 columns (6 * PieceSize = 288)
        // The middle bar sits between them at x = 6 * PieceSize
        public const int BarX = 6 * PieceSize;           // 288
        public const int BarWidth = PieceSize + Padding * 2; // 64

        // House column sits after the second half of the board
        public const int HouseX = BarX + BarWidth + 6 * PieceSize; // 640
        public const int HouseWidth = PieceSize + Padding * 3;     // 72

        public const int WindowWidth = HouseX + HouseWidth;        // 712
        public const int WindowHeight = BoardHeight;                // 624

        public const float AnimationSpeed = 12f;
        public const int OverflowLayerSourceOffset = 19;
    }
}
