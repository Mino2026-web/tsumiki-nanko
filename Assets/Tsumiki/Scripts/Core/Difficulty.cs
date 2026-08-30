namespace Tsumiki.Core
{
    public readonly struct Difficulty
    {
        public readonly int Level, Width, Depth, MaxHeight, MinTotal, MaxTotal, MinHidden, MaxHidden;
        public Difficulty(int level, int width, int depth, int maxHeight, int minTotal, int maxTotal, int minHidden, int maxHidden)
        { Level = level; Width = width; Depth = depth; MaxHeight = maxHeight; MinTotal = minTotal; MaxTotal = maxTotal; MinHidden = minHidden; MaxHidden = maxHidden; }

        public static Difficulty ForLevel(int level) => level switch
        {
            1 => new Difficulty(1, 2, 2, 4, 3, 5, 0, 0),
            2 => new Difficulty(2, 3, 2, 4, 5, 8, 0, 1),
            3 => new Difficulty(3, 3, 3, 4, 8, 12, 1, 3),
            4 => new Difficulty(4, 3, 3, 4, 12, 18, 3, 6),
            5 => new Difficulty(5, 4, 4, 4, 18, 27, 6, 10),
            _ => throw new System.ArgumentOutOfRangeException(nameof(level))
        };
    }
}

