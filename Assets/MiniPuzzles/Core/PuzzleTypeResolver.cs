using System.Collections.Generic;
using Player;

namespace MiniPuzzles
{
    /// <summary>
    /// The ONLY place that maps a <see cref="PlayerState"/> to its allowed puzzle types.
    /// To add, remove, or re-assign a puzzle type, edit <see cref="StateMap"/> here.
    /// </summary>
    public static class PuzzleTypeResolver
    {
        // Edit this table to change which puzzles belong to which state.
        private static readonly Dictionary<PlayerState, PuzzleType[]> StateMap = new()
        {
            { PlayerState.Yin,  new[] { PuzzleType.ZIP,    PuzzleType.Tango     } },
            { PlayerState.Yang, new[] { PuzzleType.Queens, PuzzleType.LightsOut } },
        };

        /// <summary>
        /// Picks one puzzle type belonging to the given state. Selection is deterministic
        /// for a given seed, so a fixed seed always yields the same type.
        /// </summary>
        /// <param name="state">The player's current state.</param>
        /// <param name="seed">Generation seed used to pick among the state's types.</param>
        /// <returns>A puzzle type valid for the state.</returns>
        public static PuzzleType Resolve(PlayerState state, int seed)
        {
            PuzzleType[] options = StateMap[state];
            // Non-negative index from the seed, deterministic.
            int index = (seed & 0x7fffffff) % options.Length;
            return options[index];
        }
    }
}
