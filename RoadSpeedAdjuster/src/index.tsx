import { ModRegistrar, ModuleRegistry } from "cs2/modding";
import { VanillaComponentResolver } from "./mods/VanillaComponentResolver";
import { initialize as initVC } from "./mods/Components";
import { SpeedPanel } from "./mods/SpeedWindow";
import { RoadSpeedInfoSection } from "./mods/RoadSpeedInfoSection";

const register: ModRegistrar = (moduleRegistry: ModuleRegistry) => {
    console.log("🚀 RoadSpeedAdjuster UI Init");
    (window as any).rsareg = moduleRegistry;


    // 1. Allow Klyte resolver style (optional but harmless)


    // 2. Load Vanilla Components / SCSS so the Slider looks correct
    //initVC(registry);
    VanillaComponentResolver.setRegistry(moduleRegistry);
    // 3. Register speed panel - standalone floating panel (you can remove this if you only want the integrated version)
    //registry.append("Game", SpeedPanel);

    // 4. Inject our speed slider into the Selected Info Panel for roads
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        RoadSpeedInfoSection
    );

    console.log("RoadSpeedAdjuster UI module registrations completed.");
};

export default register;
