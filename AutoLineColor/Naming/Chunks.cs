using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Debug = System.Diagnostics.Debug;

namespace AutoLineColor.Naming
{
    /// <summary>
    /// Part of a line name that can be reformatted for uniqueness and length.
    /// </summary>
    /// <remarks>
    /// <para>Use <see cref="System.Object.ToString"/> to format the chunk as-is.</para>
    /// <para>Use <see cref="GetVariants"/> to reformat the chunk for uniqueness, then
    /// <see cref="GetShortenings"/> to reformat it for length.</para>
    /// <para>Variants are meaningfully different names, while shortenings are
    /// different ways to write the same name. If we need to change the name
    /// "Green Street" because it doesn't fit, we can use the shortening
    /// "Green St". But if there's already a Green Street Line, we shouldn't
    /// create a separate Green St Line; we need to use a variant, maybe a different
    /// street name.</para>
    /// </remarks>
    interface IChunk
    {
        IEnumerable<IChunk> GetVariants();

        IEnumerable<string> GetShortenings();
    }

    static class IChunkExtensions
    {
        public static IEnumerable<IChunk> GetThisAndVariants(this IChunk chunk)
        {
            return Enumerable.Concat(Enumerable.Repeat(chunk, 1), chunk.GetVariants());
        }

        public static IEnumerable<string> GetThisAndShortenings(this IChunk chunk)
        {
            return Enumerable.Concat(Enumerable.Repeat(chunk.ToString(), 1), chunk.GetShortenings());
        }

        public static string VaryAndShortenToFit(this IChunk chunk)
        {
            return VaryAndShortenToFit(chunk, null, s => s.Length <= 32) ?? chunk.ToString();
        }

        public static string VaryAndShortenToFit(
            this IChunk chunk,
            Predicate<string> variantPredicate,
            Predicate<string> shorteningPredicate)
        {
            var logger = Console.Instance;

            foreach (var v in chunk.GetThisAndVariants())
            {
                if (variantPredicate != null)
                {
                    var vs = v.ToString();

                    logger.Message(string.Format("VaryAndShortenToFit: check variant '{0}'", vs));

                    if (!variantPredicate(vs))
                        continue;
                }

                foreach (var s in v.GetThisAndShortenings())
                {
                    logger.Message(string.Format("VaryAndShortenToFit: check shortening '{0}'", s));

                    if (shorteningPredicate == null || shorteningPredicate(s))
                        return s;
                }
            }

            logger.Message(string.Format("VaryAndShortenToFit: gave up"));

            return null;
        }
    }

    abstract class CompositeChunk : IChunk
    {
        protected readonly IChunk[] parts;

        protected CompositeChunk(IChunk[] parts)
        {
            Debug.Assert(parts != null && parts.Length > 0);
            this.parts = parts;
        }

        public abstract override string ToString();
        public abstract IEnumerable<string> GetShortenings();
        public abstract IEnumerable<IChunk> GetVariants();

        protected static IEnumerable<string> ShortenAllPartsEachIteration(IEnumerable<IChunk> parts)
        {
            return ShortenAllPartsEachIteration(parts, string.Concat);
        }

        protected static IEnumerable<T> ShortenAllPartsEachIteration<T>(IEnumerable<IChunk> parts, Func<string[], T> combiner)
        {
            // on each iteration, try shortening every part, until all parts are exhausted:
            //   Abc Wxyz 12 -> Ab Wxy 1 -> A Wx 1 -> A W 1

            var partsArray = parts.ToArray();
            var shortenedParts = partsArray.Select(p => p.ToString()).ToArray();
            var enumerators = partsArray.Select(p => p.GetShortenings().GetEnumerator()).ToArray();

            try
            {
                while (true)
                {
                    bool shortenedAnyPart = false;

                    for (int i = 0; i < enumerators.Length; i++)
                    {
                        var e = enumerators[i];

                        if (e.MoveNext())
                        {
                            shortenedParts[i] = e.Current;
                            shortenedAnyPart = true;
                        }
                    }

                    if (!shortenedAnyPart)
                        break;

                    yield return combiner(shortenedParts);
                }
            }
            finally
            {
                foreach (var e in enumerators)
                    e.Dispose();
            }
        }

