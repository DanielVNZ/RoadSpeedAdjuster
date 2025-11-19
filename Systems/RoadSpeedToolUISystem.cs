using Colossal.UI.Binding;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using RoadSpeedAdjuster.Components;
using RoadSpeedAdjuster.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using CarLane = Game.Net.CarLane;
using CarLaneFlags = Game.Net.CarLaneFlags;
using SubLane = Game.Net.SubLane;
using Updated = Game.Common.Updated;
using Owner = Game.Common.Owner;

namespace RoadSpeedAdjuster.Systems
{
    [UpdateInGroup(typeof(SelectedInfoUISystem))]
    public partial class RoadSpeedToolUISystem : ExtendedInfoSectionBase
    {
        private SelectedInfoUISystem _selectedInfoUISystem;

        private Entity _selectedEntity;
        private Entity _lastCheckedEntity;

        private readonly List<Entity> _streetEdges = new();
        private readonly List<float> _speeds = new();

        // Binds TO JS: visible + initial value
        private ValueBindingHelper<float> _initialSpeedBinding;
        private ValueBindingHelper<bool> _visibleBinding;

        protected override string group => "RoadSpeedAdjuster.Systems.RoadSpeedToolUISystem";

        protected override bool displayForUnderConstruction => false;

        protected override void OnCreate()
        {
            base.OnCreate();

            Mod.log.Info("RoadSpeedToolUI: OnCreate");

            // Register this section with the info panel
            m_InfoUISystem.AddMiddleSection(this);

            _selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();

            // Initial value shown on the slider
            _initialSpeedBinding = CreateBinding("INFOPANEL_ROAD_SPEED", 50f);

            // Visibility
            _visibleBinding = CreateBinding("INFOPANEL_VISIBLE", false);

            // Apply button trigger
            CreateTrigger<float>("APPLY_SPEED", HandleApplySpeed);

            // Initialize with safe values
            _initialSpeedBinding.Value = 50f;
            _visibleBinding.Value = false;

            Mod.log.Info("RoadSpeedToolUI: Bindings + SelectedInfoUISystem registered.");
        }

        protected override void Reset()
        {
            // Reset state when selection changes
            _selectedEntity = Entity.Null;
            _lastCheckedEntity = Entity.Null;
            _streetEdges.Clear();
            _initialSpeedBinding.Value = 50f;
            _visibleBinding.Value = false;
            visible = false;
        }

        protected override void OnProcess()
        {
            var currentSelection = _selectedInfoUISystem.selectedEntity;

            if (currentSelection == _lastCheckedEntity)
                return;

            _lastCheckedEntity = currentSelection;

            if (currentSelection == Entity.Null)
            {
                _initialSpeedBinding.Value = 50f;
                _visibleBinding.Value = false;
                visible = false;
                return;
            }

            if (!EntityManager.HasComponent<Aggregate>(currentSelection))
            {
                _initialSpeedBinding.Value = 50f;
                _visibleBinding.Value = false;
                visible = false;
                return;
            }

            FindStreetRoads(currentSelection);

            if (_streetEdges.Count == 0)
            {
                _initialSpeedBinding.Value = 50f;
                _visibleBinding.Value = false;
                visible = false;
                return;
            }

            _selectedEntity = _streetEdges[0];

            float speed = GetStreetSpeed(_selectedEntity);

            if (speed > 0)
            {
                _initialSpeedBinding.Value = speed;
                _visibleBinding.Value = true;
                visible = true;
            }
            else 
            {
                _visibleBinding.Value = false;
                visible = false;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // TEMPORARY DEBUG: Force visible
            visible = true;

            // Re-push current values to UI every frame (stability requirement)
            if (_initialSpeedBinding != null)
            {
                _initialSpeedBinding.Value = _initialSpeedBinding.Value;
            }

            if (_visibleBinding != null)
            {
                _visibleBinding.Value = _visibleBinding.Value;
            }

            RequestUpdate();
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            // Write any additional properties if needed
            // For now, we're using bindings so this can be empty
        }

        private float GetStreetSpeed(Entity edge)
        {
            Entity baseEdge = edge;

            if (EntityManager.HasComponent<Temp>(edge))
                baseEdge = EntityManager.GetComponentData<Temp>(edge).m_Original;

            // Check if custom speed exists
            if (EntityManager.HasComponent<CustomSpeed>(baseEdge))
                return EntityManager.GetComponentData<CustomSpeed>(baseEdge).m_Speed;

            return GetAverageSpeed(edge);
        }

        private void FindStreetRoads(Entity aggregate)
        {
            _streetEdges.Clear();

            var query = SystemAPI.QueryBuilder()
                .WithAll<Edge, Aggregated>()
                .Build();

            var edges = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var e in edges)
            {
                var a = EntityManager.GetComponentData<Aggregated>(e);
                if (a.m_Aggregate == aggregate)
                    _streetEdges.Add(e);
            }

            edges.Dispose();
        }

