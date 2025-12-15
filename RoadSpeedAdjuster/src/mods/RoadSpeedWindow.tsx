import { useState, useEffect, useRef } from "react";
import { useValue, trigger } from "cs2/api";
import { TOOL_ACTIVE, SHOW_METRIC, SELECTION_COUNTER, IS_TRACK_TYPE, UNIT_MODE, DOUBLE_SPEED_DISPLAY, ApplySpeed, ResetSpeed, ToggleUnit, getSharedWindowPosition, setSharedWindowPosition } from "./bindings";
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
    // Watch tool active state
    let toolActive = false;
    try {
        const value = useValue(TOOL_ACTIVE);
        toolActive = value ?? false;
    } catch (e) {
        toolActive = false;
    }
    
    // Watch unit system (true = metric/km/h, false = imperial/mph)
    let showMetric = true;
    try {
        const value = useValue(SHOW_METRIC);
        showMetric = value ?? true;
    } catch (e) {
        showMetric = true;
    }
    
    // Watch track type (true = train/tram/subway, false = road)
    let isTrackType = false;
    try {
        const value = useValue(IS_TRACK_TYPE);
        isTrackType = value ?? false;
    } catch (e) {
        isTrackType = false;
    }
    
    // Watch unit mode (0 = Auto, 1 = Metric, 2 = Imperial)
    let unitMode = 0;
    try {
        const value = useValue(UNIT_MODE);
        unitMode = value ?? 0;
    } catch (e) {
        unitMode = 0;
    }
    
    // Watch double speed display setting
    let doubleSpeedDisplay = false;
    try {
        const value = useValue(DOUBLE_SPEED_DISPLAY);
        doubleSpeedDisplay = value ?? false;
    } catch (e) {
        doubleSpeedDisplay = false;
    }
    
    // Watch selection counter to detect when user selects a new road
    let selectionCounter = 0;
    try {
        const value = useValue(SELECTION_COUNTER);
        selectionCounter = value ?? 0;
    } catch (e) {
        selectionCounter = 0;
    }
    
    const [visible, setVisible] = useState(false);
    const [pendingSpeedKmh, setPendingSpeedKmh] = useState(5); // Always store as km/h internally
    const [isApplying, setIsApplying] = useState(false);
    const [isResetting, setIsResetting] = useState(false);
    const lastSelectionCounter = useRef(0);
    
    // Dragging state (use shared position)
    const [position, setPosition] = useState(getSharedWindowPosition());
    const [isDragging, setIsDragging] = useState(false);
    const dragRef = useRef({ startX: 0, startY: 0, initialX: 0, initialY: 0 });
    
    // Hover state for close button
    const [isCloseHovered, setIsCloseHovered] = useState(false);
    
    // Hover state for help icon
    const [isHelpHovered, setIsHelpHovered] = useState(false);

    const resolver = VanillaComponentResolver.instance;
    const FOCUS_DISABLED = resolver.FOCUS_DISABLED;

    // Convert km/h to mph (maintaining precision for display)
    const kmhToMph = (kmh: number): number => {
        return Math.round(kmh * 0.621371);
    };
    
    // Convert mph to km/h (ensuring the result will round back correctly)
    const mphToKmh = (mph: number): number => {
        // Use exact conversion to ensure round-trip accuracy
        // mph / 0.621371 gives us the km/h value
        const kmh = mph / 0.621371;
        // Round to nearest integer to avoid floating point errors
        return Math.round(kmh);
    };
    
    // Get default speed based on current unit system
    const getDefaultSpeed = (): number => {
        if (showMetric) {
            return 5; // 5 km/h actual (will display as 10 if doubling enabled)
        } else {
            return mphToKmh(5); // 5 mph actual converted to km/h (~8 km/h actual)
        }
    };

    // Show panel ONLY when a road selection is made (selection counter increases)
    useEffect(() => {
        if (toolActive && selectionCounter > 0 && selectionCounter !== lastSelectionCounter.current) {
            const defaultSpeed = getDefaultSpeed();
            setVisible(true);
            setPendingSpeedKmh(defaultSpeed); // Reset to 5 in current units
            lastSelectionCounter.current = selectionCounter;
        } else if (!toolActive || selectionCounter === 0) {
            // Hide panel when tool is deactivated OR when selection counter is reset to 0
            setVisible(false);
            lastSelectionCounter.current = 0; // Reset counter
        }
    }, [selectionCounter, toolActive, showMetric]);

    const handleSliderChange = (value: number) => {
        if (showMetric) {
            // Metric mode: if doubling is enabled, divide by 2 to get actual km/h
            const actualKmh = doubleSpeedDisplay ? value / 2 : value;
            const roundedValue = Math.round(actualKmh / 5) * 5;
            setPendingSpeedKmh(roundedValue);
        } else {
            // Imperial mode: if doubling is enabled, divide by 2 to get actual mph
            const actualMph = doubleSpeedDisplay ? value / 2 : value;
            const roundedMph = Math.round(actualMph / 5) * 5;
            const kmh = mphToKmh(roundedMph);
            setPendingSpeedKmh(kmh);
        }
    };

    const handleApply = () => {
        setIsApplying(true);
        ApplySpeed(pendingSpeedKmh); // Always send km/h to C#
        setTimeout(() => setIsApplying(false), 500);
    };

    const handleReset = () => {
        setIsResetting(true);
        ResetSpeed();
        setTimeout(() => {
            setIsResetting(false);
            const defaultSpeed = getDefaultSpeed();
            setPendingSpeedKmh(defaultSpeed); // Reset to 5 in current units
        }, 500);
    };
    
    const handleClose = () => {
        //console.log("[RoadSpeedWindow] Close button clicked - deactivating tool");
        trigger("RoadSpeedAdjuster", "TRIGGER:ACTIVATE_TOOL", false);
    };
    
    const handleMouseDown = (e: React.MouseEvent) => {
        // Only start drag if clicking on the header area (not the close button)
        if ((e.target as HTMLElement).tagName === 'BUTTON') {
            return;
        }
        
        setIsDragging(true);
        dragRef.current = {
            startX: e.clientX,
            startY: e.clientY,
            initialX: position.x,
            initialY: position.y
        };
    };
    
    useEffect(() => {
        if (!isDragging) return;
        
        const handleMouseMove = (e: MouseEvent) => {
            const deltaX = e.clientX - dragRef.current.startX;
            const deltaY = e.clientY - dragRef.current.startY;
            
            const newPosition = {
                x: dragRef.current.initialX + deltaX,
                y: dragRef.current.initialY + deltaY
            };
            setPosition(newPosition);
            setSharedWindowPosition(newPosition);
        };
        
        const handleMouseUp = () => {
            setIsDragging(false);
        };
        
        document.addEventListener('mousemove', handleMouseMove);
        document.addEventListener('mouseup', handleMouseUp);
        
        return () => {
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
        };
    }, [isDragging]);

    if (!visible) return null;

    // Calculate display values based on unit system and track type
    let displaySpeed: number;
    let sliderValue: number;
    let sliderMin: number;
    let sliderMax: number;
    let sliderStep: number;
    const unitLabel = showMetric ? "km/h" : "mph";
    const multiplier = doubleSpeedDisplay ? 2 : 1;
    
    if (showMetric) {
        // Metric mode: optionally show km/h at 2x the actual value
        displaySpeed = pendingSpeedKmh * multiplier;
        sliderValue = pendingSpeedKmh * multiplier;
        sliderMin = 5 * multiplier;
        // Dynamic max: tracks (trains/trams/subways) can go to 240, roads only to 140 (actual values)
        sliderMax = (isTrackType ? 240 : 140) * multiplier;
        sliderStep = 5 * multiplier;
    } else {
        // Imperial mode: optionally show mph at 2x the actual value
        const actualMph = kmhToMph(pendingSpeedKmh);
        displaySpeed = actualMph * multiplier;
        sliderValue = displaySpeed;
        sliderMin = 5 * multiplier;
        // Dynamic max: tracks can go to 150 mph, roads only to 85 mph (actual values)
        sliderMax = (isTrackType ? 150 : 85) * multiplier;
        sliderStep = 5 * multiplier;
    }

    return (
        <>
            <div style={{
                position: "absolute",
                left: `${position.x}px`,
                top: `${position.y}px`,
                width: "400rem",
                pointerEvents: "auto",
            }}>
                <Panel
                    header={
                        <div 
                            onMouseDown={handleMouseDown}
                            style={{ 
                                display: "flex", 
                                justifyContent: "space-between", 
                                alignItems: "center",
                                width: "100%",
                                cursor: isDragging ? "grabbing" : "grab"
                            }}
                        >
                        <span style={{ paddingLeft: "10rem" }}>Road Speed Adjuster</span>
                        <div style={{ display: "flex", alignItems: "center" }}>
                            <button
                                onClick={() => ToggleUnit()}
                                style={{
                                    background: "rgba(100, 100, 100, 0.3)",
                                    border: "1px solid rgba(200, 200, 200, 0.4)",
                                    borderRadius: "50%",
                                    width: "18rem",
                                    height: "18rem",
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                    cursor: "pointer",
                                    fontSize: "12rem",
                                    fontWeight: "bold",
                                    color: "#ccc",
                                    lineHeight: "1",
                                    transition: "all 0.2s ease",
                                    padding: "0",
                                    marginRight: "5rem"
                                }}
                                title="Toggle unit preference (Auto / Metric / Imperial)"
                            >
                                {unitMode === 0 ? "A" : unitMode === 1 ? "M" : "I"}
                            </button>
                            <button
                                onMouseEnter={() => setIsHelpHovered(true)}
                                onMouseLeave={() => setIsHelpHovered(false)}
                                style={{
                                    background: "rgba(100, 100, 100, 0.3)",
                                    border: "1px solid rgba(200, 200, 200, 0.4)",
                                    borderRadius: "50%",
                                    width: "18rem",
                                    height: "18rem",
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                    cursor: "help",
                                    fontSize: "12rem",
                                    fontWeight: "bold",
                                    color: "#ccc",
                                    padding: "0",
                                    lineHeight: "1",
                                    transition: "all 0.2s ease",
                                    marginRight: "10rem"
                                }}
                            >
                                ?
                            </button>
                                <button 
                                    onClick={handleClose}
                                    onMouseEnter={() => setIsCloseHovered(true)}
                                    onMouseLeave={() => setIsCloseHovered(false)}
                                    style={{
                                        background: isCloseHovered ? "rgba(255, 68, 68, 0.1)" : "transparent",
                                        border: "none",
                                        color: isCloseHovered ? "#ff6666" : "#ff4444",
                                        fontSize: "18rem",
                                        fontWeight: "bold",
                                        cursor: "pointer",
                                        padding: "0 10rem",
                                        lineHeight: "1",
                                        borderRadius: "4rem",
                                        transition: "background-color 0.2s ease, color 0.2s ease, transform 0.2s ease",
                                        transform: isCloseHovered ? "scale(1.1)" : "scale(1)"
                                    }}
                                >
                                    X
                                </button>
                            </div>
                        </div>
                    }
                    className={PanelTheme.panel}
                >
            <div style={{ padding: "16rem", pointerEvents: "auto" }}>
                {/* Speed Slider */}
                <div style={{ marginBottom: "12rem" }}>
                    <div style={{ 
                        marginBottom: "8rem", 
                        fontSize: "16rem", 
                        fontWeight: "bold",
                        textAlign: "center",
                        whiteSpace: "nowrap"
                    }}>
                        {`${displaySpeed} ${unitLabel}`}
                    </div>
                    <Slider
                        start={sliderMin}
                        end={sliderMax}
                        step={sliderStep}
                        value={sliderValue}
                        onChange={handleSliderChange}
                    />
                    <div style={{
                        display: "flex",
                        justifyContent: "space-between",
                        fontSize: "11rem",
                        color: "#fff",
                        marginTop: "4rem"
                    }}>
                        <span>{sliderMin} {unitLabel}</span>
                        <span>{sliderMax} {unitLabel}</span>
                    </div>
                </div>

                {/* Buttons */}
                <div style={{
                    display: "flex"
                }}>
                    <div style={{ flex: 1, marginRight: "8rem" }}>
                        <Button
                            focusKey={FOCUS_DISABLED}
                            selected={isResetting}
                            disabled={isResetting}
                            onSelect={handleReset}
                            variant="neutral"
                        >
                            {isResetting ? "✓ Reset" : "Reset"}
                        </Button>
                    </div>
                    <div style={{ flex: 1, marginLeft: "8rem" }}>
                        <Button
                            focusKey={FOCUS_DISABLED}
                            selected={isApplying}
                            disabled={isApplying}
                            onSelect={handleApply}
                        >
                            {isApplying ? "✓ Applied" : "Apply"}
                        </Button>
                    </div>
                </div>
            </div>
        </Panel>
        </div>
        
        {/* Tooltip rendered at root level to escape Panel overflow */}
        {isHelpHovered && (
            <div style={{
                position: "fixed",
                left: `${position.x + 320}px`,
                top: `${position.y + 40}px`,
                backgroundColor: "rgba(40, 40, 40, 0.95)",
                color: "#fff",
                padding: "10rem 14rem",
                borderRadius: "4rem",
                fontSize: "11rem",
                lineHeight: "1.4",
                zIndex: 1000000,
                border: "1px solid rgba(200, 200, 200, 0.3)",
                boxShadow: "0 4rem 8rem rgba(0, 0, 0, 0.3)",
                pointerEvents: "none",
                maxWidth: "200rem"
            }}>
                <div>Select a road, train track,</div>
                <div>subway line or tram track</div>
                <div>and adjust its speed limit.</div>
                <div style={{ marginTop: "6rem" }}>Click and drag to select</div>
                <div>multiple roads/tracks.</div>
                <div style={{ marginTop: "6rem", fontWeight: "bold" }}>Unit Toggle (A/M/I):</div>
                <div>A = Auto (detects map theme)</div>
                <div>M = Metric (km/h)</div>
                <div>I = Imperial (mph)</div>
            </div>
        )}
        </>
    );
};

