import { IInputs, IOutputs } from "./generated/ManifestTypes";

/**
 * LinearInputComponent
 *
 * Renders an HTML range slider (<input type="range">) bound to a numeric
 * Dataverse column.  The current value is displayed beside the slider.
 *
 * Properties (configured in ControlManifest.Input.xml):
 *   controlValue – bound whole-number column
 *   minValue     – slider minimum (default 0)
 *   maxValue     – slider maximum (default 100)
 */
export class LinearInputComponent
    implements ComponentFramework.StandardControl<IInputs, IOutputs>
{
    private _notifyOutputChanged: () => void;
    private _container: HTMLDivElement;
    private _slider: HTMLInputElement;
    private _valueLabel: HTMLSpanElement;
    private _currentValue: number = 0;
    private _isDisabled: boolean = false;

    public init(
        context: ComponentFramework.Context<IInputs>,
        notifyOutputChanged: () => void,
        _state: ComponentFramework.Dictionary,
        container: HTMLDivElement
    ): void {
        this._notifyOutputChanged = notifyOutputChanged;
        this._container = container;

        // Build the slider
        this._slider = document.createElement("input");
        this._slider.type = "range";
        this._slider.className = "pcf-linear-slider";
        this._slider.style.cssText = "width: 200px; vertical-align: middle;";
        this._slider.addEventListener("input", this._onSliderChange.bind(this));

        // Value label
        this._valueLabel = document.createElement("span");
        this._valueLabel.style.cssText = "margin-left: 8px; font-weight: bold;";

        this._container.appendChild(this._slider);
        this._container.appendChild(this._valueLabel);

        this.updateView(context);
    }

    public updateView(context: ComponentFramework.Context<IInputs>): void {
        const min = context.parameters.minValue.raw ?? 0;
        const max = context.parameters.maxValue.raw ?? 100;
        const val = context.parameters.controlValue.raw ?? 0;

        this._slider.min   = String(min);
        this._slider.max   = String(max);
        this._slider.value = String(val);
        this._valueLabel.textContent = String(val);

        this._currentValue = val;
        this._isDisabled   = context.mode.isControlDisabled;
        this._slider.disabled = this._isDisabled;
    }

    public getOutputs(): IOutputs {
        return { controlValue: this._currentValue };
    }

    public destroy(): void {
        this._slider.removeEventListener("input", this._onSliderChange.bind(this));
    }

    private _onSliderChange(): void {
        this._currentValue = Number(this._slider.value);
        this._valueLabel.textContent = String(this._currentValue);
        this._notifyOutputChanged();
    }
}
