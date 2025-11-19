import { ModuleRegistry } from "cs2/modding";
import type { IVanillaComponents, IVanillaThemes, IVanillaFocus } from "./types";

// -------------------------------------------------------
// Vanilla UI Components
// -------------------------------------------------------
const modulePaths = [
    {
        path: "game-ui/game/components/selected-info-panel/selected-info-sections/road-section/road-section.tsx",
        components: ["RoadSection"],
    },
    {
        path: "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
        components: ["InfoSection"],
    },
    {
        path: "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
        components: ["InfoRow"],
    },
    {
        path: "game-ui/game/components/selected-info-panel/shared-components/info-link/info-link.tsx",
        components: ["InfoLink"],
    },
    {
        path: "game-ui/common/input/slider/slider.tsx",
        components: ["Slider"],
    },
    {
        path: "game-ui/game/components/tool-options/tool-button/tool-button.tsx",
        components: ["ToolButton"],
    },
];

// -------------------------------------------------------
// SCSS Themes — REAL slider theme
// -------------------------------------------------------
const themePaths = [
    {
        path: "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss",
        name: "infoRow",
    },
    {
        path: "game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss",
        name: "tooltip",
    },
    {
        path: "game-ui/common/panel/themes/default.module.scss",
        name: "panel",
    },
    {
        path: "game-ui/common/input/slider/themes/default.module.scss",
        name: "slider",
    },
    {
        path: "game-ui/game/components/tool-options/tool-button/tool-button.module.scss",
        name: "toolButton",
    },
];

// -------------------------------------------------------
// Containers exposed to your mod
// -------------------------------------------------------
export const VC = {} as IVanillaComponents;
export const VT = {} as IVanillaThemes;
export const VF = {} as IVanillaFocus;

// -------------------------------------------------------
// INITIALIZER
// -------------------------------------------------------
export const initialize = (moduleRegistry: ModuleRegistry) => {

    // Load components
    modulePaths.forEach(({ path, components }) => {
        const module = moduleRegistry.registry.get(path);
        if (!module) return;

        components.forEach((component) => {
            VC[component] = module?.[component];
        });
    });

    // Load SCSS themes
    themePaths.forEach(({ path, name }) => {
        const module = moduleRegistry.registry.get(path)?.classes;
        VT[name] = module ?? {};
    });

    // Load focus
    const focusKeyModule = moduleRegistry.registry.get("game-ui/common/focus/focus-key.ts");
    VF.FOCUS_DISABLED = focusKeyModule?.FOCUS_DISABLED;
    VF.FOCUS_AUTO = focusKeyModule?.FOCUS_AUTO;
};
