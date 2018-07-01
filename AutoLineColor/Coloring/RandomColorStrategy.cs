using UnityEngine;

namespace AutoLineColor.Coloring
{
    public class RandomColorStrategy : IColorStrategy
    {
        public Color32 GetColor(in TransportLine transportLine, System.Collections.Generic.List<Color32> usedColors)
        {
            return RandomColor.GetColor(ColorFamily.Any, usedColors);
        }
    }
}