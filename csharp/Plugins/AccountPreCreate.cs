using System;
using Microsoft.Xrm.Sdk;

namespace D365.Plugins
{
    /// <summary>
    /// Pre-Create plugin on the Account entity.
    ///
    /// Registration:
    ///   Message : Create
    ///   Entity  : account
    ///   Stage   : Pre-Operation (20)
    ///   Mode    : Synchronous
    /// </summary>
    public class AccountPreCreate : PluginBase
    {
        protected override void ExecutePlugin(PluginContext ctx)
        {
            ctx.Tracing.Trace("AccountPreCreate: start");

            if (ctx.ExecutionContext.MessageName != "Create")
                throw new InvalidPluginExecutionException(
                    "AccountPreCreate is registered for Create only.");

            var target = ctx.GetTargetEntity();

            // Ensure the account name is title-cased
            if (target.TryGetAttributeValue<string>("name", out var name) &&
                !string.IsNullOrWhiteSpace(name))
            {
                target["name"] = ToTitleCase(name);
                ctx.Tracing.Trace("AccountPreCreate: name normalised to '{0}'", target["name"]);
            }

            // Default the account number when not supplied
            if (!target.Contains("accountnumber"))
            {
                target["accountnumber"] = "ACC-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                ctx.Tracing.Trace("AccountPreCreate: accountnumber defaulted");
            }

            ctx.Tracing.Trace("AccountPreCreate: end");
        }

        private static string ToTitleCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;
                words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1).ToLowerInvariant();
            }
            return string.Join(" ", words);
        }
    }
}
