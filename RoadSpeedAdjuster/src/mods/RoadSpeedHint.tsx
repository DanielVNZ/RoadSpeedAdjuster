import { useValue } from "cs2/api";
import { useState, useRef, useEffect } from "react";
import { TOOL_ACTIVE, SELECTION_COUNTER, getSharedWindowPosition, setSharedWindowPosition } from "./bindings";
import { getModule } from "cs2/modding";
import { trigger } from "cs2/api";

// Load vanilla components
const Panel: any = getModule("game-ui/common/panel/panel.tsx", "Panel");
const PanelTheme: any = getModule(
    "game-ui/common/panel/themes/default.module.scss",
    "classes"
);

export const RoadSpeedHint = () => {
    // Watch tool active state
    let toolActive = false;
    try {
        const value = useValue(TOOL_ACTIVE);
        toolActive = value ?? false;
    } catch (e) {
        toolActive = false;
    }
    
    // Watch selection counter to detect when user selects a road
    let selectionCounter = 0;
    try {
        const value = useValue(SELECTION_COUNTER);
        selectionCounter = value ?? 0;
    } catch (e) {
        selectionCounter = 0;
    }
    
    // Only show when tool is active but no selection has been made yet
    const shouldShow = toolActive && selectionCounter === 0;
    
    // Dragging state (use shared position)
    const [position, setPosition] = useState(getSharedWindowPosition());
    const [isDragging, setIsDragging] = useState(false);
    const dragRef = useRef({ startX: 0, startY: 0, initialX: 0, initialY: 0 });
    
    // Hover state for close button
    const [isCloseHovered, setIsCloseHovered] = useState(false);
    
    const handleClose = () => {
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
    
    if (!shouldShow) return null;

    return (
        <div style={{
            position: "absolute",
            left: `${position.x}px`,
            top: `${position.y}px`,
            width: "350rem",
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
                <div style={{ 
                    padding: "16rem",
                    textAlign: "center"
                }}>
                    <div style={{
                        fontSize: "14rem",
                        color: "#aaa",
                        fontStyle: "italic",
                        marginTop: "4rem"
                    }}>
                        Click (or click and drag) to select road or rail segments
                    </div>
                </div>
            </Panel>
        </div>
    );
};
