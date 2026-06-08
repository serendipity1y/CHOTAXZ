using System;

namespace MiniPuzzles
{
    /// <summary>Generated Lights Out level. Plain data only.</summary>
    public struct LightsOutLevelData
    {
        public int size;          // grid is size x size
        public bool[] initial;    // true = light on, length size*size
        public int scrambleMoves; // number of seeded moves applied from solved
    }

    /// <summary>
    /// Generates a guaranteed-solvable Lights Out board by starting from the solved
    /// (all-off) state and applying a seeded number of random legal moves. Because each
    /// move is its own inverse, replaying the same moves solves the board.
    /// </summary>
    public static class LightsOutGenerator
    {
        /// <summary>Generates a Lights Out level for the given difficulty and seed.</summary>
        public static LightsOutLevelData Generate(DifficultyLevel difficulty, int seed)
        {
            int size = difficulty switch
            {
                DifficultyLevel.Easy => 3,
                DifficultyLevel.Medium => 4,
                _ => 5
            };

            var rng = new System.Random(seed);
            int cells = size * size;
            var state = new bool[cells];

            int moves = size * size; // scales with board size
            int applied = 0;
            for (int i = 0; i < moves; i++)
            {
                int cell = rng.Next(cells);
                Toggle(state, size, cell);
                applied++;
            }

            // Guard against the rare all-off result; force at least one lit cell.
            bool anyOn = false;
            for (int i = 0; i < cells; i++) if (state[i]) { anyOn = true; break; }
            if (!anyOn)
            {
                Toggle(state, size, rng.Next(cells));
                applied++;
            }

            return new LightsOutLevelData { size = size, initial = state, scrambleMoves = applied };
        }

        /// <summary>Toggles a cell and its orthogonal neighbors. Shared legal-move rule.</summary>
        public static void Toggle(bool[] state, int size, int cell)
        {
            int r = cell / size, c = cell % size;
            Flip(state, size, r, c);
            Flip(state, size, r - 1, c);
            Flip(state, size, r + 1, c);
            Flip(state, size, r, c - 1);
            Flip(state, size, r, c + 1);
        }

        private static void Flip(bool[] state, int size, int r, int c)
        {
            if (r < 0 || r >= size || c < 0 || c >= size) return;
            state[r * size + c] = !state[r * size + c];
        }
    }
}
