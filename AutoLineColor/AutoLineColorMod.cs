using ICities;
using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace AutoLineColor
{
    [UsedImplicitly(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.WithMembers)]
    public class AutoLineColorMod : IUserMod
    {
        private static Console _logger = Console.Instance;

        private Configuration _config;
        public string Name => Constants.ModName;
        public string Description => Constants.Description;

        public void OnSettingsUI(UIHelperBase helper)
        {
            _config = Configuration.Instance;
            _config.FlushStagedChanges(); //make sure no prior changes are still around

            // Section: Colors & Names

            var group = helper.AddGroup("Colors & Names");

            var colorStrategies = Enum.GetNames(typeof(ColorStrategy));
            var namingStrategies = Enum.GetNames(typeof(NamingStrategy));

            group.AddDropdown("Color Strategy", colorStrategies, (int)_config.ColorStrategy,
                _config.ColorStrategyChange);
            group.AddDropdown("Naming Strategy", namingStrategies, (int)_config.NamingStrategy,
                _config.NamingStrategyChange);

            // Section: Advanced Settings

            helper.AddSpace(5);
            group = helper.AddGroup("Advanced Settings");

            Debug.Assert(_config.MaxDiffColorPickAttempt != null, "Config.MaxDiffColorPickAttempt != null");
            group.AddSlider("Max Different Color Picks", 1f, 20f, 1f, (float)_config.MaxDiffColorPickAttempt,
                _config.MaxDiffColorPickChange);

            Debug.Assert(_config.MinColorDiffPercentage != null, "Config.MinColorDiffPercentage != null");
            group.AddSlider("MinColorDifference", 1f, 100f, 5f, (float)_config.MinColorDiffPercentage,
                _config.MinColorDiffChange);

            group.AddCheckbox("Debug", _logger.Debug, _logger.SetDebug);

            // Bottom: Save

            helper.AddSpace(5);
            helper.AddButton("Save", _config.Save);
        }
    }
}
