import React, { useEffect, useState, useRef } from "react";
import { VC, VF, VT } from "./Components";
import { ROAD_DUMMY_VALUE, SetRoadDummyValue } from "./bindings";
import { Slider } from "../slider/slider";

export const RoadSpeedSelectedInfoPanelComponent = (componentList: any) => {

    const Component: React.FC = () => {

        const [value, setValue] = useState(10);
        const dragging = useRef(false);

        useEffect(() => {
            let last = value;

            const interval = setInterval(() => {
                if (dragging.current) return;

                try {
                    const v = ROAD_DUMMY_VALUE.value;
                    if (typeof v === "number" && v !== last) {
                        last = v;
                        setValue(v);
                    }
                } catch { }
            }, 120);

            return () => clearInterval(interval);
        }, []);

        const handleChange = (v: number) => {
            dragging.current = true;
            setValue(v);
            SetRoadDummyValue(v);
            setTimeout(() => dragging.current = false, 80);
        };

        return (
            <VC.InfoSection disableFocus={true}>
                <VC.InfoRow
                    left="Speed Limit"
                    right={
                        <div style={{
                            width: "100%",
                            padding: "4px 0",
                            display: "flex",
                            alignItems: "center",
                        }}>
                            <Slider
                                start={10}
                                end={200}
                                value={value}
                                onChange={handleChange}
                                className={VT.slider?.slider}
                                theme={VT.slider}
                            />
                        </div>
                    }
                />
            </VC.InfoSection>
        );
    };

    componentList["RoadSpeedAdjuster.Systems.RoadSpeedAdjusterInfoPanelSystem"] = Component;
    return componentList;
};
