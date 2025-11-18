using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Game.Common;
using Game.Net;
using Game.SceneFlow;
using Game.Tools;
using Game.UI;
using RoadSpeedAdjuster.Components;
using RoadSpeedAdjuster.Utils;
using Unity.Entities;
using UnityEngine;

namespace RoadSpeedAdjuster.Systems
{
    public partial class RoadSpeedToolUISystem : UISystemBase
    {
        private PrefixedLogger _log;
        
        private ToolSystem _toolSystem;
        private EntitySelectorToolSystem _entitySelectorTool;
        private DefaultToolSystem _defaultTool;
        
        private Entity _selectedEntity;
        private bool _changingSpeed;
        private List<float> _speeds = new List<float>();
        
        private ValueBinding<bool> _toolActiveBinding;
        private ValueBinding<float> _speedBinding;
        private ValueBinding<bool> _visibleBinding;
        
        public bool ToolActive
        {
            get => _toolActiveBinding.value;
            set
            {
                if (value == _toolActiveBinding.value)
                    return;
                _toolActiveBinding.Update(value);
                _toolSystem.activeTool = _toolActiveBinding.value ? _entitySelectorTool : _defaultTool;
                _toolSystem.selected = Entity.Null;
            }
        }
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            _log = new PrefixedLogger("RoadSpeedToolUI");
            
            // Create bindings
            AddBinding(_toolActiveBinding = new ValueBinding<bool>(Mod.Id, "toolActive", false));
            AddBinding(_speedBinding = new ValueBinding<float>(Mod.Id, "BINDING:INFOPANEL_ROAD_SPEED", 50f));
            AddBinding(_visibleBinding = new ValueBinding<bool>(Mod.Id, "BINDING:INFOPANEL_VISIBLE", false));
            AddBinding(new TriggerBinding<float>(Mod.Id, "TRIGGER:INFOPANEL_ROAD_SPEED", HandleSpeedChange));
            AddBinding(new TriggerBinding(Mod.Id, "TRIGGER:ToggleTool", HandleToggleTool));
            
            // Get tool systems
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            _entitySelectorTool = World.GetOrCreateSystemManaged<EntitySelectorToolSystem>();
            _defaultTool = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            
            _selectedEntity = Entity.Null;
            
            _log.Info("RoadSpeedToolUISystem created");
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            if (_changingSpeed)
                return;
            
            // Check if selected entity changed
            if (_selectedEntity != _toolSystem.selected)
            {
                if (ToolActive && _toolSystem.activeTool != _entitySelectorTool)
                    _toolSystem.activeTool = _entitySelectorTool;
                
                if (_toolSystem.selected == Entity.Null)
                {
                    // No entity selected
                    _selectedEntity = Entity.Null;
                    _speedBinding.Update(50f);
                    _visibleBinding.Update(false);
                    return;
                }
                
                // New entity selected
                var entity = _toolSystem.selected;
                float averageSpeed = GetAverageSpeed(entity);
                
                if (averageSpeed > 0)
                {
                    _selectedEntity = entity;
                    _speedBinding.Update(averageSpeed);
                    _visibleBinding.Update(true);
                    _log.Info($"Road selected: {entity.Index}, Speed: {averageSpeed}");
                }
                else
                {
                    _selectedEntity = Entity.Null;
                    _speedBinding.Update(50f);
                    _visibleBinding.Update(false);
                }
            }
            else if (ToolActive && _toolSystem.activeTool != _entitySelectorTool)
            {
                ToolActive = false;
            }
        }
        
        private float GetAverageSpeed(Entity entity)
        {
            _speeds.Clear();
            
            try
            {
                if (EntityManager.TryGetBuffer<SubLane>(entity, true, out var subLanes))
                {
                    foreach (var subLane in subLanes)
                    {
                        var ignoreFlags = CarLaneFlags.Unsafe | CarLaneFlags.SideConnection;
                        
                        if (EntityManager.TryGetComponent(subLane.m_SubLane, out CarLane carLane) && 
                            ((carLane.m_Flags & ignoreFlags) != ignoreFlags))
                        {
                            _speeds.Add(carLane.m_SpeedLimit);
                        }
                        
                        if (EntityManager.TryGetComponent(subLane.m_SubLane, out TrackLane trackLane))
                        {
                            _speeds.Add(trackLane.m_SpeedLimit);
                        }
                    }
                }
                
                return _speeds.Any() ? _speeds.Average() : -1f;
            }
            finally
            {
                _speeds.Clear();
            }
        }
        
        private void HandleSpeedChange(float newSpeed)
        {
            if (_selectedEntity == Entity.Null || newSpeed <= 0)
            {
                _log.Warn("Speed change requested but no valid entity selected");
                return;
            }
            
            _changingSpeed = true;
            
            try
            {
                // Handle Temp entities (entities being edited)
                if (EntityManager.TryGetComponent(_selectedEntity, out Temp temp))
                {
                    if (!EntityManager.HasComponent<CustomSpeed>(temp.m_Original))
                        EntityManager.AddComponent<CustomSpeed>(temp.m_Original);
                    
                    EntityManager.SetComponentData(temp.m_Original, new CustomSpeed { m_Speed = newSpeed });
                    
                    if (!EntityManager.HasComponent<Updated>(temp.m_Original))
                        EntityManager.AddComponent<Updated>(temp.m_Original);
                }
                else
                {
                    if (!EntityManager.HasComponent<CustomSpeed>(_selectedEntity))
                        EntityManager.AddComponent<CustomSpeed>(_selectedEntity);
                    
                    EntityManager.SetComponentData(_selectedEntity, new CustomSpeed { m_Speed = newSpeed });
                    
                    if (!EntityManager.HasComponent<Updated>(_selectedEntity))
                        EntityManager.AddComponent<Updated>(_selectedEntity);
                }
                
                _log.Info($"Speed changed to {newSpeed}");
            }
            finally
            {
                _changingSpeed = false;
            }
        }
        
        private void HandleToggleTool()
        {
            ToolActive = !ToolActive;
            _log.Info($"Tool active: {ToolActive}");
        }
    }
}
