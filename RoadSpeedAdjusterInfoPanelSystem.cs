using Colossal.Entities;
using Colossal.UI.Binding;
using Game.UI.InGame;
using RoadSpeedAdjuster.Extensions;
using RoadSpeedAdjuster.Utils;
using Unity.Entities;

namespace RoadSpeedAdjuster.Systems
{
    [UpdateInGroup(typeof(Game.UI.InGame.SelectedInfoUISystem))]
    public partial class RoadSpeedAdjusterInfoPanelSystem : ExtendedInfoSectionBase
    {
        private PrefixedLogger m_Log;

        private ValueBindingHelper<float> m_DummyValueBinding;

        private SelectedInfoUISystem m_SelectedInfoUISystem;

        protected override string group => "RoadSpeedAdjuster";

        protected override bool displayForUnderConstruction => false;

        public override void OnWriteProperties(IJsonWriter writer) { }

        protected override void OnProcess() { }

        protected override void Reset()
        {
            visible = false;

            if (m_DummyValueBinding != null)
                m_DummyValueBinding.Value = 10f;   // safe slider min
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_Log = new PrefixedLogger(nameof(RoadSpeedAdjusterInfoPanelSystem));
            m_Log.Info("OnCreate");

            m_InfoUISystem.AddMiddleSection(this);

            m_SelectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();

            // -----------------------------
            // Binding (UI reads this)
            // -----------------------------
            m_DummyValueBinding = CreateBinding("INFOPANEL_ROAD_DUMMY_VALUE", 10f);

            // initial safe update inside slider range
            m_DummyValueBinding.Value = 10f;

            // -----------------------------
            // Trigger (UI writes this)
            // -----------------------------
            CreateTrigger<float>("INFOPANEL_ROAD_DUMMY_VALUE_CHANGED", OnDummyValueChanged);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // force visible while debugging
            visible = true;

            // re-push current value to UI every frame (stability requirement)
            m_DummyValueBinding.Value = m_DummyValueBinding.Value;

            RequestUpdate();
        }

        private void OnDummyValueChanged(float newValue)
        {
            m_Log.Info($"Slider changed: {newValue}");

            // reflect slider change back to UI
            m_DummyValueBinding.Value = newValue;

            // future: apply to road
        }
    }
}
