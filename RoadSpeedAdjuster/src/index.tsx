import { ModRegistrar } from "cs2/modding";
import { VanillaComponentResolver } from "./mods/VanillaComponentResolver";
import { initialize as initVC } from "./mods/Components";
import { SpeedPanel } from "./mods/SpeedWindow";

const register: ModRegistrar = (registry) => {

    console.log("🚀 RoadSpeedAdjuster UI Init");

    // 1. Allow Klyte resolver style (optional but harmless)
    VanillaComponentResolver.setRegistry(registry);

    // 2. Load Vanilla Components / SCSS so the Slider looks correct
    initVC(registry);

    // 3. Register speed panel - shows automatically when a road is selected
    registry.append("Game", SpeedPanel);

    console.log("RoadSpeedAdjuster UI module registrations completed.");
};

export default register;