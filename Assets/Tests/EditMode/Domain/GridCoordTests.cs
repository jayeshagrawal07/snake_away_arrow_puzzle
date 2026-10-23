using NUnit.Framework;
using SnakeEscape.Domain;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class GridCoordTests
    {
        [Test]
        public void Constructor_SetsColAndRow()
        {
            var c = new GridCoord(3, 7);
            Assert.AreEqual(3, c.Col);
            Assert.AreEqual(7, c.Row);
        }

        [Test]
        public void Addition_IsCorrect()
        {
            var result = new GridCoord(1, 2) + new GridCoord(3, 4);
            Assert.AreEqual(new GridCoord(4, 6), result);
        }

        [Test]
        public void Subtraction_IsCorrect()
        {
            var result = new GridCoord(5, 3) - new GridCoord(2, 1);
            Assert.AreEqual(new GridCoord(3, 2), result);
        }

        [Test]
        public void Equality_SameValues_AreEqual()
        {
            Assert.AreEqual(new GridCoord(4, 5), new GridCoord(4, 5));
        }

        [Test]
        public void Equality_DifferentValues_AreNotEqual()
        {
            Assert.AreNotEqual(new GridCoord(1, 2), new GridCoord(2, 1));
        }

        [Test]
        public void GetHashCode_SameCoords_SameHash()
        {
            var a = new GridCoord(7, 3);
            var b = new GridCoord(7, 3);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void IsCardinallyAdjacentTo_Adjacent_ReturnsTrue()
        {
            var origin = new GridCoord(2, 2);
            Assert.IsTrue(origin.IsCardinallyAdjacentTo(new GridCoord(3, 2))); // right
            Assert.IsTrue(origin.IsCardinallyAdjacentTo(new GridCoord(1, 2))); // left
            Assert.IsTrue(origin.IsCardinallyAdjacentTo(new GridCoord(2, 3))); // up
            Assert.IsTrue(origin.IsCardinallyAdjacentTo(new GridCoord(2, 1))); // down
        }

        [Test]
        public void IsCardinallyAdjacentTo_Diagonal_ReturnsFalse()
        {
            var origin = new GridCoord(2, 2);
            Assert.IsFalse(origin.IsCardinallyAdjacentTo(new GridCoord(3, 3)));
        }

        [Test]
        public void IsCardinallyAdjacentTo_SameCell_ReturnsFalse()
        {
            var origin = new GridCoord(2, 2);
            Assert.IsFalse(origin.IsCardinallyAdjacentTo(new GridCoord(2, 2)));
        }

        [Test]
        public void IsCardinalUnit_CardinalConstants_ReturnsTrue()
        {
            Assert.IsTrue(GridCoord.Up.IsCardinalUnit);
            Assert.IsTrue(GridCoord.Down.IsCardinalUnit);
            Assert.IsTrue(GridCoord.Left.IsCardinalUnit);
            Assert.IsTrue(GridCoord.Right.IsCardinalUnit);
        }

        [Test]
        public void IsCardinalUnit_Zero_ReturnsFalse()
        {
            Assert.IsFalse(GridCoord.Zero.IsCardinalUnit);
        }

        [Test]
        public void IsCardinalUnit_Diagonal_ReturnsFalse()
        {
            Assert.IsFalse(new GridCoord(1, 1).IsCardinalUnit);
        }

        [Test]
        public void DirectionTo_AdjacentRight_ReturnsRight()
        {
            var from = new GridCoord(2, 2);
            var to   = new GridCoord(3, 2);
            Assert.AreEqual(GridCoord.Right, from.DirectionTo(to));
        }

        [Test]
        public void DirectionTo_AdjacentUp_ReturnsUp()
        {
            var from = new GridCoord(2, 2);
            var to   = new GridCoord(2, 3);
            Assert.AreEqual(GridCoord.Up, from.DirectionTo(to));
        }

        [Test]
        public void DirectionTo_NonAdjacent_ReturnsZero()
        {
            var from = new GridCoord(0, 0);
            var to   = new GridCoord(2, 0);
            Assert.AreEqual(GridCoord.Zero, from.DirectionTo(to));
        }

        [Test]
        public void Constants_HaveExpectedValues()
        {
            Assert.AreEqual(new GridCoord( 0,  1), GridCoord.Up);
            Assert.AreEqual(new GridCoord( 0, -1), GridCoord.Down);
            Assert.AreEqual(new GridCoord(-1,  0), GridCoord.Left);
            Assert.AreEqual(new GridCoord( 1,  0), GridCoord.Right);
            Assert.AreEqual(new GridCoord( 0,  0), GridCoord.Zero);
        }
    }
}

