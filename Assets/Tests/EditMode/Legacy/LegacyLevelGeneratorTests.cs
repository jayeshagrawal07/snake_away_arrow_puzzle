using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SnakeEscape.Tests
{
    [TestFixture]
    public class LegacyLevelGeneratorTests
    {
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
    }
}
