import React, { useEffect, useState, useRef } from "react";
import { VC, VT } from "./Components";
import { SPEED_VALUE, SetSpeedValue } from "./bindings";
import { Slider } from "../slider/slider";

export const RoadSpeedSelectedInfoPanelComponent = (componentList: any) => {

    const Component: React.FC = () => {
        const [value, setValue] = useState(10);
        const dragging = useRef(false);

        // Poll C# → UI (SPEED_VALUE) but do NOT stomp local value while dragging
        useEffect(() => {
            let last = value;

            const interval = setInterval(() => {
                if (dragging.current) return;

                try {
                    const v = SPEED_VALUE.value;
                    if (typeof v === "number" && !Number.isNaN(v) && v !== last) {
                        last = v;
                        setValue(v);
                    }
                } catch {
                    // ignore binding errors
                }
            }, 120);

            return () => clearInterval(interval);
        }, []);

        const handleChange = (v: number) => {
            dragging.current = true;
            setValue(v);

            // UI → C#
            SetSpeedValue(v);

            // small delay so polling loop doesn't immediately overwrite while sliding
            setTimeout(() => {
                dragging.current = false;
            }, 80);
        };

        return (
            <VC.InfoSection disableFocus={true}>
                <VC.InfoRow
                    left="Speed Limit"
                    right={
                        <div
                            style={{
                                width: "100%",
                                padding: "4px 0",
                                display: "flex",
                                alignItems: "center",
                            }}
                        >
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

    // Hook our React panel to the C# info section system
    componentList["RoadSpeedAdjuster.Systems.RoadSpeedAdjusterInfoPanelSystem"] = Component;
    return componentList;
};
