using System;
using System.Collections.Generic;

namespace MiniPuzzles
{
    /// <summary>Generated ZIP level. Plain data only.</summary>
    public struct ZipLevelData
    {
        public int size;            // grid is size x size
        public int startIndex;      // cell index (row * size + col)
        public int endIndex;
        public int[] solutionPath;  // full Hamiltonian path, length size*size (start..end)
    }

    /// <summary>
    /// Generates a seeded Hamiltonian path through every cell of a square grid via
    /// randomized DFS with backtracking. The path guarantees a solvable trace from
    /// start to end visiting each cell exactly once.
    /// </summary>
    public static class ZipGenerator
    {
        /// <summary>Generates a ZIP level for the given difficulty and seed.</summary>
        public static ZipLevelData Generate(DifficultyLevel difficulty, int seed)
        {
            int size = difficulty switch
            {
                DifficultyLevel.Easy => 4,
                DifficultyLevel.Medium => 5,
                _ => 6
            };

            var rng = new System.Random(seed);
            int cells = size * size;
            int[] path = null;

            // Retry from different starts until a full Hamiltonian path is found.
            for (int attempt = 0; attempt < 64 && path == null; attempt++)
            {
                int start = rng.Next(cells);
                var visited = new bool[cells];
                var current = new List<int>(cells);
                if (TryBuild(start, size, visited, current, rng))
                {
                    path = current.ToArray();
                }
            }

            // Fallback: deterministic boustrophedon (snake) path always exists.
            if (path == null) path = SnakePath(size);

            return new ZipLevelData
            {
                size = size,
                startIndex = path[0],
                endIndex = path[path.Length - 1],
                solutionPath = path
            };
        }

        private static bool TryBuild(int cell, int size, bool[] visited, List<int> path, System.Random rng)
        {
            visited[cell] = true;
            path.Add(cell);

            if (path.Count == size * size) return true;

            foreach (int next in Neighbors(cell, size, rng))
            {
                if (!visited[next] && TryBuild(next, size, visited, path, rng))
                    return true;
            }

            visited[cell] = false;
            path.RemoveAt(path.Count - 1);
            return false;
        }

        private static IEnumerable<int> Neighbors(int cell, int size, System.Random rng)
        {
            int r = cell / size, c = cell % size;
            var list = new List<int>(4);
            if (r > 0) list.Add(cell - size);
            if (r < size - 1) list.Add(cell + size);
            if (c > 0) list.Add(cell - 1);
            if (c < size - 1) list.Add(cell + 1);

            // Fisher-Yates shuffle for seeded randomness.
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private static int[] SnakePath(int size)
        {
            var path = new int[size * size];
            int k = 0;
            for (int r = 0; r < size; r++)
            {
                if (r % 2 == 0)
                    for (int c = 0; c < size; c++) path[k++] = r * size + c;
                else
                    for (int c = size - 1; c >= 0; c--) path[k++] = r * size + c;
            }
            return path;
        }
    }
}
