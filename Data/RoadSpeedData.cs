using System;
using System.Collections.Generic;

namespace RoadSpeedAdjuster.Data
{
    /// <summary>
    /// Data structure for a single road's speed information
    /// </summary>
    [Serializable]
    public class RoadSpeedEntry
    {
        /// <summary>
        /// Entity ID of the road
        /// </summary>
        public int RoadId { get; set; }
        
        /// <summary>
        /// Original/default speed in km/h
        /// </summary>
        public float DefaultSpeed { get; set; }
        
        /// <summary>
        /// Current/custom speed in km/h
        /// </summary>
        public float CurrentSpeed { get; set; }
        
        /// <summary>
        /// Timestamp when this road was last modified
        /// </summary>
        public DateTime LastModified { get; set; }
    }
    
    /// <summary>
    /// Root data structure for a map's road speed data
    /// </summary>
    [Serializable]
    public class MapSpeedData
    {
        /// <summary>
        /// Name of the map/save game
        /// </summary>
        public string MapName { get; set; }
        
        /// <summary>
        /// Unique identifier for this save game
        /// </summary>
        public string SaveGameId { get; set; }
        
        /// <summary>
        /// When this data was last saved
        /// </summary>
        public DateTime LastSaved { get; set; }
        
        /// <summary>
        /// Dictionary of road speeds (key = road entity ID)
        /// </summary>
        public Dictionary<int, RoadSpeedEntry> Roads { get; set; } = new Dictionary<int, RoadSpeedEntry>();
        
        /// <summary>
        /// Version of the data format
        /// </summary>
        public int Version { get; set; } = 1;
    }
}
