using System;
using System.Threading;
using AutoLineColor.Coloring;
using AutoLineColor.Naming;
using ColossalFramework;
using ICities;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace AutoLineColor
{
    [UsedImplicitly]
    public class ColorMonitor : ThreadingExtensionBase
    {
        private static readonly Console Logger = Console.Instance;
        private static DateTimeOffset _nextUpdateTime = DateTimeOffset.Now;

        private bool _initialized;
        private IColorStrategy _colorStrategy;
        private INamingStrategy _namingStrategy;
        private List<Color32> _usedColors;
        private Configuration _config;

        public override void OnCreated(IThreading threading)
        {
            Logger.Message("===============================");
            Logger.Message("Initializing auto color monitor");
            Logger.Message("Initializing colors");
            RandomColor.Initialize();
            CategorisedColor.Initialize();
            GenericNames.Initialize();

            Logger.Message("Loading current config");
            _config = Configuration.Instance;
            _colorStrategy = SetColorStrategy(_config.ColorStrategy);
            _namingStrategy = SetNamingStrategy(_config.NamingStrategy);
            _usedColors = new List<Color32>();

            Logger.Message("Found color strategy of " + _config.ColorStrategy);
            Logger.Message("Found naming strategy of " + _config.NamingStrategy);

            _initialized = true;

            Logger.Message("done creating");
            base.OnCreated(threading);
        }

        private static INamingStrategy SetNamingStrategy(NamingStrategy namingStrategy)
        {
            Logger.Message($"Naming Strategy: {namingStrategy}");
            switch (namingStrategy)
            {
                case NamingStrategy.None:
                    return new NoNamingStrategy();
                case NamingStrategy.Districts:
                    return new DistrictNamingStrategy();
                case NamingStrategy.London:
                    return new LondonNamingStrategy();
                case NamingStrategy.Roads:
                    return new RoadNamingStrategy();
                default:
                    Logger.Error("unknown naming strategy");
                    return new NoNamingStrategy();
            }
        }

        private static IColorStrategy SetColorStrategy(ColorStrategy colorStrategy)
        {
            Logger.Message($"Color Strategy: {colorStrategy}");
            switch (colorStrategy)
            {
                case ColorStrategy.RandomHue:
                    return new RandomHueStrategy();
                case ColorStrategy.RandomColor:
                    return new RandomColorStrategy();
                case ColorStrategy.CategorisedColor:
                    return new CategorisedColorStrategy();
                default:
                    Logger.Error("unknown color strategy");
                    return new RandomHueStrategy();
            }
        }

        // TODO: make this whole thing a coroutine?
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            TransportManager theTransportManager;
            SimulationManager theSimulationManager;
            TransportLine[] lines;

            try
            {
                //Digest changes
                if (_config.UndigestedChanges)
                {
                    Logger.Message("Applying undigested changes");
                    _colorStrategy = SetColorStrategy(_config.ColorStrategy);
                    _namingStrategy = SetNamingStrategy(_config.NamingStrategy);
                    _config.UndigestedChanges = false;
                }

                if (_initialized == false)
                    return;

                // try and limit how often we are scanning for lines. this ain't that important
                if (_nextUpdateTime >= DateTimeOffset.Now)
                    return;

                if (!Singleton<TransportManager>.exists || !Singleton<SimulationManager>.exists)
                    return;

                theTransportManager = Singleton<TransportManager>.instance;
                theSimulationManager = Singleton<SimulationManager>.instance;
                lines = theTransportManager.m_lines.m_buffer;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
                return;
            }

            var locked = false;

            try
            {
                _nextUpdateTime = DateTimeOffset.Now.AddSeconds(Constants.UpdateIntervalSeconds);

                locked = Monitor.TryEnter(lines, SimulationManager.SYNCHRONIZE_TIMEOUT);

                if (!locked)
                    return;

                _usedColors = lines.Where(l => l.IsActive()).Select(l => l.m_color).ToList();

                for (ushort i = 0; i < lines.Length - 1; i++)
                {
                    var transportLine = lines[i];
                    //logger.Message(string.Format("Starting on line {0}", i));

                    if (transportLine.m_flags == TransportLine.Flags.None)
                        continue;

                    if (!transportLine.Complete)
                        continue;

                    // only worry about fully created lines
                    if (!transportLine.IsActive() || transportLine.HasCustomName() || !transportLine.m_color.IsDefaultColor())
                        continue;

                    Logger.Message($"Working on line {i}");

                    var lineName = theTransportManager.GetLineName(i);
                    var newName = _namingStrategy.GetName(transportLine);

                    if (!transportLine.HasCustomColor() || transportLine.m_color.IsDefaultColor())
                    {
                        var color = _colorStrategy.GetColor(transportLine, _usedColors);

                        Logger.Message($"Changing line {i} color from {theTransportManager.GetLineColor(i)} to {color}");

                        theSimulationManager.AddAction(theTransportManager.SetLineColor(i, color));
                    }

                    if (string.IsNullOrEmpty(newName) || transportLine.HasCustomName())
                        continue;

                    Logger.Message($"Changing line {i} name from '{lineName}' to '{newName}'");

                    theSimulationManager.AddAction(theTransportManager.SetLineName(i, newName));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(lines);
                }
            }
        }
    }

    internal static class LineExtensions
    {
        private static readonly Color32 BlackColor = new Color32(0, 0, 0, 0);
        private static readonly Color32 DefaultBusColor = new Color32(44, 142, 191, 255);
        private static readonly Color32 DefaultMetroColor = new Color32(0, 184, 0, 255);
        private static readonly Color32 DefaultTrainColor = new Color32(219, 86, 0, 255);

        public static bool IsDefaultColor(this Color32 color)
        {
            return color.IsColorEqual(BlackColor) ||
                   color.IsColorEqual(DefaultBusColor) ||
                   color.IsColorEqual(DefaultMetroColor) ||
                   color.IsColorEqual(DefaultTrainColor);
        }

        public static bool IsColorEqual(this Color32 color1, Color32 color2)
        {
            return color1.r == color2.r && color1.g == color2.g && color1.b == color2.b && color1.a == color2.a;
        }

        public static bool IsActive(this TransportLine transportLine)
        {
            if ((transportLine.m_flags & (TransportLine.Flags.Created | TransportLine.Flags.Hidden)) == 0)
                return false;

            // stations are marked with this flag
            return (transportLine.m_flags & TransportLine.Flags.Temporary) != TransportLine.Flags.Temporary;
        }

        public static bool HasCustomColor(this TransportLine transportLine)
        {
            return (transportLine.m_flags & TransportLine.Flags.CustomColor) == TransportLine.Flags.CustomColor;
        }

        public static bool HasCustomName(this TransportLine transportLine)
        {
            return (transportLine.m_flags & TransportLine.Flags.CustomName) == TransportLine.Flags.CustomName;
        }
    }

    internal static class EnumerableExtensions
    {
        public static IEnumerable<TResult> Scan<TSource, TAccumulate, TResult>(
            this IEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> updater,
            Func<TSource, TAccumulate, TResult> resultSelector)
        {
            var accumulator = seed;

            foreach (var item in source)
            {
                accumulator = updater(accumulator, item);
                yield return resultSelector(item, accumulator);
            }
        }
    }
}