import { ModRegistrar, ModuleRegistry } from "cs2/modding";
import { VanillaComponentResolver } from "./mods/VanillaComponentResolver";
import { RoadSpeedWindow } from "./mods/RoadSpeedWindow";
import { RoadSpeedToolbarButton } from "./mods/RoadSpeedToolbarButton";

const register: ModRegistrar = (moduleRegistry: ModuleRegistry) => {
    console.log("🚀 RoadSpeedAdjuster UI Init");
    (window as any).rsareg = moduleRegistry;

    VanillaComponentResolver.setRegistry(moduleRegistry);

    // Append our custom window to the Game component
    moduleRegistry.append("Game", RoadSpeedWindow);

    // Add our toolbar button to the top-left game UI area
    moduleRegistry.append("GameTopLeft", RoadSpeedToolbarButton);

    console.log("RoadSpeedAdjuster UI registration complete.");
};

export default register;
