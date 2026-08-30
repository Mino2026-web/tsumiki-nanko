using System;
using System.Collections.Generic;
using System.Linq;

namespace Tsumiki.Core
{
    public sealed class PuzzleGenerator
    {
        private readonly Random random;
        public PuzzleGenerator(int? seed = null) => random = seed.HasValue ? new Random(seed.Value) : new Random();

        public HeightMap Generate(int level, int maxAttempts = 20000)
        {
            var d = Difficulty.ForLevel(level);
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var map = new HeightMap(d.Width, d.Depth, d.MaxHeight);
                var target = random.Next(d.MinTotal, d.MaxTotal + 1);
                var x = random.Next(d.Width); var y = random.Next(d.Depth); map[x, y] = 1;
                while (map.TotalCount < target)
                {
                    var candidates = CandidateColumns(map).Where(p => map[p.x, p.y] < d.MaxHeight).ToList();
                    if (candidates.Count == 0) break;
                    var cell = candidates[random.Next(candidates.Count)];
                    map[cell.x, cell.y]++;
                }
                if (map.TotalCount == target && map.IsConnected() && map.HasOnlyInferableHiddenBlocks && map.HiddenCount >= d.MinHidden && map.HiddenCount <= d.MaxHidden) return map;
            }
            throw new InvalidOperationException($"Level {level} puzzle generation failed.");
        }

        private static IEnumerable<(int x, int y)> CandidateColumns(HeightMap map)
        {
            for (var x = 0; x < map.Width; x++) for (var y = 0; y < map.Depth; y++)
            {
                if (map[x, y] > 0) { yield return (x, y); continue; }
                if (map[x - 1, y] > 0 || map[x + 1, y] > 0 || map[x, y - 1] > 0 || map[x, y + 1] > 0) yield return (x, y);
            }
        }

        public IReadOnlyList<int> CountChoices(HeightMap map, int level)
        {
            var answer = map.TotalCount;
            var values = new HashSet<int> { answer };
            if (map.HiddenCount > 0) values.Add(map.VisibleCount);
            var offsets = level >= 4 ? new[] {-1, 1, -2, 2, -3, 3} : new[] {-3, 3, -2, 2, -1, 1};
            foreach (var offset in offsets) if (answer + offset > 0 && values.Count < 5) values.Add(answer + offset);
            for (var candidate = 1; values.Count < 5; candidate++) if (candidate != answer) values.Add(candidate);
            return values.OrderBy(_ => random.Next()).ToArray();
        }
    }
}
