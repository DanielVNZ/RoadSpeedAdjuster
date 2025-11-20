using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace RoadSpeedAdjuster.Data
{
    /// <summary>
    /// Manages persistent storage of original road speeds so they can be restored.
    /// Stores data per aggregate (entire road) rather than per edge segment for efficiency.
    /// </summary>
    public static class SpeedDataManager
    {
        private static readonly string ModDataDirectory = Path.Combine(
            Application.persistentDataPath,
            "ModsData",
            "RoadSpeedAdjuster"
        );

        private static readonly string SpeedDataFile = Path.Combine(ModDataDirectory, "original_speeds.json");

        private static Dictionary<int, float> _originalSpeeds = new Dictionary<int, float>();
        private static bool _isLoaded = false;

        /// <summary>
        /// Stores the original speed for a road (aggregate) before applying custom speed
        /// </summary>
        public static void StoreOriginalSpeed(int aggregateIndex, float originalSpeed)
        {
            EnsureLoaded();
            
            // Only store if not already stored (preserve the very first original speed)
            if (!_originalSpeeds.ContainsKey(aggregateIndex))
            {
                _originalSpeeds[aggregateIndex] = originalSpeed;
                Save();
                Mod.log.Info($"[SpeedData] Stored original speed for road {aggregateIndex}: {originalSpeed:F1} km/h");
            }
        }

        /// <summary>
        /// Gets the stored original speed for a road (aggregate), or null if not stored
        /// </summary>
        public static float? GetOriginalSpeed(int aggregateIndex)
        {
            EnsureLoaded();
            
            if (_originalSpeeds.TryGetValue(aggregateIndex, out float speed))
            {
                return speed;
            }
            
            return null;
        }

        /// <summary>
        /// Removes the stored original speed for a road (aggregate) when reset to default
        /// </summary>
        public static void RemoveOriginalSpeed(int aggregateIndex)
        {
            EnsureLoaded();
            
            if (_originalSpeeds.Remove(aggregateIndex))
            {
                Save();
                Mod.log.Info($"[SpeedData] Removed original speed for road {aggregateIndex}");
            }
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
                    Mod.log.Info($"[SpeedData] Loaded {_originalSpeeds.Count} original road speeds from disk");
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
                
                Mod.log.Info($"[SpeedData] Saved {_originalSpeeds.Count} original road speeds to disk");
            }
            catch (Exception ex)
            {
                Mod.log.Error($"[SpeedData] Failed to save speed data: {ex.Message}");
            }
        }
    }
}
