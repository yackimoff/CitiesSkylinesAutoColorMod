using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoLineColor.Naming
{
    // TODO: Require names to be unique.

    // TODO: More interesting naming patterns.

    /* TODO: Handle non-road networks.
     * 
     * Tram and monorail lines can use roads, so they might work in some cases already,
     * but if they only use dedicated tracks, they'll get generic names. (And if they use
     * a road for a tiny bit of their length, they'll get named after the road, which might
     * be surprising.)
     * 
     * Metro lines tend to follow roads, but they don't actually use the roads. We could
     * try to find road segments that line up with the metro tunnel, or use the roads the
     * stations are on.
     */

    class RoadNamingStrategy : NamingStrategyBase
    {
        protected override string GetGenericLineName(TransportLine transportLine)
        {
            var analysis = AnalyzeLine(transportLine);

            if (analysis.DistanceOnSegments != null)
            {
                var logger = Console.Instance;

                logger.Message(string.Format(
                    "Line traverses {0} segment names...",
                    analysis.DistanceOnSegments.Count));

                if (analysis.DistanceOnSegments.Count > 0)
                {
                    foreach (var pair in analysis.DistanceOnSegments)
                    {
                        logger.Message(string.Format(
                            "... '{0}' (for {1} units)",
                            pair.Key,
                            pair.Value));
                    }

                    var longestTraveledName = analysis.DistanceOnSegments
                        .Aggregate((max, p) => p.Value > max.Value ? p : max)
                        .Key;

                    return string.Format("#{0} {1} Line", transportLine.m_lineNumber, longestTraveledName);
                }
            }

            return null;
        }
    }
}
