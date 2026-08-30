using UnityEngine;

namespace Tsumiki.Runtime
{
    public sealed class BlockCell : MonoBehaviour
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public void Set(int x, int y) { X = x; Y = y; }
    }
}

