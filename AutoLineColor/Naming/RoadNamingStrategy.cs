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
            var lineNum = transportLine.m_lineNumber;

            if (analysis.DistanceOnSegments != null)
            {
                var logger = Console.Instance;

                logger.Message(string.Format(
                    "Line traverses {0} segment names...",
                    analysis.DistanceOnSegments.Count));

                string districtBasedName = null, roadBasedName = null;

                if (analysis.Districts.Count > 0)
                {
                    districtBasedName = string.Join("/",
                        analysis.Districts.Select((d, i) => i == analysis.Districts.Count - 1 ? AbbreviateDistrictSuffix(d) : StripDistrictSuffix(d))
                        .ToArray());
                }

                if (analysis.DistanceOnSegments.Count > 0)
                {
                    foreach (var pair in analysis.DistanceOnSegments)
                    {
                        logger.Message(string.Format(
                            "... '{0}' (for {1} units)",
                            pair.Key,
                            pair.Value));
                    }

                    // sort all segment names by descending distance traveled, and add up the cumulative distance
                    var sorted = analysis.DistanceOnSegments
                        .OrderByDescending(p => p.Value)
                        .Scan(0f, (cd, p) => cd + p.Value,
                            (p, cd) => new { name = p.Key, distance = p.Value, cumulativeDistance = cd })
                        .ToArray();

                    // find the minimum set of roads that make up more than half the total distance
                    var totalDistance = analysis.DistanceOnSegments.Sum(p => p.Value);

                    logger.Message("totalDistance=" + totalDistance);
                    logger.Message("with cumulative:");
                    foreach (var r in sorted)
                    {
                        logger.Message(string.Format("'{0}', d={1}, cd={2}", r.name, r.distance, r.cumulativeDistance));
                    }

                    var majoritySize = 1 + Array.FindIndex(sorted, r => r.cumulativeDistance > totalDistance / 2);
                    var majority = sorted.Take(majoritySize);

                    roadBasedName = string.Join("/",
                        majority.Select((r, i) => i == majoritySize - 1 ? AbbreviateRoadSuffix(r.name) : StripRoadSuffix(r.name))
                        .ToArray());
                }

                if (string.IsNullOrEmpty(districtBasedName) && string.IsNullOrEmpty(roadBasedName))
                {
                    return null;
                }

                if (string.IsNullOrEmpty(districtBasedName))
                {
                    return string.Format("#{0} {1} Line", lineNum, roadBasedName);
                }

                if (string.IsNullOrEmpty(roadBasedName))
                {
                    return string.Format("#{0} {1}", lineNum, districtBasedName);
                }

                return string.Format("#{0} {1} via {2}", lineNum, districtBasedName, roadBasedName);
            }

            return null;
        }
    }
}
