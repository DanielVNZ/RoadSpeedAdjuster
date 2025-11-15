// src/ui/index.tsx
import { ModRegistrar, ModuleRegistry } from "cs2/modding";
import { VanillaComponentResolver } from "./mods/VanillaComponentResolver";
import { initialize as initVanillaComponents } from "./mods/Components";
import { RoadSpeedSelectedInfoPanelComponent } from "./mods/InjectRoadSpeedIntoRoadSection";
import mod from "../mod.json";

const register: ModRegistrar = (moduleRegistry: ModuleRegistry) => {
    console.log("🚀 RoadSpeedAdjuster UI Init");
    (window as any).rsareg = moduleRegistry;

    // Old Klyte resolver (for tool buttons, etc.)
    VanillaComponentResolver.setRegistry(moduleRegistry);

    // ⚠️ IMPORTANT: initialize VC / VT / VF (InfoSection, InfoRow, etc.)
    initVanillaComponents(moduleRegistry);

    console.log("🔧 Extending SelectedInfoSections");

    // Platter / Recolor style: mutator ONLY, no wrapper
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        RoadSpeedSelectedInfoPanelComponent
    );

    console.log(`${mod.id} UI module registrations completed.`);
};

export default register;
