import { bindValue, trigger } from "cs2/api";

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

export function ApplySpeed(speed: number) {
  console.log(`ApplySpeed called with ${speed}`);
  trigger("RoadSpeedAdjuster", "TRIGGER:APPLY_SPEED", speed);
}

export function ResetSpeed() {
  console.log("ResetSpeed called");
  trigger("RoadSpeedAdjuster", "TRIGGER:RESET_SPEED");
}

export function ActivateTool() {
  console.log("ActivateTool called - triggering with true");
  trigger("RoadSpeedAdjuster", "TRIGGER:ACTIVATE_TOOL", true);
  console.log("ActivateTool trigger sent with argument: true");
}
