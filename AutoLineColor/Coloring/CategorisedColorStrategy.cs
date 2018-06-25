using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal class CategorisedColorStrategy : IColorStrategy
    {
        public Color32 GetColor(TransportLine transportLine, System.Collections.Generic.List<Color32> usedColors)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (transportLine.Info.m_transportType)
            {
                case TransportInfo.TransportType.Bus:
                    return CategorisedColor.GetPaleColor(usedColors);
                case TransportInfo.TransportType.Metro:
                    return CategorisedColor.GetBrightColor(usedColors);
                default:
                    return CategorisedColor.GetDarkColor(usedColors);
            }
        }
    }
}
