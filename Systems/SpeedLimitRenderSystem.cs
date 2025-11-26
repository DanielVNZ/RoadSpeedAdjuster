using Colossal.Mathematics;
using Game;
using Game.Net;
using Game.Rendering;
using Game.SceneFlow;
using Game.Settings;
using Game.City;
using Game.UI;
using RoadSpeedAdjuster.Components;
using RoadSpeedAdjuster.Extensions;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace RoadSpeedAdjuster.Systems
{
    /// <summary>
    /// Custom render system for speed limit text overlays.
    /// Uses direct mesh rendering (like AreaRenderSystem) instead of OverlayRenderSystem.
    /// This gives us full control over text appearance, color, and size.
    /// </summary>
    [Preserve]
    public partial class SpeedLimitRenderSystem : GameSystemBase
    {
        private struct TextMeshInfo
        {
            public Mesh Mesh;
            public Material Material;
            public int SpeedKmh;
        }

        private RenderingSystem m_RenderingSystem;
        private OverlayRenderSystem m_OverlayRenderSystem;
        private RoadSpeedToolSystem m_RoadSpeedToolSystem;
        private CityConfigurationSystem m_CityConfigurationSystem;
        
        private EntityQuery m_CustomSpeedQuery;
        
        // Cache of generated text meshes for each speed value
        private Dictionary<int, TextMeshInfo> m_TextMeshCache = new Dictionary<int, TextMeshInfo>();
        
        // Settings
        private Setting m_Settings;
        
        // Rendering
        private int m_FaceColorID;
        
        // Track last known theme to detect changes
        private string m_LastTheme = null;
        
        // Track last known setting to detect user changes
        private Setting.SpeedUnit m_LastUnitPreference = Setting.SpeedUnit.Auto;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_RenderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_RoadSpeedToolSystem = World.GetOrCreateSystemManaged<RoadSpeedToolSystem>();
            m_CityConfigurationSystem = World.GetOrCreateSystemManaged<CityConfigurationSystem>();
            
            // Get settings
            m_Settings = Mod.m_Setting;
            
            // Initialize last known unit preference
            m_LastUnitPreference = m_Settings?.SpeedUnitPreference ?? Setting.SpeedUnit.Auto;
            
            // Create query for roads with custom speeds
            m_CustomSpeedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Edge>(),
                    ComponentType.ReadOnly<Curve>(),
                    ComponentType.ReadOnly<CustomSpeed>()
                }
            });
            
            m_FaceColorID = Shader.PropertyToID("_FaceColor");
            
            // Hook into render pipeline
            RenderPipelineManager.beginContextRendering += Render;
        }

        [Preserve]
        protected override void OnDestroy()
        {
            RenderPipelineManager.beginContextRendering -= Render;
            
            // Clean up all cached meshes and materials
            ClearTextMeshCache();
            
            base.OnDestroy();
        }

        /// <summary>
        /// Clear all cached text meshes
        /// </summary>
        private void ClearTextMeshCache()
        {
            foreach (var meshInfo in m_TextMeshCache.Values)
            {
                if (meshInfo.Mesh != null)
                    Object.Destroy(meshInfo.Mesh);
                if (meshInfo.Material != null)
                    Object.Destroy(meshInfo.Material);
            }
            m_TextMeshCache.Clear();
        }

        [Preserve]
        protected override void OnUpdate()
        {
            // Check if theme changed (only when tool is active to avoid spam)
            if (m_RoadSpeedToolSystem != null && m_RoadSpeedToolSystem.IsActive)
            {
                string currentTheme = GetCurrentMapTheme();
                
                if (m_LastTheme != null && m_LastTheme != currentTheme)
                {
                    //Mod.log.Info($"Map theme changed from '{m_LastTheme}' to '{currentTheme}' - clearing text mesh cache");
                    ClearTextMeshCache();
                    m_LastTheme = currentTheme;
                }
                else if (m_LastTheme == null)
                {
                    m_LastTheme = currentTheme;
                    bool isEU = IsMapEuropean();
                    //Mod.log.Info($"Map theme detected: '{currentTheme}' ({(isEU ? "European/Metric" : "North American/Imperial")})");
                }
            }
            
            // Check if user changed the unit preference setting
            if (m_Settings != null)
            {
                Setting.SpeedUnit currentPreference = m_Settings.SpeedUnitPreference;
                
                if (currentPreference != m_LastUnitPreference)
                {
                    //Mod.log.Info($"Unit preference changed from '{m_LastUnitPreference}' to '{currentPreference}' - clearing text mesh cache");
                    ClearTextMeshCache();
                    m_LastUnitPreference = currentPreference;
                }
            }
        }

        private void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            try
            {
                // Only render when tool is active
                if (m_RoadSpeedToolSystem == null || !m_RoadSpeedToolSystem.IsActive)
                    return;

                // Don't render if overlays are hidden
                if (m_RenderingSystem.hideOverlay)
                    return;

                // Get all roads with custom speeds
                using var entities = m_CustomSpeedQuery.ToEntityArray(Allocator.Temp);
                
                if (entities.Length == 0)
                    return;

                // Render each speed limit text
                foreach (var edge in entities)
                {
                    if (!EntityManager.Exists(edge))
                        continue;

                    var customSpeed = EntityManager.GetComponentData<CustomSpeed>(edge);
                    int speedKmh = Mathf.RoundToInt(customSpeed.m_Speed);

                    // Get or create text mesh for this speed
                    if (!m_TextMeshCache.TryGetValue(speedKmh, out var meshInfo))
                    {
                        meshInfo = CreateTextMesh(speedKmh);
                        m_TextMeshCache[speedKmh] = meshInfo;
                    }

                    if (meshInfo.Mesh == null || meshInfo.Material == null)
                        continue;

                    // Get road position
                    var curve = EntityManager.GetComponentData<Curve>(edge);
                    float3 position = MathUtils.Position(curve.m_Bezier, 0.5f);
                    position.y += 10f; // 10 meters above road

                    // Render for each camera
                    foreach (Camera camera in cameras)
                    {
                        if (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView)
                        {
                            // Create transform matrix (facing camera)
                            Quaternion rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
                            
                            // Scale down to 0.6 to make text appear thinner and smaller
                            Vector3 scale = new Vector3(0.6f, 0.6f, 0.6f);
                            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);

                            // Draw the text mesh
                            Graphics.DrawMesh(
                                meshInfo.Mesh,
                                matrix,
                                meshInfo.Material,
                                0, // layer
                                camera,
                                0, // submesh index
                                null, // material property block
                                castShadows: false,
                                receiveShadows: false
                            );
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Mod.log.Error($"SpeedLimitRenderSystem.Render error: {ex}");
            }
        }

        private TextMeshInfo CreateTextMesh(int speedKmh)
        {
            try
            {
                // Detect if map is using EU or NA theme
                bool isEUMap = IsMapEuropean();
                
                // Determine which unit to show based on user preference
                bool showMetric = m_Settings?.ShouldShowMetric(isEUMap) ?? isEUMap;
                
                // Get the shared TextMeshPro component from OverlayRenderSystem
                TextMeshPro textMesh = m_OverlayRenderSystem.GetTextMesh();
                
                // Configure text appearance
                textMesh.rectTransform.sizeDelta = new Vector2(500f, 100f); 
                textMesh.fontSize = 35f; 
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.color = new Color(1f, 1f, 1f, 1f); // Opaque white
                
                // Don't use auto sizing - it causes inconsistent mesh generation
                textMesh.characterSpacing = 8f; 
                textMesh.fontStyle = FontStyles.Normal;
                
                // Generate speed text based on user preference
                string speedText;
                if (showMetric)
                {
                    // Show km/h
                    speedText = $"{speedKmh} km/h";
                }
                else
                {
                    // Convert to mph - use Math.Round for consistency with UI
                    // This ensures that km/h values stored from mph selections display correctly
                    int speedMph = Mathf.RoundToInt(speedKmh * 0.621371f);
                    speedText = $"{speedMph} mph";
                }
                
                TMP_TextInfo textInfo = textMesh.GetTextInfo(speedText);
                
                if (textInfo.meshInfo.Length == 0)
                {
                    //Mod.log.Warn($"No mesh info generated for speed {speedKmh}");
                    return default;
                }

                // Get the first mesh (most common case)
                TMP_MeshInfo tmpMeshInfo = textInfo.meshInfo[0];
                
                if (tmpMeshInfo.vertexCount == 0)
                {
                    //Mod.log.Warn($"No vertices generated for speed {speedKmh}");
                    return default;
                }

                // Create Unity mesh from TextMeshPro data
                Mesh mesh = new Mesh();
                mesh.name = $"SpeedLimit_{speedKmh}_{(showMetric ? "kmh" : "mph")}";
                mesh.vertices = tmpMeshInfo.vertices;
                mesh.triangles = tmpMeshInfo.triangles;
                mesh.uv = tmpMeshInfo.uvs0;
                mesh.uv2 = tmpMeshInfo.uvs2;
                mesh.colors32 = tmpMeshInfo.colors32;
                mesh.RecalculateBounds();

                // Create material with proper texture and settings
                Material material = new Material(tmpMeshInfo.material);
                material.name = $"SpeedLimitMaterial_{speedKmh}_{(showMetric ? "kmh" : "mph")}";
                material.SetColor(m_FaceColorID, new Color(1f, 1f, 1f, 1f)); // Opaque white
                
                // Slim, clean text — remove the bold SDF expansion
                material.SetFloat("_FaceDilate", 0f);
                material.SetFloat("_OutlineWidth", 0f);
                material.SetFloat("_GlowPower", 0f);
                material.SetFloat("_WeightNormal", 0f);
                material.SetFloat("_WeightBold", 0f);
                
                // Preserve correct SDF font rendering setup
                m_OverlayRenderSystem.CopyFontAtlasParameters(tmpMeshInfo.material, material);

                return new TextMeshInfo
                {
                    Mesh = mesh,
                    Material = material,
                    SpeedKmh = speedKmh
                };
            }
            catch (System.Exception ex)
            {
                Mod.log.Error($"Failed to create text mesh for speed {speedKmh}: {ex}");
                return default;
            }
        }

        /// <summary>
        /// Get the current map's theme name
        /// </summary>
        private string GetCurrentMapTheme()
        {
            try
            {
                if (m_CityConfigurationSystem?.defaultTheme != Entity.Null)
                {
                    var prefabSystem = World.GetOrCreateSystemManaged<Game.Prefabs.PrefabSystem>();
                    var theme = prefabSystem.GetPrefab<Game.Prefabs.ThemePrefab>(m_CityConfigurationSystem.defaultTheme);
                    return theme?.name ?? "Unknown";
                }
                
                return "Unknown";
            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Failed to get map theme: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Determine if the current map is using a European theme
        /// </summary>
        private bool IsMapEuropean()
        {
            string theme = GetCurrentMapTheme();
            
            // Game uses exactly "North American" or "European" as theme names
            bool isNA = theme.Equals("North American", System.StringComparison.Ordinal);
            
            return !isNA; // If not "North American", assume European (default)
        }

        [Preserve]
        public SpeedLimitRenderSystem()
        {
        }
    }
}
