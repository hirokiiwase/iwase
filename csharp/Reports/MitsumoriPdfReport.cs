using System;
using System.Net.Http;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace D365.Reports
{
    /// <summary>
    /// Post-Create plugin on the Account entity.
    /// Azure Functions を呼び出して見積書 PDF を生成し、タイムラインに添付する。
    ///
    /// Registration:
    ///   Message : Create
    ///   Entity  : account
    ///   Stage   : Post-Operation (40)
    ///   Mode    : Asynchronous
    /// </summary>
    public class MitsumoriPdfReport : IPlugin
    {
        // Azure Functions の URL（authlevel: anonymous のため API キー不要）
        private const string FunctionUrl =
            "https://iwase-pdf-func-asg5aughcmgvcmck.japanwest-01.azurewebsites.net/api/generatepdf";

        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
            var tracing = (ITracingService)
                serviceProvider.GetService(typeof(ITracingService));
            var factory = (IOrganizationServiceFactory)
                serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var svc = factory.CreateOrganizationService(context.UserId);

            try
            {
                var target = context.InputParameters["Target"] as Entity;
                if (target == null) return;

                tracing.Trace("MitsumoriPdfReport: fetching account");

                var account = svc.Retrieve("account", target.Id,
                    new ColumnSet("name", "accountnumber"));

                var clientName  = account.GetAttributeValue<string>("name")          ?? string.Empty;
                var accountNo   = account.GetAttributeValue<string>("accountnumber") ?? string.Empty;
                var quoteNumber = $"QT-{accountNo}-{DateTime.UtcNow:yyyyMMdd}";
                var issueDate   = DateTime.UtcNow;

                // サンプル明細（実運用では Quote Products を FetchXML で取得して差し替える）
                var items = new[]
                {
                    new PdfLineItem { Name = "コンサルティングサービス", Spec = "月額",   Quantity = 3, UnitPrice = 150000 },
                    new PdfLineItem { Name = "システム導入支援",         Spec = "一式",   Quantity = 1, UnitPrice = 500000 },
                    new PdfLineItem { Name = "ユーザートレーニング",     Spec = "1日",    Quantity = 2, UnitPrice =  80000 },
                    new PdfLineItem { Name = "保守サポート（年間）",     Spec = "12ヶ月", Quantity = 1, UnitPrice = 240000 },
                };

                tracing.Trace("MitsumoriPdfReport: calling Azure Functions");
                var json     = BuildJson(clientName, quoteNumber, issueDate, items);
                var pdfBytes = CallAzureFunction(json, tracing);

                var note = new Entity("annotation")
                {
                    ["objectid"]       = new EntityReference("account", target.Id),
                    ["objecttypecode"] = "account",
                    ["subject"]        = $"御見積書PDF_{issueDate:yyyyMMdd}",
                    ["filename"]       = $"mitsumori_{issueDate:yyyyMMdd}.pdf",
                    ["mimetype"]       = "application/pdf",
                    ["documentbody"]   = Convert.ToBase64String(pdfBytes),
                };

                svc.Create(note);
                tracing.Trace("MitsumoriPdfReport: done");
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracing.Trace("MitsumoriPdfReport error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    $"PDF 見積書生成中にエラーが発生しました: {ex.Message}", ex);
            }
        }

        private static string BuildJson(string clientName, string quoteNumber,
                                        DateTime issueDate, PdfLineItem[] items)
        {
            // Plugin サンドボックスでは Newtonsoft.Json が使えないため手書き JSON
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"clientName\":\"{0}\",",  EscapeJson(clientName));
            sb.AppendFormat("\"quoteNumber\":\"{0}\",", EscapeJson(quoteNumber));
            sb.AppendFormat("\"issueDate\":\"{0}\",",   issueDate.ToString("yyyy-MM-dd"));
            sb.Append("\"items\":[");

            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                sb.Append("{");
                sb.AppendFormat("\"name\":\"{0}\",",      EscapeJson(it.Name));
                sb.AppendFormat("\"spec\":\"{0}\",",      EscapeJson(it.Spec));
                sb.AppendFormat("\"quantity\":{0},",      it.Quantity);
                sb.AppendFormat("\"unitPrice\":{0}",      it.UnitPrice);
                sb.Append("}");
                if (i < items.Length - 1) sb.Append(",");
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private static byte[] CallAzureFunction(string json, ITracingService tracing)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                tracing.Trace("MitsumoriPdfReport: POST {0}", FunctionUrl);
                var response = client.PostAsync(FunctionUrl, content).GetAwaiter().GetResult();

                tracing.Trace("MitsumoriPdfReport: HTTP {0}", (int)response.StatusCode);
                response.EnsureSuccessStatusCode();

                return response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            }
        }

        private static string EscapeJson(string value) =>
            value.Replace("\\", "\\\\")
                 .Replace("\"", "\\\"")
                 .Replace("\n", "\\n")
                 .Replace("\r", "\\r")
                 .Replace("\t", "\\t");

        private struct PdfLineItem
        {
            public string  Name;
            public string  Spec;
            public int     Quantity;
            public decimal UnitPrice;
        }
    }
}
