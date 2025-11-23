using System.Collections.Generic;

namespace RoadSpeedAdjuster.Data
{
    /// <summary>
    /// Manages road speed data in memory. Data is persisted via the RoadSpeedSaveData component
    /// which uses the game's built-in save/load system.
    /// </summary>
    public static class SpeedDataManager
    {
        private static Dictionary<int, float> _originalSpeeds = new Dictionary<int, float>();
        private static Dictionary<int, float> _customSpeedRoads = new Dictionary<int, float>(); // Now stores entity index -> speed value

        public static void StoreOriginalSpeed(int entityIndex, float speedKmh)
        {
            if (!_originalSpeeds.ContainsKey(entityIndex))
            {
                _originalSpeeds[entityIndex] = speedKmh;
                Mod.log.Info($"Stored original speed for entity {entityIndex}: {speedKmh} km/h");
            }
        }

        public static float? GetOriginalSpeed(int entityIndex)
        {
            if (_originalSpeeds.TryGetValue(entityIndex, out float speed))
            {
                return speed;
            }
            return null;
        }

        public static void RemoveOriginalSpeed(int entityIndex)
        {
            if (_originalSpeeds.Remove(entityIndex))
            {
                Mod.log.Info($"Removed original speed for entity {entityIndex}");
            }
        }

        /// <summary>
        /// Mark a road segment as having a custom speed set
        /// </summary>
        public static void AddCustomSpeedRoad(int entityIndex, float speedKmh)
        {
            _customSpeedRoads[entityIndex] = speedKmh;
            Mod.log.Info($"Added custom speed road: {entityIndex} = {speedKmh} km/h");
        }

        /// <summary>
        /// Remove a road segment from the custom speed tracking (when reset to default)
        /// </summary>
        public static void RemoveCustomSpeedRoad(int entityIndex)
        {
            if (_customSpeedRoads.Remove(entityIndex))
            {
                Mod.log.Info($"Removed custom speed road: {entityIndex}");
            }
        }

        /// <summary>
        /// Get all road entity indices that have custom speeds
        /// </summary>
        public static IReadOnlyCollection<int> GetCustomSpeedRoads()
        {
            return _customSpeedRoads.Keys;
        }

        /// <summary>
        /// Get custom speed data (for serialization)
        /// </summary>
        public static IReadOnlyDictionary<int, float> GetAllCustomSpeedData()
        {
            return _customSpeedRoads;
        }

        /// <summary>
        /// Get original speed data (for serialization)
        /// </summary>
        public static IReadOnlyDictionary<int, float> GetAllOriginalSpeeds()
        {
            return _originalSpeeds;
        }

        /// <summary>
        /// Get the custom speed for a specific road
        /// </summary>
        public static float? GetCustomSpeed(int entityIndex)
        {
            if (_customSpeedRoads.TryGetValue(entityIndex, out float speed))
            {
                return speed;
            }
            return null;
        }

        /// <summary>
        /// Clear all stored data (called when loading a new save)
        /// </summary>
        public static void ClearAll()
        {
            _originalSpeeds.Clear();
            _customSpeedRoads.Clear();
            Mod.log.Info("Cleared all speed data");
        }
    }
}
