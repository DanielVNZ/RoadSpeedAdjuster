// src/ui/bindings.ts
import { bindValue, trigger } from "cs2/api";
import mod from "../../mod.json";

// ----------------------------
// Binding Keys (match C#)
// ----------------------------
// C#:
//   new ValueBinding<float>("RoadSpeedAdjuster", "INFOPANEL_SPEED_VALUE", 10f);
//   new TriggerBinding<float>("RoadSpeedAdjuster", "INFOPANEL_SPEED_CHANGED", OnSliderChanged);
export const SPEED_VALUE_KEY = "INFOPANEL_SPEED_VALUE";
export const SPEED_CHANGED_KEY = "INFOPANEL_SPEED_CHANGED";

// ----------------------------
// Value Binding (C# → UI)
// ----------------------------
export const SPEED_VALUE = bindValue<number>(
    mod.id,
    SPEED_VALUE_KEY
);

// ----------------------------
// Trigger (UI → C#)
// ----------------------------
export const SetSpeedValue = (value: number) =>
    trigger(mod.id, SPEED_CHANGED_KEY, value);
