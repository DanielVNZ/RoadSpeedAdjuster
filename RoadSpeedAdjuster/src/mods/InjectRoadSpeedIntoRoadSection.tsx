import React from "react";
import { VC, VF } from "./Components";
import { ROAD_DUMMY_VALUE } from "./bindings";

// Mutator: receives component map, adds our entry, returns it
export const RoadSpeedSelectedInfoPanelComponent = (componentList: any) => {
    const Component: React.FC = () => {
        return (
            <VC.InfoSection focusKey={VF.FOCUS_DISABLED} disableFocus={true}>
                <VC.InfoRow
                    left="Speed Limit"
                    right={`${ROAD_DUMMY_VALUE.value} km/h`}
                    disableFocus={true}
                />
            </VC.InfoSection>
        );
    };

    // MUST exactly match your C# system name
    componentList["RoadSpeedAdjuster.Systems.RoadSpeedAdjusterInfoPanelSystem"] = Component;

    return componentList;
};
