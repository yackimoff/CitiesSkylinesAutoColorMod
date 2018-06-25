using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal class RandomHueStrategy : IColorStrategy
    {
        public Color32 GetColor(TransportLine transportLine, System.Collections.Generic.List<Color32> usedColors)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (transportLine.Info.m_transportType)
            {
                case TransportInfo.TransportType.Bus:
                    return RandomColor.GetColor(ColorFamily.Blue, usedColors);
                case TransportInfo.TransportType.Metro:
                    return RandomColor.GetColor(ColorFamily.Green, usedColors);
                case TransportInfo.TransportType.Train:
                    return RandomColor.GetColor(ColorFamily.Orange, usedColors);
                default:
                    return RandomColor.GetColor(ColorFamily.Any, usedColors);
            }
        }
    }
}