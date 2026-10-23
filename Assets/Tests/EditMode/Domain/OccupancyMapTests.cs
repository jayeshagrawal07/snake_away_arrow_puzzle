using System.Collections.Generic;
using NUnit.Framework;
using SnakeEscape.Domain;
using UnityEngine;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class OccupancyMapTests
    {
        // Helper: creates a minimal LevelDefinition with one snake
        private static LevelDefinition MakeLevel(string snakeId, List<GridCoord> cells,
                                                   GridCoord exitDir, int cols = 8, int rows = 8)
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "test";
            level.Cols = cols;
            level.Rows = rows;
            var snake = new SnakeDefinition
            {
                Id            = snakeId,
                BodyCells     = cells,
                ExitDirection = exitDir
            };
            level.Snakes = new List<SnakeDefinition> { snake };
            return level;
        }

        [Test]
        public void BuildFrom_ActiveSnake_OccupiesCells()
        {
            var cells = new List<GridCoord> { new GridCoord(0, 0), new GridCoord(1, 0) };
            var level = MakeLevel("s1", cells, GridCoord.Right);
            var map   = OccupancyMap.BuildFrom(level, new HashSet<string>());

            Assert.IsTrue(map.IsOccupied(new GridCoord(0, 0)));
            Assert.IsTrue(map.IsOccupied(new GridCoord(1, 0)));
        }

        [Test]
        public void BuildFrom_EscapedSnake_CellsNotOccupied()
        {
            var cells = new List<GridCoord> { new GridCoord(0, 0), new GridCoord(1, 0) };
            var level = MakeLevel("s1", cells, GridCoord.Right);
            var escaped = new HashSet<string> { "s1" };
            var map = OccupancyMap.BuildFrom(level, escaped);

            Assert.IsFalse(map.IsOccupied(new GridCoord(0, 0)));
        }

        [Test]
        public void TryGetOccupant_OccupiedCell_ReturnsCorrectId()
        {
            var cells = new List<GridCoord> { new GridCoord(3, 3) };
            var level = MakeLevel("snake_a", cells, GridCoord.Up);
            var map   = OccupancyMap.BuildFrom(level, new HashSet<string>());

            bool found = map.TryGetOccupant(new GridCoord(3, 3), out string id);
            Assert.IsTrue(found);
            Assert.AreEqual("snake_a", id);
        }

        [Test]
        public void TryGetOccupant_EmptyCell_ReturnsFalse()
        {
            var level = MakeLevel("s1", new List<GridCoord> { new GridCoord(0, 0) }, GridCoord.Right);
            var map   = OccupancyMap.BuildFrom(level, new HashSet<string>());

            bool found = map.TryGetOccupant(new GridCoord(5, 5), out _);
            Assert.IsFalse(found);
        }

        [Test]
        public void Vacate_RemovesAllCellsForSnake()
        {
            var cells = new List<GridCoord>
            {
                new GridCoord(1, 0), new GridCoord(2, 0), new GridCoord(3, 0)
            };
            var level = MakeLevel("s1", cells, GridCoord.Right);
            var map   = OccupancyMap.BuildFrom(level, new HashSet<string>());

            map.Vacate("s1");

            foreach (var cell in cells)
                Assert.IsFalse(map.IsOccupied(cell));
        }

        [Test]
        public void Vacate_UnknownId_DoesNotThrow()
        {
            var level = MakeLevel("s1", new List<GridCoord> { new GridCoord(0, 0) }, GridCoord.Up);
            var map   = OccupancyMap.BuildFrom(level, new HashSet<string>());
            Assert.DoesNotThrow(() => map.Vacate("nonexistent"));
        }
    }
}

