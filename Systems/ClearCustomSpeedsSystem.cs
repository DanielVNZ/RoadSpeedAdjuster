using Game;
using Game.Common;
using Game.Net;
using RoadSpeedAdjuster.Components;
using RoadSpeedAdjuster.Data;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace RoadSpeedAdjuster.Systems
{
    /// <summary>
    /// System responsible for clearing all custom speeds when requested via settings
    /// </summary>
    public partial class ClearCustomSpeedsSystem : GameSystemBase
    {
        private bool m_ClearRequested = false;
        private EntityQuery m_CustomSpeedQuery;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Query for all entities with CustomSpeed component
            m_CustomSpeedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<CustomSpeed>(),
                    ComponentType.ReadOnly<Edge>()
                }
            });
            
            Enabled = true; // Always enabled to listen for clear requests
        }

        /// <summary>
        /// Called by Setting.cs to request clearing all custom speeds
        /// </summary>
        public void RequestClearAllCustomSpeeds()
        {
            m_ClearRequested = true;
            Mod.log.Info("ClearCustomSpeedsSystem: Clear requested");
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if (!m_ClearRequested)
                return;

            m_ClearRequested = false;
            Mod.log.Info("ClearCustomSpeedsSystem: Processing clear request");

            try
            {
                // Get all entities with CustomSpeed component
                var entities = m_CustomSpeedQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                if (entities.Length == 0)
                {
                    Mod.log.Info("No roads with custom speeds found");
                    entities.Dispose();
                    return;
                }

                Mod.log.Info($"Found {entities.Length} roads with CustomSpeed component");

                int clearedCount = 0;

                // Process each entity
                foreach (var entity in entities)
                {
                    // Try to get original speed from memory cache first, then persistent storage
                    float? originalSpeed = SpeedDataManager.GetOriginalSpeed(entity.Index);
                    if (!originalSpeed.HasValue)
                    {
                        originalSpeed = PersistentSpeedStorage.GetDefaultSpeed(entity.Index);
                    }
                    
                    if (!originalSpeed.HasValue)
                    {
                        // If no original speed stored, just remove the CustomSpeed component
                        // The game will use the prefab's default speed
                        Mod.log.Warn($"No original speed found for entity {entity.Index}, removing CustomSpeed component");
                        EntityManager.RemoveComponent<CustomSpeed>(entity);
                        SpeedDataManager.RemoveCustomSpeedRoad(entity.Index);
                        PersistentSpeedStorage.RemoveRoad(entity.Index);
                        clearedCount++;
                        continue;
                    }

                    float speedGameUnits = originalSpeed.Value / 1.8f;
                    
                    // Restore speed to all sublanes
                    if (EntityManager.HasBuffer<SubLane>(entity))
                    {
                        var subLanes = EntityManager.GetBuffer<SubLane>(entity);
                        for (int i = 0; i < subLanes.Length; i++)
                        {
                            var laneEntity = subLanes[i].m_SubLane;
                            
                            // Restore speed for CarLane (roads)
                            if (EntityManager.HasComponent<Game.Net.CarLane>(laneEntity))
                            {
                                var carLane = EntityManager.GetComponentData<Game.Net.CarLane>(laneEntity);
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
                    
                    // Remove CustomSpeed component (this removes the rendered overlay)
                    EntityManager.RemoveComponent<CustomSpeed>(entity);
                    
                    // Remove from all tracking systems
                    SpeedDataManager.RemoveOriginalSpeed(entity.Index);
                    SpeedDataManager.RemoveCustomSpeedRoad(entity.Index);
                    PersistentSpeedStorage.RemoveRoad(entity.Index);
                    
                    clearedCount++;
                }

                entities.Dispose();
                
                Mod.log.Info($"Successfully cleared custom speeds from {clearedCount} roads");
            }
            catch (System.Exception ex)
            {
                Mod.log.Error($"Failed to clear custom speeds: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
