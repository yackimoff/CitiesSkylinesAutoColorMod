using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace AutoLineColor
{
    [Serializable]
    public class Configuration
    {
        public ColorStrategy ColorStrategy { get; private set; }
        public NamingStrategy NamingStrategy { get; private set; }
        public int? MinColorDiffPercentage { get; private set; }
        public int? MaxDiffColorPickAttempt { get; private set; }

        [XmlIgnore]
        public volatile bool UndigestedChanges;

        //Staged changes. These are not applied until 'Save' is clicked
        private ColorStrategy? StagedColorStrategy { get; set; }
        private NamingStrategy? StagedNamingStrategy { get; set; }
        private int? StagedMinColorDiffPercentage { get; set; }
        private int? StagedMaxDiffColorPickAttempt { get; set; }


        private const int DefaultMaxDiffColorPickAttempt = 10;
        private const int DefaultMinColorDiffPercent = 5;

        private static Configuration _instance;
        private static readonly Console _logger = Console.Instance;

        private static Configuration LoadConfig()
        {
            var isDirty = false;
            Configuration config;
            try
            {
                var serializer = new XmlSerializer(typeof(Configuration));
                const string fullConfigPath = Constants.ConfigFileName;

                if (File.Exists(fullConfigPath) == false)
                {
                    _logger.Message("No config file. Building default and writing it to " + fullConfigPath);
                    config = GetDefaultConfig();
                    isDirty = true;
                }
                else
                {
                    _logger.Message("Config file exists. Using it");
                    using (var reader = XmlReader.Create(fullConfigPath)) {
                        config = (Configuration)serializer.Deserialize(reader);
                    }

                    //check new configuration properties
                    if (!config.MaxDiffColorPickAttempt.HasValue ||
                        !config.MinColorDiffPercentage.HasValue) {

                        config.UndigestedChanges = false;
                        config.MaxDiffColorPickAttempt = config.MaxDiffColorPickAttempt ??
                                                         DefaultMaxDiffColorPickAttempt;
                        config.MinColorDiffPercentage = config.MinColorDiffPercentage ??
                                                        DefaultMinColorDiffPercent;

                        isDirty = true;
                    }
                }
            }
            catch (Exception ex)
            {
                //Don't save changes if it failed for some reason
                _logger.Error("Error reading configuration settings - " + ex);
                config = GetDefaultConfig();
            }

            if (isDirty)
            {
                config.Save();
            }

            return config;
        }

        public void ColorStrategyChange(int Strategy)
        {
            this.StagedColorStrategy = (ColorStrategy)Strategy;
        }

        public void NamingStrategyChange(int Strategy)
        {
            this.StagedNamingStrategy = (NamingStrategy)Strategy;
        }

        public void MinColorDiffChange(float MinDiff)
        {
            this.StagedMinColorDiffPercentage = (int)MinDiff;
        }

        public void MaxDiffColorPickChange(float MaxColorPicks)
        {
            this.StagedMaxDiffColorPickAttempt = (int)MaxColorPicks;
        }

        public void FlushStagedChanges()
        {
            StagedColorStrategy = null;
            StagedNamingStrategy = null;
            StagedMaxDiffColorPickAttempt = null;
            StagedMinColorDiffPercentage = null;
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(Configuration));

            _logger.Message("Saving changes to config file");

            //If any changes have occured, apply them, otherwise keep the current value
            this.ColorStrategy = this.StagedColorStrategy ?? this.ColorStrategy;
            this.NamingStrategy = this.StagedNamingStrategy ?? this.NamingStrategy;
            this.MaxDiffColorPickAttempt = this.StagedMaxDiffColorPickAttempt ?? this.MaxDiffColorPickAttempt;
            this.MinColorDiffPercentage = this.StagedMinColorDiffPercentage ?? this.MinColorDiffPercentage;

            //clear changes and log
            if (this.StagedColorStrategy.HasValue)
            {
                _logger.Message($"ColorStrategy changed to {this.StagedColorStrategy.Value}");
            }

            if (this.StagedNamingStrategy.HasValue)
            {
                _logger.Message($"NamingStrategy changed to {this.StagedNamingStrategy.Value}");
            }

            if (this.StagedMaxDiffColorPickAttempt.HasValue)
            {
                _logger.Message($"MaxDiffColorPickAttempt changed to {this.StagedMaxDiffColorPickAttempt.Value}");
            }

            if (this.StagedMinColorDiffPercentage.HasValue)
            {
                _logger.Message($"MinColorDiffPercentage changed to {this.StagedMinColorDiffPercentage.Value}");
            }

            FlushStagedChanges();

            //How we let the ColorMonitor thread know to update the strategies
            _logger.Message("Marking undigested changes");
            this.UndigestedChanges = true;

            //Save to disk
            using (var writer = XmlWriter.Create(Constants.ConfigFileName))
            {
                serializer.Serialize(writer, this);
            }
        }

        public static string GetModFileName(string fileName)
        {
            return fileName;
        }

        private static Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                ColorStrategy = ColorStrategy.RandomColor,
                NamingStrategy = NamingStrategy.Districts,
                MaxDiffColorPickAttempt = DefaultMaxDiffColorPickAttempt,
                MinColorDiffPercentage = DefaultMinColorDiffPercent,
                UndigestedChanges = false
            };
        }

        public static Configuration Instance => _instance ?? (_instance = LoadConfig());
    }

    public enum ColorStrategy
    {
        RandomHue,
        RandomColor,
        CategorisedColor
    }

    public enum NamingStrategy
    {
        None,
        Districts,
        London,
        Roads
    }
}
