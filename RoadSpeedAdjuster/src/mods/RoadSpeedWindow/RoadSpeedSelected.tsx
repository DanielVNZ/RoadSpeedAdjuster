import { useState, useEffect, useRef } from "react";
import { useValue } from "cs2/api";
import { SELECTION_COUNTER, IS_TRACK_TYPE, DOUBLE_SPEED_DISPLAY, ApplySpeed, ResetSpeed } from "../bindings";
import { VanillaComponentResolver } from "../VanillaComponentResolver";
import { Button } from "../Button";
import { Slider } from "../../slider/slider";
import { UnitMode } from "../types";

export const RoadSpeedSelected = ({unitMode}: {unitMode: Exclude<UnitMode, UnitMode.Auto>}) => {
    // Watch track type (true = train/tram/subway, false = road)
    let isTrackType = false;
    try {
        const value = useValue(IS_TRACK_TYPE);
        isTrackType = value ?? false;
    } catch (e) {
        isTrackType = false;
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
    
    const [pendingSpeedKmh, setPendingSpeedKmh] = useState(5); // Always store as km/h internally
    const [isApplying, setIsApplying] = useState(false);
    const [isResetting, setIsResetting] = useState(false);
    const lastSelectionCounter = useRef(0);

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
        if (unitMode == UnitMode.Metric) {
            return 5; // 5 km/h actual (will display as 10 if doubling enabled)
        } else {
            return mphToKmh(5); // 5 mph actual converted to km/h (~8 km/h actual)
        }
    };

    // Show panel ONLY when a road selection is made (selection counter increases)
    useEffect(() => {
        const defaultSpeed = getDefaultSpeed();
        setPendingSpeedKmh(defaultSpeed); // Reset to 5 in current units
        lastSelectionCounter.current = selectionCounter;
    }, [selectionCounter, unitMode]);

    const handleSliderChange = (value: number) => {
        if (unitMode == UnitMode.Metric) {

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

    // Calculate display values based on unit system and track type
    let displaySpeed: number;
    let sliderValue: number;
    let sliderMin: number;
    let sliderMax: number;
    let sliderStep: number;
    const unitLabel = {[UnitMode.Metric]:"km/h", [UnitMode.Imperial] : "mph"}[unitMode];
    const multiplier = doubleSpeedDisplay ? 2 : 1;
    
    if (unitMode == UnitMode.Metric) {
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

    return <div style={{ padding: "16rem", pointerEvents: "auto" }}>
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
            <div style={{ flex: '1 1 0', marginRight: "8rem" }}>
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
            <div style={{ flex: '1 1 0', marginLeft: "8rem" }}>
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
    </div>;
};

