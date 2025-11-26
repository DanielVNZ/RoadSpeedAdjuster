using Colossal.Entities;
using Game;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Game.UI;
using RoadSpeedAdjuster.Components;
using System.Collections.Generic;
using RoadSpeedAdjuster.Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;
using CarLane = Game.Net.CarLane;
using SubLane = Game.Net.SubLane;

namespace RoadSpeedAdjuster.Systems
{
    /// <summary>
    /// Custom tool for selecting road segments to adjust their speed limits.
    /// Click individual segment or click-and-drag across multiple segments.
    /// Selection is finalized when mouse button is released.
    /// </summary>
    public partial class RoadSpeedToolSystem : ToolBaseSystem
    {
        public const string kToolID = "Road Speed Tool";

        private RoadSpeedToolUISystem m_UISystem;
        
        private EntityQuery m_RoadQuery;
        private readonly List<Entity> m_SelectedRoads = new();
        private readonly HashSet<Entity> m_TempSelection = new();
        
        private bool m_IsDragging = false;
        private Entity m_HoverEntity = Entity.Null;  // Track what we're currently hovering over
        
        public override string toolID => kToolID;

        public bool IsActive => m_ToolSystem?.activeTool == this;

        public IReadOnlyList<Entity> SelectedRoads => m_SelectedRoads;

        /// <summary>
        /// Toggle the tool on/off - called by UI button
        /// </summary>
        public void ToggleTool(bool enable)
        {
            //Mod.log.Info($"ToggleTool called: enable={enable}, current tool={m_ToolSystem?.activeTool?.toolID ?? "null"}");
            
            // If trying to enable and we're not active, activate
            if (enable && m_ToolSystem.activeTool != this)
            {
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = this;
                //Mod.log.Info("RoadSpeedToolSystem: Tool activated via ToggleTool");
            }
            // If trying to enable but we're already active, treat it as a toggle-off
            else if (enable && m_ToolSystem.activeTool == this)
            {
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = m_DefaultToolSystem;
                //Mod.log.Info("RoadSpeedToolSystem: Tool toggled off (was already active)");
            }
            // If trying to disable and we're active, deactivate
            else if (!enable && m_ToolSystem.activeTool == this)
            {
                m_ToolSystem.selected = Entity.Null;
                m_ToolSystem.activeTool = m_DefaultToolSystem;
                //Mod.log.Info("RoadSpeedToolSystem: Tool deactivated via ToggleTool");
            }
        }

        [Preserve]
        protected override void OnCreate()
        {
            //Mod.log.Info("RoadSpeedToolSystem: OnCreate - start");
            
            // Set Enabled to false per documentation
            Enabled = false;
            
            m_UISystem = World.GetOrCreateSystemManaged<RoadSpeedToolUISystem>();

            // Query for road edges
            m_RoadQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Edge>(),
                    ComponentType.ReadOnly<Curve>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });

            // Call base.OnCreate() - this initializes applyAction for us!
            base.OnCreate();
            
