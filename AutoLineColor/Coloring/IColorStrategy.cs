using JetBrains.Annotations;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal interface IColorStrategy
    {
        Color32 GetColor(TransportLine transportLine, [CanBeNull] System.Collections.Generic.List<Color32> usedColors);
    }
}