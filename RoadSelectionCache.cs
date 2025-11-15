// RoadSelectionCache.cs
using Unity.Entities;

namespace RoadSpeedAdjuster
{
    /// <summary>
    /// Simple static cache for the road currently shown in the info panel.
    /// Written by the info-panel system, read by the UI binding system.
    /// </summary>
    public static class RoadSelectionCache
    {
        public static Entity LastSelectedRaw = Entity.Null;
    }
}