            //Mod.log.Info("RoadSpeedToolSystem: OnCreate complete - applyAction ready");
        }

        [Preserve]
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_SelectedRoads.Clear();
            m_TempSelection.Clear();
            m_IsDragging = false;
            
            // Enable applyAction - exactly like Better Bulldozer mod
            applyAction.shouldBeEnabled = true;
            //Mod.log.Info($"Set applyAction.shouldBeEnabled=true (enabled={applyAction.enabled})");
            
            //Mod.log.Info("Tool started running");
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            
            // Log WHY the tool is stopping
            var stackTrace = new System.Diagnostics.StackTrace(true);
            //Mod.log.Info($"OnStopRunning called! Stack trace:\n{stackTrace.ToString()}");
            
            // Disable applyAction - exactly like Better Bulldozer mod
            applyAction.shouldBeEnabled = false;
            //Mod.log.Info("Set applyAction.shouldBeEnabled=false");
            
            // Remove all highlights when tool deactivates
            RemoveAllHighlights();
            
            // Clear selection state - this allows hover to work again on next tool activation
            m_SelectedRoads.Clear();
            m_TempSelection.Clear();
            m_IsDragging = false;
            m_HoverEntity = Entity.Null;
            
            //Mod.log.Info("Tool stopped running");
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            
            // Set up raycast to detect roads, train tracks, tram tracks, subway tracks, and waterways
            m_ToolRaycastSystem.typeMask = TypeMask.Net;
            m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack | Layer.Waterway;
            m_ToolRaycastSystem.raycastFlags = RaycastFlags.SubElements | RaycastFlags.Markers;
        }

        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        protected override bool GetAllowApply()
        {
            if (GetRaycastResult(out var controlPoint))
            {
                if (controlPoint.m_OriginalEntity != Entity.Null && 
                    EntityManager.HasComponent<Edge>(controlPoint.m_OriginalEntity))
                {
                    m_ToolSystem.selected = controlPoint.m_OriginalEntity;
                    return true;
                }
            }
            
            m_ToolSystem.selected = Entity.Null;
            return false;
        }

        [Preserve]
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            ControlPoint currentPoint;
            bool hasHit = GetRaycastResult(out currentPoint);
            
            // Handle hover highlighting (only when not dragging and no active selection)
            if (!m_IsDragging && m_SelectedRoads.Count == 0)
            {
                if (hasHit && currentPoint.m_OriginalEntity != Entity.Null && EntityManager.HasComponent<Edge>(currentPoint.m_OriginalEntity))
                {
                    // Hovering over a road
                    if (currentPoint.m_OriginalEntity != m_HoverEntity)
                    {
                        // Remove highlight from previous hover
                        if (m_HoverEntity != Entity.Null && EntityManager.Exists(m_HoverEntity))
                        {
                            if (EntityManager.HasComponent<Highlighted>(m_HoverEntity))
                            {
                                EntityManager.RemoveComponent<Highlighted>(m_HoverEntity);
                                EntityManager.AddComponent<BatchesUpdated>(m_HoverEntity);
                            }
                        }
                        
                        // Add highlight to new hover
                        if (!EntityManager.HasComponent<Highlighted>(currentPoint.m_OriginalEntity))
                        {
                            EntityManager.AddComponent<Highlighted>(currentPoint.m_OriginalEntity);
                            EntityManager.AddComponent<BatchesUpdated>(currentPoint.m_OriginalEntity);
                        }
                        
                        m_HoverEntity = currentPoint.m_OriginalEntity;
                    }
                }
                else
                {
                    // Not hovering over anything - clear hover highlight
                    if (m_HoverEntity != Entity.Null && EntityManager.Exists(m_HoverEntity))
                    {
                        if (EntityManager.HasComponent<Highlighted>(m_HoverEntity))
                        {
                            EntityManager.RemoveComponent<Highlighted>(m_HoverEntity);
                            EntityManager.AddComponent<BatchesUpdated>(m_HoverEntity);
                        }
                        m_HoverEntity = Entity.Null;
                    }
                }
            }
            
            // Detect mouse click
            if (applyAction.WasPressedThisFrame())
            {
                m_IsDragging = true;
                m_TempSelection.Clear();
                
                // Clear hover highlight when starting selection
                if (m_HoverEntity != Entity.Null && EntityManager.Exists(m_HoverEntity))
                {
                    if (EntityManager.HasComponent<Highlighted>(m_HoverEntity))
                    {
                        EntityManager.RemoveComponent<Highlighted>(m_HoverEntity);
                        EntityManager.AddComponent<BatchesUpdated>(m_HoverEntity);
                    }
                    m_HoverEntity = Entity.Null;
                }
                
                // Clear any previous selection highlights
                var query = GetEntityQuery(ComponentType.ReadOnly<Highlighted>());
                var highlighted = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var entity in highlighted)
                {
                    EntityManager.RemoveComponent<Highlighted>(entity);
                    EntityManager.AddComponent<BatchesUpdated>(entity);
                }
                highlighted.Dispose();
                
                //Mod.log.Info("Mouse pressed - starting selection");
                
                if (hasHit && currentPoint.m_OriginalEntity != Entity.Null && EntityManager.HasComponent<Edge>(currentPoint.m_OriginalEntity))
                {
                    m_TempSelection.Add(currentPoint.m_OriginalEntity);
                    
                    // Add highlight to the selected entity
                    EntityManager.AddComponent<Highlighted>(currentPoint.m_OriginalEntity);
                    EntityManager.AddComponent<BatchesUpdated>(currentPoint.m_OriginalEntity);
                    
                    //Mod.log.Info($"Initial segment added: {currentPoint.m_OriginalEntity.Index}");
                }
            }

            // Continue adding segments while dragging
            if (m_IsDragging && applyAction.IsPressed())
            {
                if (hasHit && currentPoint.m_OriginalEntity != Entity.Null && EntityManager.HasComponent<Edge>(currentPoint.m_OriginalEntity))
                {
                    if (!m_TempSelection.Contains(currentPoint.m_OriginalEntity))
                    {
                        m_TempSelection.Add(currentPoint.m_OriginalEntity);
                        
                        if (!EntityManager.HasComponent<Highlighted>(currentPoint.m_OriginalEntity))
                        {
                            EntityManager.AddComponent<Highlighted>(currentPoint.m_OriginalEntity);
                        }
                        EntityManager.AddComponent<BatchesUpdated>(currentPoint.m_OriginalEntity);
                        
                        //Mod.log.Info($"Added segment during drag: {currentPoint.m_OriginalEntity.Index} (total: {m_TempSelection.Count})");
                    }
                }
            }

            // Finalize selection when mouse is released
            if (m_IsDragging && applyAction.WasReleasedThisFrame())
            {
                m_IsDragging = false;
                //Mod.log.Info($"Mouse released - finalizing selection of {m_TempSelection.Count} segments");
                FinalizeSelection();
                
                //Mod.log.Info("Selection complete - tool STAYING ACTIVE");
            }

            // Cancel with ESC
            if (cancelAction != null && cancelAction.WasPressedThisFrame())
            {
                //Mod.log.Info("ESC pressed - deactivating tool");
                m_ToolSystem.activeTool = m_DefaultToolSystem;
                return inputDeps;
            }

            return inputDeps;
        }

        private void RemoveAllHighlights()
        {
            // Remove highlights from all entities
            var query = GetEntityQuery(ComponentType.ReadOnly<Highlighted>());
            EntityManager.AddComponent<BatchesUpdated>(query);
            EntityManager.RemoveComponent<Highlighted>(query);
        }

        private void FinalizeSelection()
        {
            m_SelectedRoads.Clear();
            m_SelectedRoads.AddRange(m_TempSelection);
            
            //Mod.log.Info($"=== FinalizeSelection START: {m_SelectedRoads.Count} segments ===");
            
            if (m_UISystem != null && m_SelectedRoads.Count > 0)
            {
                Entity aggregate = Entity.Null;
                if (EntityManager.HasComponent<Aggregated>(m_SelectedRoads[0]))
                {
                    aggregate = EntityManager.GetComponentData<Aggregated>(m_SelectedRoads[0]).m_Aggregate;
                }
                
                //Mod.log.Info($"FinalizeSelection: Calling OnRoadSelectedByTool with aggregate={aggregate.Index}");
                m_UISystem.OnRoadSelectedByTool(aggregate, m_SelectedRoads);
                //Mod.log.Info($"FinalizeSelection: OnRoadSelectedByTool returned");
            }
            else if (m_SelectedRoads.Count == 0)
            {
                //Mod.log.Info("No segments were selected - clearing any existing selection");
                // If user clicked on empty space, clear the selection and remove highlights
                RemoveAllHighlights();
            }
            
            //Mod.log.Info($"=== FinalizeSelection END ===");
        }

        public void ClearSelection()
        {
            //Mod.log.Info("ClearSelection: Removing highlights and clearing selection");
            RemoveAllHighlights();
            m_SelectedRoads.Clear();
            m_TempSelection.Clear();
            m_IsDragging = false;
        }

        public void ApplySpeedToSelection(float speedKmh)
        {
            if (m_SelectedRoads.Count == 0)
            {
                //Mod.log.Warn("ApplySpeedToSelection: No roads selected");
                return;
            }

            //Mod.log.Info($"ApplySpeedToSelection: Applying {speedKmh} km/h to {m_SelectedRoads.Count} segments");

            float speedGameUnits = speedKmh / 1.8f;

            foreach (var edge in m_SelectedRoads)
            {
                Entity targetEdge = edge;
                
                if (EntityManager.HasComponent<Temp>(edge))
                {
                    var temp = EntityManager.GetComponentData<Temp>(edge);
                    targetEdge = temp.m_Original;
                }

                // Get original speed (only if not already stored)
                var existingEntry = PersistentSpeedStorage.GetRoadSpeed(targetEdge.Index);
                float originalSpeed = 0f;
                
                if (existingEntry == null)
                {
                    // First time modifying this edge - get and store original speed
                    if (EntityManager.HasBuffer<SubLane>(edge))
                    {
                        var subLanes = EntityManager.GetBuffer<SubLane>(edge);
                        var ignore = Game.Net.CarLaneFlags.Unsafe | Game.Net.CarLaneFlags.SideConnection;
                        float totalSpeed = 0f;
                        int count = 0;
                        
                        foreach (var subLane in subLanes)
                        {
                            // Check for CarLane (roads)
                            if (EntityManager.HasComponent<Game.Net.CarLane>(subLane.m_SubLane))
                            {
                                var carLane = EntityManager.GetComponentData<Game.Net.CarLane>(subLane.m_SubLane);
                                if ((carLane.m_Flags & ignore) != 0)
                                    continue;
                                
                                totalSpeed += carLane.m_SpeedLimit * 1.8f; // Convert to km/h
                                count++;
                            }
                            // Check for TrackLane (trains, trams, subways)
                            else if (EntityManager.HasComponent<Game.Net.TrackLane>(subLane.m_SubLane))
                            {
                                var trackLane = EntityManager.GetComponentData<Game.Net.TrackLane>(subLane.m_SubLane);
                                totalSpeed += trackLane.m_SpeedLimit * 1.8f; // Convert to km/h
                                count++;
                            }
                        }
                        
                        if (count > 0)
                        {
                            originalSpeed = totalSpeed / count;
                            //Mod.log.Info($"Stored original speed for entity {targetEdge.Index}: {originalSpeed:F1} km/h");
                        }
                    }
                }
                else
                {
                    // Use previously stored default speed
                    originalSpeed = existingEntry.DefaultSpeed;
                }

                // Store to persistent JSON (default speed + current speed)
                PersistentSpeedStorage.StoreRoadSpeed(targetEdge.Index, originalSpeed, speedKmh);

                // Add/Update CustomSpeed component
                if (!EntityManager.HasComponent<CustomSpeed>(targetEdge))
                {
                    EntityManager.AddComponent<CustomSpeed>(targetEdge);
                }

                var customSpeed = new CustomSpeed(speedKmh);
                EntityManager.SetComponentData(targetEdge, customSpeed);
                
                SetCarLaneSpeedsImmediate(edge, speedGameUnits);
            }

            //Mod.log.Info("ApplySpeedToSelection: Complete");
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

        public void ResetSpeedToOriginal()
        {
            if (m_SelectedRoads.Count == 0)
            {
                //Mod.log.Warn("ResetSpeedToOriginal: No roads selected");
                return;
            }

            //Mod.log.Info($"ResetSpeedToOriginal: Resetting {m_SelectedRoads.Count} segments to original speeds");

            foreach (var edge in m_SelectedRoads)
            {
                Entity targetEdge = edge;
                
                if (EntityManager.HasComponent<Temp>(edge))
                {
                    var temp = EntityManager.GetComponentData<Temp>(edge);
                    targetEdge = temp.m_Original;
                }

                // Get the original speed from persistent storage
                float? originalSpeed = PersistentSpeedStorage.GetDefaultSpeed(targetEdge.Index);
                if (!originalSpeed.HasValue)
                {
                    //Mod.log.Warn($"ResetSpeedToOriginal: No original speed found for edge {targetEdge.Index}");
                    continue;
                }

                float speedGameUnits = originalSpeed.Value / 1.8f;
                
                // Restore speed to all lanes
                if (EntityManager.HasBuffer<SubLane>(edge))
                {
                    var subLanes = EntityManager.GetBuffer<SubLane>(edge);
                    foreach (var subLane in subLanes)
                    {
                        // Restore speed for CarLane (roads)
                        if (EntityManager.HasComponent<Game.Net.CarLane>(subLane.m_SubLane))
                        {
                            var carLane = EntityManager.GetComponentData<Game.Net.CarLane>(subLane.m_SubLane);
                            carLane.m_SpeedLimit = speedGameUnits;
                            EntityManager.SetComponentData(subLane.m_SubLane, carLane);
                        }
                        // Restore speed for TrackLane (trains, trams, subways)
                        else if (EntityManager.HasComponent<Game.Net.TrackLane>(subLane.m_SubLane))
                        {
                            var trackLane = EntityManager.GetComponentData<Game.Net.TrackLane>(subLane.m_SubLane);
                            trackLane.m_SpeedLimit = speedGameUnits;
                            EntityManager.SetComponentData(subLane.m_SubLane, trackLane);
                        }
                    }
                }

                // Remove CustomSpeed component
                if (EntityManager.HasComponent<CustomSpeed>(targetEdge))
                {
                    EntityManager.RemoveComponent<CustomSpeed>(targetEdge);
                }

                // Remove from persistent storage
                PersistentSpeedStorage.RemoveRoad(targetEdge.Index);
                
                //Mod.log.Info($"Reset edge {targetEdge.Index} to {originalSpeed.Value:F1} km/h");
            }

            //Mod.log.Info("ResetSpeedToOriginal: Complete");
        }

        public void DeactivateTool()
        {
            if (IsActive)
            {
                m_ToolSystem.activeTool = m_DefaultToolSystem;
                ClearSelection();
            }
        }

        /// <summary>
        /// Called by Setting.cs to clear all custom speeds and update the UI
        /// </summary>
        public void ClearAllCustomSpeedsAndUI()
        {
            // Get all roads with custom speeds from persistent storage
            var persistentRoads = PersistentSpeedStorage.GetAllRoads();
            
            if (persistentRoads == null || persistentRoads.Count == 0)
            {
                Mod.log.Info("No custom speed roads to clear");
                return;
            }

            Mod.log.Info($"Clearing {persistentRoads.Count} roads with custom speeds");

            // Build a list of valid entities to reset
            var entitiesToReset = new List<Entity>();
            
            foreach (var roadEntry in persistentRoads)
            {
                var entity = new Entity { Index = roadEntry.Key, Version = 1 };
                
                if (EntityManager.Exists(entity) && EntityManager.HasComponent<CustomSpeed>(entity))
                {
                    entitiesToReset.Add(entity);
                }
            }

            if (entitiesToReset.Count == 0)
            {
                Mod.log.Info("No valid entities found to reset");
                PersistentSpeedStorage.Clear();
                return;
            }

            Mod.log.Info($"Resetting {entitiesToReset.Count} road entities");

            // Reset each entity
            foreach (var entity in entitiesToReset)
            {
                // Get original speed from persistent storage
                float? originalSpeed = PersistentSpeedStorage.GetDefaultSpeed(entity.Index);
                
                if (!originalSpeed.HasValue)
                {
                    Mod.log.Warn($"No original speed found for entity {entity.Index}");
                    continue;
                }

                float speedGameUnits = originalSpeed.Value / 1.8f;
                
                // Restore speed to sublanes
                if (EntityManager.HasBuffer<SubLane>(entity))
                {
                    var subLanes = EntityManager.GetBuffer<SubLane>(entity);
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
                
                // Remove CustomSpeed component (this will also remove the rendered overlay)
                if (EntityManager.HasComponent<CustomSpeed>(entity))
                {
                    EntityManager.RemoveComponent<CustomSpeed>(entity);
                }
            }

            // Clear persistent storage
            PersistentSpeedStorage.Clear();
            
            Mod.log.Info($"Successfully cleared custom speeds from {entitiesToReset.Count} roads");
        }
    }
}
