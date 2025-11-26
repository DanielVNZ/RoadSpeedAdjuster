using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Game.SceneFlow;

namespace RoadSpeedAdjuster.Data
{
    /// <summary>
    /// Manages persistent storage of road speed data to JSON files.
    /// Each map/save game gets its own JSON file.
    /// </summary>
    public static class PersistentSpeedStorage
    {
        private static MapSpeedData _currentMapData;
        private static string _currentFilePath;
        private static string _baseDirectory;
        
        /// <summary>
        /// Base directory for storing road speed data
        /// </summary>
        public static string BaseDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_baseDirectory))
                {
                    // Use the game's LocalLow directory
                    string localLowPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";
                    _baseDirectory = Path.Combine(localLowPath, "Colossal Order", "Cities Skylines II", "ModsData", "RoadSpeedAdjuster");
                    
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(_baseDirectory))
                    {
                        Directory.CreateDirectory(_baseDirectory);
                        //Mod.log.Info($"Created ModsData directory: {_baseDirectory}");
                    }
                }
                return _baseDirectory;
            }
        }
        
        /// <summary>
        /// Initialize storage for the current map/save game
        /// </summary>
        /// <param name="saveGameName">Name of the save game</param>
        /// <param name="saveGameId">Unique ID of the save game</param>
        public static void Initialize(string saveGameName, string saveGameId)
        {
            try
            {
                // Sanitize filename
                string sanitizedName = SanitizeFileName(saveGameName);
                string fileName = $"{sanitizedName}_{saveGameId}.json";
                _currentFilePath = Path.Combine(BaseDirectory, fileName);
                
                // Load existing data or create new
                if (File.Exists(_currentFilePath))
                {
                    LoadFromFile();
                    //Mod.log.Info($"Loaded existing road speed data from: {_currentFilePath}");
                }
                else
                {
                    _currentMapData = new MapSpeedData
                    {
                        MapName = saveGameName,
                        SaveGameId = saveGameId,
                        LastSaved = DateTime.Now
                    };
                    //Mod.log.Info($"Created new road speed data for: {saveGameName}");
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Failed to initialize persistent storage: {ex}");
                _currentMapData = new MapSpeedData();
            }
        }
        
        /// <summary>
        /// Store a road's speed information
        /// </summary>
        public static void StoreRoadSpeed(int roadId, float defaultSpeed, float currentSpeed)
        {
            if (_currentMapData == null)
            {
                //Mod.log.Warn("Storage not initialized. Call Initialize first.");
                return;
            }
            
            var entry = new RoadSpeedEntry
            {
                RoadId = roadId,
                DefaultSpeed = defaultSpeed,
                CurrentSpeed = currentSpeed,
                LastModified = DateTime.Now
            };
            
            _currentMapData.Roads[roadId] = entry;
            //Mod.log.Info($"Stored road {roadId}: default={defaultSpeed:F1} km/h, current={currentSpeed:F1} km/h");
            
            // Auto-save after storing
            Save();
        }
        
        /// <summary>
        /// Get a road's stored speed data
        /// </summary>
        public static RoadSpeedEntry GetRoadSpeed(int roadId)
        {
            if (_currentMapData == null || !_currentMapData.Roads.ContainsKey(roadId))
                return null;
            
            return _currentMapData.Roads[roadId];
        }
        
        /// <summary>
        /// Get default speed for a road
        /// </summary>
        public static float? GetDefaultSpeed(int roadId)
        {
            var entry = GetRoadSpeed(roadId);
            return entry?.DefaultSpeed;
        }
        
        /// <summary>
        /// Get current speed for a road
        /// </summary>
        public static float? GetCurrentSpeed(int roadId)
        {
            var entry = GetRoadSpeed(roadId);
            return entry?.CurrentSpeed;
        }
        
        /// <summary>
        /// Remove a road from storage
        /// </summary>
        public static void RemoveRoad(int roadId)
        {
            if (_currentMapData == null)
                return;
            
            if (_currentMapData.Roads.Remove(roadId))
            {
                //Mod.log.Info($"Removed road {roadId} from storage");
                Save();
            }
        }
        
        /// <summary>
        /// Get all modified roads
        /// </summary>
        public static IReadOnlyDictionary<int, RoadSpeedEntry> GetAllRoads()
        {
            if (_currentMapData == null)
                return new Dictionary<int, RoadSpeedEntry>();
            
            return _currentMapData.Roads;
        }
        
        /// <summary>
        /// Save current data to JSON file
        /// </summary>
        public static void Save()
        {
            if (_currentMapData == null || string.IsNullOrEmpty(_currentFilePath))
            {
                //Mod.log.Warn("Cannot save: storage not initialized");
                return;
            }
            
            try
            {
                _currentMapData.LastSaved = DateTime.Now;
                
                string json = JsonConvert.SerializeObject(_currentMapData, Formatting.Indented);
                File.WriteAllText(_currentFilePath, json);
                
                //Mod.log.Info($"Saved {_currentMapData.Roads.Count} roads to {_currentFilePath}");
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Failed to save road speed data: {ex}");
            }
        }
        
        /// <summary>
        /// Load data from JSON file
        /// </summary>
        private static void LoadFromFile()
        {
            try
            {
                string json = File.ReadAllText(_currentFilePath);
                _currentMapData = JsonConvert.DeserializeObject<MapSpeedData>(json);
                
                if (_currentMapData == null)
                {
                    //Mod.log.Warn("Loaded null data, creating new");
                    _currentMapData = new MapSpeedData();
                }
                else
                {
                    //Mod.log.Info($"Loaded {_currentMapData.Roads.Count} roads from storage");
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Failed to load road speed data: {ex}");
                _currentMapData = new MapSpeedData();
            }
        }
        
        /// <summary>
        /// Clear all data for current map
        /// </summary>
        public static void Clear()
        {
            if (_currentMapData != null)
            {
                _currentMapData.Roads.Clear();
                Save();
                //Mod.log.Info("Cleared all road speed data for current map");
            }
        }
        
        /// <summary>
        /// Sanitize a filename by removing invalid characters
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";
            
            char[] invalids = Path.GetInvalidFileNameChars();
            string sanitized = string.Join("_", fileName.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            
            // Limit length
            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);
            
            return string.IsNullOrEmpty(sanitized) ? "unnamed" : sanitized;
        }
        
        /// <summary>
        /// Get statistics about current storage
        /// </summary>
        public static string GetStats()
        {
            if (_currentMapData == null)
                return "Storage not initialized";
            
            return $"Map: {_currentMapData.MapName}, Roads: {_currentMapData.Roads.Count}, Last saved: {_currentMapData.LastSaved}";
        }
    }
}
