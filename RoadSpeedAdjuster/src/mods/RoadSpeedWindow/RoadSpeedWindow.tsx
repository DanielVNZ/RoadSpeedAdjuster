import { useState, useEffect, useRef } from "react";
import { useValue, trigger } from "cs2/api";
import { TOOL_ACTIVE, SELECTION_COUNTER, UNIT_MODE, ToggleUnit, getSharedWindowPosition, setSharedWindowPosition, SHOW_METRIC } from "../bindings";
import { getModule } from "cs2/modding";
import { UnitMode } from "../types";
import { RoadSpeedNoSelect } from "./RoadSpeedNoSelect";
import { RoadSpeedSelected } from "./RoadSpeedSelected";

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
    let isMapMetric = true;
    try {
        const value = useValue(SHOW_METRIC);
        isMapMetric = value ?? true;
    } catch (e) {
        isMapMetric = true;
    }
    
    // Watch unit mode (0 = Auto, 1 = Metric, 2 = Imperial)
    let unitMode = UnitMode.Auto;
    try {
        const value = useValue(UNIT_MODE);
        unitMode = value ?? UnitMode.Auto;
    } catch (e) {
        unitMode = UnitMode.Auto;
    }
    
    // Watch selection counter to detect when user selects a new road
    let selectionCounter = 0;
    try {
        const value = useValue(SELECTION_COUNTER);
        selectionCounter = value ?? 0;
    } catch (e) {
        selectionCounter = 0;
    }

    const defaultedUnitMode = unitMode === UnitMode.Auto ? (isMapMetric ? UnitMode.Metric : UnitMode.Imperial) : unitMode;
    
    // Dragging state (use shared position)
    const [position, setPosition] = useState(getSharedWindowPosition());
    const [isDragging, setIsDragging] = useState(false);
    const dragRef = useRef({ startX: 0, startY: 0, initialX: 0, initialY: 0 });
    
    // Hover state for close button
    const [isCloseHovered, setIsCloseHovered] = useState(false);
    
    // Hover state for help icon
    const [isHelpHovered, setIsHelpHovered] = useState(false);

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

    return toolActive ? (
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
                {selectionCounter == 0 ? <RoadSpeedNoSelect /> : <RoadSpeedSelected unitMode={defaultedUnitMode} />}
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
    ) : null;
};

