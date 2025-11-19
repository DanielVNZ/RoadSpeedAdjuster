using Unity.Entities;

namespace RoadSpeedAdjuster.Components
{
    public struct SpeedOverride : IComponentData
    {
        public float Speed;
    }
}