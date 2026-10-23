using System.Collections.Generic;
using UnityEngine;

namespace SnakeEscape.Domain
{
    /// <summary>
    /// Generates intricate, addictive interlocking serpentine puzzle levels with long backbones.
    /// Uses reverse topological carving to guarantee the puzzle is 100% solvable.
    /// </summary>
    public static class LongSpineMazeGenerator
    {
        private static readonly Color NavyBlack = new Color(0.04f, 0.06f, 0.12f);

        public static LevelDefinition Generate(int levelNumber, int seed = 0)
        {
            if (seed == 0) seed = levelNumber * 104729 + 17;
            Random.InitState(seed);

            int cols, rows, spineMinLen, spineMaxLen, targetSnakeCap;
            int shapeType = 0;

            if (levelNumber == 1)
            {
                // Level 1: Gentle intro (8 snakes)
                cols = 9;
                rows = 11;
                spineMinLen = 4;
                spineMaxLen = 7;
                targetSnakeCap = 8;
                shapeType = 0;
            }
            else if (levelNumber <= 4)
            {
                // Levels 2 to 4: Fast-paced intro (15-20 snakes)
                cols = 11 + levelNumber;
                rows = 13 + levelNumber;
                spineMinLen = 5;
                spineMaxLen = 9;
                targetSnakeCap = 12 + levelNumber * 3;
                shapeType = 0;
            }
            else if (levelNumber == 5 || levelNumber == 6)
            {
                // Heart shape or organic shape (30-40 snakes)
                cols = 16;
                rows = 18;
                spineMinLen = 6;
                spineMaxLen = 14;
                targetSnakeCap = 38;
                shapeType = (levelNumber == 6) ? 1 : 5;
            }
            else
            {
                // Levels 7+ (like Image 2 Level 10): 35 to 65+ dense interlocking snakes!
                cols = Mathf.Clamp(15 + (levelNumber - 6) / 2, 16, 20);
                rows = Mathf.Clamp(19 + (levelNumber - 6) / 2, 20, 24);
                spineMinLen = 7;
                spineMaxLen = 16;
                targetSnakeCap = Mathf.Clamp(36 + (levelNumber - 6) * 3, 36, 75);
                int[] patterns = { 5, 1, 2, 3, 4, 0 };
                shapeType = patterns[(levelNumber - 7) % patterns.Length];
            }

            bool[,] unassigned = new bool[cols, rows];
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    unassigned[x, y] = IsCellInsideShape(x, y, cols, rows, shapeType, seed);

            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };

            List<List<GridCoord>> carvedSnakes = new List<List<GridCoord>>();
            HashSet<GridCoord> allVacated = new HashSet<GridCoord>();
            HashSet<GridCoord> lastVacated = new HashSet<GridCoord>();

            int safety = 0;
            while (safety < 850 && carvedSnakes.Count < targetSnakeCap)
            {
                safety++;
                bool requireBlocker = (levelNumber >= 4) && (carvedSnakes.Count >= 1);
                bool buildLongSpine = (carvedSnakes.Count % 3 != 2);
                int minL = buildLongSpine ? spineMinLen : 4;
                int maxL = buildLongSpine ? spineMaxLen : 7;

                List<GridCoord> snake = CarveNextInterlockingSnake(unassigned, allVacated, lastVacated, cols, rows, dx, dy, minL, maxL, requireBlocker, buildLongSpine);
                if (snake == null)
                    snake = CarveNextInterlockingSnake(unassigned, allVacated, lastVacated, cols, rows, dx, dy, 3, 6, false, false);
                if (snake == null)
                    snake = CarveNextInterlockingSnake(unassigned, allVacated, lastVacated, cols, rows, dx, dy, 2, 4, false, false);

                if (snake != null)
                {
                    carvedSnakes.Add(snake);
                    lastVacated.Clear();
                    foreach (var c in snake)
                    {
                        unassigned[c.Col, c.Row] = false;
                        allVacated.Add(c);
                        lastVacated.Add(c);
                    }
                }
                else break;
            }

            // Create LevelDefinition
            var levelDef = ScriptableObject.CreateInstance<LevelDefinition>();
            levelDef.LevelId = $"Level {levelNumber}";
            levelDef.CampaignChapter = "Campaign 1";
            levelDef.SortOrder = levelNumber;
            levelDef.Cols = cols;
            levelDef.Rows = rows;
            levelDef.DifficultyCategory = levelNumber < 4 ? "Easy" : (levelNumber < 8 ? "Medium" : "Hard");