        private float GetAverageSpeed(Entity edge)
        {
            _speeds.Clear();

            if (!EntityManager.HasBuffer<SubLane>(edge))
                return -1;

            var sub = EntityManager.GetBuffer<SubLane>(edge);
            var ignore = CarLaneFlags.Unsafe | CarLaneFlags.SideConnection;

            foreach (var s in sub)
            {
                var lane = s.m_SubLane;

                if (!EntityManager.HasComponent<CarLane>(lane))
                    continue;

                var car = EntityManager.GetComponentData<CarLane>(lane);

                if ((car.m_Flags & ignore) != 0)
                    continue;

                // CarLane.m_SpeedLimit is in game units (2x m/s), convert to km/h
                // 1 game unit = 0.5 m/s, so multiply by 1.8 to get km/h
                _speeds.Add(car.m_SpeedLimit * 1.8f);
            }

            return _speeds.Count > 0 ? _speeds.Average() : -1f;
        }

        // APPLY BUTTON PUSHED
        private void HandleApplySpeed(float newSpeed)
        {
            if (_selectedEntity == Entity.Null || _streetEdges.Count == 0)
            {
                Mod.log.Warn("ApplySpeed with no valid selection/edges.");
                return;
            }

            newSpeed = math.clamp(newSpeed, 5f, 300f);
            // Game uses 2x m/s units, so divide by 1.8 instead of 3.6
            float speedGameUnits = newSpeed / 1.8f;

            Mod.log.Info($"ApplySpeed({newSpeed} km/h = {speedGameUnits} game units) → {_streetEdges.Count} edges");

            foreach (var edge in _streetEdges)
            {
                // Handle temporary entities (being placed/edited)
                Entity targetEdge = edge;
                if (EntityManager.HasComponent<Temp>(edge))
                {
                    var temp = EntityManager.GetComponentData<Temp>(edge);
                    targetEdge = temp.m_Original;
                }

                // Store CustomSpeed permanently (in km/h)
                if (!EntityManager.HasComponent<CustomSpeed>(targetEdge))
                    EntityManager.AddComponent<CustomSpeed>(targetEdge);

                EntityManager.SetComponentData(targetEdge, new CustomSpeed { m_Speed = newSpeed });

                // *** IMMEDIATE FIX: Set CarLane speeds NOW ***
                SetCarLaneSpeedsImmediate(edge, speedGameUnits);

                // NOTE: We DON'T mark edges as Updated because that triggers intersection rebuilds
                // The ApplySystem will restore speeds when the game naturally updates roads
                // EntityManager.AddComponent<Updated>(targetEdge);
            }

            // Reflect new speed immediately in UI binding
            _initialSpeedBinding.Value = newSpeed;

            Mod.log.Info("ApplySpeed complete - speeds applied, flags preserved.");
        }

        /// <summary>
        /// IMMEDIATELY set the CarLane m_SpeedLimit and m_DefaultSpeedLimit for all sublanes
        /// </summary>
        private void SetCarLaneSpeedsImmediate(Entity edge, float speedGameUnits)
        {
            if (!EntityManager.HasBuffer<SubLane>(edge))
                return;

            var subLanes = EntityManager.GetBuffer<SubLane>(edge);
            int modifiedCount = 0;

            for (int i = 0; i < subLanes.Length; i++)
            {
                var subLane = subLanes[i];
                var laneEntity = subLane.m_SubLane;

                if (!EntityManager.HasComponent<CarLane>(laneEntity))
                    continue;

                var carLane = EntityManager.GetComponentData<CarLane>(laneEntity);

                // Store the original flags EXPLICITLY
                var originalFlags = carLane.m_Flags;

                // ONLY modify speed fields
                carLane.m_DefaultSpeedLimit = speedGameUnits;
                carLane.m_SpeedLimit = speedGameUnits;
                
                // Ensure flags are preserved (should already be, but being explicit)
                carLane.m_Flags = originalFlags;
                
                EntityManager.SetComponentData(laneEntity, carLane);
                modifiedCount++;
            }

            Mod.log.Info($"  → Modified {modifiedCount} lanes (speed: {speedGameUnits} game units, all flags preserved)");
        }
    }
}
