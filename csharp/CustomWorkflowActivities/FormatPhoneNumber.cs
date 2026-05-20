using System;
using System.Activities;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

namespace D365.CustomWorkflowActivities
{
    /// <summary>
    /// Custom Workflow Activity: Format Phone Number
    ///
    /// Strips non-numeric characters from a phone string and formats it as
    /// +1 (NXX) NXX-XXXX for 11-digit US numbers, or returns the digits-only
    /// string for other lengths.
    ///
    /// Usage in a Classic Workflow or Power Automate (CDS connector):
    ///   Input  : RawPhone  – the raw phone string (e.g. "1 800 555-1234")
    ///   Output : Formatted – the formatted phone string
    /// </summary>
    public class FormatPhoneNumber : CodeActivity
    {
        [Input("Raw Phone Number")]
        [RequiredArgument]
        public InArgument<string> RawPhone { get; set; } = default!;

        [Output("Formatted Phone Number")]
        public OutArgument<string> Formatted { get; set; } = default!;

        protected override void Execute(CodeActivityContext context)
        {
            var tracingService = context.GetExtension<ITracingService>();
            tracingService?.Trace("FormatPhoneNumber: start");

            var raw = RawPhone.Get(context) ?? string.Empty;
            var digits = Regex.Replace(raw, @"\D", "");

            string result;
            if (digits.Length == 11 && digits[0] == '1')
            {
                result = $"+1 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7)}";
            }
            else if (digits.Length == 10)
            {
                result = $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}";
            }
            else
            {
                result = digits;
            }

            tracingService?.Trace("FormatPhoneNumber: '{0}' → '{1}'", raw, result);
            Formatted.Set(context, result);
        }
    }
}
