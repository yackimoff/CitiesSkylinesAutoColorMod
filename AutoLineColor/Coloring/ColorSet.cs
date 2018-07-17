using System.Collections.Generic;
using AutoLineColor.Util;
using UnityEngine;

namespace AutoLineColor.Coloring
{
    internal sealed class ColorSet : IColorSet
    {
        private readonly IReadOnlyList<Color32> _colors;

        public ColorSet(IList<Color32> colors)
        {
            _colors = new ReadOnlyList<Color32>(colors);
        }

        public IReadOnlyList<Color32> GetColors()
        {
            return _colors;
        }
    }
}
