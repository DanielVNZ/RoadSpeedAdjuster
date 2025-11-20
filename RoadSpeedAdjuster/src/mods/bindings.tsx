import { bindValue, trigger } from "cs2/api";

export const INITIAL_SPEED = bindValue<number>(
  "RoadSpeedAdjuster",
  "BINDING:INFOPANEL_ROAD_SPEED"
);

// Used only when APPLY is pressed
export const ApplySpeed = (v: number) =>
  trigger("RoadSpeedAdjuster", "TRIGGER:APPLY_SPEED", v);

export const ResetSpeed = () =>
  trigger("RoadSpeedAdjuster", "TRIGGER:RESET_SPEED");
