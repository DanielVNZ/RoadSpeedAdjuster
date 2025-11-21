import { useState, useEffect, useRef } from "react";
import { useValue } from "cs2/api";
import { INITIAL_SPEED, TOOL_ACTIVE, SELECTION_COUNTER, ApplySpeed, ResetSpeed } from "./bindings";
import { getModule } from "cs2/modding";
import { VanillaComponentResolver } from "./VanillaComponentResolver";
import { Button } from "./Button";
import { Slider } from "../slider/slider";

// Load vanilla components
const Panel: any = getModule("game-ui/common/panel/panel.tsx", "Panel");
const PanelTheme: any = getModule(
    "game-ui/common/panel/themes/default.module.scss",
    "classes"
);

export const RoadSpeedWindow = () => {
    // Safely access binding value with error handling
    let initialSpeed = 50;
    try {
        const value = useValue(INITIAL_SPEED);
        initialSpeed = value ?? 50;
    } catch (e) {
        // Binding not ready yet, use default
        initialSpeed = 50;
    }
    
    // Watch tool active state
    let toolActive = false;
    try {
        const value = useValue(TOOL_ACTIVE);
        toolActive = value ?? false;
    } catch (e) {
        toolActive = false;
    }
    
    // Watch selection counter
    let selectionCounter = 0;
    try {
        const value = useValue(SELECTION_COUNTER);
        selectionCounter = value ?? 0;
    } catch (e) {
        selectionCounter = 0;
    }
    
    const [visible, setVisible] = useState(false);
    const [pendingSpeed, setPendingSpeed] = useState(50);
    const [isApplying, setIsApplying] = useState(false);
    const [isResetting, setIsResetting] = useState(false);
    const dragging = useRef(false);
    const lastToolActive = useRef(false);
    const lastSelectionCounter = useRef(0);

    const resolver = VanillaComponentResolver.instance;
    const FOCUS_DISABLED = resolver.FOCUS_DISABLED;

    // Convert km/h to mph
    const kmhToMph = (kmh: number): number => {
        return Math.round(kmh * 0.621371);
    };

    // Hide panel when tool becomes inactive
    useEffect(() => {
        console.log(`[RoadSpeedWindow] Tool active changed: ${lastToolActive.current} -> ${toolActive}`);
        
        if (!toolActive && lastToolActive.current) {
            console.log("[RoadSpeedWindow] Tool deactivated - hiding panel");
            setVisible(false);
            lastSelectionCounter.current = 0;
        }
        
        lastToolActive.current = toolActive;
    }, [toolActive]);

    // Show panel when selection counter changes (new selection made)
    useEffect(() => {
        console.log(`[RoadSpeedWindow] Selection counter changed: ${lastSelectionCounter.current} -> ${selectionCounter}, speed=${initialSpeed}, toolActive=${toolActive}`);
        
        // Only process if tool is active
        if (!toolActive) {
            console.log("[RoadSpeedWindow] Tool not active, ignoring selection");
            return;
        }
        
        // Selection counter increased means a new selection was made
        if (selectionCounter > 0 && selectionCounter !== lastSelectionCounter.current) {
            console.log(`[RoadSpeedWindow] NEW SELECTION DETECTED! Counter: ${lastSelectionCounter.current} -> ${selectionCounter}, Speed: ${initialSpeed}`);
            
            if (typeof initialSpeed === "number" && initialSpeed >= 5 && initialSpeed <= 140) {
                setVisible(true);
                setPendingSpeed(initialSpeed);
                lastSelectionCounter.current = selectionCounter;
                console.log("[RoadSpeedWindow] Panel now visible");
            }
        }
    }, [selectionCounter, initialSpeed, toolActive]);

    const handleSliderChange = (value: number) => {
        dragging.current = true;
        const roundedValue = Math.round(value / 5) * 5;
        setPendingSpeed(roundedValue);
        // Keep dragging flag true for a bit longer to prevent snap-back
        setTimeout(() => {
            dragging.current = false;
        }, 100);
    };

    const handleApply = () => {
        setIsApplying(true);
        ApplySpeed(pendingSpeed);
        setTimeout(() => setIsApplying(false), 500);
    };

    const handleReset = () => {
        setIsResetting(true);
        ResetSpeed();
        setTimeout(() => setIsResetting(false), 500);
    };

    if (!visible) return null;

    return (
        <Panel
            header={<span style={{ paddingLeft: "10rem" }}>Road Speed Adjuster</span>}
            className={PanelTheme.panel}
            onClose={() => setVisible(false)}
            style={{
                position: "absolute",
                right: "10rem",
                top: "10rem",
                width: "400rem",
            }}
        >
            <div style={{ padding: "16rem" }}>
                {/* Current Speed Display */}
                <div style={{
                    display: "flex",
                    justifyContent: "space-between",
                    marginBottom: "12rem",
                    fontSize: "14rem"
                }}>
                    <span style={{ fontWeight: "bold" }}>Current Speed Limit:</span>
                    <span>{Math.round(pendingSpeed)} km/h ({kmhToMph(pendingSpeed)} mph)</span>
                </div>

                {/* Speed Slider */}
                <div style={{ marginBottom: "12rem" }}>
                    <div style={{ marginBottom: "8rem", fontSize: "14rem", fontWeight: "bold" }}>
                        Adjust Speed Limit
                    </div>
                    <Slider
                        start={5}
                        end={140}
                        step={5}
                        value={pendingSpeed}
                        onChange={handleSliderChange}
                    />
                </div>

                {/* Buttons */}
                <div style={{
                    display: "flex"
                }}>
                    <div style={{ flex: 1, marginRight: "8rem" }}>
                        <Button
                            focusKey={FOCUS_DISABLED}
                            selected={isApplying}
                            disabled={isApplying}
                            onSelect={handleApply}
                        >
                            {isApplying ? "✓ Applied" : "Apply"}
                        </Button>
                    </div>
                    <div style={{ flex: 1, marginLeft: "8rem" }}>
                        <Button
                            focusKey={FOCUS_DISABLED}
                            selected={isResetting}
                            disabled={isResetting}
                            onSelect={handleReset}
                        >
                            {isResetting ? "✓ Reset" : "Reset"}
                        </Button>
                    </div>
                </div>
            </div>
        </Panel>
    );
};
