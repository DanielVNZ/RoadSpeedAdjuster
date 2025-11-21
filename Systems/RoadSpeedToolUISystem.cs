using Colossal.UI.Binding;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using RoadSpeedAdjuster.Components;
using RoadSpeedAdjuster.Data;
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
        private ToolSystem _toolSystem;
        private RoadSpeedToolSystem _roadSpeedTool;
        private DefaultToolSystem _defaultToolSystem;
        private SelectedInfoUISystem _selectedInfoUISystem;
        private Entity _selectedEntity;
        private Entity _lastCheckedEntity;
        private readonly List<Entity> _streetEdges = new();
        private readonly List<float> _speeds = new();

        private ValueBindingHelper<float> _initialSpeedBinding;
        private ValueBindingHelper<bool> _toolActiveBinding;
        private ValueBindingHelper<int> _selectionCounterBinding;

        protected override string group => "RoadSpeedAdjuster.Systems.RoadSpeedToolUISystem";

        protected override bool displayForUnderConstruction => false;

        protected override void OnCreate()
        {
            base.OnCreate();
            Mod.log.Info("RoadSpeedToolUI: OnCreate");

            m_InfoUISystem.AddMiddleSection(this);
            _selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            _roadSpeedTool = World.GetOrCreateSystemManaged<RoadSpeedToolSystem>();
            _defaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();

            _initialSpeedBinding = CreateBinding("INFOPANEL_ROAD_SPEED", 50f);
            _toolActiveBinding = CreateBinding("TOOL_ACTIVE", false);
            _selectionCounterBinding = CreateBinding("SELECTION_COUNTER", 0);
            
            CreateTrigger<float>("APPLY_SPEED", HandleApplySpeed);
            CreateTrigger("RESET_SPEED", HandleResetSpeed);
            
            Mod.log.Info("RoadSpeedToolUI: Creating ACTIVATE_TOOL trigger with bool parameter...");
            CreateTrigger<bool>("ACTIVATE_TOOL", HandleActivateTool);
            Mod.log.Info("RoadSpeedToolUI: ACTIVATE_TOOL trigger created successfully");

            // Initialize binding values immediately
            _initialSpeedBinding.Value = 50f;
            _toolActiveBinding.Value = false;
            _selectionCounterBinding.Value = 0;
            
            // Force an immediate update to ensure bindings are available to React
            // This prevents "update was not called before getValueUnsafe" errors
            RequestUpdate();

            Mod.log.Info("RoadSpeedToolUI: Registered as info section");
        }

        protected override void Reset()
        {
            _selectedEntity = Entity.Null;
            _lastCheckedEntity = Entity.Null;
            _streetEdges.Clear();
            _initialSpeedBinding.Value = 50f;
            _toolActiveBinding.Value = false;
            _selectionCounterBinding.Value = 0;
            visible = false;
        }

        /// <summary>
        /// Called by RoadSpeedToolSystem when a road is selected via the tool
        /// </summary>
        public void OnRoadSelectedByTool(Entity aggregate, IReadOnlyList<Entity> edges)
        {
            Mod.log.Info($"=== OnRoadSelectedByTool START: {edges.Count} segments ===");
            Mod.log.Info($"OnRoadSelectedByTool: Current binding value BEFORE change = {_initialSpeedBinding.Value}");
            
            _streetEdges.Clear();
            _streetEdges.AddRange(edges);
            
            if (edges.Count > 0)
            {
                _selectedEntity = edges[0];
                float speed = GetStreetSpeed(_selectedEntity);
                
                Mod.log.Info($"OnRoadSelectedByTool: Got speed {speed} km/h for entity {_selectedEntity.Index}");
                
                if (speed > 0)
                {
                    Mod.log.Info($"OnRoadSelectedByTool: Setting binding from {_initialSpeedBinding.Value} to {speed}");
                    
                    // Set the binding value
                    _initialSpeedBinding.Value = speed;
                    visible = true;
                    
                    // INCREMENT the selection counter to force React to detect a new selection
                    // even if the speed value is the same as before
                    _selectionCounterBinding.Value++;
                    
                    Mod.log.Info($"OnRoadSelectedByTool: Binding is now {_initialSpeedBinding.Value}, visible={visible}, counter={_selectionCounterBinding.Value}");
                    Mod.log.Info($"OnRoadSelectedByTool: Calling RequestUpdate()...");
                    
                    // Call RequestUpdate to flush changes to React
                    RequestUpdate();
                    
                    Mod.log.Info($"OnRoadSelectedByTool: RequestUpdate() complete");
                }
                else
                {
                    Mod.log.Warn($"OnRoadSelectedByTool: Speed was {speed}, not showing panel");
                }
            }
            else
            {
                Mod.log.Warn("OnRoadSelectedByTool: No edges provided!");
            }
            
            Mod.log.Info($"=== OnRoadSelectedByTool END ===");
        }

        protected override void OnProcess()
        {
            // Empty - using OnUpdate instead
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Update tool active state
            bool toolActive = _roadSpeedTool != null && _roadSpeedTool.IsActive;
            bool toolStateChanged = _toolActiveBinding.Value != toolActive;
            
            _toolActiveBinding.Value = toolActive;
            
            // If tool just became inactive, clear the panel
            if (!toolActive && toolStateChanged)
            {
                Mod.log.Info("RoadSpeedToolUI: Tool deactivated, clearing panel");
                _initialSpeedBinding.Value = 50f;
                _selectionCounterBinding.Value = 0;  // Reset counter so panel doesn't appear on next activation
                visible = false;
                _streetEdges.Clear();
                _selectedEntity = Entity.Null;
                RequestUpdate();
                return;
            }
            
            // Only request update if tool state changed (not every frame!)
            if (toolStateChanged)
            {
                RequestUpdate();
            }
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

            Mod.log.Info($"ApplySpeed: {newSpeed} km/h → {_streetEdges.Count} edges");

            // If the tool is active, use its selection
            if (_roadSpeedTool != null && _roadSpeedTool.IsActive && _roadSpeedTool.SelectedRoads.Count > 0)
            {
                // Use the tool's selection
                Mod.log.Info("Using tool's selection for speed application");
                _roadSpeedTool.ApplySpeedToSelection(newSpeed);
            }
            else
            {
                // Fallback: Use our own edge list (for backward compatibility)
                Mod.log.Info("Using UI system's edge list for speed application");
                ApplySpeedToEdges(newSpeed);
            }

            _initialSpeedBinding.Value = newSpeed;
            Mod.log.Info("ApplySpeed complete.");
        }

        private void ApplySpeedToEdges(float newSpeed)
        {
            float speedGameUnits = newSpeed / 1.8f;

            // Get the aggregate (road) entity from the selected info system
            var aggregate = _selectedInfoUISystem.selectedEntity;
            
            // Store original speed ONCE per road (aggregate), not per edge segment
            if (aggregate != Entity.Null)
            {
                // Check if ANY edge has CustomSpeed already (meaning we've modified this road before)
                bool hasCustomSpeed = false;
                foreach (var edge in _streetEdges)
                {
                    Entity targetEdge = edge;
                    if (EntityManager.HasComponent<Temp>(edge))
                    {
                        var temp = EntityManager.GetComponentData<Temp>(edge);
                        targetEdge = temp.m_Original;
                    }
                    
                    if (EntityManager.HasComponent<CustomSpeed>(targetEdge))
                    {
                        hasCustomSpeed = true;
                        break;
                    }
                }
                
                // Only store original speed if this road hasn't been modified yet
                if (!hasCustomSpeed)
                {
                    float originalSpeed = GetAverageSpeed(_selectedEntity);
                    if (originalSpeed > 0)
                    {
                        SpeedDataManager.StoreOriginalSpeed(aggregate.Index, originalSpeed);
                    }
                }
            }

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

                var customSpeed = new CustomSpeed(newSpeed);
                EntityManager.SetComponentData(targetEdge, customSpeed);
                
                var verifySpeed = EntityManager.GetComponentData<CustomSpeed>(targetEdge);
                Mod.log.Info($"  Edge {targetEdge.Index}: Set speed = {verifySpeed.m_Speed} km/h ({verifySpeed.m_SpeedMPH:F0} mph)");

                SetCarLaneSpeedsImmediate(edge, speedGameUnits);
            }
        }

        private void HandleResetSpeed()
        {
            if (_selectedEntity == Entity.Null || _streetEdges.Count == 0)
            {
                Mod.log.Warn("ResetSpeed with no valid selection/edges.");
                return;
            }

            Mod.log.Info($"ResetSpeed: Restoring default speeds for {_streetEdges.Count} edges");

            // Get the aggregate (road) entity
            var aggregate = _selectedInfoUISystem.selectedEntity;
            
            if (aggregate == Entity.Null)
            {
                Mod.log.Warn("ResetSpeed: No aggregate entity found");
                return;
            }

            // Get the stored original speed for this road (aggregate)
            float? originalSpeed = SpeedDataManager.GetOriginalSpeed(aggregate.Index);
            
            if (!originalSpeed.HasValue)
            {
                Mod.log.Warn($"ResetSpeed: No original speed stored for road {aggregate.Index}");
                return;
            }

            float speedGameUnits = originalSpeed.Value / 1.8f;
            Mod.log.Info($"ResetSpeed: Restoring road {aggregate.Index} to {originalSpeed.Value:F1} km/h");

            foreach (var edge in _streetEdges)
            {
                Entity targetEdge = edge;
                if (EntityManager.HasComponent<Temp>(edge))
                {
                    var temp = EntityManager.GetComponentData<Temp>(edge);
                    targetEdge = temp.m_Original;
                }

                // Restore speed to all lanes on this edge
                RestoreSpeed(edge, speedGameUnits);

                // Remove CustomSpeed component
                if (EntityManager.HasComponent<CustomSpeed>(targetEdge))
                {
                    EntityManager.RemoveComponent<CustomSpeed>(targetEdge);
                }
            }

            // Remove from stored data (only once per road, not per edge)
            SpeedDataManager.RemoveOriginalSpeed(aggregate.Index);

            // Update UI to show the restored default speed
            _initialSpeedBinding.Value = originalSpeed.Value;

            Mod.log.Info("ResetSpeed complete.");
        }

        private void RestoreSpeed(Entity edge, float speedGameUnits)
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
                
                // Restore to the specified speed
                carLane.m_SpeedLimit = speedGameUnits;

                EntityManager.SetComponentData(laneEntity, carLane);
            }
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

                // Only modify m_SpeedLimit - preserve m_DefaultSpeedLimit for Reset functionality
                carLane.m_SpeedLimit = speedGameUnits;
                carLane.m_Flags = originalFlags;

                EntityManager.SetComponentData(laneEntity, carLane);
            }
        }

        private void HandleActivateTool(bool enable)
        {
            Mod.log.Info($"HandleActivateTool: CALLED with enable={enable}");
            
            if (_toolSystem == null)
            {
                Mod.log.Error("HandleActivateTool: _toolSystem is NULL!");
                return;
            }
            
            if (_roadSpeedTool == null)
            {
                Mod.log.Error("HandleActivateTool: _roadSpeedTool is NULL!");
                return;
            }
            
            Mod.log.Info($"HandleActivateTool: Current tool: {_toolSystem.activeTool?.toolID ?? "none"}");
            
            // Use the ToggleTool method which properly handles tool activation
            _roadSpeedTool.ToggleTool(enable);
            
            // Update binding to reflect actual state
            _toolActiveBinding.Value = _roadSpeedTool.IsActive;
            
            Mod.log.Info($"HandleActivateTool: Complete. Tool active: {_roadSpeedTool.IsActive}, Active tool: {_toolSystem.activeTool?.toolID ?? "none"}");
        }
    }
}
