using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;

namespace RoadSpeedAdjuster
{
    [FileLocation(nameof(RoadSpeedAdjuster))]
    [SettingsUIGroupOrder(kDisplayGroup)]
    [SettingsUIShowGroupName(kDisplayGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kDisplayGroup = "Display";

        public Setting(IMod mod) : base(mod)
        {
        }

        /// <summary>
        /// Unit preference for speed display in floating text overlays
        /// </summary>
        [SettingsUISection(kSection, kDisplayGroup)]
        public SpeedUnit SpeedUnitPreference { get; set; } = SpeedUnit.Auto;

        public override void SetDefaults()
        {
            SpeedUnitPreference = SpeedUnit.Auto;
        }

        /// <summary>
        /// Determines which unit to display based on user preference
        /// </summary>
        public bool ShouldShowMetric(bool isEUMap)
        {
            return SpeedUnitPreference switch
            {
                SpeedUnit.Auto => isEUMap,          // Auto: EU map = metric, US map = imperial
                SpeedUnit.Metric => true,            // Force metric
                SpeedUnit.Imperial => false,         // Force imperial
                _ => isEUMap
            };
        }

        public enum SpeedUnit
        {
            Auto,      // Detect from map theme (EU = km/h, US = mph)
            Metric,    // Always show km/h
            Imperial   // Always show mph
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod title and section
                { m_Setting.GetSettingsLocaleID(), "Road Speed Adjuster" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Settings" },

                // Display group
                { m_Setting.GetOptionGroupLocaleID(Setting.kDisplayGroup), "Display Options" },

                // Speed Unit Preference
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SpeedUnitPreference)), "Speed Unit" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SpeedUnitPreference)), "Choose how speed limits are displayed in floating text overlays. 'Auto' uses map theme (EU=km/h, US=mph)." },

                // Enum values
                { m_Setting.GetEnumValueLocaleID(Setting.SpeedUnit.Auto), "Auto (Detect from Map)" },
                { m_Setting.GetEnumValueLocaleID(Setting.SpeedUnit.Metric), "Metric (km/h)" },
                { m_Setting.GetEnumValueLocaleID(Setting.SpeedUnit.Imperial), "Imperial (mph)" },
            };
        }

        public void Unload()
        {
        }
    }
}
