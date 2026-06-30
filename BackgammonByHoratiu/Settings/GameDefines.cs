using NuciXNA.Primitives;

namespace BackgammonByHoratiu.Settings
{
    public static class GameDefines
    {
        public const int PieceSize = 48;

        public const int ColBarP1 = 100;
        public const int ColBarP2 = 101;
        public const int ColHouseP1 = 200;
        public const int ColHouseP2 = 201;

        public const int Padding = 8;

        public const int ColumnHeight = PieceSize * 5;

        public const int FrameWidth = 390;
        public const int FrameHeight = 816;

        // Frame bezel = 108px in the source spritesheet, uniform on all sides.
        // Rendered: 108 * 388 / 853 = 49  (same for vertical: 108 * 839 / 1844 = 49)
        public const int FrameBorder = 108 * FrameWidth / FrameHeight;    // 49

        // Board half content dimensions (inside the frame border)
        public const int BoardHalfWidth = FrameWidth - FrameBorder * 2;    // 290
        public const int BoardHalfHeight = FrameHeight - FrameBorder * 2;  // 741

        public const int BarX = FrameWidth;

        public const int HouseX = BarX + FrameWidth;
        public const int HouseWidth = PieceSize + Padding * 3;

        public const int WindowWidth = HouseX + HouseWidth;
        public const int WindowHeight = FrameHeight;

        public static Size2D ColumnFrameSize => new(105, 512);
        public static Size2D DieFrameSize => new(200, 200);
        public const int DieSize = 48;

        public const float AnimationSpeed = 12f;
        public const int OverflowLayerSourceOffset = 19;
        public const int TotalPiecesPerPlayer = 15;
        public const int TotalColumns = 24;
        public const int PiecesPerColumnLayer = 5;
        // Right frame has a slightly different measured top border (pixels)
        public const int RightFrameTopY = 47;
        // Gap between left-half last column right edge and right-half first column left edge
        public const int HalfSeparatorWidth = 102;
    }
}
