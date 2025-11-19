import { VanillaComponentResolver } from "../mods/VanillaComponentResolver";

export const Slider = (props: any) => {
    const VSlider = VanillaComponentResolver.instance.Slider;

    return (
        <VSlider
            {...props}
            step={props.step}
            onChange={(v: number) => props.onChange(v)}
        />
    );
};