            levelDef.Snakes = new List<SnakeDefinition>();
            for (int i = 0; i < carvedSnakes.Count; i++)
            {
                var path = carvedSnakes[i];
                if (path.Count < 2) continue;

                // Head is index 0; exit direction points forward away from neck (index 1)
                GridCoord head = path[0];
                GridCoord neck = path[1];
                GridCoord exitDir = head.DirectionTo(head + (head - neck));
                if (!exitDir.IsCardinalUnit) exitDir = GridCoord.Up;

                var sDef = new SnakeDefinition
                {
                    Id = $"snake_{i}",
                    BodyColor = NavyBlack,
                    ExitDirection = exitDir,
                    BodyCells = new List<GridCoord>(path)
                };
                levelDef.Snakes.Add(sDef);
            }

            return levelDef;
        }

        private static List<GridCoord> CarveNextInterlockingSnake(bool[,] unassigned, HashSet<GridCoord> allVacated, HashSet<GridCoord> lastVacated,
                                                                  int cols, int rows, int[] dx, int[] dy, int minLen, int maxLen,
                                                                  bool strictLock, bool preferLongCorridors)
        {
            List<Vector3Int> validHeads = new List<Vector3Int>();

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (!unassigned[x, y]) continue;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x - dx[d], ny = y - dy[d];
                        if (nx < 0 || nx >= cols || ny < 0 || ny >= rows || !unassigned[nx, ny]) continue;
                        if (IsRayClearOfUnassigned(unassigned, cols, rows, x, y, dx[d], dy[d]))
                        {
                            if (strictLock && !DoesRayCrossAnyCell(x, y, dx[d], dy[d], cols, rows, allVacated))
                                continue;
                            validHeads.Add(new Vector3Int(x, y, d));
                        }
                    }
                }
            }

            if (validHeads.Count == 0) return null;

            // Shuffle
            for (int i = 0; i < validHeads.Count; i++)
            {
                int sw = Random.Range(i, validHeads.Count);
                Vector3Int tmp = validHeads[i]; validHeads[i] = validHeads[sw]; validHeads[sw] = tmp;
            }

            validHeads.Sort((a, b) =>
            {
                int scoreA = ScoreInterlockHead(a.x, a.y, dx[a.z], dy[a.z], cols, rows, allVacated, lastVacated);
                int scoreB = ScoreInterlockHead(b.x, b.y, dx[b.z], dy[b.z], cols, rows, allVacated, lastVacated);
                return scoreA.CompareTo(scoreB);
            });

            int maxTry = Mathf.Min(validHeads.Count, 45);
            for (int i = 0; i < maxTry; i++)
            {
                Vector3Int h = validHeads[i];
                int hx = h.x, hy = h.y, dir = h.z;
                int nx = hx - dx[dir], ny = hy - dy[dir];

                List<GridCoord> cells = new List<GridCoord> { new GridCoord(hx, hy), new GridCoord(nx, ny) };
                int targetLen = Random.Range(minLen, maxLen + 1);
                int cx = nx, cy = ny, lastDir = dir, straightRun = 1;
                int desiredRunBeforeTurn = preferLongCorridors ? Random.Range(3, 6) : Random.Range(1, 3);

                for (int step = 2; step < targetLen; step++)
                {
                    List<int> dirs = new List<int> { 0, 1, 2, 3 };
                    for (int k = 0; k < 4; k++)
                    {
                        int sw = Random.Range(k, 4);
                        int t = dirs[k]; dirs[k] = dirs[sw]; dirs[sw] = t;
                    }

                    dirs.Sort((dA, dB) =>
                    {
                        GridCoord pA = new GridCoord(cx - dx[dA], cy - dy[dA]);
                        GridCoord pB = new GridCoord(cx - dx[dB], cy - dy[dB]);
                        int scA = -CountExposedSides(pA.Col, pA.Row, cols, rows, unassigned, cells) * 2;
                        int scB = -CountExposedSides(pB.Col, pB.Row, cols, rows, unassigned, cells) * 2;

                        if (straightRun < desiredRunBeforeTurn)
                        {
                            if (dA == lastDir) scA -= 5;
                            if (dB == lastDir) scB -= 5;
                        }
                        else
                        {
                            if (dA != lastDir) scA -= 6;
                            if (dB != lastDir) scB -= 6;
                        }
                        return scA.CompareTo(scB);
                    });

                    bool added = false;
                    foreach (int nextD in dirs)
                    {
                        int bx = cx - dx[nextD], by = cy - dy[nextD];
                        if (bx < 0 || bx >= cols || by < 0 || by >= rows) continue;
                        var bCoord = new GridCoord(bx, by);
                        if (!unassigned[bx, by] || cells.Contains(bCoord)) continue;
                        if (IsCellOnRay(hx, hy, dx[dir], dy[dir], bx, by)) continue;

                        cells.Add(bCoord);
                        cx = bx; cy = by;
                        if (nextD == lastDir)
                        {
                            straightRun++;
                        }
                        else
                        {
                            straightRun = 1;
                            desiredRunBeforeTurn = preferLongCorridors ? Random.Range(2, 5) : Random.Range(1, 3);
                        }
                        lastDir = nextD;
                        added = true;
                        break;
                    }
                    if (!added) break;
                }

                if (cells.Count >= minLen) return cells;
            }
            return null;
        }

        private static bool DoesRayCrossAnyCell(int hx, int hy, int rdx, int rdy, int cols, int rows, HashSet<GridCoord> targetCells)
        {
            int x = hx + rdx, y = hy + rdy;
            while (x >= 0 && x < cols && y >= 0 && y < rows)
            {
                if (targetCells.Contains(new GridCoord(x, y))) return true;
                x += rdx; y += rdy;
            }
            return false;
        }

        private static int ScoreInterlockHead(int hx, int hy, int rdx, int rdy, int cols, int rows, HashSet<GridCoord> allVacated, HashSet<GridCoord> lastVacated)
        {
            int score = 0, x = hx + rdx, y = hy + rdy, blockersCrossed = 0;
            bool hitsLast = false;
            while (x >= 0 && x < cols && y >= 0 && y < rows)
            {
                GridCoord pt = new GridCoord(x, y);
                if (allVacated.Contains(pt)) blockersCrossed++;
                if (lastVacated.Contains(pt)) hitsLast = true;
                x += rdx; y += rdy;
            }
            if (hitsLast) score -= 45;
            score -= blockersCrossed * 9;
            if (blockersCrossed == 0 && allVacated.Count > 0) score += 120;
            return score;
        }

        private static int CountExposedSides(int x, int y, int cols, int rows, bool[,] unassigned, List<GridCoord> currentPath)
        {
            if (x < 0 || x >= cols || y < 0 || y >= rows) return 0;
            int exposed = 0;
            int[] nx = { x + 1, x - 1, x, x };
            int[] ny = { y, y, y + 1, y - 1 };
            for (int i = 0; i < 4; i++)
            {
                if (nx[i] < 0 || nx[i] >= cols || ny[i] < 0 || ny[i] >= rows || !unassigned[nx[i], ny[i]] || currentPath.Contains(new GridCoord(nx[i], ny[i])))
                    exposed++;
            }
            return exposed;
        }

        private static bool IsRayClearOfUnassigned(bool[,] unassigned, int cols, int rows, int hx, int hy, int rdx, int rdy)
        {
            int x = hx + rdx, y = hy + rdy;
            while (x >= 0 && x < cols && y >= 0 && y < rows)
            {
                if (unassigned[x, y]) return false;
                x += rdx; y += rdy;
            }
            return true;
        }

        private static bool IsCellOnRay(int hx, int hy, int rdx, int rdy, int tx, int ty)
        {
            int x = hx + rdx, y = hy + rdy;
            while (x >= 0 && x < 45 && y >= 0 && y < 45)
            {
                if (x == tx && y == ty) return true;
                x += rdx; y += rdy;
            }
            return false;
        }

        private static bool IsCellInsideShape(int x, int y, int cols, int rows, int shape, int seed)
        {
            float nx = ((x / (float)(cols - 1)) * 2f) - 1f;
            float ny = ((y / (float)(rows - 1)) * 2f) - 1f;

            if (shape == 1) // Heart
            {
                float hy = ny * 1.12f + 0.16f, hx = nx * 1.12f;
                float a = hx * hx + hy * hy - 1f;
                return (a * a * a - hx * hx * hy * hy * hy) <= 0.14f;
            }
            else if (shape == 2) // Diamond
            {
                return (Mathf.Abs(nx) + Mathf.Abs(ny)) <= 1.38f;
            }
            else if (shape == 3) // Cross
            {
                return (Mathf.Abs(nx) <= 0.56f || Mathf.Abs(ny) <= 0.56f);
            }
            else if (shape == 4) // Hourglass
            {
                float pinch = 0.52f + 0.42f * ny * ny;
                return Mathf.Abs(nx) <= pinch && Mathf.Abs(ny) <= 0.95f;
            }
            else if (shape == 5) // Organic Island
            {
                float angle = Mathf.Atan2(ny, nx);
                float s1 = Mathf.Sin(angle * 3f + seed * 0.13f) * 0.16f;
                float s2 = Mathf.Cos(angle * 5f - seed * 0.29f) * 0.11f;
                float s3 = Mathf.Sin((nx + ny) * 4.5f + seed) * 0.08f;
                float maxR = 0.84f + s1 + s2 + s3;
                float dist = Mathf.Sqrt(nx * nx * 1.08f + ny * ny * 0.92f);
                return dist <= maxR;
            }
            return true;
        }
    }
}
