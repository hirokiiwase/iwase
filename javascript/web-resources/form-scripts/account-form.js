/**
 * Account form script – starter sample
 *
 * Register on the Account form:
 *   OnLoad  → AccountFormScripts.onLoad
 *   OnSave  → AccountFormScripts.onSave
 *   Revenue OnChange → AccountFormScripts.onRevenueChange
 */

"use strict";

var AccountFormScripts = AccountFormScripts || {};

/**
 * Form OnLoad handler.
 * @param {Xrm.Events.EventContext} executionContext
 */
AccountFormScripts.onLoad = function (executionContext) {
    var formContext = executionContext.getFormContext();
    var formType = formContext.ui.getFormType();

    // 1 = Create, 2 = Update, 3 = Read-only, 4 = Disabled
    if (formType === 1) {
        // Default the phone field visible only on create
        AccountFormScripts._setFieldVisible(formContext, "telephone1", true);
    }

    // Register an onChange handler dynamically from code
    var revenueAttr = formContext.getAttribute("revenue");
    if (revenueAttr) {
        revenueAttr.addOnChange(AccountFormScripts.onRevenueChange);
    }
};

/**
 * Form OnSave handler – prevent save when name is empty.
 * @param {Xrm.Events.SaveEventContext} executionContext
 */
AccountFormScripts.onSave = function (executionContext) {
    var formContext = executionContext.getFormContext();
    var nameAttr = formContext.getAttribute("name");

    if (nameAttr && !nameAttr.getValue()) {
        executionContext.getEventArgs().preventDefault();
        Xrm.Navigation.openAlertDialog({ text: "Account Name is required." });
    }
};

/**
 * Revenue field OnChange handler – show a warning for unusually high values.
 * @param {Xrm.Events.EventContext} executionContext
 */
AccountFormScripts.onRevenueChange = function (executionContext) {
    var formContext = executionContext.getFormContext();
    var revenue = formContext.getAttribute("revenue").getValue();

    if (revenue !== null && revenue > 1000000000) {
        formContext.ui.setFormNotification(
            "Revenue exceeds $1B – please verify the value.",
            "WARNING",
            "revenueWarning"
        );
    } else {
        formContext.ui.clearFormNotification("revenueWarning");
    }
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

AccountFormScripts._setFieldVisible = function (formContext, fieldName, visible) {
    var ctrl = formContext.getControl(fieldName);
    if (ctrl) {
        ctrl.setVisible(visible);
    }
};
