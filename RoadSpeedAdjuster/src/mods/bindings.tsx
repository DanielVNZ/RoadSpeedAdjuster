// src/ui/bindings.ts
import { bindValue } from "cs2/api";
import mod from "../../mod.json";

export const ROAD_DUMMY_VALUE = bindValue<number>(
    mod.id,
    "INFOPANEL_ROAD_DUMMY_VALUE",
    0
);
