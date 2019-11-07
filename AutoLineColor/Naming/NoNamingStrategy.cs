namespace AutoLineColor.Naming
{
    internal class NoNamingStrategy : INamingStrategy
    {
        public string GetName(in TransportLine transportLine)
        {
            return null;
        }
    }
}