using System;

namespace MiniPuzzles
{
    /// <summary>Generated Tango level. Plain data only. Cell values: 0 = sun, 1 = moon.</summary>
    public struct TangoLevelData
    {
        public int size;        // even grid size
        public int[] solution;  // full valid solution, length size*size
        public int[] givens;    // -1 empty, 0 sun, 1 moon (pre-revealed clues)
    }

    /// <summary>
    /// Generates a Tango board: a full valid solution (no 3 consecutive same symbol in any
    /// row/column, equal sun/moon count per row and column) via seeded backtracking, then
    /// reveals ~30% of cells as fixed clues.
    /// </summary>
    public static class TangoGenerator
    {
        private const int Empty = -1;
        private const int Sun = 0;
        private const int Moon = 1;

        /// <summary>Generates a Tango level for the given difficulty and seed.</summary>
        public static TangoLevelData Generate(DifficultyLevel difficulty, int seed)
        {
            int size = difficulty switch
            {
                DifficultyLevel.Easy => 4,
                DifficultyLevel.Medium => 6,
                _ => 8
            };

            var rng = new System.Random(seed);
            int[] solution = new int[size * size];
            for (int i = 0; i < solution.Length; i++) solution[i] = Empty;

            Solve(solution, size, 0, rng); // fills solution in place

            // Reveal ~30% of cells as clues.
            int[] givens = new int[size * size];
            for (int i = 0; i < givens.Length; i++) givens[i] = Empty;

            int revealCount = Math.Max(1, (int)Math.Round(size * size * 0.30));
            var indices = new int[size * size];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            Shuffle(indices, rng);
            for (int i = 0; i < revealCount; i++) givens[indices[i]] = solution[indices[i]];

            return new TangoLevelData { size = size, solution = solution, givens = givens };
        }

        private static bool Solve(int[] grid, int size, int index, System.Random rng)
        {
            if (index == size * size) return true;

            int first = rng.Next(2);
            for (int t = 0; t < 2; t++)
            {
                int value = t == 0 ? first : 1 - first;
                grid[index] = value;
                if (IsPartialValid(grid, size, index) && Solve(grid, size, index + 1, rng))
                    return true;
            }
            grid[index] = Empty;
            return false;
        }

        // Validates the just-placed cell against consecutive and count constraints.
        private static bool IsPartialValid(int[] grid, int size, int index)
        {
            int r = index / size, c = index % size;
            int v = grid[index];
            int half = size / 2;

            // No 3 consecutive horizontally (check the run ending at this cell).
            if (c >= 2 && grid[index - 1] == v && grid[index - 2] == v) return false;
            // No 3 consecutive vertically.
            if (r >= 2 && grid[index - size] == v && grid[index - 2 * size] == v) return false;

            // Row count: this cell completes its row only at the last column.
            int rowCount = 0;
            for (int cc = 0; cc <= c; cc++) if (grid[r * size + cc] == v) rowCount++;
            if (rowCount > half) return false;

            // Column count.
            int colCount = 0;
            for (int rr = 0; rr <= r; rr++) if (grid[rr * size + c] == v) colCount++;
            if (colCount > half) return false;

            return true;
        }

        private static void Shuffle(int[] arr, System.Random rng)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
