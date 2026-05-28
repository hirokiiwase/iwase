using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace D365.Reports
{
    /// <summary>
    /// Post-Create plugin on the Account entity.
    /// Generates an Excel (.xlsx) report and saves it as an annotation on the record.
    ///
    /// Registration:
    ///   Message : Create
    ///   Entity  : account
    ///   Stage   : Post-Operation (40)
    ///   Mode    : Asynchronous
    /// </summary>
    public class AccountExcelReport : IPlugin
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

                tracing.Trace("AccountExcelReport: fetching account {0}", target.Id);

                var account = svc.Retrieve("account", target.Id,
                    new ColumnSet(
                        "name",
                        "accountnumber",
                        "telephone1",
                        "emailaddress1",
                        "address1_city",
                        "address1_stateorprovince"));

                tracing.Trace("AccountExcelReport: generating Excel");
                var xlsxBytes = BuildXlsx(account);

                var note = new Entity("annotation")
                {
                    ["objectid"]       = new EntityReference("account", target.Id),
                    ["objecttypecode"] = "account",
                    ["subject"]        = $"取引先レポート {DateTime.UtcNow:yyyy-MM-dd}",
                    ["filename"]       = $"account_{DateTime.UtcNow:yyyyMMdd}.xlsx",
                    ["mimetype"]       = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ["documentbody"]   = Convert.ToBase64String(xlsxBytes),
                };

                svc.Create(note);
                tracing.Trace("AccountExcelReport: annotation saved");
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracing.Trace("AccountExcelReport error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    $"Excel 生成中にエラーが発生しました: {ex.Message}", ex);
            }
        }

        private static byte[] BuildXlsx(Entity account)
        {
            var headers = new[] { "項目", "値" };

            var rows = new List<string[]>
            {
                new[] { "取引先名",       account.GetAttributeValue<string>("name")                      ?? string.Empty },
                new[] { "取引先番号",     account.GetAttributeValue<string>("accountnumber")             ?? string.Empty },
                new[] { "電話番号",       account.GetAttributeValue<string>("telephone1")                ?? string.Empty },
                new[] { "メールアドレス", account.GetAttributeValue<string>("emailaddress1")             ?? string.Empty },
                new[] { "都道府県",       account.GetAttributeValue<string>("address1_stateorprovince")  ?? string.Empty },
                new[] { "市区町村",       account.GetAttributeValue<string>("address1_city")             ?? string.Empty },
                new[] { "出力日時 (UTC)", DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss")                               },
            };

            return SimpleXlsxWriter.Build("取引先情報", headers, rows);
        }
    }
}
