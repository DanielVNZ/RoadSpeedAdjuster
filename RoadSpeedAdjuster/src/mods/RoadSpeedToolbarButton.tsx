import { useValue } from "cs2/api";
import { Button, Tooltip } from "cs2/ui";
import { ActivateTool, TOOL_ACTIVE } from "./bindings";
import classNames from "classnames";
import styles from "./RoadSpeedToolbarButton.module.scss";

export const RoadSpeedToolbarButton = () => {
    // Safely access binding value with error handling
    let toolActive = false;
    try {
        const value = useValue(TOOL_ACTIVE);
        toolActive = value ?? false;
    } catch (e) {
        // Binding not ready yet, use default
        toolActive = false;
    }

    const handleClick = () => {
        console.log("Road Speed Tool button clicked - activating tool");
        ActivateTool();
    };

    return (
        <Tooltip tooltip="Road Speed Adjuster - Click to select road segments">
            <Button
                variant="floating"
                className={classNames({ [styles.selected]: toolActive }, styles.toggle)}
                onSelect={handleClick}
            >
                <img style={{ 
                    maskImage: `url(coui://ui-mods/images/road-speed-icon.svg)`,
                    WebkitMaskImage: `url(coui://ui-mods/images/road-speed-icon.svg)`
                }} />
            </Button>
        </Tooltip>
    );
};