        protected static IEnumerable<IChunk> VaryOnePartAtATime(IEnumerable<IChunk> parts, Func<IChunk[], IChunk> combiner)
        {
            // on each iteration, try varying a different part in sequence, until all parts are exhausted:
            //   Red Foo -> Green Foo -> Green Bar -> Blue Bar -> Yellow Bar
            // TODO: try every combination

            var variedParts = parts.ToArray();
            var enumerators = Array.ConvertAll(variedParts, p => p.GetVariants().GetEnumerator());

            if (variedParts.Length == 0)
            {
                Console.Instance.Error("VaryOnePartAtATime: nothing to vary!");
                yield break;
            }

            try
            {
                int lastVaried = -1;

                while (true)
                {
                    lastVaried = (lastVaried + 1) % enumerators.Length;

                    var sequence =
                        Enumerable.Range(lastVaried, enumerators.Length - lastVaried)
                        .Concat(Enumerable.Range(0, lastVaried));

                    bool variedAnyPart = false;

                    foreach (int i in sequence)
                    {
                        var e = enumerators[i];

                        if (e != null)
                        {
                            if (e.MoveNext())
                            {
                                variedParts[i] = e.Current;
                                variedAnyPart = true;
                                lastVaried = i;
                                break;
                            }
                            else
                            {
                                enumerators[i] = null;
                                e.Dispose();
                            }
                        }
                    }

                    if (!variedAnyPart)
                        break;

                    yield return combiner(variedParts);
                }
            }
            finally
            {
                foreach (var e in enumerators)
                    if (e != null)
                        e.Dispose();
            }
        }
    }

    class ConcatChunk : CompositeChunk
    {
        public ConcatChunk(params IChunk[] parts) : base(parts) { }

        public override string ToString()
        {
            return string.Concat(parts);
        }

        public override IEnumerable<string> GetShortenings()
        {
            return ShortenAllPartsEachIteration(parts);
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            return VaryOnePartAtATime(parts, parts => new ConcatChunk(parts));
        }
    }

    class AlternativesChunk : CompositeChunk
    {
        public AlternativesChunk(params IChunk[] parts) : base(parts) { }

        public override string ToString()
        {
            return parts[0].ToString();
        }

        public override IEnumerable<string> GetShortenings()
        {
            return parts[0].GetShortenings();
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            return parts.Skip(1);
        }
    }

    enum DecayMode
    {
        RespectEndpoints,
        RespectPriority,
    }

    class DecayingListChunk : CompositeChunk
    {
        protected readonly string delimiter;
        protected readonly DecayMode mode;

        public DecayingListChunk(DecayMode mode, params IChunk[] parts) : this(mode, parts, "/") { }

        public DecayingListChunk(DecayMode mode, IChunk[] parts, string delimiter) : base(parts)
        {
            this.delimiter = delimiter;
            this.mode = mode;
        }

        protected string JoinWithDelimiter(IEnumerable<object> chunks)
        {
            var sb = new StringBuilder();

            foreach (var c in chunks)
            {
                if (sb.Length > 0)
                    sb.Append(delimiter);

                sb.Append(c);
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            return JoinWithDelimiter(parts);
        }

        public override IEnumerable<string> GetShortenings()
        {
            return ShortenAllPartsEachIteration(parts, JoinWithDelimiter);
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            return GetVariantsInner().SelectMany(chunks => chunks);
        }

        private DecayingListChunk CloneWithOtherParts(IChunk[] otherParts)
        {
            return new DecayingListChunk(mode, otherParts, delimiter);
        }

        private IEnumerable<IEnumerable<IChunk>> GetVariantsInner()
        {
            // try varying the items first
            yield return VaryOnePartAtATime(parts, CloneWithOtherParts);

            switch (mode)
            {
                case DecayMode.RespectEndpoints:
                    // drop parts one at a time, leaving the middle/first/last parts until the end
                    yield return DecayPartsRespectingEndpoints(parts)
                        .SelectMany(c => Enumerable.Concat(
                            Enumerable.Repeat<IChunk>(CloneWithOtherParts(c), 1),
                            VaryOnePartAtATime(c, CloneWithOtherParts)));
                    break;

                case DecayMode.RespectPriority:
                    // drop parts one at a time, leaving the earliest parts until the end
                    yield return DecayPartsRespectingPriority(parts)
                        .SelectMany(c => Enumerable.Concat(
                            Enumerable.Repeat<IChunk>(CloneWithOtherParts(c), 1),
                            VaryOnePartAtATime(c, CloneWithOtherParts)));
                    break;
            }
        }

        private static IEnumerable<IChunk[]> DecayPartsRespectingEndpoints(IChunk[] parts)
        {
            var logger = Console.Instance;

            var temp = parts.ToArray();     // clone

            if (temp.Length == 0)
            {
                logger.Error("no parts to decay!");
                yield break;
            }

            int remaining = temp.Length;
            int mid = temp.Length / 2;
            int last = temp.Length - 1;
            int dropEarly = 1, dropLate = mid + 1;
            bool early = true;

            logger.Message(string.Format("decaying (endpoints): {0} items to start", remaining));

            while (true)
            {
                if (remaining <= 1)
                {
                    yield return temp.Where(c => c != null).ToArray();
                    yield break;
                }

                bool canDropEarly = dropEarly > 0 && dropEarly < mid;
                bool canDropLate = dropLate > mid && dropLate < last;

                if (canDropEarly && (early || !canDropLate))
                {
                    logger.Message(string.Format("early: dropping item {0}", dropEarly));
                    temp[dropEarly] = null;
                    dropEarly++;
                    early = false;
                }
                else if (canDropLate && (!early || !canDropEarly))
                {
                    logger.Message(string.Format("late: dropping item {0}", dropLate));
                    temp[dropLate] = null;
                    dropLate++;
                    early = true;
                }
                else if (mid != 0 && mid != last && temp[mid] != null)
                {
                    logger.Message(string.Format("mid: dropping item {0}", mid));
                    temp[mid] = null;
                }
                else if (temp[0] != null)
                {
                    logger.Message("first: dropping item 0");
                    temp[0] = null;
                }
                else
                {
                    // shouldn't get here
                    logger.Error("unexpected case!");
                    Debug.Assert(false, "dropped parts too fast");
                    break;
                }

                remaining--;

                yield return temp.Where(c => c != null).ToArray();
            }
        }

        private static IEnumerable<IChunk[]> DecayPartsRespectingPriority(IChunk[] parts)
        {
            var logger = Console.Instance;

            var temp = parts.ToArray();     // clone

            if (temp.Length == 0)
            {
                logger.Error("no parts to decay!");
                yield break;
            }

            int remaining = temp.Length;

            logger.Message(string.Format("decaying (priority): {0} items to start", remaining));

            while (true)
            {
                remaining--;

                if (remaining <= 1)
                {
                    yield return new[] { temp[0] };
                    yield break;
                }

                logger.Message(string.Format("decaying: returning first {0} items", remaining));

                var result = new IChunk[remaining];
                Array.Copy(temp, result, remaining);
                yield return result;
            }
        }
    }

