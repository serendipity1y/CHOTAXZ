using System;

namespace MiniPuzzles
{
    /// <summary>Generated Tango level. Cell values: 0 = Yin, 1 = Yang.</summary>
    public struct TangoLevelData
    {
        public int size;
        public int[] solution;
        public int[] givens; // -1 empty, 0 Yin, 1 Yang
    }

    /// <summary>
    /// Generates a valid Tango board with enough clues for a unique, deducible puzzle.
    /// </summary>
    public static class TangoGenerator
    {
        private const int Empty = -1;
        private const int Yin = 0;
        private const int Yang = 1;

        public static TangoLevelData Generate(DifficultyLevel difficulty, int seed)
        {
            int size = difficulty switch
            {
                DifficultyLevel.Easy => 4,
                DifficultyLevel.Medium => 6,
                _ => 8
            };

            var rng = new Random(seed);
            int[] solution = BuildSolution(size, rng);

            int targetClues = difficulty switch
            {
                DifficultyLevel.Easy => (int)Math.Round(size * size * 0.55),
                DifficultyLevel.Medium => (int)Math.Round(size * size * 0.45),
                _ => (int)Math.Round(size * size * 0.42)
            };
            int maxClues = (int)Math.Round(size * size * 0.58);

            int[] givens = BuildGivens(solution, size, targetClues, maxClues, rng);
            return new TangoLevelData { size = size, solution = solution, givens = givens };
        }

        private static int[] BuildSolution(int size, Random rng)
        {
            for (int attempt = 0; attempt < 32; attempt++)
            {
                var grid = new int[size * size];
                for (int i = 0; i < grid.Length; i++) grid[i] = Empty;
                if (Solve(grid, size, 0, rng) && IsCompleteValid(grid, size))
                    return grid;
            }

            // Deterministic fallback pattern (always valid on even sizes).
            var fallback = new int[size * size];
            for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                fallback[r * size + c] = ((r + c) % 2 == 0) ? Yin : Yang;
            return fallback;
        }

        private static int[] BuildGivens(int[] solution, int size, int targetClues, int maxClues, Random rng)
        {
            int cells = size * size;
            var givens = new int[cells];
            for (int i = 0; i < cells; i++) givens[i] = Empty;

            var indices = new int[cells];
            for (int i = 0; i < cells; i++) indices[i] = i;
            Shuffle(indices, rng);

            int revealed = 0;
            for (int i = 0; i < cells && revealed < targetClues; i++)
            {
                givens[indices[i]] = solution[indices[i]];
                revealed++;
            }

            // Add clues until the puzzle has exactly one solution.
            for (int i = revealed; i < cells && CountSolutions(givens, size, 2) != 1; i++)
            {
                if (givens[indices[i]] != Empty) continue;
                givens[indices[i]] = solution[indices[i]];
                revealed++;
                if (revealed >= maxClues) break;
            }

            return givens;
        }

        /// <summary>Counts solutions up to <paramref name="limit"/> (stops early).</summary>
        public static int CountSolutions(int[] givens, int size, int limit = 2)
        {
            var grid = (int[])givens.Clone();
            return CountSolutionsRecursive(grid, size, 0, limit, 0);
        }

        private static int CountSolutionsRecursive(int[] grid, int size, int index, int limit, int count)
        {
            while (index < size * size && grid[index] != Empty) index++;
            if (index == size * size)
                return IsCompleteValid(grid, size) ? count + 1 : count;
            if (count >= limit) return count;

            for (int value = Yin; value <= Yang; value++)
            {
                grid[index] = value;
                if (!IsPartialValid(grid, size, index)) continue;
                count = CountSolutionsRecursive(grid, size, index + 1, limit, count);
                if (count >= limit) return count;
            }

            grid[index] = Empty;
            return count;
        }

        private static bool Solve(int[] grid, int size, int index, Random rng)
        {
            if (index == size * size) return IsCompleteValid(grid, size);

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

        private static bool IsCompleteValid(int[] grid, int size)
        {
            int half = size / 2;

            for (int r = 0; r < size; r++)
            {
                int yin = 0;
                for (int c = 0; c < size; c++)
                {
                    int v = grid[r * size + c];
                    if (v == Yin) yin++;
                    if (c >= 2 && v != Empty && v == grid[r * size + c - 1] && v == grid[r * size + c - 2])
                        return false;
                }
                if (yin != half) return false;
            }

            for (int c = 0; c < size; c++)
            {
                int yin = 0;
                for (int r = 0; r < size; r++)
                {
                    int v = grid[r * size + c];
                    if (v == Yin) yin++;
                    if (r >= 2 && v != Empty && v == grid[(r - 1) * size + c] && v == grid[(r - 2) * size + c])
                        return false;
                }
                if (yin != half) return false;
            }

            return true;
        }

        private static bool IsPartialValid(int[] grid, int size, int index)
        {
            int r = index / size, c = index % size;
            int v = grid[index];
            int half = size / 2;

            if (c >= 2 && grid[index - 1] == v && grid[index - 2] == v) return false;
            if (r >= 2 && grid[index - size] == v && grid[index - 2 * size] == v) return false;

            int rowCount = 0;
            for (int cc = 0; cc <= c; cc++)
                if (grid[r * size + cc] == v) rowCount++;
            if (rowCount > half) return false;

            int colCount = 0;
            for (int rr = 0; rr <= r; rr++)
                if (grid[rr * size + c] == v) colCount++;
            if (colCount > half) return false;

            // Finished row must have exact balance.
            if (c == size - 1)
            {
                int rowYin = 0;
                for (int cc = 0; cc < size; cc++)
                    if (grid[r * size + cc] == Yin) rowYin++;
                if (rowYin != half) return false;
            }

            if (r == size - 1)
            {
                int colYin = 0;
                for (int rr = 0; rr < size; rr++)
                    if (grid[rr * size + c] == Yin) colYin++;
                if (colYin != half) return false;
            }

            return true;
        }

        private static void Shuffle(int[] arr, Random rng)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
