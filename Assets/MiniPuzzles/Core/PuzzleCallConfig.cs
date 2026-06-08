using Player; // reuse the project's existing PlayerState enum (Yin / Yang)

namespace MiniPuzzles
{
    /// <summary>
    /// Difficulty tiers. Each puzzle generator interprets these into its own grid sizes.
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// The four supported puzzle modules. Adding a new value here also requires
    /// an entry in <see cref="PuzzleTypeResolver"/> and a prefab on the manager.
    /// </summary>
    public enum PuzzleType
    {
        ZIP,
        Tango,
        Queens,
        LightsOut
    }

    /// <summary>
    /// Plain configuration passed by a caller (e.g. a door) when requesting a puzzle.
    /// Contains no puzzle type — the type is resolved from player state at call time.
    /// </summary>
    public struct PuzzleCallConfig
    {
        /// <summary>Requested difficulty tier.</summary>
        public DifficultyLevel difficulty;

        /// <summary>Generation seed. 0 means "randomize a fresh seed each call".</summary>
        public int seed;

        /// <summary>Flavor text shown in the puzzle header.</summary>
        public string contextLabel;
    }

    /// <summary>
    /// Runtime context a concrete puzzle receives when it is started. Plain data only.
    /// </summary>
    public struct PuzzleContext
    {
        /// <summary>Difficulty tier to generate.</summary>
        public DifficultyLevel difficulty;

        /// <summary>Resolved generation seed (never 0).</summary>
        public int seed;

        /// <summary>Header flavor text.</summary>
        public string contextLabel;

        /// <summary>Player state that selected this puzzle (for the header badge).</summary>
        public PlayerState playerState;
    }
}
