using Game;
using RoadSpeedAdjuster.Components;
using Unity.Entities;
using UnityEngine.Scripting;

namespace RoadSpeedAdjuster.Systems
{
    /// <summary>
    /// System that manages the RoadSpeedSaveData singleton component.
    /// This ensures our speed data is saved/loaded with the game.
    /// </summary>
    public partial class RoadSpeedSaveDataSystem : GameSystemBase
    {
        private Entity m_SaveDataEntity;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Create singleton entity to hold save data
            m_SaveDataEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(m_SaveDataEntity, new RoadSpeedSaveData());
            
            Mod.log.Info("RoadSpeedSaveDataSystem: Created save data entity");
        }

        [Preserve]
        protected override void OnUpdate()
        {
            // Nothing to do - serialization happens automatically
        }
    }
}
