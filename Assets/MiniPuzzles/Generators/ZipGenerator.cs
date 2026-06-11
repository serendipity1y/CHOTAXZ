using System;
using System.Collections.Generic;

namespace MiniPuzzles
{
    /// <summary>Generated ZIP level. Plain data only.</summary>
    public struct ZipLevelData
    {
        public int size;
        public int waypointCount;
        public int[] waypointAtCell;  // 0 = none, else 1..K
        public byte[] wallMask;       // per cell: 1=up 2=right 4=down 8=left
        public int[] solutionPath;    // full Hamiltonian path, length size*size
    }

    /// <summary>
    /// Generates a seeded Hamiltonian path with numbered waypoints and optional walls
    /// that never block the solution route.
    /// </summary>
    public static class ZipGenerator
    {
        public const byte WallUp = 1;
        public const byte WallRight = 2;
        public const byte WallDown = 4;
        public const byte WallLeft = 8;

        public static ZipLevelData Generate(DifficultyLevel difficulty, int seed)
        {
            int size = difficulty switch
            {
                DifficultyLevel.Easy => 5,
                DifficultyLevel.Medium => 6,
                _ => 7
            };

            var rng = new Random(seed);
            int cells = size * size;
            int[] path = null;

            for (int attempt = 0; attempt < 16 && path == null; attempt++)
            {
                int start = rng.Next(cells);
                var visited = new bool[cells];
                path = TryWarnsdorff(start, size, visited, rng);
            }

            if (path == null) path = SnakePath(size);

            int minWaypoints = difficulty switch
            {
                DifficultyLevel.Easy => 3,
                DifficultyLevel.Medium => 5,
                _ => 7
            };
            int maxWaypoints = difficulty switch
            {
                DifficultyLevel.Easy => 5,
                DifficultyLevel.Medium => 7,
                _ => 9
            };
            int waypointCount = rng.Next(minWaypoints, maxWaypoints + 1);
            waypointCount = Math.Min(waypointCount, path.Length);

            var waypointAtCell = PlaceWaypoints(path, waypointCount, rng);
            var wallMask = new byte[cells];
            PlaceWalls(path, size, wallMask, difficulty, rng);

            return new ZipLevelData
            {
                size = size,
                waypointCount = waypointCount,
                waypointAtCell = waypointAtCell,
                wallMask = wallMask,
                solutionPath = path
            };
        }

        private static int[] PlaceWaypoints(int[] path, int waypointCount, Random rng)
        {
            int cells = path.Length;
            var waypointAtCell = new int[cells];

            var middle = new List<int>();
            for (int i = 1; i < path.Length - 1; i++) middle.Add(i);
            Shuffle(middle, rng);

            var pathIndices = new List<int> { 0 };
            int midCount = Math.Max(0, waypointCount - 2);
            for (int i = 0; i < midCount && i < middle.Count; i++)
                pathIndices.Add(middle[i]);
            if (waypointCount > 1) pathIndices.Add(path.Length - 1);
            pathIndices.Sort();

            for (int i = 0; i < pathIndices.Count; i++)
                waypointAtCell[path[pathIndices[i]]] = i + 1;

            return waypointAtCell;
        }

        private static void PlaceWalls(int[] path, int size, byte[] wallMask, DifficultyLevel difficulty, Random rng)
        {
            int cells = size * size;
            var pathStep = new HashSet<long>();
            for (int i = 0; i < path.Length - 1; i++)
                pathStep.Add(EdgeKey(path[i], path[i + 1]));

            int minWalls = difficulty switch
            {
                DifficultyLevel.Easy => 2,
                DifficultyLevel.Medium => 5,
                _ => 9
            };
            int maxWalls = difficulty switch
            {
                DifficultyLevel.Easy => 4,
                DifficultyLevel.Medium => 8,
                _ => 14
            };
            int target = rng.Next(minWalls, maxWalls + 1);

            var candidates = new List<(int a, int b)>();
            for (int a = 0; a < cells; a++)
            {
                foreach (int b in OrthogonalNeighbors(a, size))
                {
                    if (a < b && !pathStep.Contains(EdgeKey(a, b)))
                        candidates.Add((a, b));
                }
            }

            Shuffle(candidates, rng);
            int placed = 0;
            foreach (var (a, b) in candidates)
            {
                if (placed >= target) break;
                if (HasWall(wallMask, a, b, size)) continue;
                AddWall(wallMask, a, b, size);
                placed++;
            }
        }

