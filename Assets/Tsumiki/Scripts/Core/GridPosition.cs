using System;

namespace Tsumiki.Core
{
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public GridPosition(int x, int y, int z) { X = x; Y = y; Z = z; }
        public bool Equals(GridPosition other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is GridPosition other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X},{Y},{Z})";
    }
}

