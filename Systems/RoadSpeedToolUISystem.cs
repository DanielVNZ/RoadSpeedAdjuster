using Colossal.UI.Binding;
using Game.City;
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
        private ValueBindingHelper<bool> _showMetricBinding;
        private ValueBindingHelper<bool> _isTrackTypeBinding;
        private ValueBindingHelper<int> _unitModeBinding;
        
        private CityConfigurationSystem _cityConfigurationSystem;
        private Setting _settings;

        protected override string group => "RoadSpeedAdjuster.Systems.RoadSpeedToolUISystem";

        protected override bool displayForUnderConstruction => false;

        protected override void OnCreate()
        {
            base.OnCreate();
            //Mod.log.Info("RoadSpeedToolUI: OnCreate");

            m_InfoUISystem.AddMiddleSection(this);
            _selectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            _roadSpeedTool = World.GetOrCreateSystemManaged<RoadSpeedToolSystem>();
            _defaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            _cityConfigurationSystem = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            
            // Get mod settings
            _settings = Mod.m_Setting;

            _initialSpeedBinding = CreateBinding("INFOPANEL_ROAD_SPEED", 50f);
            _toolActiveBinding = CreateBinding("TOOL_ACTIVE", false);
            _selectionCounterBinding = CreateBinding("SELECTION_COUNTER", 0);
            _showMetricBinding = CreateBinding("SHOW_METRIC", true);
            _isTrackTypeBinding = CreateBinding("IS_TRACK_TYPE", false);
            _unitModeBinding = CreateBinding("UNIT_MODE", 0);
            
            CreateTrigger<float>("APPLY_SPEED", HandleApplySpeed);
            CreateTrigger("RESET_SPEED", HandleResetSpeed);
            CreateTrigger("TOGGLE_UNIT", HandleToggleUnit);
            
            //Mod.log.Info("RoadSpeedToolUI: Creating ACTIVATE_TOOL trigger with bool parameter...");
            CreateTrigger<bool>("ACTIVATE_TOOL", HandleActivateTool);
            //Mod.log.Info("RoadSpeedToolUI: ACTIVATE_TOOL trigger created successfully");

            // Initialize binding values immediately
            _initialSpeedBinding.Value = 50f;
            _toolActiveBinding.Value = false;
            _selectionCounterBinding.Value = 0;
            _showMetricBinding.Value = ShouldShowMetric();
            _unitModeBinding.Value = (int)(_settings?.SpeedUnitPreference ?? Setting.SpeedUnit.Auto);
            
            // Force an immediate update to ensure bindings are available to React
            // This prevents "update was not called before getValueUnsafe" errors
            RequestUpdate();

            //Mod.log.Info("RoadSpeedToolUI: Registered as info section");
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
            //Mod.log.Info($"=== OnRoadSelectedByTool START: {edges.Count} segments ===");
            //Mod.log.Info($"OnRoadSelectedByTool: Current binding value BEFORE change = {_initialSpeedBinding.Value}");
            
            _streetEdges.Clear();
            _streetEdges.AddRange(edges);
            
            if (edges.Count > 0)
            {
                _selectedEntity = edges[0];
                float speed = GetStreetSpeed(_selectedEntity);
                bool isTrack = IsTrackType(_selectedEntity);
                
                //Mod.log.Info($"OnRoadSelectedByTool: Got speed {speed} km/h for entity {_selectedEntity.Index}, isTrack={isTrack}");
                
                if (speed > 0)
                {
                    //Mod.log.Info($"OnRoadSelectedByTool: Setting binding from {_initialSpeedBinding.Value} to {speed}");
                    
                    // Set the binding values
                    _initialSpeedBinding.Value = speed;
                    _isTrackTypeBinding.Value = isTrack;
                    visible = true;
                    
                    // INCREMENT the selection counter to force React to detect a new selection
                    // even if the speed value is the same as before
                    _selectionCounterBinding.Value++;
                    
                    //Mod.log.Info($"OnRoadSelectedByTool: Binding is now {_initialSpeedBinding.Value}, visible={visible}, counter={_selectionCounterBinding.Value}, isTrack={isTrack}");
                    //Mod.log.Info($"OnRoadSelectedByTool: Calling RequestUpdate()...");
                    
                    // Call RequestUpdate to flush changes to React
                    RequestUpdate();
                    
                    //Mod.log.Info($"OnRoadSelectedByTool: RequestUpdate() complete");
                }
                else
                {
                    //Mod.log.Warn($"OnRoadSelectedByTool: Speed was {speed}, not showing panel");
                }
            }
            else
            {
                //Mod.log.Warn("OnRoadSelectedByTool: No edges provided!");
            }
            
            //Mod.log.Info($"=== OnRoadSelectedByTool END ===");
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
            
            // Update unit preference (checks map theme + user settings)
            bool showMetric = ShouldShowMetric();
            if (_showMetricBinding.Value != showMetric)
            {
                _showMetricBinding.Value = showMetric;
            }
            
            // If tool just became inactive, clear the panel
            if (!toolActive && toolStateChanged)
            {
                //Mod.log.Info("RoadSpeedToolUI: Tool deactivated, clearing panel and resetting counter");
                _initialSpeedBinding.Value = 50f;
                _selectionCounterBinding.Value = 0;  // Reset counter so hint shows on next activation
                visible = false;
                _streetEdges.Clear();
                _selectedEntity = Entity.Null;
                _lastCheckedEntity = Entity.Null;  // Also reset this to ensure clean state
                RequestUpdate();
                return;
            }
            
            // Only request update if tool state changed (not every frame!)
            if (toolStateChanged)
            {
                RequestUpdate();
            }
        }
        
        /// <summary>
        /// Determine if we should show metric (km/h) or imperial (mph) based on map theme and user settings
        /// </summary>
        private bool ShouldShowMetric()
        {
            try
            {
                // Get map theme
                bool isEUMap = true; // Default to European
                
                if (_cityConfigurationSystem?.defaultTheme != Entity.Null)
                {
                    var prefabSystem = World.GetOrCreateSystemManaged<Game.Prefabs.PrefabSystem>();
                    var theme = prefabSystem.GetPrefab<Game.Prefabs.ThemePrefab>(_cityConfigurationSystem.defaultTheme);
                    string themeName = theme?.name ?? "Unknown";
                    
                    // Check if North American theme
                    isEUMap = !themeName.Equals("North American", System.StringComparison.Ordinal);
                }
                
                // Use mod settings to determine final preference
                return _settings?.ShouldShowMetric(isEUMap) ?? isEUMap;
            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Failed to determine unit preference: {ex.Message}");
                return true; // Default to metric
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
            var carIgnore = CarLaneFlags.Unsafe | CarLaneFlags.SideConnection;

            foreach (var s in sub)
            {
                var lane = s.m_SubLane;

                // Check for CarLane (roads)
                if (EntityManager.HasComponent<CarLane>(lane))
                {
                    var car = EntityManager.GetComponentData<CarLane>(lane);
                    if ((car.m_Flags & carIgnore) == 0)
                    {
                        _speeds.Add(car.m_SpeedLimit * 1.8f);
                    }
                }
                // Check for TrackLane (trains, trams, subways)
                else if (EntityManager.HasComponent<Game.Net.TrackLane>(lane))
                {
                    var track = EntityManager.GetComponentData<Game.Net.TrackLane>(lane);
                    _speeds.Add(track.m_SpeedLimit * 1.8f);
                }
            }

            return _speeds.Count > 0 ? _speeds.Average() : -1f;
        }
        
        /// <summary>
        /// Check if the selected entity contains TrackLane (trains/trams/subways) rather than CarLane (roads)
        /// </summary>
        private bool IsTrackType(Entity edge)
        {
            if (!EntityManager.HasBuffer<SubLane>(edge))
                return false;

            var sub = EntityManager.GetBuffer<SubLane>(edge);
            
            foreach (var s in sub)
            {
                var lane = s.m_SubLane;
                
                // If we find any TrackLane, it's a track type
                if (EntityManager.HasComponent<Game.Net.TrackLane>(lane))
                {
                    return true;
                }
            }
            
            return false;
        }

        private void HandleApplySpeed(float newSpeed)
        {
            if (_selectedEntity == Entity.Null || _streetEdges.Count == 0)
            {
                //Mod.log.Warn("ApplySpeed with no valid selection/edges.");
                return;
            }

            newSpeed = math.clamp(newSpeed, 5f, 240f);

            //Mod.log.Info($"ApplySpeed: {newSpeed} km/h → {_streetEdges.Count} edges");

            // If the tool is active, use its selection
            if (_roadSpeedTool != null && _roadSpeedTool.IsActive && _roadSpeedTool.SelectedRoads.Count > 0)
            {
                // Use the tool's selection
                //Mod.log.Info("Using tool's selection for speed application");
                _roadSpeedTool.ApplySpeedToSelection(newSpeed);
            }
            else
            {
                // Fallback: Use our own edge list (for backward compatibility)
                //Mod.log.Info("Using UI system's edge list for speed application");
                ApplySpeedToEdges(newSpeed);
            }

            _initialSpeedBinding.Value = newSpeed;
            //Mod.log.Info("ApplySpeed complete.");
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
                        // Store in memory cache
                        SpeedDataManager.StoreOriginalSpeed(aggregate.Index, originalSpeed);
                        
                        // Store in persistent JSON file
                        PersistentSpeedStorage.StoreRoadSpeed(aggregate.Index, originalSpeed, newSpeed);
                    }
                }
                else
                {
                    // Road already modified, just update the current speed in persistent storage
                    float? existingDefault = PersistentSpeedStorage.GetDefaultSpeed(aggregate.Index);
                    if (existingDefault.HasValue)
                    {
                        PersistentSpeedStorage.StoreRoadSpeed(aggregate.Index, existingDefault.Value, newSpeed);
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
                
                // Track this road as having a custom speed (with the speed value)
                SpeedDataManager.AddCustomSpeedRoad(targetEdge.Index, newSpeed);
                
                var verifySpeed = EntityManager.GetComponentData<CustomSpeed>(targetEdge);
                //Mod.log.Info($"  Edge {targetEdge.Index}: Set speed = {verifySpeed.m_Speed} km/h ({verifySpeed.m_SpeedMPH:F0} mph)");

                SetCarLaneSpeedsImmediate(edge, speedGameUnits);
            }
        }

        private void HandleResetSpeed()
        {
            //Mod.log.Info("HandleResetSpeed: Called from UI");
            
            // If the tool is active and has a selection, use its reset method
            if (_roadSpeedTool != null && _roadSpeedTool.IsActive && _roadSpeedTool.SelectedRoads.Count > 0)
            {
                //Mod.log.Info("Using tool's reset method for active selection");
                _roadSpeedTool.ResetSpeedToOriginal();
                
                // Update UI to show default speed after reset
                if (_streetEdges.Count > 0)
                {
                    // Get the aggregate (road) entity - THIS is what we store original speeds against
                    var aggregateEntity = _selectedInfoUISystem.selectedEntity;
                    if (aggregateEntity != Entity.Null)
                    {
                        // Try memory cache first, then persistent storage
                        float? restoredSpeed = SpeedDataManager.GetOriginalSpeed(aggregateEntity.Index);
                        if (!restoredSpeed.HasValue)
                        {
                            restoredSpeed = PersistentSpeedStorage.GetDefaultSpeed(aggregateEntity.Index);
                        }
                        
                        if (restoredSpeed.HasValue)
                        {
                            _initialSpeedBinding.Value = restoredSpeed.Value;
                        }
                        else
                        {
                            // If no stored speed, get it from the actual lanes
                            float avgSpeed = GetAverageSpeed(_streetEdges[0]);
                            if (avgSpeed > 0)
                            {
                                _initialSpeedBinding.Value = avgSpeed;
                            }
                        }
                    }
                }
                return;
            }
            
            // Fallback to old logic for backward compatibility (when tool is not active)
            if (_selectedEntity == Entity.Null || _streetEdges.Count == 0)
            {
                //Mod.log.Warn("ResetSpeed with no valid selection/edges.");
                return;
            }

            //Mod.log.Info($"ResetSpeed: Restoring default speeds for {_streetEdges.Count} edges");

            // Get the aggregate (road) entity
            var aggregate = _selectedInfoUISystem.selectedEntity;
            
            if (aggregate == Entity.Null)
            {
                //Mod.log.Warn("ResetSpeed: No aggregate entity found");
                return;
            }

            // Try to get the stored original speed (memory cache first, then persistent storage)
            float? originalSpeed = SpeedDataManager.GetOriginalSpeed(aggregate.Index);
            if (!originalSpeed.HasValue)
            {
                originalSpeed = PersistentSpeedStorage.GetDefaultSpeed(aggregate.Index);
            }
            
            if (!originalSpeed.HasValue)
            {
                //Mod.log.Warn($"ResetSpeed: No original speed stored for road {aggregate.Index}");
                return;
            }

            float speedGameUnits = originalSpeed.Value / 1.8f;
            //Mod.log.Info($"ResetSpeed: Restoring road {aggregate.Index} to {originalSpeed.Value:F1} km/h");

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
                
                // Remove from custom speed tracking
                SpeedDataManager.RemoveCustomSpeedRoad(targetEdge.Index);
            }

            // Remove from both memory cache and persistent storage
            SpeedDataManager.RemoveOriginalSpeed(aggregate.Index);
            PersistentSpeedStorage.RemoveRoad(aggregate.Index);

            // Update UI to show the restored default speed
            _initialSpeedBinding.Value = originalSpeed.Value;

            //Mod.log.Info("ResetSpeed complete.");
        }

        private void RestoreSpeed(Entity edge, float speedGameUnits)
        {
            if (!EntityManager.HasBuffer<SubLane>(edge))
                return;

            var subLanes = EntityManager.GetBuffer<SubLane>(edge);

            for (int i = 0; i < subLanes.Length; i++)
            {
                var laneEntity = subLanes[i].m_SubLane;

                // Restore speed for CarLane (roads)
                if (EntityManager.HasComponent<CarLane>(laneEntity))
                {
                    var carLane = EntityManager.GetComponentData<CarLane>(laneEntity);
                    carLane.m_SpeedLimit = speedGameUnits;
                    EntityManager.SetComponentData(laneEntity, carLane);
                }
                // Restore speed for TrackLane (trains, trams, subways)
                else if (EntityManager.HasComponent<Game.Net.TrackLane>(laneEntity))
                {
                    var trackLane = EntityManager.GetComponentData<Game.Net.TrackLane>(laneEntity);
                    trackLane.m_SpeedLimit = speedGameUnits;
                    EntityManager.SetComponentData(laneEntity, trackLane);
                }
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

                // Set speed for CarLane (roads)
                if (EntityManager.HasComponent<CarLane>(laneEntity))
                {
                    var carLane = EntityManager.GetComponentData<CarLane>(laneEntity);
                    var originalFlags = carLane.m_Flags;

                    carLane.m_SpeedLimit = speedGameUnits;
                    carLane.m_Flags = originalFlags;

                    EntityManager.SetComponentData(laneEntity, carLane);
                }
                // Set speed for TrackLane (trains, trams, subways)
                else if (EntityManager.HasComponent<Game.Net.TrackLane>(laneEntity))
                {
                    var trackLane = EntityManager.GetComponentData<Game.Net.TrackLane>(laneEntity);
                    trackLane.m_SpeedLimit = speedGameUnits;
                    EntityManager.SetComponentData(laneEntity, trackLane);
                }
            }
        }

        private void HandleToggleUnit()
        {
            if (_settings == null)
                return;
            
            // Cycle through: Auto -> Metric -> Imperial -> Auto
            _settings.SpeedUnitPreference = _settings.SpeedUnitPreference switch
            {
                Setting.SpeedUnit.Auto => Setting.SpeedUnit.Metric,
                Setting.SpeedUnit.Metric => Setting.SpeedUnit.Imperial,
                Setting.SpeedUnit.Imperial => Setting.SpeedUnit.Auto,
                _ => Setting.SpeedUnit.Auto
            };
            
            // Apply the setting changes
            _settings.ApplyAndSave();
            
            // Update the bindings immediately
            _showMetricBinding.Value = ShouldShowMetric();
            _unitModeBinding.Value = (int)_settings.SpeedUnitPreference;
            RequestUpdate();
            
            //Mod.log.Info($"Unit preference toggled to: {_settings.SpeedUnitPreference}");
        }
        
        private void HandleActivateTool(bool enable)
        {
            //Mod.log.Info($"HandleActivateTool: CALLED with enable={enable}");
            
            if (_toolSystem == null)
            {
                //Mod.log.Error("HandleActivateTool: _toolSystem is NULL!");
                return;
            }
            
            if (_roadSpeedTool == null)
            {
                //Mod.log.Error("HandleActivateTool: _roadSpeedTool is NULL!");
                return;
            }
            
            //Mod.log.Info($"HandleActivateTool: Current tool: {_toolSystem.activeTool?.toolID ?? "none"}");
            
            // If disabling, reset the counter BEFORE toggling the tool
            if (!enable)
            {
                _selectionCounterBinding.Value = 0;
                _initialSpeedBinding.Value = 50f;
                visible = false;
                _streetEdges.Clear();
                _selectedEntity = Entity.Null;
                _lastCheckedEntity = Entity.Null;
            }
            
            // Use the ToggleTool method which properly handles tool activation
            _roadSpeedTool.ToggleTool(enable);
            
            // Update binding to reflect actual state
            _toolActiveBinding.Value = _roadSpeedTool.IsActive;
            
            // Force an update to ensure React gets the new values immediately
            RequestUpdate();
            
            //Mod.log.Info($"HandleActivateTool: Complete. Tool active: {_roadSpeedTool.IsActive}, Active tool: {_toolSystem.activeTool?.toolID ?? "none"}");
        }
    }
}
