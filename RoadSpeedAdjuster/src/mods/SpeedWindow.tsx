import { useState, useEffect } from "react";
import { useValue } from "cs2/api";
import { INITIAL_SPEED, VISIBLE, ApplySpeed } from "./bindings";
import { getModule } from "cs2/modding";
import { Slider } from "../slider/slider";

// load vanilla
const Panel: any = getModule("game-ui/common/panel/panel.tsx", "Panel");
const PanelTheme: any = getModule(
    "game-ui/common/panel/themes/default.module.scss",
    "classes"
);

export const SpeedPanel = () => {
    const visible = useValue(VISIBLE);
    const initialValue = useValue(INITIAL_SPEED);

    // Internal UI-only state
    const [pending, setPending] = useState(initialValue);

    // When a new road is selected → update slider state
    useEffect(() => {
        setPending(initialValue);
    }, [initialValue]);

    if (!visible) return null;

    return (
        <Panel
            header="Road Speed Limit"
            className={PanelTheme.panel}
            style={{
                position: "absolute",
                right: "10rem",
                top: "10rem",
                width: "400rem",
            }}
        >
            <div style={{ padding: "10rem" }}>
                <div style={{ marginBottom: "5rem", fontSize: "14rem" }}>
                    Speed: {Math.round(pending)} km/h
                </div>

                <Slider
                    start={5}
                    end={300}
                    value={pending}
                    onChange={(v: number) => setPending(v)}
                />

                <button
                    style={{
                        marginTop: "10rem",
                        padding: "8rem",
                        width: "100%",
                        fontSize: "14rem",
                        cursor: "pointer"
                    }}
                    onClick={() => ApplySpeed(pending)}
                >
                    Apply
                </button>
            </div>
        </Panel>
    );
};
