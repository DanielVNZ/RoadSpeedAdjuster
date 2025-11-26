import { useValue } from "cs2/api";
import { Button, Tooltip } from "cs2/ui";
import { ActivateTool, TOOL_ACTIVE } from "./bindings";
import classNames from "classnames";
import styles from "./RoadSpeedToolbarButton.module.scss";
import iconSvg from "../images/icon.svg";

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
        ActivateTool();
    };

    return (
        <Tooltip tooltip="Road Speed Adjuster - Click to select road segments">
            <Button
                variant="floating"
                className={classNames({ [styles.selected]: toolActive }, styles.toggle)}
                onSelect={handleClick}
            >
                <img 
                    src={iconSvg}
                    style={{ 
                        width: "100%",
                        height: "100%"
                    }} 
                />
            </Button>
        </Tooltip>
    );
};
