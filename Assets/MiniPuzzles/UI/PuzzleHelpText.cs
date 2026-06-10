using Player;

namespace MiniPuzzles
{
    /// <summary>Centralized rule copy for each puzzle type.</summary>
    public static class PuzzleHelpText
    {
        /// <summary>One short line for the footer — does not repeat state (shown in header badge).</summary>
        public static string Short(PuzzleType type)
        {
            return type switch
            {
                PuzzleType.ZIP =>
                    "Drag from 1, hit numbered dots in order, fill every cell. Walls block you.",
                PuzzleType.Tango =>
                    "Tap: empty → Yin → Yang. Equal count per row/column. No three in a row.",
                PuzzleType.Queens =>
                    "Place one queen per row, column, and color region. No touching.",
                PuzzleType.LightsOut =>
                    "Tap toggles cell + neighbors. Turn all lights off.",
                _ => string.Empty
            };
        }

        public static string For(PuzzleType type, PlayerState state) => Short(type);
    }
}