        public static bool HasWall(byte[] wallMask, int from, int to, int size)
        {
            int dr = to / size - from / size;
            int dc = to % size - from % size;
            if (dr == -1) return (wallMask[from] & WallUp) != 0;
            if (dr == 1) return (wallMask[from] & WallDown) != 0;
            if (dc == 1) return (wallMask[from] & WallRight) != 0;
            if (dc == -1) return (wallMask[from] & WallLeft) != 0;
            return true;
        }

        private static void AddWall(byte[] wallMask, int a, int b, int size)
        {
            int dr = b / size - a / size;
            int dc = b % size - a % size;
            if (dr == -1) { wallMask[a] |= WallUp; wallMask[b] |= WallDown; }
            else if (dr == 1) { wallMask[a] |= WallDown; wallMask[b] |= WallUp; }
            else if (dc == 1) { wallMask[a] |= WallRight; wallMask[b] |= WallLeft; }
            else if (dc == -1) { wallMask[a] |= WallLeft; wallMask[b] |= WallRight; }
        }

        private static long EdgeKey(int a, int b)
        {
            int lo = Math.Min(a, b);
            int hi = Math.Max(a, b);
            return ((long)lo << 32) | (uint)hi;
        }

        private static IEnumerable<int> OrthogonalNeighbors(int cell, int size)
        {
            int r = cell / size, c = cell % size;
            if (r > 0) yield return cell - size;
            if (r < size - 1) yield return cell + size;
            if (c > 0) yield return cell - 1;
            if (c < size - 1) yield return cell + 1;
        }

        // Warnsdorff's heuristic: always step to the unvisited neighbour with the fewest
        // onward moves. Finds Hamiltonian paths on grid graphs in O(n) with no backtracking.
        private static int[] TryWarnsdorff(int start, int size, bool[] visited, Random rng)
        {
            int cells = size * size;
            var path = new int[cells];
            Span<int> buf = stackalloc int[4];

            path[0] = start;
            visited[start] = true;

            for (int step = 1; step < cells; step++)
            {
                int cur = path[step - 1];
                int nCount = FillNeighbors(cur, size, buf);

                // shuffle for random tie-breaking
                for (int i = nCount - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (buf[i], buf[j]) = (buf[j], buf[i]);
                }

                int best = -1;
                int bestDeg = int.MaxValue;
                for (int i = 0; i < nCount; i++)
                {
                    int n = buf[i];
                    if (visited[n]) continue;
                    int deg = CountUnvisited(n, size, visited);
                    if (deg < bestDeg) { bestDeg = deg; best = n; }
                }

                if (best < 0) return null;
                visited[best] = true;
                path[step] = best;
            }
            return path;
        }

        private static int FillNeighbors(int cell, int size, Span<int> buf)
        {
            int r = cell / size, c = cell % size, n = 0;
            if (r > 0)        buf[n++] = cell - size;
            if (r < size - 1) buf[n++] = cell + size;
            if (c > 0)        buf[n++] = cell - 1;
            if (c < size - 1) buf[n++] = cell + 1;
            return n;
        }

        private static int CountUnvisited(int cell, int size, bool[] visited)
        {
            int r = cell / size, c = cell % size, n = 0;
            if (r > 0        && !visited[cell - size]) n++;
            if (r < size - 1 && !visited[cell + size]) n++;
            if (c > 0        && !visited[cell - 1])    n++;
            if (c < size - 1 && !visited[cell + 1])    n++;
            return n;
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

        private static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
