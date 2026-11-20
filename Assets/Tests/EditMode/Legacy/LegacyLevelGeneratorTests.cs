using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SnakeEscape.Domain;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class LegacyLevelGeneratorTests
    {
        private static readonly int[] ProgressionLevels = { 2, 5, 6, 7, 13 };

        [Test]
        public void ProgressionLevels_GenerateRepeatableValidAndSolvablePaths()
        {
            Type gameManagerType = Type.GetType("GameManager, Assembly-CSharp");
            Assert.IsNotNull(gameManagerType, "Could not find the existing legacy GameManager.");

            var host = new GameObject("LegacyLevelGeneratorAudit");
            host.SetActive(false);
            Component gameManager = host.AddComponent(gameManagerType);

            MethodInfo generatePaths = gameManagerType.GetMethod(
                "GenerateSnakePaths",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(generatePaths, "Could not find the legacy snake-path generator.");

            MethodInfo withSeededRandomState = gameManagerType.GetMethod(
                "WithSeededRandomState",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(withSeededRandomState, "Could not find the legacy generator's seeded-random scope.");

            try
            {
                foreach (int levelNumber in ProgressionLevels)
                {
                    GeneratedLevel first = Generate(gameManager, generatePaths, withSeededRandomState, levelNumber);
                    GeneratedLevel repeated = Generate(gameManager, generatePaths, withSeededRandomState, levelNumber);

                    Assert.AreEqual(first.Cols, repeated.Cols, $"Level {levelNumber} board width changed for the same seed.");
                    Assert.AreEqual(first.Rows, repeated.Rows, $"Level {levelNumber} board height changed for the same seed.");
                    AssertPathsEqual(first.Paths, repeated.Paths, levelNumber);
                    AssertGeneratedLevelIsValidAndSolvable(first, levelNumber);
                    TestContext.WriteLine(
                        $"Level {levelNumber}: {first.Cols}x{first.Rows}, {first.Paths.Count} snakes, " +
                        $"{CountOccupiedCells(first.Paths)} occupied cells.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void SeededGeneration_RestoresGlobalRandomState()
        {
            Type gameManagerType = Type.GetType("GameManager, Assembly-CSharp");
            Assert.IsNotNull(gameManagerType, "Could not find the existing legacy GameManager.");

            MethodInfo withSeededRandomState = gameManagerType.GetMethod(
                "WithSeededRandomState",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(withSeededRandomState, "Could not find the legacy generator's seeded-random scope.");

            Random.State originalState = Random.state;

            try
            {
                const int randomSeed = 8675309;
                Random.InitState(randomSeed);
                var expectedValues = new float[4];
                for (int i = 0; i < expectedValues.Length; i++)
                    expectedValues[i] = Random.value;

                Random.InitState(randomSeed);
                withSeededRandomState.Invoke(null, new object[]
                {
                    9123,
                    (System.Action)(() =>
                    {
                        for (int i = 0; i < expectedValues.Length; i++)
                            Random.Range(0, int.MaxValue);
                    })
                });

                for (int i = 0; i < expectedValues.Length; i++)
                    Assert.AreEqual(expectedValues[i], Random.value, $"Global random value {i} changed during level generation.");
            }
            finally
            {
                Random.state = originalState;
            }
        }

        private static GeneratedLevel Generate(
            Component gameManager,
            MethodInfo generatePaths,
            MethodInfo withSeededRandomState,
            int levelNumber)
        {
            int seed = levelNumber * 81919 + 53;
            var args = new object[] { levelNumber, seed, false, 0, 0, 0 };
            List<List<Vector2Int>> paths = null;

            withSeededRandomState.Invoke(null, new object[]
            {
                seed,
                (Action)(() => paths = (List<List<Vector2Int>>)generatePaths.Invoke(gameManager, args))
            });

            return new GeneratedLevel((int)args[4], (int)args[5], paths);
        }

        private static void AssertPathsEqual(
            List<List<Vector2Int>> expected,
            List<List<Vector2Int>> actual,
            int levelNumber)
        {
            Assert.AreEqual(expected.Count, actual.Count, $"Level {levelNumber} snake count changed for the same seed.");
            for (int snakeIndex = 0; snakeIndex < expected.Count; snakeIndex++)
                CollectionAssert.AreEqual(expected[snakeIndex], actual[snakeIndex], $"Level {levelNumber}, snake {snakeIndex} changed.");
        }

        private static void AssertGeneratedLevelIsValidAndSolvable(GeneratedLevel generated, int levelNumber)
        {
            Assert.Greater(generated.Paths.Count, 0, $"Level {levelNumber} generated no snakes.");
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = $"legacy_level_{levelNumber}";
            level.Cols = generated.Cols;
            level.Rows = generated.Rows;

            var occupied = new HashSet<Vector2Int>();
            try
            {
                for (int snakeIndex = 0; snakeIndex < generated.Paths.Count; snakeIndex++)
                {
                    List<Vector2Int> path = generated.Paths[snakeIndex];
                    Assert.GreaterOrEqual(path.Count, 2, $"Level {levelNumber}, snake {snakeIndex} is too short.");

                    var snake = new SnakeDefinition
                    {
                        Id = $"snake_{snakeIndex}",
                        ExitDirection = new GridCoord(path[0].x - path[1].x, path[0].y - path[1].y)
                    };

                    foreach (Vector2Int cell in path)
                    {
                        Assert.That(cell.x, Is.InRange(0, generated.Cols - 1), $"Level {levelNumber} has an out-of-bounds column.");
                        Assert.That(cell.y, Is.InRange(0, generated.Rows - 1), $"Level {levelNumber} has an out-of-bounds row.");
                        Assert.IsTrue(occupied.Add(cell), $"Level {levelNumber} has overlapping snakes at {cell}.");
                        snake.BodyCells.Add(new GridCoord(cell.x, cell.y));
                    }

                    for (int cellIndex = 0; cellIndex < path.Count - 1; cellIndex++)
                    {
                        int distance = Mathf.Abs(path[cellIndex].x - path[cellIndex + 1].x)
                            + Mathf.Abs(path[cellIndex].y - path[cellIndex + 1].y);
                        Assert.AreEqual(1, distance, $"Level {levelNumber}, snake {snakeIndex} has a disconnected path.");
                    }

                    level.Snakes.Add(snake);
                }

                List<string> errors = PuzzleRules.ValidateLevel(level);
                Assert.IsEmpty(errors, $"Level {levelNumber} is structurally invalid: {string.Join(", ", errors)}");

                var state = new PuzzleState(level);
                for (int snakeIndex = 0; snakeIndex < level.Snakes.Count; snakeIndex++)
                {
                    SnakeDefinition snake = level.Snakes[snakeIndex];
                    MoveResult move = PuzzleRules.EvaluateMove(snake.Id, state);
                    Assert.IsTrue(move.IsValid, $"Level {levelNumber} cannot clear snake {snakeIndex} in carve order: {move.Message}");
                    state.CommitEscape(snake.Id);
                }

                Assert.IsTrue(state.IsComplete, $"Level {levelNumber} did not reach a completed state.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(level);
            }
        }

        private static int CountOccupiedCells(List<List<Vector2Int>> paths)
        {
            int count = 0;
            foreach (List<Vector2Int> path in paths)
                count += path.Count;
            return count;
        }

        private sealed class GeneratedLevel
        {
            public readonly int Cols;
            public readonly int Rows;
            public readonly List<List<Vector2Int>> Paths;

            public GeneratedLevel(int cols, int rows, List<List<Vector2Int>> paths)
            {
                Cols = cols;
                Rows = rows;
                Paths = paths;
            }
        }
    }
}
