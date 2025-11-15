using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Net;
using Game.Prefabs;
using Game.UI.InGame;
using RoadSpeedAdjuster.Extensions;
using RoadSpeedAdjuster.Utils;
using Unity.Entities;

namespace RoadSpeedAdjuster.Systems
{
    /// <summary>
    /// Adds a new custom info section to the Selected Info Panel
    /// when a road is selected. No logic yet — just UI binding.
    /// </summary>
    public partial class RoadSpeedAdjusterInfoPanelSystem : ExtendedInfoSectionBase
    {
        private PrefixedLogger m_Log;

        // Binding to send values to UI (float for now)
        private ValueBindingHelper<float> m_DummyValueBinding;

        // Access to vanilla UI system
        private SelectedInfoUISystem m_SelectedInfoUISystem;

        /// Display group (tab)
        protected override string group => "RoadSpeedAdjuster";

        /// Only show when selected entity is a road
        protected override bool displayForUnderConstruction => false;

        public override void OnWriteProperties(IJsonWriter writer)
        {
            // Nothing yet
        }

        protected override void OnProcess()
        {
            // No logic yet
        }

        protected override void Reset()
        {
            visible = false;
            m_DummyValueBinding.Value = 0f;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_Log = new PrefixedLogger(nameof(RoadSpeedAdjusterInfoPanelSystem));
            m_Log.Info("OnCreate");

            // Register this UI section
            m_InfoUISystem.AddMiddleSection(this);

            // Get vanilla UI system
            m_SelectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();

            // Create UI value binding
            m_DummyValueBinding = CreateBinding("INFOPANEL_ROAD_DUMMY_VALUE", 0f);

            // Create UI trigger → calls Method DummyTrigger()
            CreateTrigger("INFOPANEL_ROAD_DUMMY_TRIGGER", DummyTrigger);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // Check the selected entity
            var entity = selectedEntity;

            // Validate: does it have a Road component?
            /*if (EntityManager.Exists(entity) &&
            EntityManager.HasComponent<Segment>(entity))
            {
                visible = true;
            }
            else
            {
                visible = false;
            }*/
            visible = true;          // 👈 force it always visible
            m_DummyValueBinding.Value = 999f; // test value so you’ll see it

            RequestUpdate(); // Tell UI to refresh
        }

        private void DummyTrigger()
        {
            m_Log.Info("UI Trigger Clicked");
        }
    }
}
