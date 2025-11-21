using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace RoadSpeedAdjuster.Data
{
    /// <summary>
    /// Manages persistent storage of original road speeds so they can be restored.
    /// Stores data per individual edge segment for precise control.
    /// </summary>
    public static class SpeedDataManager
    {
        private static readonly string ModDataDirectory = Path.Combine(
            Application.persistentDataPath,
            "ModsData",
            "RoadSpeedAdjuster"
        );

        private static readonly string SpeedDataFile = Path.Combine(ModDataDirectory, "segment_speeds.json");

        private static Dictionary<int, float> _originalSpeeds = new Dictionary<int, float>();
        private static bool _isLoaded = false;

        /// <summary>
        /// Stores the original speed for an individual edge segment before applying custom speed
        /// </summary>
        public static void StoreOriginalSpeed(int edgeIndex, float originalSpeed)
        {
            EnsureLoaded();
            
            // Only store if not already stored (preserve the very first original speed)
            if (!_originalSpeeds.ContainsKey(edgeIndex))
            {
                _originalSpeeds[edgeIndex] = originalSpeed;
                Save();
                Mod.log.Info($"[SpeedData] Stored original speed for segment {edgeIndex}: {originalSpeed:F1} km/h");
            }
        }

        /// <summary>
        /// Stores original speeds for multiple segments at once
        /// </summary>
        public static void StoreOriginalSpeeds(Dictionary<int, float> segments)
        {
            EnsureLoaded();
            
            int storedCount = 0;
            foreach (var kvp in segments)
            {
                if (!_originalSpeeds.ContainsKey(kvp.Key))
                {
                    _originalSpeeds[kvp.Key] = kvp.Value;
                    storedCount++;
                }
            }
            
            if (storedCount > 0)
            {
                Save();
                Mod.log.Info($"[SpeedData] Stored original speeds for {storedCount} segments");
            }
        }

        /// <summary>
        /// Gets the stored original speed for an edge segment, or null if not stored
        /// </summary>
        public static float? GetOriginalSpeed(int edgeIndex)
        {
            EnsureLoaded();
            
            if (_originalSpeeds.TryGetValue(edgeIndex, out float speed))
            {
                return speed;
            }
            
            return null;
        }

        /// <summary>
        /// Gets original speeds for multiple segments at once
        /// </summary>
        public static Dictionary<int, float> GetOriginalSpeeds(IEnumerable<int> edgeIndices)
        {
            EnsureLoaded();
            
            var result = new Dictionary<int, float>();
            foreach (var index in edgeIndices)
            {
                if (_originalSpeeds.TryGetValue(index, out float speed))
                {
                    result[index] = speed;
                }
            }
            
            return result;
        }

        /// <summary>
        /// Removes the stored original speed for an edge segment when reset to default
        /// </summary>
        public static void RemoveOriginalSpeed(int edgeIndex)
        {
            EnsureLoaded();
            
            if (_originalSpeeds.Remove(edgeIndex))
            {
                Save();
                Mod.log.Info($"[SpeedData] Removed original speed for segment {edgeIndex}");
            }
        }

        /// <summary>
        /// Removes original speeds for multiple segments at once
        /// </summary>
        public static void RemoveOriginalSpeeds(IEnumerable<int> edgeIndices)
        {
            EnsureLoaded();
            
            int removedCount = 0;
            foreach (var index in edgeIndices)
            {
                if (_originalSpeeds.Remove(index))
                {
                    removedCount++;
                }
            }
            
            if (removedCount > 0)
            {
                Save();
                Mod.log.Info($"[SpeedData] Removed original speeds for {removedCount} segments");
            }
        }

        /// <summary>
        /// Checks if a segment has a stored original speed
        /// </summary>
        public static bool HasOriginalSpeed(int edgeIndex)
        {
            EnsureLoaded();
            return _originalSpeeds.ContainsKey(edgeIndex);
        }

        /// <summary>
        /// Clears all stored speeds (for cleanup/reset)
        /// </summary>
        public static void Clear()
        {
            _originalSpeeds.Clear();
            Save();
            Mod.log.Info("[SpeedData] Cleared all stored speeds");
        }

        /// <summary>
        /// Gets the total number of segments with stored speeds
        /// </summary>
        public static int Count
        {
            get
            {
                EnsureLoaded();
                return _originalSpeeds.Count;
            }
        }

        private static void EnsureLoaded()
        {
            if (!_isLoaded)
            {
                Load();
            }
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(SpeedDataFile))
                {
                    string json = File.ReadAllText(SpeedDataFile);
                    _originalSpeeds = JsonConvert.DeserializeObject<Dictionary<int, float>>(json) 
                                      ?? new Dictionary<int, float>();
                    Mod.log.Info($"[SpeedData] Loaded {_originalSpeeds.Count} segment speeds from disk");
                }
                else
                {
                    _originalSpeeds = new Dictionary<int, float>();
                    Mod.log.Info("[SpeedData] No saved speed data found, starting fresh");
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[SpeedData] Failed to load speed data: {ex.Message}");
                _originalSpeeds = new Dictionary<int, float>();
            }
            
            _isLoaded = true;
        }

        private static void Save()
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(ModDataDirectory);
                
                // Serialize to JSON
                string json = JsonConvert.SerializeObject(_originalSpeeds, Formatting.Indented);
                
                // Write to file
                File.WriteAllText(SpeedDataFile, json);
                
                Mod.log.Info($"[SpeedData] Saved {_originalSpeeds.Count} segment speeds to disk");
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[SpeedData] Failed to save speed data: {ex.Message}");
            }
        }
    }
}
