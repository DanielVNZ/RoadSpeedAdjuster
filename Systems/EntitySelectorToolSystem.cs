using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;

namespace RoadSpeedAdjuster.Systems
{
    /// <summary>
    /// Simple tool that raycasts net (roads / tracks) so the SelectedInfoUISystem
    /// ends up with the road entity / label you clicked.
    /// </summary>
    public partial class EntitySelectorToolSystem : DefaultToolSystem
    {
        public override string toolID => "EntitySelectorTool";

        protected override void OnCreate()
        {
            base.OnCreate();

            requireAreas = AreaTypeMask.None;
            requireNet = Layer.Road;
        }

        public override PrefabBase GetPrefab()
        {
            return null;
        }

        public override bool TrySetPrefab(PrefabBase prefab)
        {
            return false;
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();

            m_ToolRaycastSystem.raycastFlags =
                RaycastFlags.SubElements |
                RaycastFlags.Cargo |
                RaycastFlags.Passenger |
                RaycastFlags.EditorContainers;

            m_ToolRaycastSystem.collisionMask =
                CollisionMask.OnGround |
                CollisionMask.Overground |
                CollisionMask.Underground;

            m_ToolRaycastSystem.typeMask = TypeMask.Net;

            m_ToolRaycastSystem.netLayerMask =
                Layer.Road |
                Layer.PublicTransportRoad |
                Layer.TrainTrack |
                Layer.TramTrack |
                Layer.SubwayTrack;

            m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.None;
            m_ToolRaycastSystem.routeType = RouteType.None;
            m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.None;
            m_ToolRaycastSystem.rayOffset = new float3();
        }
    }
}
