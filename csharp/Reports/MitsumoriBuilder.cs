using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace D365.Reports
{
    /// <summary>
    /// 外部ライブラリ不要で見積書 .xlsx を生成するクラス。
    /// .xlsx = ZIP + XML の構造を直接組み立てる。
    /// </summary>
    internal static class MitsumoriBuilder
    {
        internal class LineItem
        {
            public string Name     { get; set; }
            public string Spec     { get; set; }
            public int    Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Amount => Quantity * UnitPrice;
        }

        internal static byte[] Build(
            string clientName,
            string quoteNumber,
            DateTime issueDate,
            IList<LineItem> items)
        {
            decimal subtotal = 0;
            foreach (var item in items) subtotal += item.Amount;
            decimal tax   = Math.Floor(subtotal * 0.10m);
            decimal total = subtotal + tax;

            // 共有文字列テーブル（全セル文字列を登録）
            var strings = new List<string>();
            var strIdx  = new Dictionary<string, int>();

            int Reg(string s)
            {
                s = s ?? string.Empty;
                if (!strIdx.TryGetValue(s, out var i))
                {
                    i = strings.Count;
                    strings.Add(s);
                    strIdx[s] = i;
                }
                return i;
            }

            Reg("御見積書");
            Reg($"見積番号：{quoteNumber}");
            Reg($"発行日：{issueDate:yyyy/MM/dd}");
            Reg($"有効期限：{issueDate.AddMonths(1):yyyy/MM/dd}");
            Reg($"{clientName}　御中");
            Reg("下記の通りお見積り申し上げます。");
            Reg("品名"); Reg("規格"); Reg("数量"); Reg("単価"); Reg("金額");
            foreach (var item in items) { Reg(item.Name); Reg(item.Spec ?? "-"); }
            Reg("小　計"); Reg("消費税（10%）"); Reg("税 込 合 計");

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    WriteEntry(zip, "[Content_Types].xml",      ContentTypes());
                    WriteEntry(zip, "_rels/.rels",              Rels());
                    WriteEntry(zip, "xl/workbook.xml",          Workbook());
                    WriteEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
                    WriteEntry(zip, "xl/styles.xml",            Styles());
                    WriteEntry(zip, "xl/sharedStrings.xml",     SharedStrings(strings));
                    WriteEntry(zip, "xl/worksheets/sheet1.xml",
                        Sheet(strIdx, quoteNumber, issueDate, clientName, items, subtotal, tax, total));
                }
                return ms.ToArray();
            }
        }

        // ---- シート XML ----

        private static string Sheet(
            Dictionary<string, int> idx,
            string quoteNumber, DateTime issueDate, string clientName,
            IList<LineItem> items,
            decimal subtotal, decimal tax, decimal total)
        {
            int S(string s) => idx.TryGetValue(s ?? string.Empty, out var i) ? i : 0;

            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            sb.AppendLine(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");

            // 列幅
            sb.AppendLine("  <cols>");
            sb.AppendLine(@"    <col min=""1"" max=""1"" width=""28"" customWidth=""1""/>");  // A 品名
            sb.AppendLine(@"    <col min=""2"" max=""2"" width=""14"" customWidth=""1""/>");  // B 規格
            sb.AppendLine(@"    <col min=""3"" max=""3"" width=""8""  customWidth=""1""/>");  // C 数量
            sb.AppendLine(@"    <col min=""4"" max=""4"" width=""14"" customWidth=""1""/>");  // D 単価
            sb.AppendLine(@"    <col min=""5"" max=""5"" width=""16"" customWidth=""1""/>");  // E 金額
            sb.AppendLine("  </cols>");

            sb.AppendLine("  <sheetData>");
            int r = 1;

            // ① タイトル「御見積書」（A1:E1 結合 / スタイル1=タイトル）
            Row(sb, r, () => Cs(sb, "A", r, S("御見積書"), 1)); r++;

            // ② 見積番号 / 発行日
            Row(sb, r, () =>
            {
                Cs(sb, "A", r, S($"見積番号：{quoteNumber}"),        7);
                Cs(sb, "D", r, S($"発行日：{issueDate:yyyy/MM/dd}"), 7);
            }); r++;

            // ③ 有効期限
            Row(sb, r, () => Cs(sb, "A", r, S($"有効期限：{issueDate.AddMonths(1):yyyy/MM/dd}"), 7)); r++;

            // ④ 空行
            sb.AppendLine($@"    <row r=""{r}""></row>"); r++;

            // ⑤ 顧客名（A5:E5 結合 / スタイル8=大きめ太字）
            Row(sb, r, () => Cs(sb, "A", r, S($"{clientName}　御中"), 8), ht: 24); r++;

            // ⑥ 空行
            sb.AppendLine($@"    <row r=""{r}""></row>"); r++;

            // ⑦ 挨拶文
            Row(sb, r, () => Cs(sb, "A", r, S("下記の通りお見積り申し上げます。"), 7)); r++;

            // ⑧ 空行
            sb.AppendLine($@"    <row r=""{r}""></row>"); r++;

            // ⑨ ヘッダー行（スタイル2=青背景白太字罫線）
            Row(sb, r, () =>
            {
                Cs(sb, "A", r, S("品名"), 2);
                Cs(sb, "B", r, S("規格"), 2);
                Cs(sb, "C", r, S("数量"), 2);
                Cs(sb, "D", r, S("単価"), 2);
                Cs(sb, "E", r, S("金額"), 2);
            }); r++;

            // ⑩ 明細行
            foreach (var item in items)
            {
                int cur = r;
                Row(sb, r, () =>
                {
                    Cs(sb,  "A", cur, S(item.Name),        3);  // 文字
                    Cs(sb,  "B", cur, S(item.Spec ?? "-"), 3);  // 文字
                    Cn(sb,  "C", cur, (double)item.Quantity,  4); // 数値
                    Cn(sb,  "D", cur, (double)item.UnitPrice, 4); // 数値
                    Cn(sb,  "E", cur, (double)item.Amount,    4); // 数値
                });
                r++;
            }

            // ⑪ 小計（A:D 結合 / スタイル5=合計ラベル）
            int subtotalRow = r;
            Row(sb, r, () =>
            {
                Cs(sb, "A", r, S("小　計"),    5);
                Cn(sb, "E", r, (double)subtotal, 6);
            }); r++;

            // ⑫ 消費税
            int taxRow = r;
            Row(sb, r, () =>
            {
                Cs(sb, "A", r, S("消費税（10%）"), 5);
                Cn(sb, "E", r, (double)tax,         6);
            }); r++;

            // ⑬ 税込合計（スタイル9/10=紺背景白太字）
            int totalRow = r;
            Row(sb, r, () =>
            {
                Cs(sb, "A", r, S("税 込 合 計"), 9);
                Cn(sb, "E", r, (double)total,     10);
            }, ht: 22); r++;

            sb.AppendLine("  </sheetData>");

            // セル結合定義
            var merges = new[]
            {
                "A1:E1",
                "A2:C2", "D2:E2",
                "A3:E3",
                $"A{subtotalRow}:D{subtotalRow}",
                $"A{taxRow}:D{taxRow}",
                $"A{totalRow}:D{totalRow}",
            };
            sb.AppendLine($@"  <mergeCells count=""{merges.Length}"">");
            foreach (var m in merges)
                sb.AppendLine($@"    <mergeCell ref=""{m}""/>");
            sb.AppendLine("  </mergeCells>");

            sb.Append("</worksheet>");
            return sb.ToString();
        }

        // ---- セル出力ヘルパー ----

        private static void Row(StringBuilder sb, int r, Action cells, double ht = 0)
        {
            var htAttr = ht > 0 ? $@" ht=""{ht}"" customHeight=""1""" : string.Empty;
            sb.AppendLine($@"    <row r=""{r}""{htAttr}>");
            cells();
            sb.AppendLine("    </row>");
        }

        // 文字列セル（共有文字列インデックスを参照）
        private static void Cs(StringBuilder sb, string col, int row, int sIdx, int style) =>
            sb.AppendLine($@"      <c r=""{col}{row}"" t=""s"" s=""{style}""><v>{sIdx}</v></c>");

        // 数値セル
        private static void Cn(StringBuilder sb, string col, int row, double val, int style) =>
            sb.AppendLine($@"      <c r=""{col}{row}"" t=""n"" s=""{style}""><v>{val}</v></c>");

        // ---- styles.xml ----

        private static string Styles() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <numFmts count=""1"">
    <numFmt numFmtId=""164"" formatCode=""#,##0""/>
  </numFmts>
  <fonts count=""5"">
    <font><sz val=""11""/><name val=""Calibri""/></font>                              <!-- 0 通常 -->
    <font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font> <!-- 1 白太字 -->
    <font><b/><sz val=""20""/><name val=""Calibri""/></font>                          <!-- 2 タイトル -->
    <font><b/><sz val=""11""/><name val=""Calibri""/></font>                          <!-- 3 太字 -->
    <font><b/><sz val=""14""/><name val=""Calibri""/></font>                          <!-- 4 顧客名 -->
  </fonts>
  <fills count=""5"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF4472C4""/></patternFill></fill> <!-- 2 青 ヘッダー -->
    <fill><patternFill patternType=""solid""><fgColor rgb=""FFEDEDED""/></patternFill></fill> <!-- 3 薄グレー 小計 -->
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF1F3864""/></patternFill></fill> <!-- 4 紺 合計 -->
  </fills>
  <borders count=""3"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border>
      <left style=""thin""><color auto=""1""/></left>
      <right style=""thin""><color auto=""1""/></right>
      <top style=""thin""><color auto=""1""/></top>
      <bottom style=""thin""><color auto=""1""/></bottom>
      <diagonal/>
    </border>
    <border>
      <left style=""medium""><color auto=""1""/></left>
      <right style=""medium""><color auto=""1""/></right>
      <top style=""medium""><color auto=""1""/></top>
      <bottom style=""medium""><color auto=""1""/></bottom>
      <diagonal/>
    </border>
  </borders>
  <cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>
  <cellXfs count=""11"">
    <!-- 0  デフォルト -->
    <xf numFmtId=""0""   fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <!-- 1  タイトル（大文字・中央揃え） -->
    <xf numFmtId=""0""   fontId=""2"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1"" applyAlignment=""1"">
      <alignment horizontal=""center"" vertical=""center""/>
    </xf>
    <!-- 2  列ヘッダー（青背景・白太字・罫線・中央） -->
    <xf numFmtId=""0""   fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1"">
      <alignment horizontal=""center""/>
    </xf>
    <!-- 3  明細テキスト（罫線） -->
    <xf numFmtId=""0""   fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""/>
    <!-- 4  明細数値（カンマ・右揃え・罫線） -->
    <xf numFmtId=""164"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyNumberFormat=""1"" applyBorder=""1"" applyAlignment=""1"">
      <alignment horizontal=""right""/>
    </xf>
    <!-- 5  小計ラベル（薄グレー・太字・右揃え・罫線） -->
    <xf numFmtId=""0""   fontId=""3"" fillId=""3"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1"">
      <alignment horizontal=""right""/>
    </xf>
    <!-- 6  小計数値（薄グレー・太字・カンマ・右揃え・罫線） -->
    <xf numFmtId=""164"" fontId=""3"" fillId=""3"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyNumberFormat=""1"" applyBorder=""1"" applyAlignment=""1"">
      <alignment horizontal=""right""/>
    </xf>
    <!-- 7  情報テキスト（罫線なし） -->
    <xf numFmtId=""0""   fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <!-- 8  顧客名（大きめ太字） -->
    <xf numFmtId=""0""   fontId=""4"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>
    <!-- 9  税込合計ラベル（紺背景・白太字・太罫線・右揃え） -->
    <xf numFmtId=""0""   fontId=""1"" fillId=""4"" borderId=""2"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1"" applyAlignment=""1"">
      <alignment horizontal=""right"" vertical=""center""/>
    </xf>
    <!-- 10 税込合計数値（紺背景・白太字・カンマ・太罫線・右揃え） -->
    <xf numFmtId=""164"" fontId=""1"" fillId=""4"" borderId=""2"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyNumberFormat=""1"" applyBorder=""1"" applyAlignment=""1"">
      <alignment horizontal=""right"" vertical=""center""/>
    </xf>
  </cellXfs>
</styleSheet>";

        // ---- 共有文字列 ----

        private static string SharedStrings(List<string> strings)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            sb.AppendLine($@"<sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" count=""{strings.Count}"" uniqueCount=""{strings.Count}"">");
            foreach (var s in strings)
                sb.AppendLine($"  <si><t xml:space=\"preserve\">{EscapeXml(s)}</t></si>");
            sb.Append("</sst>");
            return sb.ToString();
        }

        // ---- 共通パーツ ----

        private static string ContentTypes() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">
  <Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>
  <Default Extension=""xml""  ContentType=""application/xml""/>
  <Override PartName=""/xl/workbook.xml""            ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>
  <Override PartName=""/xl/worksheets/sheet1.xml""   ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>
  <Override PartName=""/xl/sharedStrings.xml""       ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>
  <Override PartName=""/xl/styles.xml""              ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>
</Types>";

        private static string Rels() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>
</Relationships>";

        private static string Workbook() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""
          xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""見積書"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>";

        private static string WorkbookRels() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet""     Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>
  <Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles""        Target=""styles.xml""/>
</Relationships>";

        private static void WriteEntry(ZipArchive zip, string path, string xml)
        {
            var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
            using (var sw = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                sw.Write(xml);
        }

        private static string EscapeXml(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
             .Replace("\"", "&quot;").Replace("'", "&apos;");
    }
}
