using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Routes;
using Game.Tools;
using Unity.Mathematics;

namespace RoadSpeedAdjuster.Systems
{
    public partial class EntitySelectorToolSystem : DefaultToolSystem
    {
        public override string toolID => "RoadSpeedAdjuster_EntitySelector";

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        public override void InitializeRaycast()
        {
            base.InitializeRaycast();
            
            m_ToolRaycastSystem.raycastFlags = RaycastFlags.SubElements | RaycastFlags.Cargo | RaycastFlags.Passenger | RaycastFlags.EditorContainers;
            m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground | CollisionMask.Underground;
            m_ToolRaycastSystem.typeMask = TypeMask.Net;
            m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.PublicTransportRoad | Layer.TrainTrack | Layer.TramTrack | Layer.SubwayTrack;
            m_ToolRaycastSystem.areaTypeMask = AreaTypeMask.None;
            m_ToolRaycastSystem.routeType = RouteType.None;
            m_ToolRaycastSystem.transportType = TransportType.None;
            m_ToolRaycastSystem.iconLayerMask = IconLayerMask.None;
            m_ToolRaycastSystem.utilityTypeMask = UtilityTypes.None;
            m_ToolRaycastSystem.rayOffset = new float3();
        }
    }
}
