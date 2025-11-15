// src/ui/bindings.ts
import { bindValue, trigger } from "cs2/api";
import mod from "../../mod.json";

// ----------------------------
// Binding Keys (match C#)
// ----------------------------
export const ROAD_DUMMY_VALUE_KEY = "INFOPANEL_ROAD_DUMMY_VALUE";
export const ROAD_DUMMY_TRIGGER_KEY = "INFOPANEL_ROAD_DUMMY_VALUE_CHANGED";

// ----------------------------
// Value Binding (C# → UI)
// ----------------------------
export const ROAD_DUMMY_VALUE = bindValue<number>(
    mod.id,
    ROAD_DUMMY_VALUE_KEY
);

// ----------------------------
// Trigger (UI → C#)
// ----------------------------
export const SetRoadDummyValue = (value: number) =>
    trigger(mod.id, ROAD_DUMMY_TRIGGER_KEY, value);
