using JetBrains.Annotations;

namespace AutoLineColor.Naming
{
    internal interface INamingStrategy
    {
        [CanBeNull]
        string GetName(in TransportLine transportLine);
    }
}