using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using RoadSpeedAdjuster.Systems;
using UnityEngine;


namespace RoadSpeedAdjuster
{
    public class Mod : IMod
    {
        public static readonly string Id = "RoadSpeedAdjuster";

        public static ILog log = LogManager.GetLogger($"{nameof(RoadSpeedAdjuster)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(true);
        
        public static Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info("OnLoad()");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));


            AssetDatabase.global.LoadSettings(nameof(RoadSpeedAdjuster), m_Setting, new Setting(this));

            // Register save/load system (must be early)
            updateSystem.UpdateAt<RoadSpeedSaveDataSystem>(SystemUpdatePhase.Deserialize);

            // Register the road speed tool system
            updateSystem.UpdateAt<RoadSpeedToolSystem>(SystemUpdatePhase.ToolUpdate);

            // Apply speeds at ModificationEnd
            updateSystem.UpdateAt<RoadSpeedApplySystem>(SystemUpdatePhase.ModificationEnd);
            
            // Clear custom speeds system (handles clearing from settings)
            updateSystem.UpdateAt<ClearCustomSpeedsSystem>(SystemUpdatePhase.ModificationEnd);

            // InfoSection systems need to be registered at UIUpdate phase
            updateSystem.UpdateAt<RoadSpeedToolUISystem>(SystemUpdatePhase.UIUpdate);
            
            // Register speed limit render system (direct mesh rendering, no overlay buffer)
            updateSystem.UpdateAt<SpeedLimitRenderSystem>(SystemUpdatePhase.Rendering);

            log.Info("Systems registered.");
        }

        public void OnDispose()
        {
            log.Info("OnDispose()");
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}
