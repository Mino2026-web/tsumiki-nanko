using NUnit.Framework;
using Tsumiki.Core;

namespace Tsumiki.Tests
{
    public sealed class CoreTests
    {
        [Test]
        public void HiddenRule_UsesPositiveDiagonal()
        {
            var map = new HeightMap(2, 2, 2);
            map[0, 0] = 1; map[1, 1] = 2;
            Assert.That(map.HiddenCount, Is.EqualTo(1));
            Assert.That(map.VisibleCount, Is.EqualTo(2));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void Generator_RespectsDifficulty(int level)
        {
            var generator = new PuzzleGenerator(100 + level);
            var map = generator.Generate(level);
            var d = Difficulty.ForLevel(level);
            Assert.That(map.TotalCount, Is.InRange(d.MinTotal, d.MaxTotal));
            Assert.That(map.HiddenCount, Is.InRange(d.MinHidden, d.MaxHidden));
            Assert.That(map.IsConnected(), Is.True);
            Assert.That(map.HasOnlyInferableHiddenBlocks, Is.True);
            Assert.That(generator.CountChoices(map, level).Count, Is.EqualTo(5));
            Assert.That(generator.CountChoices(map, level), Is.Ordered.Ascending);
        }
    }
}
