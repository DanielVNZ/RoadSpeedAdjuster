using Colossal.Mathematics;
using Game;
using Game.Net;
using Game.Rendering;
using Game.SceneFlow;
using Game.Settings;
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
        
        private EntityQuery m_CustomSpeedQuery;
        
        // Cache of generated text meshes for each speed value
        private Dictionary<int, TextMeshInfo> m_TextMeshCache = new Dictionary<int, TextMeshInfo>();
        
        // Settings
        private Setting m_Settings;
        
        // Rendering
        private int m_FaceColorID;
        
        // Track last known unit system to detect changes
        private InterfaceSettings.UnitSystem? m_LastUnitSystem = null;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_RenderingSystem = World.GetOrCreateSystemManaged<RenderingSystem>();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_RoadSpeedToolSystem = World.GetOrCreateSystemManaged<RoadSpeedToolSystem>();
            
            // Get settings
            m_Settings = Mod.m_Setting;
            
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
            
            Mod.log.Info("SpeedLimitRenderSystem created - using direct mesh rendering");
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
            Mod.log.Info("Text mesh cache cleared");
        }

        [Preserve]
        protected override void OnUpdate()
        {
            // Check if unit system changed (poll every frame when tool is active)
            if (m_RoadSpeedToolSystem != null && m_RoadSpeedToolSystem.IsActive)
            {
                var currentUnitSystem = GetCurrentUnitSystem();
                
                if (m_LastUnitSystem.HasValue && m_LastUnitSystem.Value != currentUnitSystem)
                {
                    Mod.log.Info($"Unit system changed from {m_LastUnitSystem.Value} to {currentUnitSystem} - clearing text mesh cache");
                    ClearTextMeshCache();
                    m_LastUnitSystem = currentUnitSystem;
                }
                else if (!m_LastUnitSystem.HasValue)
                {
                    m_LastUnitSystem = currentUnitSystem;
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
                // Detect if using metric or imperial from game settings
                var currentUnitSystem = GetCurrentUnitSystem();
                bool isMetric = currentUnitSystem == InterfaceSettings.UnitSystem.Metric;
                
                // Determine which unit to show based on user preference
                bool showMetric = m_Settings?.ShouldShowMetric(isMetric) ?? isMetric;
                
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
                    // Convert to mph
                    int speedMph = Mathf.RoundToInt(speedKmh * 0.621371f);
                    speedText = $"{speedMph} mph";
                }
                
                TMP_TextInfo textInfo = textMesh.GetTextInfo(speedText);
                
                if (textInfo.meshInfo.Length == 0)
                {
                    Mod.log.Warn($"No mesh info generated for speed {speedKmh}");
                    return default;
                }

                // Get the first mesh (most common case)
                TMP_MeshInfo tmpMeshInfo = textInfo.meshInfo[0];
                
                if (tmpMeshInfo.vertexCount == 0)
                {
                    Mod.log.Warn($"No vertices generated for speed {speedKmh}");
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

                Mod.log.Info($"Created text mesh for speed {speedKmh}: {tmpMeshInfo.vertexCount} vertices (showing {(showMetric ? "metric" : "imperial")} primary, unit system: {currentUnitSystem})");

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
        /// Get the current unit system from game settings
        /// </summary>
        private InterfaceSettings.UnitSystem GetCurrentUnitSystem()
        {
            try
            {
                // Use SharedSettings.instance.userInterface (as shown in OptionsUISystem)
                var interfaceSettings = SharedSettings.instance?.userInterface;
                
                if (interfaceSettings != null)
                {
                    return interfaceSettings.unitSystem;
                }
                
                Mod.log.Warn("SharedSettings.instance.userInterface is null, defaulting to Metric");
                return InterfaceSettings.UnitSystem.Metric;
            }
            catch (System.Exception ex)
            {
                Mod.log.Error($"Exception in GetCurrentUnitSystem: {ex}");
                return InterfaceSettings.UnitSystem.Metric;
            }
        }

        [Preserve]
        public SpeedLimitRenderSystem()
        {
        }
    }
}
