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
using Temp = Game.Tools.Temp;

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

        private ValueBindingHelper<float> _initialSpeedBinding;

        protected override string group => "RoadSpeedAdjuster.Systems.RoadSpeedToolUISystem";

        protected override bool displayForUnderConstruction => false;

        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("RoadSpeedToolUI: OnCreate");

            m_InfoUISystem.AddMiddleSection(this);
            _selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();

            _initialSpeedBinding = CreateBinding("INFOPANEL_ROAD_SPEED", 50f);
            CreateTrigger<float>("APPLY_SPEED", HandleApplySpeed);

            _initialSpeedBinding.Value = 50f;

            Mod.log.Info("RoadSpeedToolUI: Registered as info section");
        }

        protected override void Reset()
        {
            _selectedEntity = Entity.Null;
            _lastCheckedEntity = Entity.Null;
            _streetEdges.Clear();
            _initialSpeedBinding.Value = 50f;
            visible = false;
        }

        protected override void OnProcess()
        {
            // Empty - using OnUpdate instead
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var currentSelection = _selectedInfoUISystem.selectedEntity;

            if (currentSelection == _lastCheckedEntity)
            {
                if (_initialSpeedBinding != null)
                {
                    _initialSpeedBinding.Value = _initialSpeedBinding.Value;
                }
                RequestUpdate();
                return;
            }

            _lastCheckedEntity = currentSelection;

            if (currentSelection == Entity.Null)
            {
                _initialSpeedBinding.Value = 50f;
                visible = false;
                RequestUpdate();
                return;
            }

            if (!EntityManager.HasComponent<Aggregate>(currentSelection))
            {
                visible = false;
                RequestUpdate();
                return;
            }

            FindStreetRoads(currentSelection);

            if (_streetEdges.Count == 0)
            {
                visible = false;
                RequestUpdate();
                return;
            }

            _selectedEntity = _streetEdges[0];
            float speed = GetStreetSpeed(_selectedEntity);

            if (speed > 0)
            {
                _initialSpeedBinding.Value = speed;
                visible = true;
            }
            else
            {
                visible = false;
            }

            RequestUpdate();
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
        }

        private float GetStreetSpeed(Entity edge)
        {
            Entity baseEdge = edge;

            if (EntityManager.HasComponent<Temp>(edge))
                baseEdge = EntityManager.GetComponentData<Temp>(edge).m_Original;

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

                _speeds.Add(car.m_SpeedLimit * 1.8f);
            }

            return _speeds.Count > 0 ? _speeds.Average() : -1f;
        }

        private void HandleApplySpeed(float newSpeed)
        {
            if (_selectedEntity == Entity.Null || _streetEdges.Count == 0)
            {
                Mod.log.Warn("ApplySpeed with no valid selection/edges.");
                return;
            }

            newSpeed = math.clamp(newSpeed, 5f, 140f);
            float speedGameUnits = newSpeed / 1.8f;

            Mod.log.Info($"ApplySpeed: {newSpeed} km/h → {_streetEdges.Count} edges");

            foreach (var edge in _streetEdges)
            {
                Entity targetEdge = edge;
                if (EntityManager.HasComponent<Temp>(edge))
                {
                    var temp = EntityManager.GetComponentData<Temp>(edge);
                    targetEdge = temp.m_Original;
                }

                if (!EntityManager.HasComponent<CustomSpeed>(targetEdge))
                    EntityManager.AddComponent<CustomSpeed>(targetEdge);

                EntityManager.SetComponentData(targetEdge, new CustomSpeed { m_Speed = newSpeed });

                SetCarLaneSpeedsImmediate(edge, speedGameUnits);
            }

            _initialSpeedBinding.Value = newSpeed;
            Mod.log.Info("ApplySpeed complete.");
        }

        private void SetCarLaneSpeedsImmediate(Entity edge, float speedGameUnits)
        {
            if (!EntityManager.HasBuffer<SubLane>(edge))
                return;

            var subLanes = EntityManager.GetBuffer<SubLane>(edge);

            for (int i = 0; i < subLanes.Length; i++)
            {
                var laneEntity = subLanes[i].m_SubLane;

                if (!EntityManager.HasComponent<CarLane>(laneEntity))
                    continue;

                var carLane = EntityManager.GetComponentData<CarLane>(laneEntity);
                var originalFlags = carLane.m_Flags;

                carLane.m_DefaultSpeedLimit = speedGameUnits;
                carLane.m_SpeedLimit = speedGameUnits;
                carLane.m_Flags = originalFlags;

                EntityManager.SetComponentData(laneEntity, carLane);
            }
        }
    }
}
