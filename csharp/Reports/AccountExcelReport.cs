using System;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

// 既存の PluginBase / PluginContext を参照するため同じ名前空間にしない
// → D365.Reports として独立させ、IPlugin を直接実装する
namespace D365.Reports
{
    /// <summary>
    /// Post-Create / Post-Update plugin on the Account entity.
    /// Generates an Excel report and saves it as an annotation (attachment) on the record.
    ///
    /// Registration:
    ///   Message : Create (または Update)
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

                // 最新データを取得（Post 操作なので DB に確定済み）
                var account = svc.Retrieve("account", target.Id,
                    new ColumnSet(
                        "name",
                        "accountnumber",
                        "telephone1",
                        "emailaddress1",
                        "address1_city",
                        "address1_stateorprovince"));

                tracing.Trace("AccountExcelReport: generating Excel");
                var excelBytes = BuildExcel(account);

                // Annotation（添付ファイル）としてレコードに保存
                var note = new Entity("annotation")
                {
                    ["objectid"]       = new EntityReference("account", target.Id),
                    ["objecttypecode"] = "account",
                    ["subject"]        = $"取引先レポート {DateTime.UtcNow:yyyy-MM-dd}",
                    ["filename"]       = $"account_{DateTime.UtcNow:yyyyMMdd}.xlsx",
                    ["mimetype"]       = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    ["documentbody"]   = Convert.ToBase64String(excelBytes),
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

        private static byte[] BuildExcel(Entity account)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("取引先情報");

                // --- タイトル ---
                ws.Cell("A1").Value = "取引先レポート";
                ws.Cell("A1").Style.Font.Bold = true;
                ws.Cell("A1").Style.Font.FontSize = 18;
                ws.Range("A1:B1").Merge();

                ws.Cell("A2").Value = $"出力日時: {DateTime.UtcNow:yyyy/MM/dd HH:mm} (UTC)";
                ws.Cell("A2").Style.Font.FontColor = XLColor.Gray;
                ws.Range("A2:B2").Merge();

                // --- ヘッダー行 ---
                ws.Cell("A4").Value = "項目";
                ws.Cell("B4").Value = "値";
                var headerRange = ws.Range("A4:B4");
                headerRange.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                headerRange.Style.Font.FontColor       = XLColor.White;
                headerRange.Style.Font.Bold            = true;

                // --- データ行 ---
                var rows = new[]
                {
                    ("取引先名",       account.GetAttributeValue<string>("name")),
                    ("取引先番号",     account.GetAttributeValue<string>("accountnumber")),
                    ("電話番号",       account.GetAttributeValue<string>("telephone1")),
                    ("メールアドレス", account.GetAttributeValue<string>("emailaddress1")),
                    ("都道府県",       account.GetAttributeValue<string>("address1_stateorprovince")),
                    ("市区町村",       account.GetAttributeValue<string>("address1_city")),
                };

                int row = 5;
                foreach (var (label, value) in rows)
                {
                    ws.Cell(row, 1).Value = label;
                    ws.Cell(row, 2).Value = value ?? string.Empty;

                    // 偶数行に薄い背景色
                    if (row % 2 == 0)
                        ws.Range(row, 1, row, 2).Style.Fill.BackgroundColor = XLColor.LightGray;

                    row++;
                }

                // --- 罫線・列幅 ---
                ws.Range(4, 1, row - 1, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(4, 1, row - 1, 2).Style.Border.InsideBorder  = XLBorderStyleValues.Thin;
                ws.Column(1).Width = 20;
                ws.Column(2).Width = 35;

                using (var ms = new MemoryStream())
                {
                    wb.SaveAs(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
