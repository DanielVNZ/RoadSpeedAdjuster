using Colossal.Entities;
using Game.Common;
using RoadSpeedAdjuster.Components;
using Unity.Collections;
using Unity.Entities;
using Net = Game.Net;
using Prefab = Game.Prefabs;

namespace RoadSpeedAdjuster.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class RoadSpeedApplySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var eMgr = EntityManager;

            // Query all road segments with Updated + RoadComposition
            var query = SystemAPI.QueryBuilder()
                .WithAll<Updated>()
                .WithAll<Prefab.RoadComposition>()
                .Build();

            using var roads = query.ToEntityArray(Allocator.Temp);

            foreach (var road in roads)
            {
                // Get new speed
                if (!eMgr.TryGetComponent(road, out Prefab.RoadComposition comp))
                    continue;

                float newSpeed = comp.m_SpeedLimit;

                // Road must have sublanes (the actual lanes you drive on)
                if (!eMgr.TryGetBuffer<Net.SubLane>(road, false, out var sublanes))
                    continue;

                for (int i = 0; i < sublanes.Length; i++)
                {
                    Entity lane = sublanes[i].m_SubLane;

                    if (!eMgr.TryGetComponent(lane, out Net.CarLane car))
                        continue;

                    car.m_SpeedLimit = newSpeed;
                    eMgr.SetComponentData(lane, car);
                }

                // Remove marker so it doesn’t reapply forever
                eMgr.RemoveComponent<Updated>(road);
            }
        }
    }
}
