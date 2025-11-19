using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using RoadSpeedAdjuster.Systems;

namespace RoadSpeedAdjuster
{
    public class Mod : IMod
    {
        public static readonly string Id = "RoadSpeedAdjuster";

        public static ILog log = LogManager.GetLogger($"{nameof(RoadSpeedAdjuster)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(true);

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info("OnLoad()");

            // --- Apply speeds when Updated component is present ---
            updateSystem.UpdateAt<RoadSpeedApplySystem>(SystemUpdatePhase.ModificationEnd);

            // --- UI always LAST ---
            updateSystem.UpdateAt<RoadSpeedToolUISystem>(SystemUpdatePhase.UIUpdate);

            log.Info("Systems registered.");
        }

        public void OnDispose()
        {
            log.Info("OnDispose()");
        }
    }
}
