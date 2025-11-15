using Colossal.UI.Binding;
using Game.UI.InGame;
using RoadSpeedAdjuster.Extensions;
using RoadSpeedAdjuster.Utils;
using Unity.Entities;

namespace RoadSpeedAdjuster.Systems
{
    public partial class RoadSpeedAdjusterInfoPanelSystem : ExtendedInfoSectionBase
    {
        private PrefixedLogger _log;

        // MUST use ValueBindingHelper, not ValueBinding
        private ValueBindingHelper<float> _speed;

        protected override string group => "RoadSpeedAdjuster";
        protected override bool displayForUnderConstruction => false;

        protected override void OnCreate()
        {
            base.OnCreate();

            _log = new PrefixedLogger("InfoPanelSystem");

            // Register our section in the SelectedInfoUISystem
            m_InfoUISystem.AddMiddleSection(this);

            // Create 2-way binding to the UI
            _speed = CreateBinding("INFOPANEL_ROAD_SPEED", 0f);

            _log.Info("Info panel system initialized");
        }

        protected override void OnUpdate()
        {
            // Always visible when road is selected
            visible = true;

            // Sync UI continuously
            _speed.Value = _speed.Value;

            RequestUpdate();
        }

        // ===== REQUIRED ABSTRACT METHODS =====

        protected override void Reset()
        {
            // Reset the value when selection changes
            _speed.Value = 0f;
        }

        protected override void OnProcess()
        {
            // Nothing to process, but must exist
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.TypeBegin("RoadSpeedAdjusterSection");
            writer.PropertyName("speed");
            writer.Write(_speed.Value);
            writer.TypeEnd();
        }

    }
}
