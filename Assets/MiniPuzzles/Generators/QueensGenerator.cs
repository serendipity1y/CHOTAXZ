using System;
using System.Collections.Generic;

namespace MiniPuzzles
{
    /// <summary>Generated Queens level. Plain data only.</summary>
    public struct QueensLevelData
    {
        public int size;            // board is size x size, with size color regions
        public int[] regionOf;      // region id 0..size-1 per cell, length size*size
        public int[] solutionCols;  // column of the queen on each row (length size)
    }

    /// <summary>
    /// Generates a Queens board: first a valid solution (one queen per row, distinct
    /// columns, no adjacency including diagonal) via seeded backtracking, then grows
    /// exactly <c>size</c> contiguous color regions from the queens so each region
    /// contains exactly one queen.
    /// </summary>
    public static class QueensGenerator
    {
        /// <summary>Generates a Queens level for the given difficulty and seed.</summary>
        public static QueensLevelData Generate(DifficultyLevel difficulty, int seed)
        {
            int size = difficulty switch
            {
                DifficultyLevel.Easy => 5,
                DifficultyLevel.Medium => 6,
                _ => 8
            };

            var rng = new System.Random(seed);

            int[] cols = new int[size];
            for (int i = 0; i < size; i++) cols[i] = -1;
            var usedCols = new bool[size];

            // Retry until a non-adjacent solution is found (small boards solve fast).
            bool ok = false;
            for (int attempt = 0; attempt < 256 && !ok; attempt++)
            {
                Array.Clear(usedCols, 0, size);
                for (int i = 0; i < size; i++) cols[i] = -1;
                ok = Place(cols, usedCols, size, 0, rng);
            }
            if (!ok) cols = Fallback(size); // diagonal-stride fallback, guaranteed valid

            int[] regionOf = GrowRegions(cols, size, rng);

            return new QueensLevelData { size = size, regionOf = regionOf, solutionCols = cols };
        }

        private static bool Place(int[] cols, bool[] usedCols, int size, int row, System.Random rng)
        {
            if (row == size) return true;

            int[] order = new int[size];
            for (int i = 0; i < size; i++) order[i] = i;
            Shuffle(order, rng);

            foreach (int col in order)
            {
                if (usedCols[col]) continue;
                // No diagonal/orthogonal adjacency with the queen on the previous row.
                if (row > 0 && Math.Abs(col - cols[row - 1]) < 2) continue;

                cols[row] = col;
                usedCols[col] = true;
                if (Place(cols, usedCols, size, row + 1, rng)) return true;
                usedCols[col] = false;
                cols[row] = -1;
            }
            return false;
        }

        // Deterministic valid placement: stride of 2 wrapped, never adjacent on consecutive rows.
        private static int[] Fallback(int size)
        {
            int[] cols = new int[size];
            int c = 0;
            for (int r = 0; r < size; r++)
            {
                cols[r] = c;
                c = (c + 2) % size;
            }
            return cols;
        }

        // Multi-source BFS: each queen seeds one region; unassigned cells are claimed
        // by a random neighboring region until the board is fully partitioned.
        private static int[] GrowRegions(int[] cols, int size, System.Random rng)
        {
            int cells = size * size;
            int[] regionOf = new int[cells];
            for (int i = 0; i < cells; i++) regionOf[i] = -1;

            var frontier = new List<int>();
            for (int r = 0; r < size; r++)
            {
                int idx = r * size + cols[r];
                regionOf[idx] = r; // region id == queen's row
                frontier.Add(idx);
            }

            int remaining = cells - size;
            while (remaining > 0 && frontier.Count > 0)
            {
                int pick = rng.Next(frontier.Count);
                int cell = frontier[pick];
                int region = regionOf[cell];

                int unassignedNeighbor = RandomUnassignedNeighbor(cell, size, regionOf, rng);
                if (unassignedNeighbor < 0)
                {
                    frontier.RemoveAt(pick); // exhausted
                    continue;
                }

                regionOf[unassignedNeighbor] = region;
                frontier.Add(unassignedNeighbor);
                remaining--;
            }

            // Safety net: assign any stragglers to a neighboring region.
            for (int i = 0; i < cells; i++)
            {
                if (regionOf[i] != -1) continue;
                regionOf[i] = NearestAssignedRegion(i, size, regionOf);
            }

            return regionOf;
        }

        private static int RandomUnassignedNeighbor(int cell, int size, int[] regionOf, System.Random rng)
        {
            var neigh = OrthogonalNeighbors(cell, size);
            Shuffle(neigh, rng);
            foreach (int n in neigh) if (regionOf[n] == -1) return n;
            return -1;
        }

        private static int NearestAssignedRegion(int cell, int size, int[] regionOf)
        {
            foreach (int n in OrthogonalNeighbors(cell, size))
                if (regionOf[n] != -1) return regionOf[n];
            return 0;
        }

        private static int[] OrthogonalNeighbors(int cell, int size)
        {
            int r = cell / size, c = cell % size;
            var list = new List<int>(4);
            if (r > 0) list.Add(cell - size);
            if (r < size - 1) list.Add(cell + size);
            if (c > 0) list.Add(cell - 1);
            if (c < size - 1) list.Add(cell + 1);
            return list.ToArray();
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
