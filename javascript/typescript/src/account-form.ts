/**
 * TypeScript version of the Account form script.
 * Uses @types/xrm for full IntelliSense on the Xrm Client API.
 *
 * Compile: npm run build
 * Upload:  dist/account-form.js → Dynamics 365 Web Resource
 */

namespace AccountFormScripts {

    export function onLoad(executionContext: Xrm.Events.EventContext): void {
        const formContext = executionContext.getFormContext();
        const formType = formContext.ui.getFormType();

        if (formType === XrmEnum.FormType.Create) {
            setFieldVisible(formContext, "telephone1", true);
        }

        const revenueAttr = formContext.getAttribute<Xrm.Attributes.NumberAttribute>("revenue");
        revenueAttr?.addOnChange(onRevenueChange);
    }

    export function onSave(executionContext: Xrm.Events.SaveEventContext): void {
        const formContext = executionContext.getFormContext();
        const name = formContext.getAttribute<Xrm.Attributes.StringAttribute>("name")?.getValue();

        if (!name) {
            executionContext.getEventArgs().preventDefault();
            Xrm.Navigation.openAlertDialog({ text: "Account Name is required." });
        }
    }

    export function onRevenueChange(executionContext: Xrm.Events.EventContext): void {
        const formContext = executionContext.getFormContext();
        const revenue = formContext.getAttribute<Xrm.Attributes.NumberAttribute>("revenue").getValue();

        if (revenue !== null && revenue > 1_000_000_000) {
            formContext.ui.setFormNotification(
                "Revenue exceeds $1B – please verify the value.",
                "WARNING",
                "revenueWarning"
            );
        } else {
            formContext.ui.clearFormNotification("revenueWarning");
        }
    }

    function setFieldVisible(formContext: Xrm.FormContext, fieldName: string, visible: boolean): void {
        formContext.getControl(fieldName)?.setVisible(visible);
    }
}
