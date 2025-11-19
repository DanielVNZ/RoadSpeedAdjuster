using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Game.Common;
using Game.Net;
using RoadSpeedAdjuster.Components;

namespace RoadSpeedAdjuster.Systems
{
    public partial class RoadSpeedApplySystem : SystemBase
    {
        private EntityQuery _entitiesToRestoreQuery;
        private EntityCommandBufferSystem _commandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Query for edges with CustomSpeed that need restoration
            _entitiesToRestoreQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<CustomSpeed>()
                },
                Any = new[]
                {
                    ComponentType.ReadOnly<Updated>(),
                }
            });

            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            JobHandle jobHandle = new RestoreSpeedJob
            {
                EntityType = EntityManager.GetEntityTypeHandle(),
                AggregatedType = GetComponentTypeHandle<Aggregated>(true),
                EntityManager = EntityManager,
                CommandBuffer = commandBuffer,
            }.ScheduleParallel(_entitiesToRestoreQuery, Dependency);

            _commandBufferSystem.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;
        }

        [BurstCompile]
        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach", Justification = "This is a burst method, so it's better with for loops.")]
        private struct RestoreSpeedJob : IJobChunk
        {
            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public ComponentTypeHandle<Aggregated> AggregatedType;

            [ReadOnly]
            public EntityManager EntityManager;

            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var array = chunk.GetNativeArray(this.EntityType);

                for (int i = 0; i < array.Length; i++)
                {
                    var entity = array[i];

                    if (this.EntityManager.HasComponent<CustomSpeed>(entity))
                    {
                        var customSpeed = this.EntityManager.GetComponentData<CustomSpeed>(entity);
                        SetSpeed(entity, customSpeed.m_Speed);
                        
                        // Remove Updated component from this edge
                        CommandBuffer.RemoveComponent<Updated>(unfilteredChunkIndex, entity);
                    }
                }
            }

            [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach", Justification = "This is a burst method, so it's better with for loops.")]
            private void SetSpeed(Entity entity, float speedKmh)
            {
                // Convert km/h to game units (2x m/s)
                // Game uses 2x m/s, so divide by 1.8 instead of 3.6
                float speedGameUnits = speedKmh / 1.8f;

                if (this.EntityManager.HasBuffer<SubLane>(entity))
                {
                    var subLanes = this.EntityManager.GetBuffer<SubLane>(entity);

                    for (int i = 0; i < subLanes.Length; i++)
                    {
                        var subLane = subLanes[i];
                        SetSpeedSubLane(subLane.m_SubLane, speedGameUnits);
                        // DON'T reassign subLanes[i] - it breaks connectivity!
                    }
                }
            }

            private void SetSpeedSubLane(Entity laneEntity, float speedGameUnits)
            {
                var ignoreFlags = CarLaneFlags.Unsafe | CarLaneFlags.SideConnection;

                if (this.EntityManager.HasComponent<CarLane>(laneEntity))
                {
                    var carLane = this.EntityManager.GetComponentData<CarLane>(laneEntity);

                    if ((carLane.m_Flags & ignoreFlags) == 0)
                    {
                        // Set BOTH DefaultSpeedLimit AND SpeedLimit
                        carLane.m_DefaultSpeedLimit = speedGameUnits;
                        carLane.m_SpeedLimit = speedGameUnits;
                        this.EntityManager.SetComponentData(laneEntity, carLane);
                    }
                }
            }
        }
    }
}
