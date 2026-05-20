/**
 * Ribbon / Command Bar button handlers – starter sample
 *
 * Register in your RibbonDiff XML:
 *   Library  → this web resource
 *   Command  → RibbonCommands.sendConfirmation
 *   Enable   → RibbonCommands.isRecordSaved
 */

"use strict";

var RibbonCommands = RibbonCommands || {};

/**
 * Enable rule – hide the button until the record is saved.
 * @param {string} primaryEntityTypeName
 * @param {string} firstSelectedItemId   Record GUID (populated by CRM for EnableRule)
 * @returns {boolean}
 */
RibbonCommands.isRecordSaved = function (primaryEntityTypeName, firstSelectedItemId) {
    return !!firstSelectedItemId;
};

/**
 * Command handler – open a confirmation dialog then call a custom action.
 * @param {string} primaryEntityTypeName
 * @param {string} firstSelectedItemId
 */
RibbonCommands.sendConfirmation = function (primaryEntityTypeName, firstSelectedItemId) {
    var confirmStrings = {
        title: "Send Confirmation",
        text: "Are you sure you want to send a confirmation for this record?",
        confirmButtonLabel: "Send",
        cancelButtonLabel: "Cancel"
    };

    Xrm.Navigation.openConfirmDialog(confirmStrings).then(function (result) {
        if (!result.confirmed) { return; }

        // Call a Dataverse custom action (replace with your action schema name)
        Xrm.WebApi.online.execute({
            getMetadata: function () {
                return {
                    boundParameter: null,
                    parameterTypes: {
                        "Target": { typeName: "Microsoft.Dynamics.CRM.activitypointer", structuralProperty: 5 }
                    },
                    operationType: 0,  // Action
                    operationName: "new_SendConfirmationAction"
                };
            },
            Target: {
                "@odata.type": "Microsoft.Dynamics.CRM." + primaryEntityTypeName,
                [primaryEntityTypeName + "id"]: firstSelectedItemId
            }
        }).then(function () {
            Xrm.Navigation.openAlertDialog({ text: "Confirmation sent successfully." });
        }).catch(function (error) {
            Xrm.Navigation.openAlertDialog({ text: "Error: " + error.message });
        });
    });
};
