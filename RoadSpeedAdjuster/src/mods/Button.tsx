import React from "react";
import styles from "./RoadSpeedButton.module.scss";

export const Button = (props: any) => {
    const { selected, disabled, style, className, onSelect, variant, children, ...rest } = props;
    
    const buttonClass = [
        styles.button,
        variant === 'neutral' ? styles.neutral : '',
        selected ? styles.selected : '',
        className
    ].filter(Boolean).join(' ');

    return (
        <button
            {...rest}
            className={buttonClass}
            disabled={disabled}
            onClick={onSelect}
            style={style}
        >
            {children}
        </button>
    );
};
