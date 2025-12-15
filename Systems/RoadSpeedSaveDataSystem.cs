using Game;
using Game.SceneFlow;
using RoadSpeedAdjuster.Components;
using RoadSpeedAdjuster.Data;
using Unity.Entities;
using UnityEngine.Scripting;

namespace RoadSpeedAdjuster.Systems
{
    /// <summary>
    /// System that manages persistent JSON storage for road speeds.
    /// Initializes per-city JSON storage when a game is loaded.
    /// Storage is based on city name, so settings persist across sessions for the same city.
    /// </summary>
    public partial class RoadSpeedSaveDataSystem : GameSystemBase
    {
        private GameManager _gameManager;
        private bool _initialized = false;
        private string _lastCityName = null;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            
            _gameManager = GameManager.instance;
            
            //Mod.log.Info("RoadSpeedSaveDataSystem: System created");
        }

        [Preserve]
        protected override void OnUpdate()
        {
            // Initialize persistent storage once the game is loaded
            // Check if we're in Game or Editor mode (not MainMenu) and world is ready
            if (!_initialized && _gameManager != null && 
                _gameManager.state >= GameManager.State.WorldReady &&
                (_gameManager.gameMode == GameMode.Game || _gameManager.gameMode == GameMode.Editor))
            {
                InitializePersistentStorage();
                _initialized = true;
            }
            // Reset if we go back to main menu
            else if (_initialized && _gameManager.gameMode == GameMode.MainMenu)
            {
                _initialized = false;
                _lastCityName = null;
                //Mod.log.Info("Returned to main menu - storage will reinitialize on next game load");
            }
        }
        
        /// <summary>
        /// Initialize the persistent JSON storage for the current city.
        /// Uses the city name from the game's CityConfigurationSystem.
        /// </summary>
        private void InitializePersistentStorage()
        {
            try
            {
                // Get the city name from the game
                string cityName = GetCityName();
                
                // Sanitize the city name for use as a filename
                cityName = SanitizeFileName(cityName);
                
                // Only initialize if this is a different city
                if (_lastCityName == cityName)
                {
                    //Mod.log.Info($"Already initialized for city: '{cityName}'");
                    return;
                }
                
                _lastCityName = cityName;
                
                //Mod.log.Info($"Initializing persistent storage for city: '{cityName}'");
                
                // Initialize the JSON storage with city name
                // The storage will create/load: CityName.json
                PersistentSpeedStorage.Initialize(cityName, cityName);
                
                //Mod.log.Info("Persistent storage initialized successfully");
                //Mod.log.Info(PersistentSpeedStorage.GetStats());
            }
            catch (System.Exception ex)
            {
                Mod.log.Error($"Failed to initialize persistent storage: {ex}");
            }
        }
        
        /// <summary>
        /// Get the city name from the CityConfigurationSystem.
        /// Falls back to a default name if the system is not available.
        /// </summary>
        private string GetCityName()
        {
            try
            {
                // Try to get the CityConfigurationSystem which holds the city name
                var cityConfigSystem = World.GetExistingSystemManaged<Game.City.CityConfigurationSystem>();
                if (cityConfigSystem != null)
                {
                    // Access the city name through the system
                    string name = cityConfigSystem.cityName;
                    if (!string.IsNullOrEmpty(name))
                    {
                        //Mod.log.Info($"Retrieved city name from CityConfigurationSystem: '{name}'");
                        return name;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Could not retrieve city name from CityConfigurationSystem: {ex.Message}");
            }
            
            // Fallback to a default name based on game mode
            string fallbackName = _gameManager.gameMode == GameMode.Game ? "City" : "EditorCity";
            //Mod.log.Info($"Using fallback city name: '{fallbackName}'");
            return fallbackName;
        }
        
        /// <summary>
        /// Sanitize a filename by removing or replacing invalid characters.
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "UnnamedCity";
            }
            
            // Remove invalid filename characters
            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }
            
            // Also replace some additional problematic characters
            fileName = fileName.Replace(' ', '_');
            fileName = fileName.Replace('.', '_');
            
            // Limit length to avoid filesystem issues
            if (fileName.Length > 100)
            {
                fileName = fileName.Substring(0, 100);
            }
            
            return fileName;
        }
        
        [Preserve]
        protected override void OnDestroy()
        {
            // Save data when system is destroyed
            if (_initialized)
            {
                //Mod.log.Info("Saving road speed data before system destruction");
                PersistentSpeedStorage.Save();
            }
            
            base.OnDestroy();
        }
    }
}
