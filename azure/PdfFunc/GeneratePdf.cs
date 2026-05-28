using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace PdfFunc;

public class GeneratePdf
{
    private readonly ILogger<GeneratePdf> _logger;

    public GeneratePdf(ILogger<GeneratePdf> logger)
    {
        _logger = logger;
    }

    [Function("GeneratePdf")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonSerializer.Deserialize<QuoteRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (request == null)
            return new BadRequestObjectResult("Invalid request body");

        _logger.LogInformation("Generating PDF for {Client}", request.ClientName);

        var pdfBytes = BuildPdf(request);
        return new FileContentResult(pdfBytes, "application/pdf")
        {
            FileDownloadName = $"mitsumori_{DateTime.UtcNow:yyyyMMdd}.pdf"
        };
    }

    private static byte[] BuildPdf(QuoteRequest req)
    {
        var subtotal = req.Items.Sum(i => (decimal)i.Quantity * i.UnitPrice);
        var tax      = Math.Floor(subtotal * 0.10m);
        var total    = subtotal + tax;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontFamily("Yu Gothic").FontSize(10));

                page.Content().Column(col =>
                {
                    // タイトル
                    col.Item().AlignCenter()
                        .Text("御　見　積　書").FontSize(22).Bold();

                    col.Item().Height(16);

                    // 見積番号・発行日
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"見積番号：{req.QuoteNumber}");
                        row.RelativeItem().AlignRight()
                            .Text($"発行日：{req.IssueDate:yyyy年MM月dd日}");
                    });
                    col.Item().Text("有効期限：発行日より30日間").FontColor("#666666");

                    col.Item().Height(24);

                    // 宛先
                    col.Item().Text($"{req.ClientName}　御中").FontSize(16).Bold();

                    col.Item().Height(16);

                    // 挨拶文
                    col.Item().Text(
                        "平素より格別のご高配を賜り、厚く御礼申し上げます。\n" +
                        "下記の通りお見積り申し上げます。");

                    col.Item().Height(20);

                    // 明細テーブル
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(4); // 品名
                            c.RelativeColumn(2); // 規格
                            c.RelativeColumn(1); // 数量
                            c.RelativeColumn(2); // 単価
                            c.RelativeColumn(2); // 金額
                        });

                        // ヘッダー
                        static IContainer HeaderCell(IContainer c) =>
                            c.Background("#1F4E79").Padding(6).AlignCenter();

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("品名").FontColor(Colors.White).Bold();
                            h.Cell().Element(HeaderCell).Text("規格").FontColor(Colors.White).Bold();
                            h.Cell().Element(HeaderCell).Text("数量").FontColor(Colors.White).Bold();
                            h.Cell().Element(HeaderCell).Text("単価").FontColor(Colors.White).Bold();
                            h.Cell().Element(HeaderCell).Text("金額").FontColor(Colors.White).Bold();
                        });

                        // 明細行
                        foreach (var item in req.Items)
                        {
                            var amount = (decimal)item.Quantity * item.UnitPrice;

                            static IContainer DataCell(IContainer c) =>
                                c.BorderBottom(0.5f).BorderColor("#CCCCCC").Padding(6);

                            table.Cell().Element(DataCell).Text(item.Name);
                            table.Cell().Element(DataCell).AlignCenter().Text(item.Spec);
                            table.Cell().Element(DataCell).AlignRight().Text(item.Quantity.ToString());
                            table.Cell().Element(DataCell).AlignRight().Text($"¥{item.UnitPrice:#,##0}");
                            table.Cell().Element(DataCell).AlignRight().Text($"¥{amount:#,##0}");
                        }
                    });

                    col.Item().Height(12);

                    // 合計欄（右寄せ）
                    col.Item().AlignRight().Column(totals =>
                    {
                        TotalRow(totals, "小　計", subtotal, "#F2F2F2", "#000000");
                        TotalRow(totals, "消費税（10%）", tax, "#F2F2F2", "#000000");
                        TotalRow(totals, "合　計　金　額", total, "#1F4E79", "#FFFFFF");
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void TotalRow(ColumnDescriptor col, string label, decimal value,
                                  string bgColor, string textColor)
    {
        col.Item().MinWidth(280).Row(row =>
        {
            row.RelativeItem().Background(bgColor).Padding(6)
                .Text(label).FontColor(textColor).Bold();
            row.ConstantItem(120).Background(bgColor).Padding(6).AlignRight()
                .Text($"¥{value:#,##0}").FontColor(textColor).Bold();
        });
    }
}

public class QuoteRequest
{
    public string ClientName  { get; set; } = string.Empty;
    public string QuoteNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public List<LineItem> Items { get; set; } = new();
}

public class LineItem
{
    public string  Name      { get; set; } = string.Empty;
    public string  Spec      { get; set; } = string.Empty;
    public int     Quantity  { get; set; }
    public decimal UnitPrice { get; set; }
}
