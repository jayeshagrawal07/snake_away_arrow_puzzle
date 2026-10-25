using System.Linq;
using NUnit.Framework;
using SnakeEscape.Domain;

namespace SnakeEscape.Tests
{
    /// <summary>
    /// Automated campaign validation tests: proves every authored level in the campaign has valid geometry
    /// and has at least one deterministic solution sequence.
    /// </summary>
    [TestFixture]
    public class CampaignValidationTests
    {
        [Test]
        public void SampleCampaign_AllLevels_HaveValidGeometry_AndAreSolvable()
        {
            var campaign = SnakeEscape.Presentation.GameController.CreateSampleCampaign();
            Assert.GreaterOrEqual(campaign.Count, 3);

            var solver = new PuzzleSolver();

            for (int i = 0; i < campaign.Count; i++)
            {
                var level = campaign[i];
                var errors = PuzzleRules.ValidateLevel(level);
                Assert.AreEqual(0, errors.Count, $"Level {i + 1} ({level.LevelId}) validation failed: " + string.Join(", ", errors));

                var result = solver.Solve(level);
                Assert.IsTrue(result.IsSolved, $"Level {i + 1} ({level.LevelId}) is unsolvable: {result.Message}");
                Assert.Greater(result.SolutionSequence.Count, 0);
            }
        }

        [Test]
        public void SampleLevel1_SolvesDeterministicallyWithEverySnake()
        {
            var level = SnakeEscape.Presentation.GameController.CreateSampleLevel();
            var solver = new PuzzleSolver();
            var result = solver.Solve(level);
            var repeatedResult = solver.Solve(level);

            Assert.IsTrue(result.IsSolved);
            Assert.AreEqual(level.Snakes.Count, result.SolutionSequence.Count);
            CollectionAssert.AreEquivalent(level.Snakes.Select(snake => snake.Id), result.SolutionSequence);
            CollectionAssert.AreEqual(result.SolutionSequence, repeatedResult.SolutionSequence);
        }
    }
}
