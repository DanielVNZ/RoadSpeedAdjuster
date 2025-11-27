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
    [SettingsUITabOrder(kMainTab, kAboutTab)]
    [SettingsUIGroupOrder(kDisplayGroup, kActionsGroup, kAboutGroup, kCreditsGroup)]
    [SettingsUIShowGroupName(kDisplayGroup, kActionsGroup, kAboutGroup, kCreditsGroup)]
    public class Setting : ModSetting
    {
        public const string kMainTab = "Main";
        public const string kAboutTab = "About";
        public const string kDisplayGroup = "Display";
        public const string kActionsGroup = "Actions";
        public const string kAboutGroup = "About";
        public const string kCreditsGroup = "Special Thanks";

        public Setting(IMod mod) : base(mod)
        {
        }

        /// <summary>
        /// Unit preference for speed display in floating text overlays
        /// </summary>
        [SettingsUISection(kMainTab, kDisplayGroup)]
        public SpeedUnit SpeedUnitPreference { get; set; } = SpeedUnit.Auto;

        /// <summary>
        /// Button to clear all custom speeds from roads in the current city
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kMainTab, kActionsGroup)]
        public bool ClearAllCustomSpeeds
        {
            set
            {
                if (value)
                {
                    ClearAllCustomSpeedsAction();
                }
            }
        }

        // About Tab
        /// <summary>
        /// Version information
        /// </summary>
        [SettingsUISection(kAboutTab, kAboutGroup)]
        public string Version => "Version 1.0.2";

        /// <summary>
        /// Creator information
        /// </summary>
        [SettingsUISection(kAboutTab, kAboutGroup)]
        public string Creator => "Created by DanielVNZ";

        /// <summary>
        /// Special thanks section
        /// </summary>
        [SettingsUISection(kAboutTab, kCreditsGroup)]
        public string SpecialThanks => "Thanks to Luca, Konsi, Aberro (creator of the old Speed Limit Mod), TDW (Road Builder code helped me alot) and everyone else who tested this mod and helped me along the way!";

        public override void SetDefaults()
        {
            SpeedUnitPreference = SpeedUnit.Auto;
        }

        private void ClearAllCustomSpeedsAction()
        {
            try
            {
                // Get the World and ClearCustomSpeedsSystem
                var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                if (world == null)
                {
                    Mod.log.Error("Cannot clear speeds: World is null");
                    return;
                }

                var clearSystem = world.GetExistingSystemManaged<Systems.ClearCustomSpeedsSystem>();
                if (clearSystem == null)
                {
                    Mod.log.Error("Cannot clear speeds: ClearCustomSpeedsSystem not found");
                    return;
                }

                // Request the system to clear all custom speeds
                clearSystem.RequestClearAllCustomSpeeds();
                
                Mod.log.Info("Requested clear all custom speeds");
            }
            catch (System.Exception ex)
            {
                Mod.log.Error($"Failed to request clear speeds: {ex.Message}\n{ex.StackTrace}");
            }
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
                // Mod title and tabs
                { m_Setting.GetSettingsLocaleID(), "Road Speed Adjuster" },
                { m_Setting.GetOptionTabLocaleID(Setting.kMainTab), "Settings" },
                { m_Setting.GetOptionTabLocaleID(Setting.kAboutTab), "About" },

                // Main tab groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kDisplayGroup), "Display Options" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kActionsGroup), "Actions" },
                
                // About tab groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kAboutGroup), "About" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kCreditsGroup), "Credits" },

                // Speed Unit Preference
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SpeedUnitPreference)), "Speed Unit" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SpeedUnitPreference)), "Choose how speed limits are displayed in floating text overlays. 'Auto' uses map theme (EU=km/h, US=mph)." },

                // Enum values
                { m_Setting.GetEnumValueLocaleID(Setting.SpeedUnit.Auto), "Auto (Detect from Map)" },
                { m_Setting.GetEnumValueLocaleID(Setting.SpeedUnit.Metric), "Metric (km/h)" },
                { m_Setting.GetEnumValueLocaleID(Setting.SpeedUnit.Imperial), "Imperial (mph)" },
                
                // Clear All Custom Speeds button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ClearAllCustomSpeeds)), "Clear All Custom Speeds" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ClearAllCustomSpeeds)), "Remove all custom speed limits from roads, tracks, and waterways in the current city and restore them to their default values. This action cannot be undone." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ClearAllCustomSpeeds)), "Are you sure you want to clear all custom speed limits? This will affect all modified roads, tracks, and waterways in your city and cannot be undone." },
                
                // About tab
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Version)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Version)), "" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Creator)), "Creator" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Creator)), "" },
                
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.SpecialThanks)), "" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.SpecialThanks)), "" },
            };
        }

        public void Unload()
        {
        }
    }
}