    sealed class OptionalCosmeticChunk : IChunk
    {
        public static readonly OptionalCosmeticChunk Line = new OptionalCosmeticChunk(new StaticChunk(" Line"));

        private readonly IChunk part;

        public OptionalCosmeticChunk(IChunk part)
        {
            Debug.Assert(part != null);
            this.part = part;
        }

        public override string ToString()
        {
            return part.ToString();
        }

        public IEnumerable<string> GetShortenings()
        {
            yield return "";
        }

        public IEnumerable<IChunk> GetVariants()
        {
            yield break;
        }
    }

    abstract class StringChunk : IChunk
    {
        protected readonly string text;

        public StringChunk(string text)
        {
            this.text = text;
        }

        public override string ToString()
        {
            return text;
        }

        public abstract IEnumerable<string> GetShortenings();

        public abstract IEnumerable<IChunk> GetVariants();
    }

    sealed class StaticChunk : StringChunk
    {
        public static readonly StaticChunk Via = new StaticChunk(" via ");

        public StaticChunk(string text) : base(text) { }

        public override IEnumerable<string> GetShortenings()
        {
            yield break;
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            yield break;
        }
    }

    enum AbbreviationMode
    {
        Original,
        AbbreviateSuffix,
        StripSuffix,
        AutoShortenWords,
    }

    class DistrictNameChunk : StringChunk
    {
        protected AbbreviationMode mode;

        public DistrictNameChunk(string text) : this(text, AbbreviationMode.Original) { }

        public DistrictNameChunk(string text, AbbreviationMode mode) : base(text)
        {
            this.mode = mode;
        }

        public override string ToString()
        {
            switch (mode)
            {
                case AbbreviationMode.AbbreviateSuffix:
                    return text.AbbreviateDistrictSuffix();

                case AbbreviationMode.StripSuffix:
                    return text.StripDistrictSuffix();

                case AbbreviationMode.AutoShortenWords:
                    return text.StripDistrictSuffix().AutoShortenWords();

                default:
                    return text;
            }
        }

        public override IEnumerable<string> GetShortenings()
        {
            if (mode < AbbreviationMode.AbbreviateSuffix)
                yield return text.AbbreviateDistrictSuffix();

            if (mode < AbbreviationMode.StripSuffix)
                yield return text.StripDistrictSuffix();
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            yield break;
        }
    }

    class RoadNameChunk : StringChunk
    {
        protected AbbreviationMode mode;

        public RoadNameChunk(string text) : this(text, AbbreviationMode.Original) { }

        public RoadNameChunk(string text, AbbreviationMode mode) : base(text)
        {
            this.mode = mode;
        }

        public override string ToString()
        {
            switch (mode)
            {
                case AbbreviationMode.AbbreviateSuffix:
                    return text.AbbreviateRoadSuffix();

                case AbbreviationMode.StripSuffix:
                    return text.StripRoadSuffix();

                case AbbreviationMode.AutoShortenWords:
                    return text.StripRoadSuffix().AutoShortenWords();

                default:
                    return text;
            }
        }

        public override IEnumerable<string> GetShortenings()
        {
            if (mode < AbbreviationMode.AbbreviateSuffix)
                yield return text.AbbreviateRoadSuffix();

            if (mode < AbbreviationMode.StripSuffix)
                yield return text.StripRoadSuffix();
        }

        public override IEnumerable<IChunk> GetVariants()
        {
            yield break;
        }
    }
}
