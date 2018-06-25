using JetBrains.Annotations;

namespace AutoLineColor.Naming
{
    internal interface INamingStrategy
    {
        [CanBeNull]
        string GetName(TransportLine transportLine);
    }
}