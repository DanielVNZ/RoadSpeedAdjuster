import { useState, useEffect, useRef } from "react";
import { useValue, trigger } from "cs2/api";
import { TOOL_ACTIVE, SHOW_METRIC, SELECTION_COUNTER, IS_TRACK_TYPE, ApplySpeed, ResetSpeed, getSharedWindowPosition, setSharedWindowPosition } from "./bindings";
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
            return 5; // 5 km/h
        } else {
            return mphToKmh(5); // 5 mph converted to km/h (~8 km/h)
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
            // Metric mode: round to nearest 5 km/h
            const roundedValue = Math.round(value / 5) * 5;
            setPendingSpeedKmh(roundedValue);
        } else {
            // Imperial mode: value is in mph, convert to km/h
            // Round the value to nearest 5 mph first to ensure clean increments
            const roundedMph = Math.round(value / 5) * 5;
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
    
    if (showMetric) {
        // Metric mode: show km/h
        displaySpeed = pendingSpeedKmh;
        sliderValue = pendingSpeedKmh;
        sliderMin = 5;
        // Dynamic max: tracks (trains/trams/subways) can go to 240, roads only to 140
        sliderMax = isTrackType ? 240 : 140;
        sliderStep = 5;
    } else {
        // Imperial mode: show mph with proper 5 mph increments
        displaySpeed = kmhToMph(pendingSpeedKmh);
        sliderValue = displaySpeed; // Use the converted mph value
        sliderMin = 5; // 5 mph
        // Dynamic max: tracks can go to 150 mph, roads only to 85 mph
        sliderMax = isTrackType ? 150 : 85;
        sliderStep = 5; // 5 mph increments
    }

    return (
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
                }
                className={PanelTheme.panel}
            >
            <div style={{ padding: "16rem", pointerEvents: "auto" }}>
                {/* Info text explaining visual overlays */}
                <div style={{
                    marginBottom: "12rem",
                    fontSize: "12rem",
                    color: "#aaa",
                    fontStyle: "italic"
                }}>
                    Current speed limits are shown above each road for modified roads
                </div>

                {/* Speed Slider */}
                <div style={{ marginBottom: "12rem" }}>
                    <div style={{ 
                        marginBottom: "8rem", 
                        fontSize: "14rem", 
                        fontWeight: "bold",
                        display: "flex",
                        justifyContent: "space-between"
                    }}>
                        <span>New Speed Limit</span>
                        <span>{displaySpeed} {unitLabel}</span>
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
        </div>
    );
};

