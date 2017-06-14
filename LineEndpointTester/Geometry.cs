using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineEndpointTester
{
    // based on https://github.com/byronknoll/visibility-polygon-js (public domain)

    interface IDistanceMetric
    {
        float GetDistanceBetweenPoints(int i, int j);
        IEnumerable<PointF> GetPathBetweenPoints(int i, int j);
        PointF[] GetPointsForDrawing(out bool[] pointIsSynthetic);
    }

    class TravelDistanceMetric : IDistanceMetric
    {
        private readonly PointF[] points;
        private readonly float[] adjacentDistances;

        public TravelDistanceMetric(IList<PointF> points)
        {
            this.points = points.ToArray();
            adjacentDistances = new float[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                var next = points[(i + 1) % points.Count];

                adjacentDistances[i] = (float)Math.Sqrt(Math.Pow(pt.X - next.X, 2.0) + Math.Pow(pt.Y - next.Y, 2.0));
            }
        }

        public float GetDistanceBetweenPoints(int i, int j)
        {
            return Math.Min(ForwardDistance(i, j), ForwardDistance(j, i));
        }

        private float ForwardDistance(int i, int j)
        {
            var result = 0f;

            while (i != j)
            {
                result += adjacentDistances[i];
                i = (i + 1) % adjacentDistances.Length;
            }

            return result;
        }

        public IEnumerable<PointF> GetPathBetweenPoints(int i, int j)
        {
            if (ForwardDistance(i, j) > ForwardDistance(j, i))
            {
                var temp = i;
                i = j;
                j = temp;
            }

            yield return points[i];

            while (i != j)
            {
                i = (i + 1) % points.Length;
                yield return points[i];
            }
        }

        public PointF[] GetPointsForDrawing(out bool[] pointIsSynthetic)
        {
            pointIsSynthetic = new bool[points.Length + 1];

            if (points.Length == 0)
                return points;

            return points.Concat(Enumerable.Repeat(points[0], 1)).ToArray();
        }
    }

    class InteriorDistanceMetric : IDistanceMetric
    {
        class Node
        {
            public int Index, OrigIndex;
            public PointF Location;
            public Node[] Edges;
        }

        private readonly Node[] nodes;
        private readonly int[] pointToNodeIndex;
        private readonly float[,] costs;
        private readonly int[,] prev;

        public InteriorDistanceMetric(IList<PointF> points)
        {
            var numPoints = points.Count;

            var polygon = new Polygon(points.ToArray());

            var pathNodes = new List<Node>();
            pointToNodeIndex = new int[numPoints];

            foreach (var v in polygon.GetVertexMap())
            {
                if (v.OrigIndex >= 0)
                    pointToNodeIndex[v.OrigIndex] = pathNodes.Count;

                pathNodes.Add(new Node { Index = pathNodes.Count, OrigIndex = v.OrigIndex, Location = v.Point });
            }

            nodes = pathNodes.ToArray();
            var numNodes = nodes.Length;

            for (int i = 0; i < numNodes; i++)
            {
                var location = nodes[i].Location;
                nodes[i].Edges =
                    nodes
                    .Where((n, j) => IsAdjacent(j, i) || (j != i && polygon.Contains(new LineSegment(location, n.Location))))
                    .ToArray();

                bool IsAdjacent(int a, int b)
                {
                    if (a == b + 1 || a == b - 1)
                        return true;

                    if (a == 0 && b == numNodes - 1)
                        return true;

                    if (b == 0 && a == numNodes - 1)
                        return true;

                    return false;
                }

                //Debug.Print("Node {0} has {1} edges", i, nodes[i].Edges.Length);
            }

            InitCosts(out costs, out prev);
        }

        public PointF[] GetPointsForDrawing(out bool[] pointIsSynthetic)
        {
            var nodeList = new List<Node>(nodes);
            if (nodeList.Count > 0)
                nodeList.Add(nodeList[0]);

            pointIsSynthetic = nodeList.Select(n => n.OrigIndex == -1).ToArray();
            return nodeList.Select(n => n.Location).ToArray();
        }

        private float[,] InitCosts(out float[,] costs, out int[,] prev)
        {
            var numNodes = nodes.Length;
            costs = new float[numNodes, numNodes];
            prev = new int[numNodes, numNodes];

            // repeat Dijkstra's algorithm to measure shortest paths between all nodes
            var queue = new SimplePriorityQueue<Node, float>();

            for (int startIndex = 0; startIndex < numNodes; startIndex++)
            {
                Debug.Assert(queue.Count == 0);

                for (int j = 0; j < numNodes; j++)
                {
                    var dist = costs[startIndex, j] =
                        (j == startIndex) ? 0f : float.PositiveInfinity;
                    queue.Enqueue(nodes[j], dist);
                    prev[startIndex, j] = -1;
                }

                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    var distanceToNode = costs[startIndex, node.Index];

                    foreach (var neighbor in node.Edges)
                    {
                        if (!queue.Contains(neighbor))
                            continue;

                        var nodeToNeighbor = LineSegment.Distance(node.Location, neighbor.Location);
                        var alt = distanceToNode + nodeToNeighbor;
                        if (alt < costs[startIndex, neighbor.Index])
                        {
                            costs[startIndex, neighbor.Index] = alt;
                            prev[startIndex, neighbor.Index] = node.Index;
                            queue.UpdatePriority(neighbor, alt);
                        }
                    }
                }
            }

            return costs;
        }

        public float GetDistanceBetweenPoints(int i, int j)
        {
            return costs[pointToNodeIndex[i], pointToNodeIndex[j]];
        }

        public IEnumerable<PointF> GetPathBetweenPoints(int i, int j)
        {
            var path = new Stack<PointF>();
            var a = pointToNodeIndex[i];
            var b = pointToNodeIndex[j];

            if (prev[a, b] != -1)
            {
                do
                {
                    path.Push(nodes[b].Location);
                    b = prev[a, b];
                } while (b != -1);
            }

            return path;
        }
    }

    struct LineSegment : IEquatable<LineSegment>
    {
        public readonly PointF Start, End;

        public float Length => Distance(Start, End);

        public PointF Midpoint => new PointF((Start.X + End.X) / 2, (Start.Y + End.Y) / 2);

        public static float Distance(PointF start, PointF end)
        {
            return (float)Math.Sqrt(SqrDistance(start, end));
        }

        public static float SqrDistance(PointF start, PointF end)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            return dx * dx + dy * dy;
        }

        public LineSegment(PointF start, PointF end)
        {
            Start = start;
            End = end;
        }

        public bool Intersects(LineSegment other)
        {
            return Intersects(Start, End, other.Start, other.End);
        }

        public bool Intersects(PointF otherStart, PointF otherEnd)
        {
            return Intersects(Start, End, otherStart, otherEnd);
        }

        public static bool Intersects(PointF start1, PointF end1, PointF start2, PointF end2)
        {
            // based on http://www.stefanbader.ch/faster-line-segment-intersection-for-unity3dc/
            var a = new PointF(end1.X - start1.X, end1.Y - start1.Y);
            var b = new PointF(start2.X - end2.X, start2.Y - end2.Y);
            var c = new PointF(start1.X - start2.X, start1.Y - start2.Y);

            float alphaNumerator = b.Y * c.X - b.X * c.Y;
            float alphaDenominator = a.Y * b.X - a.X * b.Y;
            float betaNumerator = a.X * c.Y - a.Y * c.X;
            float betaDenominator = alphaDenominator;

            if (alphaDenominator == 0 || betaDenominator == 0)
            {
                return false;
            }

            if (alphaDenominator > 0)
            {
                if (alphaNumerator < 0 || alphaNumerator > alphaDenominator)
                {
                    return false;
                }
            }
            else if (alphaNumerator > 0 || alphaNumerator < alphaDenominator)
            {
                return false;
            }

            if (betaDenominator > 0)
            {
                if (betaNumerator < 0 || betaNumerator > betaDenominator)
                {
                    return false;
                }
            }
            else if (betaNumerator > 0 || betaNumerator < betaDenominator)
            {
                return false;
            }

            return true;
        }

        public PointF FindIntersection(LineSegment other)
        {
            return FindIntersection(Start, End, other.Start, other.End);
        }

        public PointF FindIntersection(PointF otherStart, PointF otherEnd)
        {
            return FindIntersection(Start, End, otherStart, otherEnd);
        }

        public static PointF FindIntersection(PointF start1, PointF end1, PointF start2, PointF end2)
        {
            var dbx = end2.X - start2.X;
            var dby = end2.Y - start2.Y;
            var dax = end1.X - start1.X;
            var day = end1.Y - start1.Y;

            var u_b = dby * dax - dbx * day;
            if (u_b != 0)
            {
                var ua = (dbx * (start1.Y - start2.Y) - dby * (start1.X - start2.X)) / u_b;
                //return new PointF(start1.X - ua * -dax, start1.Y - ua * -day);
                return new PointF(start1.X + ua * dax, start1.Y + ua * day);
            }

            throw new ArgumentException("No intersection between segments");
        }

        private static bool PointsEqualish(PointF a, PointF b)
        {
            const float EPSILON = 0.0000001f;
            return Math.Abs(a.X - b.X) < EPSILON && Math.Abs(a.Y - b.Y) < EPSILON;
        }

        public bool Crosses(LineSegment other)
        {
            return Crosses(Start, End, other.Start, other.End);
        }

        public bool Crosses(PointF otherStart, PointF otherEnd)
        {
            return Crosses(Start, End, otherStart, otherEnd);
        }

        public static bool Crosses(PointF start1, PointF end1, PointF start2, PointF end2)
        {
            if (!Intersects(start1, end1, start2, end2))
                return false;

            var pt = FindIntersection(start1, end1, start2, end2);
            return !(PointsEqualish(pt, start1) || PointsEqualish(pt, end1) ||
                     PointsEqualish(pt, start2) || PointsEqualish(pt, end2));
        }

        public bool Equals(LineSegment other)
        {
            return other.Start == this.Start && other.End == this.End;
        }

        public override bool Equals(object obj)
        {
            return obj is LineSegment other && Equals(other);
        }

        public override int GetHashCode()
        {
            return unchecked(Start.GetHashCode() << 2 ^ End.GetHashCode());
        }
    }

    class Polygon
    {
        private readonly PointF[] vertices;
        private readonly float minCoord;
        private readonly int[] inputToVertexIndex;

        public Polygon(PointF[] vertices)
        {
            this.vertices = vertices = PrepareVertices(vertices, out inputToVertexIndex);

            minCoord = float.MaxValue;

            foreach (var v in vertices)
            {
                minCoord = Math.Min(minCoord, Math.Min(v.X, v.Y));
            }
        }

        private static PointF[] PrepareVertices(PointF[] inputVertices, out int[] inputToVertexIndex)
        {
            var inputSegments = GetSegmentArray(inputVertices);
            var indexMap = new List<int>(inputSegments.Length);

            var outputSegments = new List<LineSegment>();

            foreach (var seg in inputSegments)
            {
                indexMap.Add(outputSegments.Count);
                outputSegments.AddRange(BreakIntersections(seg, inputSegments));
            }

            inputToVertexIndex = indexMap.ToArray();

            return GetVertices(outputSegments).ToArray();
        }

        public struct VertexInfo
        {
            public int OrigIndex;
            public PointF Point;
        }

        public IEnumerable<VertexInfo> GetVertexMap()
        {
            var count = vertices.Length;
            var nextOrigIndex = 0;
            var nextVertexIndex = inputToVertexIndex.Length > 0 ? inputToVertexIndex[0] : int.MaxValue;

            for (int i = 0; i < count; i++)
            {
                VertexInfo info;
                info.Point = vertices[i];

                if (i == nextVertexIndex)
                {
                    info.OrigIndex = nextOrigIndex;
                    nextOrigIndex++;

                    if (inputToVertexIndex.Length > nextOrigIndex)
                    {
                        nextVertexIndex = inputToVertexIndex[nextOrigIndex];
                    }

                    yield return info;
                }
                else
                {
                    yield return new VertexInfo { OrigIndex = -1, Point = vertices[i] };
                }
            }
        }

        public bool Contains(PointF candidate)
        {
             //var minCoord = vertices.Select(pt => pt.X)
            //    .Concat(vertices.Select(pt => pt.Y))
            //    .Aggregate(Math.Min);

            var edge = new PointF(minCoord - 1, minCoord - 1);
            var parity = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                int j = i + 1;

                if (j == vertices.Length)
                {
                    j = 0;
                }

                if (LineSegment.Intersects(edge, candidate, vertices[i], vertices[j]))
                {
                    var intersect = LineSegment.FindIntersection(edge, candidate, vertices[i], vertices[j]);

                    if (PointsEqualish(candidate, intersect))
                        return true;

                    if (PointsEqualish(intersect, vertices[i]))
                    {
                        if (Angle2(candidate, edge, vertices[j]) < 180)
                            parity++;
                    }
                    else if (PointsEqualish(intersect, vertices[j]))
                    {
                        if (Angle2(candidate, edge, vertices[i]) < 180)
                            parity++;
                    }
                    else
                    {
                        parity++;
                    }
                }
            }

            return (parity % 2) != 0;
        }

        private static float Angle(PointF a, PointF b)
        {
            return (float)(Math.Atan2(b.Y - a.Y, b.X - a.X) * 180 / Math.PI);
        }

        private static float Angle2(PointF a, PointF b, PointF c)
        {
            var a1 = Angle(a, b);
            var a2 = Angle(b, c);
            var a3 = a1 - a2;

            if (a3 < 0)
                a3 += 360;
            else if (a3 < 360)
                a3 -= 360;
            return a3;
        }

        private static IEnumerable<LineSegment> GetSegments(IList<PointF> vertices)
        {
            var count = vertices.Count;

            for (int i = 0; i < count; i++)
            {
                var j = i + 1;
                if (j == count)
                    j = 0;
                yield return new LineSegment(vertices[i], vertices[j]);
            }
        }

        private static LineSegment[] GetSegmentArray(PointF[] vertices)
        {
            var result = new LineSegment[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                int j = i + 1;
                if (j == vertices.Length)
                    j = 0;

                result[i] = new LineSegment(vertices[i], vertices[j]);
            }

            return result;
        }

        private static IEnumerable<PointF> GetVertices(IEnumerable<LineSegment> segments)
        {
            return segments.Select(seg => seg.Start);
        }

        private static bool PointsEqualish(PointF a, PointF b)
        {
            const float EPSILON = 0.0000001f;
            return Math.Abs(a.X - b.X) < EPSILON && Math.Abs(a.Y - b.Y) < EPSILON;
        }

        private static bool SegmentsEqualish(LineSegment a, LineSegment b)
        {
            return (PointsEqualish(a.Start, b.Start) && PointsEqualish(a.End, b.End)) ||
                (PointsEqualish(a.Start, b.End) && PointsEqualish(a.End, b.Start));
        }

        private IEnumerable<LineSegment> BreakIntersections(LineSegment interloper)
        {
            return BreakIntersections(interloper, GetSegments(vertices));
        }

        private sealed class PointFQueueNode : FastPriorityQueueNode
        {
            public PointFQueueNode(PointF value)
            {
                Value = value;
            }

            public PointF Value { get; }

            public static implicit operator PointFQueueNode(PointF value)
            {
                return new PointFQueueNode(value);
            }

            public static implicit operator PointF(PointFQueueNode node)
            {
                return node.Value;
            }
        }

        [ThreadStatic]
        private static FastPriorityQueue<PointFQueueNode> intersectionQueue;

        private static IEnumerable<LineSegment> BreakIntersections(LineSegment interloper, IEnumerable<LineSegment> clippers)
        {
            //SimplePriorityQueue<PointF, float> intersections;
            FastPriorityQueue<PointFQueueNode> intersections;

            if (intersectionQueue == null)
            {
                //intersectionQueue = intersections = new SimplePriorityQueue<PointF, float>();
                intersectionQueue = intersections = new FastPriorityQueue<PointFQueueNode>(64);
            }
            else
            {
                intersections = intersectionQueue;
                intersectionQueue.Clear();
            }

            foreach (var clipper in clippers)
            {
                if (SegmentsEqualish(clipper, interloper))
                    continue;

                if (clipper.Intersects(interloper))
                {
                    var intersect = clipper.FindIntersection(interloper);

                    if (PointsEqualish(intersect, interloper.Start) || PointsEqualish(intersect, interloper.End))
                        continue;

                    if (intersections.Count == intersections.MaxSize)
                        intersections.Resize(intersections.MaxSize * 2);

                    intersections.Enqueue(intersect, LineSegment.SqrDistance(interloper.Start, intersect));
                }
            }

            var position = interloper.Start;

            while (intersections.Count > 0)
            {
                var intersect = intersections.Dequeue();

                yield return new LineSegment(position, intersect);

                position = intersect;
            }

            yield return new LineSegment(position, interloper.End);
        }

        //public bool Contains(LineSegment candidate)
        //{
        //    return BreakIntersections(candidate).All(seg => Contains(seg.Midpoint));

        //    //foreach (var seg in BreakIntersections(candidate))
        //    //{
        //    //    Debug.Print("Contains: trying segment {0} - {1}", seg.Start, seg.End);
        //    //    if (!Contains(seg.Midpoint))
        //    //    {
        //    //        Debug.Print("Contains: failed due to midpoint {0}", seg.Midpoint);
        //    //        return false;
        //    //    }
        //    //}

        //    //Debug.Print("Contains: passed");
        //    //return true;
        //}

        public bool Contains(LineSegment candidate)
        {
            //foreach (var seg in GetSegments(vertices))
            for (int i = 0; i < vertices.Length; i++)
            {
                int j = i + 1;
                if (j == vertices.Length)
                    j = 0;

                if (candidate.Crosses(vertices[i], vertices[j]))
                    return false;
            }

            return Contains(candidate.Midpoint);
        }
    }
}
