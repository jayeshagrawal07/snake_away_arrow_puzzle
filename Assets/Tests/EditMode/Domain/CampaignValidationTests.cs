using System.Linq;
using NUnit.Framework;
using SnakeEscape.Application;
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
        public void AuthoredCampaign_AllResourcesHaveValidGeometry_AndAreSolvable()
        {
            var service = new LevelService();
            service.LoadCampaign();

            Assert.GreaterOrEqual(service.LevelCount, 3);
            var solver = new PuzzleSolver();

            foreach (LevelDefinition level in service.AllLevels)
            {
                Assert.IsTrue(level.ValidationPassed, $"Level {level.LevelId} was not marked as validated.");
                Assert.IsNotEmpty(level.Snakes, $"Level {level.LevelId} has no snakes.");

                foreach (SnakeDefinition snake in level.Snakes)
                    Assert.IsNotEmpty(snake.BodyCells, $"Snake {snake.Id} in {level.LevelId} has no body cells.");

                var errors = PuzzleRules.ValidateLevel(level);
                Assert.AreEqual(0, errors.Count, $"Level {level.LevelId} validation failed: " + string.Join(", ", errors));

                var result = solver.Solve(level);
                Assert.IsTrue(result.IsSolved, $"Level {level.LevelId} is unsolvable: {result.Message}");
                Assert.AreEqual(level.Snakes.Count, result.SolutionSequence.Count);
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
