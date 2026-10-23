using NUnit.Framework;
using SnakeEscape.Domain;

namespace SnakeEscape.Tests
{
    /// <summary>
    /// Automated campaign validation tests: proves every authored level has valid geometry
    /// and has at least one deterministic solution sequence.
    /// </summary>
    [TestFixture]
    public class CampaignValidationTests
    {
        [Test]
        public void SampleLevel1_HasValidGeometry_AndIsSolvable()
        {
            var level = SnakeEscape.Presentation.GameController.CreateSampleLevel();
            var errors = PuzzleRules.ValidateLevel(level);
            Assert.AreEqual(0, errors.Count, "Level 1 validation failed: " + string.Join(", ", errors));

            var solver = new PuzzleSolver();
            var result = solver.Solve(level);

            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(2, result.SolutionSequence.Count);
            Assert.AreEqual("snake_B", result.SolutionSequence[0]);
            Assert.AreEqual("snake_A", result.SolutionSequence[1]);
        }
    }
}
