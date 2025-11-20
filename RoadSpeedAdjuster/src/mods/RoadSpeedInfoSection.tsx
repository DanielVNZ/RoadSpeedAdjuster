import React, { useState, useEffect, useRef } from "react";
import { INITIAL_SPEED, ApplySpeed, ResetSpeed } from "./bindings";
import { VanillaComponentResolver } from "./VanillaComponentResolver";
import { Button } from "./Button";

export const RoadSpeedInfoSection = (componentList: any) => {
  const Component: React.FC = () => {
    const [pendingSpeed, setPendingSpeed] = useState(50);
    const [isApplying, setIsApplying] = useState(false);
    const [isResetting, setIsResetting] = useState(false);
    const dragging = useRef(false);
    const lastBindingValue = useRef(50);

    const resolver = VanillaComponentResolver.instance;
    const InfoSection = resolver.InfoSection;
    const InfoRow = resolver.InfoRow;
    const Slider = resolver.Slider;
    const FOCUS_DISABLED = resolver.FOCUS_DISABLED;

    // Convert km/h to mph
    const kmhToMph = (kmh: number): number => {
      return Math.round(kmh * 0.621371);
    };

    useEffect(() => {
      const interval = setInterval(() => {
        if (dragging.current) return;

        try {
          const v = INITIAL_SPEED.value;
          if (typeof v === "number" && v !== lastBindingValue.current && v > 0) {
            lastBindingValue.current = v;
            setPendingSpeed(v); // Update UI immediately
          }
        } catch { }
      }, 120);

      return () => clearInterval(interval);
    }, []);

    // Immediately update pendingSpeed when INITIAL_SPEED changes (new road selected)
    useEffect(() => {
      const v = INITIAL_SPEED.value;
      if (typeof v === "number" && v > 0) {
        setPendingSpeed(v);
        lastBindingValue.current = v;
      }
    }, [INITIAL_SPEED.value]);

    const handleSliderChange = (value: number) => {
      dragging.current = true;
      const roundedValue = Math.round(value / 5) * 5;
      setPendingSpeed(roundedValue);
      setTimeout(() => dragging.current = false, 80);
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

    return (
      <InfoSection disableFocus={true}>
        <InfoRow
          left="Current Speed Limit"
          right={`${Math.round(pendingSpeed)} km/h (${kmhToMph(pendingSpeed)} mph)`}
        />

        <InfoRow
          left="Adjust Speed Limit"
          right={
            <div style={{
              width: "100%",
              display: "flex",
              alignItems: "center",
              marginLeft: "-190rem", // Shift entire right section left to give more room for label
            }}>
              <div style={{
                flex: 1,
                display: "flex",
                flexDirection: "column",
                marginRight: "16rem",
              }}>
                <Slider
                  focusKey={FOCUS_DISABLED}
                  start={5}
                  end={140}
                  step={5}
                  value={pendingSpeed}
                  onChange={handleSliderChange}
                  theme={resolver.sliderTheme}
                />
              </div>
              <div style={{
                width: "65rem",
                flexShrink: 0,
              }}>
                <Button
                  focusKey={FOCUS_DISABLED}
                  selected={isApplying}
                  disabled={isApplying}
                  onSelect={handleApply}
                >
                  {isApplying ? "✓" : "Apply"}
                </Button>
              </div>
            </div>
          }
        />

        <InfoRow
          left="Reset to Default"
          right={
            <div style={{
              width: "65rem",
              flexShrink: 0,
            }}>
              <Button
                focusKey={FOCUS_DISABLED}
                selected={isResetting}
                disabled={isResetting}
                onSelect={handleReset}
              >
                {isResetting ? "✓" : "Reset"}
              </Button>
            </div>
          }
        />
      </InfoSection>
    );
  };

  // IMPORTANT: This key must match the C# group name exactly
  componentList["RoadSpeedAdjuster.Systems.RoadSpeedToolUISystem"] = Component;
  return componentList;
};
