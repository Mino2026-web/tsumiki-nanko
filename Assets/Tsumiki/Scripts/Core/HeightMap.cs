using System;
using System.Collections.Generic;
using System.Linq;

namespace Tsumiki.Core
{
    [Serializable]
    public sealed class HeightMap
    {
        private readonly int[,] heights;
        public int Width { get; }
        public int Depth { get; }
        public int MaxHeight { get; }
        public int TotalCount => heights.Cast<int>().Sum();

        public HeightMap(int width, int depth, int maxHeight)
        {
            if (width < 1 || depth < 1 || maxHeight < 1) throw new ArgumentOutOfRangeException();
            Width = width; Depth = depth; MaxHeight = maxHeight;
            heights = new int[width, depth];
        }

        public int this[int x, int y]
        {
            get => InBounds(x, y) ? heights[x, y] : 0;
            set
            {
                if (!InBounds(x, y)) throw new ArgumentOutOfRangeException();
                heights[x, y] = Math.Clamp(value, 0, MaxHeight);
            }
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Depth;
        public bool Contains(GridPosition p) => InBounds(p.X, p.Y) && p.Z >= 0 && p.Z < heights[p.X, p.Y];
        public bool IsHidden(GridPosition p) => Contains(p) && Contains(new GridPosition(p.X + 1, p.Y + 1, p.Z + 1));
        public bool IsInferableHidden(GridPosition p) => IsHidden(p) && Contains(new GridPosition(p.X, p.Y, p.Z + 1));
        public bool HasOnlyInferableHiddenBlocks => Blocks().Where(IsHidden).All(IsInferableHidden);
        public int HiddenCount => Blocks().Count(IsHidden);
        public int VisibleCount => TotalCount - HiddenCount;

        public IEnumerable<GridPosition> Blocks()
        {
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Depth; y++)
            for (var z = 0; z < heights[x, y]; z++)
                yield return new GridPosition(x, y, z);
        }

        public int[] LayerCounts()
        {
            var layers = new int[MaxHeight];
            foreach (var block in Blocks()) layers[block.Z]++;
            return layers.TakeWhile((_, i) => i == 0 || layers.Skip(i).Any(v => v > 0)).ToArray();
        }

        public bool IsConnected()
        {
            var occupied = new HashSet<(int x, int y)>();
            for (var x = 0; x < Width; x++) for (var y = 0; y < Depth; y++)
                if (heights[x, y] > 0) occupied.Add((x, y));
            if (occupied.Count == 0) return false;
            var queue = new Queue<(int x, int y)>();
            var seen = new HashSet<(int x, int y)>();
            var first = occupied.First(); queue.Enqueue(first); seen.Add(first);
            var directions = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var d in directions)
                {
                    var next = (cell.x + d.Item1, cell.y + d.Item2);
                    if (occupied.Contains(next) && seen.Add(next)) queue.Enqueue(next);
                }
            }
            return seen.Count == occupied.Count;
        }

        public int[,] TopView()
        {
            var result = new int[Width, Depth];
            for (var x = 0; x < Width; x++) for (var y = 0; y < Depth; y++) result[x, y] = heights[x, y] > 0 ? 1 : 0;
            return result;
        }

        public int[,] FrontView()
        {
            var result = new int[Width, MaxHeight];
            for (var x = 0; x < Width; x++)
            {
                var max = 0; for (var y = 0; y < Depth; y++) max = Math.Max(max, heights[x, y]);
                for (var z = 0; z < max; z++) result[x, z] = 1;
            }
            return result;
        }

        public int[,] SideView()
        {
            var result = new int[Depth, MaxHeight];
            for (var y = 0; y < Depth; y++)
            {
                var max = 0; for (var x = 0; x < Width; x++) max = Math.Max(max, heights[x, y]);
                for (var z = 0; z < max; z++) result[y, z] = 1;
            }
            return result;
        }

        public int[] Flatten()
        {
            var values = new int[Width * Depth];
            for (var y = 0; y < Depth; y++) for (var x = 0; x < Width; x++) values[y * Width + x] = heights[x, y];
            return values;
        }
    }
}
