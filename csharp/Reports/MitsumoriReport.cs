using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace D365.Reports
{
    /// <summary>
    /// Post-Create plugin on the Account entity.
    /// 見積書 (.xlsx) を生成してタイムラインに添付する。
    ///
    /// Registration:
    ///   Message : Create
    ///   Entity  : account
    ///   Stage   : Post-Operation (40)
    ///   Mode    : Asynchronous
    /// </summary>
    public class MitsumoriReport : IPlugin
    {
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

                tracing.Trace("MitsumoriReport: fetching account");

                var account = svc.Retrieve("account", target.Id,
                    new ColumnSet("name", "accountnumber"));

                var clientName  = account.GetAttributeValue<string>("name")          ?? string.Empty;
                var accountNo   = account.GetAttributeValue<string>("accountnumber") ?? string.Empty;
                var quoteNumber = $"QT-{accountNo}-{DateTime.UtcNow:yyyyMMdd}";

                // --- サンプル明細 ---
                // 実運用では Quote Products を FetchXML で取得して差し替える
                var items = new List<MitsumoriBuilder.LineItem>
                {
                    new MitsumoriBuilder.LineItem { Name = "コンサルティングサービス", Spec = "月額",  Quantity = 3, UnitPrice = 150000 },
                    new MitsumoriBuilder.LineItem { Name = "システム導入支援",         Spec = "一式",  Quantity = 1, UnitPrice = 500000 },
                    new MitsumoriBuilder.LineItem { Name = "ユーザートレーニング",     Spec = "1日",   Quantity = 2, UnitPrice =  80000 },
                    new MitsumoriBuilder.LineItem { Name = "保守サポート（年間）",     Spec = "12ヶ月",Quantity = 1, UnitPrice = 240000 },
                };

                tracing.Trace("MitsumoriReport: generating xlsx");
                var bytes = MitsumoriBuilder.Build(clientName, quoteNumber, DateTime.UtcNow, items);

                var note = new Entity("annotation")
                {
                    ["objectid"]       = new EntityReference("account", target.Id),
                    ["objecttypecode"] = "account",
                    ["subject"]        = $"御見積書_{DateTime.UtcNow:yyyyMMdd}",
                    ["filename"]       = $"mitsumori_{DateTime.UtcNow:yyyyMMdd}.xlsx",
                    ["mimetype"]       = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ["documentbody"]   = Convert.ToBase64String(bytes),
                };

                svc.Create(note);
                tracing.Trace("MitsumoriReport: done");
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracing.Trace("MitsumoriReport error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    $"見積書生成中にエラーが発生しました: {ex.Message}", ex);
            }
        }
    }
}
