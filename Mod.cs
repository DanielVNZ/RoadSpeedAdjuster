using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Simulation;
using RoadSpeedAdjuster.Systems;

namespace RoadSpeedAdjuster
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(RoadSpeedAdjuster)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);

        public const string Id = "RoadSpeedAdjuster";
        public const string ModName = "RoadSpeedAdjuster";

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info($"{ModName} OnLoad");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            updateSystem.UpdateAt<RoadSpeedAdjusterInfoPanelSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<RoadSpeedAdjusterUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<RoadSpeedApplySystem>(SystemUpdatePhase.GameSimulation);



            log.Info("Registered RoadSpeedAdjusterInfoPanelSystem in UIUpdate phase.");
        }

        public void OnDispose()
        {
            log.Info($"{ModName} OnDispose");
        }
    }
}
