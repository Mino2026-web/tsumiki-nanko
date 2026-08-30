using UnityEngine;

namespace Tsumiki.Runtime
{
    public static class TsumikiPalette
    {
        public static readonly Color Background = Hex("F5F1E8");
        public static readonly Color Outline = Hex("151218");
        public static readonly Color[] Blocks =
        {
            Hex("E98C95"),
            Hex("F6E36F"),
            Hex("8FC9AE"),
            Hex("82B7D5"),
            Hex("AA9BC8")
        };
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
    }
}
