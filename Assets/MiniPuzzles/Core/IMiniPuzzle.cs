using System;

namespace MiniPuzzles
{
    /// <summary>
    /// Contract every puzzle module implements. A puzzle is fully self-contained:
    /// it generates its own level, builds its own UI under the overlay, and reports
    /// the result only through the supplied callbacks. It knows nothing about other
    /// puzzle types, the manager, or game systems.
    /// </summary>
    public interface IMiniPuzzle
    {
        /// <summary>The type this module implements (used for header + events).</summary>
        PuzzleType Type { get; }

        /// <summary>
        /// Starts the puzzle.
        /// </summary>
        /// <param name="context">Difficulty, seed, label and player state.</param>
        /// <param name="overlay">Shared overlay used for header, status and parenting.</param>
        /// <param name="onSolved">Invoked once when the player solves the puzzle.</param>
        /// <param name="onFailed">Invoked once when the player gives up or fails.</param>
        void Begin(PuzzleContext context, PuzzleOverlay overlay, Action onSolved, Action onFailed);
    }
}
