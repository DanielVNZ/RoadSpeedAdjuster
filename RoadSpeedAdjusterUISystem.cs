using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using RoadSpeedAdjuster.Components;
using RoadSpeedAdjuster.Utils;
using Unity.Collections;
using Unity.Entities;

// Alias to avoid lane ambiguity
using Net = Game.Net;

namespace RoadSpeedAdjuster.Systems
{
    public partial class RoadSpeedAdjusterUISystem : UISystemBase
    {
        private PrefixedLogger _log;
        private SelectedInfoUISystem _info;

        private TriggerBinding<float> _trigger;   // UI → C#
        private ValueBinding<float> _binding;     // C# → UI

        private Entity _lastSelected = Entity.Null;

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = new PrefixedLogger("RoadSpeedUI");
            _info = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();

            // Slider → C#
            _trigger = new TriggerBinding<float>(
                "RoadSpeedAdjuster",
                "INFOPANEL_SPEED_CHANGED",
                OnSliderChanged
            );
            AddBinding(_trigger);

            // C# → Slider
            _binding = new ValueBinding<float>(
                "RoadSpeedAdjuster",
                "INFOPANEL_SPEED_VALUE",
                50f
            );
            AddBinding(_binding);

            _log.Info("Bindings registered.");
        }

        // ---------------------------------------------------------------
        // UI → C#
        // ---------------------------------------------------------------
        private void OnSliderChanged(float v)
        {
            Entity road = ResolveRoad(_info.selectedEntity);

            if (!EntityManager.Exists(road))
            {
                _log.Warn($"Nothing selected (cache={_info.selectedEntity.Index}/{_info.selectedEntity.Version})");
                return;
            }

            _log.Info($"Setting speed {v} on road {road.Index}/{road.Version}");
            SetRoadSpeed(road, v);
        }

        // ---------------------------------------------------------------
        // Update loop — detect new road selection
        // ---------------------------------------------------------------
        protected override void OnUpdate()
        {
            base.OnUpdate();

            Entity road = ResolveRoad(_info.selectedEntity);

            if (road == _lastSelected)
                return; // no change

            _lastSelected = road;

            if (road == Entity.Null || !EntityManager.Exists(road))
            {
                _log.Warn("Resolved road entity does not exist.");
                return;
            }

            _log.Info($"🟦 Road resolved: raw={_info.selectedEntity.Index}/{_info.selectedEntity.Version}, road={road.Index}/{road.Version}");

            float speed = GetRoadSpeed(road);

            if (speed > 0)
            {
                _log.Info($"📊 Applying slider update = {speed}");
                _binding.Update(speed);
            }
            else
            {
                _log.Warn("⚠ No speed found for selected road.");
            }
        }

        // ---------------------------------------------------------------
        // Resolve a clicked UI selection to the road SEGMENT entity
        // ---------------------------------------------------------------
        private Entity ResolveRoad(Entity selected)
        {
            var mgr = EntityManager;

            if (selected == Entity.Null)
                return Entity.Null;

            // Case 1: Direct road segment
            if (mgr.HasBuffer<Net.SubLane>(selected))
                return selected;

            // Case 2: Lane → segment
            if (mgr.TryGetComponent(selected, out Owner laneOwner))
            {
                Entity seg = laneOwner.m_Owner;
                if (mgr.Exists(seg) && mgr.HasBuffer<Net.SubLane>(seg))
                    return seg;
            }

            // Case 3: UI metadata → walk upward to segment
            return ResolveOwnerChainToSegment(_info.selectedEntity);
        }

        private Entity ResolveOwnerChainToSegment(Entity start)
        {
            var mgr = EntityManager;
            Entity cur = start;

            while (cur != Entity.Null)
            {
                if (mgr.HasBuffer<Net.SubLane>(cur))
                    return cur;

                if (!mgr.TryGetComponent(cur, out Owner o))
                    break;

                cur = o.m_Owner;
            }

            return Entity.Null;
        }

        // ---------------------------------------------------------------
        // READ speed from either RoadComposition (instance) or RoadData (prefab)
        // ---------------------------------------------------------------
        private float GetRoadSpeed(Entity road)
        {
            var mgr = EntityManager;

            // INSTANCE OVERRIDE
            if (mgr.TryGetComponent(road, out RoadComposition comp))
            {
                _log.Info($"   Instance speed = {comp.m_SpeedLimit}");
                return comp.m_SpeedLimit;
            }

            // PREFAB DEFAULT
            if (mgr.TryGetComponent(road, out PrefabRef pr)
                && mgr.TryGetComponent(pr.m_Prefab, out RoadData rd))
            {
                _log.Info($"   Prefab speed = {rd.m_SpeedLimit}");
                return rd.m_SpeedLimit;
            }

            return -1;
        }

        // ---------------------------------------------------------------
        // WRITE speed to RoadComposition on the road ENTITY
        // ---------------------------------------------------------------
        private void SetRoadSpeed(Entity road, float speed)
        {
            var mgr = EntityManager;

            // ensure the component exists
            if (!mgr.HasComponent<RoadComposition>(road))
                mgr.AddComponent<RoadComposition>(road);

            var comp = mgr.GetComponentData<RoadComposition>(road);
            comp.m_SpeedLimit = speed;
            mgr.SetComponentData(road, comp);

            _log.Info($"   ✔ Wrote instance speed = {speed}");
        }
    }
}
