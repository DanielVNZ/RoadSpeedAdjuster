import { ModRegistrar, ModuleRegistry } from "cs2/modding";
import { VanillaComponentResolver } from "./mods/VanillaComponentResolver";
import { RoadSpeedInfoSection } from "./mods/RoadSpeedInfoSection";

const register: ModRegistrar = (moduleRegistry: ModuleRegistry) => {
    console.log("🚀 RoadSpeedAdjuster UI Init");
    (window as any).rsareg = moduleRegistry;

    VanillaComponentResolver.setRegistry(moduleRegistry);

    // Extend the selectedInfoSectionComponents to add our custom section
    // RoadSpeedInfoSection is a function that takes componentList and registers itself
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        RoadSpeedInfoSection
    );

    console.log("RoadSpeedAdjuster UI registration complete.");
};

export default register;
