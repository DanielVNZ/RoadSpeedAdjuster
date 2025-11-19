using Unity.Entities;

namespace RoadSpeedAdjuster.Components
{
    public struct SpeedCloneRequest : IComponentData
    {
        public float TargetSpeed;
    }
}