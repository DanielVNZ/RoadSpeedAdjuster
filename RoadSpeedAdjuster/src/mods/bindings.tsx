import { bindValue, trigger } from "cs2/api";
import { UnitMode } from "./types";

export const INITIAL_SPEED = bindValue<number>(
  "RoadSpeedAdjuster",
  "BINDING:INFOPANEL_ROAD_SPEED"
);
export const TOOL_ACTIVE = bindValue<boolean>(
  "RoadSpeedAdjuster",
  "BINDING:TOOL_ACTIVE"
);
export const SELECTION_COUNTER = bindValue<number>(
  "RoadSpeedAdjuster",
  "BINDING:SELECTION_COUNTER"
);
export const SHOW_METRIC = bindValue<boolean>(
  "RoadSpeedAdjuster",
  "BINDING:SHOW_METRIC"
);
export const IS_TRACK_TYPE = bindValue<boolean>(
  "RoadSpeedAdjuster",
  "BINDING:IS_TRACK_TYPE"
);
export const UNIT_MODE = bindValue<UnitMode>(
  "RoadSpeedAdjuster",
  "BINDING:UNIT_MODE"
);
export const DOUBLE_SPEED_DISPLAY = bindValue<boolean>(
  "RoadSpeedAdjuster",
  "BINDING:DOUBLE_SPEED_DISPLAY"
);

export function ApplySpeed(speed: number) {
  trigger("RoadSpeedAdjuster", "TRIGGER:APPLY_SPEED", speed);
}

export function ResetSpeed() {
  trigger("RoadSpeedAdjuster", "TRIGGER:RESET_SPEED");
}

export function ToggleUnit() {
  trigger("RoadSpeedAdjuster", "TRIGGER:TOGGLE_UNIT");
}

export function ActivateTool() {
  trigger("RoadSpeedAdjuster", "TRIGGER:ACTIVATE_TOOL", true);
}

// Shared window position state
let sharedWindowPosition = { x: window.innerWidth - 600, y: 100 };

export function getSharedWindowPosition() {
  return { ...sharedWindowPosition };
}

export function setSharedWindowPosition(position: { x: number; y: number }) {
  sharedWindowPosition = position;
}

