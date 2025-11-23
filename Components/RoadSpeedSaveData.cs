using Colossal.Serialization.Entities;
using System.Collections.Generic;
using Unity.Entities;

namespace RoadSpeedAdjuster.Components
{
    /// <summary>
    /// Singleton component that stores road speed data per save game.
    /// This data is automatically serialized/deserialized by the game.
    /// </summary>
    public struct RoadSpeedSaveData : IComponentData, ISerializable
    {
        // These will be populated at runtime from the manager
        // (ECS components can't contain complex types like Dictionary directly,
        // so we'll use a manager system to handle the actual storage)
        
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            // Get data from SpeedDataManager and serialize it
            var originalSpeeds = Data.SpeedDataManager.GetAllOriginalSpeeds();
            var customSpeedRoads = Data.SpeedDataManager.GetAllCustomSpeedData();
            
            // Write original speeds
            writer.Write(originalSpeeds.Count);
            foreach (var kvp in originalSpeeds)
            {
                writer.Write(kvp.Key);  // entity index
                writer.Write(kvp.Value); // speed
            }
            
            // Write custom speed roads
            writer.Write(customSpeedRoads.Count);
            foreach (var kvp in customSpeedRoads)
            {
                writer.Write(kvp.Key);  // entity index
                writer.Write(kvp.Value); // speed
            }
            
            Mod.log.Info($"Serialized: {originalSpeeds.Count} original speeds, {customSpeedRoads.Count} custom speeds");
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            // Clear existing data
            Data.SpeedDataManager.ClearAll();
            
            // Read original speeds
            int originalCount;
            reader.Read(out originalCount);
            for (int i = 0; i < originalCount; i++)
            {
                int entityIndex;
                float speed;
                reader.Read(out entityIndex);
                reader.Read(out speed);
                Data.SpeedDataManager.StoreOriginalSpeed(entityIndex, speed);
            }
            
            // Read custom speed roads
            int customCount;
            reader.Read(out customCount);
            for (int i = 0; i < customCount; i++)
            {
                int entityIndex;
                float speed;
                reader.Read(out entityIndex);
                reader.Read(out speed);
                Data.SpeedDataManager.AddCustomSpeedRoad(entityIndex, speed);
            }
            
            Mod.log.Info($"Deserialized: {originalCount} original speeds, {customCount} custom speeds");
        }
    }
}
