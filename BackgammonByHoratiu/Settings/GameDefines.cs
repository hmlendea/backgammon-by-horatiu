using NuciXNA.Primitives;

namespace BackgammonByHoratiu.Settings
{
    public static class GameDefines
    {
        public const int PieceFrameSize = 128;
        public const int PieceSize = 48;

        public const int ColBarP1 = 100;
        public const int ColBarP2 = 101;
        public const int ColHouseP1 = 200;
        public const int ColHouseP2 = 201;

        public const int Padding = 8;

        public const int ColumnHeight = PieceSize * 5;

        public static Size2D FrameSize => new(388, 812);
        public const int FrameThickness = 49;

        public static int BoardHalfWidth => FrameSize.Width - FrameThickness * 2;
        public static int BoardHalfHeight => FrameSize.Height - FrameThickness * 2;

        public static int BarX => FrameSize.Width;

        public static int HouseX => BarX + FrameSize.Width;
        public const int HouseWidth = PieceSize + Padding * 3;

        public static int WindowWidth => HouseX + HouseWidth * 2;
        public static int WindowHeight => FrameSize.Height;

        public static Size2D ColumnFrameSize => new(105, 512);
        public static Size2D DieFrameSize => new(200, 200);
        public const int DieSize = 48;

        public const float AnimationSpeed = 10f;
        public const int OverflowLayerSourceOffset = 19;
        public const int TotalPiecesPerPlayer = 15;
        public const int TotalColumns = 24;
        public const int DiceIndicatorSpacing = 2;
        public const int DiceIndicatorSize = (PieceSize - DiceIndicatorSpacing) / 2;
        public const int PiecesPerColumnLayer = 5;
        // Right frame has a slightly different measured top border (pixels)
        public const int RightFrameTopY = 47;
        // Gap between left-half last column right edge and right-half first column left edge
        public const int HalfSeparatorWidth = 102;
    }
}
