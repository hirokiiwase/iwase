using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace D365.Reports
{
    /// <summary>
    /// 外部ライブラリ不要の最小 .xlsx 生成クラス。
    /// .xlsx は XML ファイルを ZIP で固めた形式なので
    /// System.IO.Compression だけで作れる。
    /// </summary>
    internal static class SimpleXlsxWriter
    {
        /// <summary>
        /// ヘッダー行 + データ行を受け取り .xlsx のバイト配列を返す。
        /// </summary>
        internal static byte[] Build(string sheetName, string[] headers, IList<string[]> dataRows)
        {
            // 全セルの文字列を共有文字列テーブルに登録
            var strings = new List<string>();
            var strIndex = new Dictionary<string, int>();

            int AddString(string s)
            {
                s = s ?? string.Empty;
                if (!strIndex.TryGetValue(s, out var idx))
                {
                    idx = strings.Count;
                    strings.Add(s);
                    strIndex[s] = idx;
                }
                return idx;
            }

            // ヘッダーと各行のインデックスを事前計算
            var headerIdx = new int[headers.Length];
            for (int c = 0; c < headers.Length; c++)
                headerIdx[c] = AddString(headers[c]);

            var dataIdx = new int[dataRows.Count][];
            for (int r = 0; r < dataRows.Count; r++)
            {
                dataIdx[r] = new int[dataRows[r].Length];
                for (int c = 0; c < dataRows[r].Length; c++)
                    dataIdx[r][c] = AddString(dataRows[r][c]);
            }

            using (var ms = new MemoryStream())
            {
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
                {
                    WriteEntry(zip, "[Content_Types].xml",     ContentTypes());
                    WriteEntry(zip, "_rels/.rels",             Rels());
                    WriteEntry(zip, "xl/workbook.xml",         Workbook(sheetName));
                    WriteEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
                    WriteEntry(zip, "xl/styles.xml",           Styles());
                    WriteEntry(zip, "xl/sharedStrings.xml",    SharedStrings(strings));
                    WriteEntry(zip, "xl/worksheets/sheet1.xml",
                        Sheet(headerIdx, dataIdx, headers.Length));
                }
                return ms.ToArray();
            }
        }

        // ---- ZIP エントリ書き込み ----

        private static void WriteEntry(ZipArchive zip, string path, string xml)
        {
            var entry = zip.CreateEntry(path, CompressionLevel.Fastest);
            using (var sw = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                sw.Write(xml);
        }

        // ---- 各 XML パーツ ----

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

        private static string Workbook(string sheetName) =>
$@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""
          xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
  <sheets>
    <sheet name=""{EscapeXml(sheetName)}"" sheetId=""1"" r:id=""rId1""/>
  </sheets>
</workbook>";

        private static string WorkbookRels() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">
  <Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet""     Target=""worksheets/sheet1.xml""/>
  <Relationship Id=""rId2"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"" Target=""sharedStrings.xml""/>
  <Relationship Id=""rId3"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles""        Target=""styles.xml""/>
</Relationships>";

        private static string Styles() => @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <fonts count=""2"">
    <font><sz val=""11""/><name val=""Calibri""/></font>
    <font><b/><sz val=""11""/><color rgb=""FFFFFFFF""/><name val=""Calibri""/></font>
  </fonts>
  <fills count=""3"">
    <fill><patternFill patternType=""none""/></fill>
    <fill><patternFill patternType=""gray125""/></fill>
    <fill><patternFill patternType=""solid""><fgColor rgb=""FF4472C4""/></patternFill></fill>
  </fills>
  <borders count=""2"">
    <border><left/><right/><top/><bottom/><diagonal/></border>
    <border>
      <left   style=""thin""><color auto=""1""/></left>
      <right  style=""thin""><color auto=""1""/></right>
      <top    style=""thin""><color auto=""1""/></top>
      <bottom style=""thin""><color auto=""1""/></bottom>
      <diagonal/>
    </border>
  </borders>
  <cellStyleXfs count=""1""><xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/></cellStyleXfs>
  <cellXfs count=""3"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>
    <xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""1"" xfId=""0"" applyFont=""1"" applyFill=""1"" applyBorder=""1""/>
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""1"" xfId=""0"" applyBorder=""1""/>
  </cellXfs>
</styleSheet>";

        private static string SharedStrings(List<string> strings)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            sb.AppendLine($@"<sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" count=""{strings.Count}"" uniqueCount=""{strings.Count}"">");
            foreach (var s in strings)
                sb.AppendLine($"  <si><t>{EscapeXml(s)}</t></si>");
            sb.Append("</sst>");
            return sb.ToString();
        }

        private static string Sheet(int[] headerIdx, int[][] dataIdx, int colCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>");
            sb.AppendLine(@"<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">");

            // 列幅設定
            sb.AppendLine("  <cols>");
            sb.AppendLine(@"    <col min=""1"" max=""1"" width=""22"" customWidth=""1""/>");
            sb.AppendLine(@"    <col min=""2"" max=""2"" width=""36"" customWidth=""1""/>");
            sb.AppendLine("  </cols>");

            sb.AppendLine("  <sheetData>");

            // ヘッダー行（スタイル 1 = 青背景・白太字・罫線）
            sb.AppendLine(@"    <row r=""1"">");
            for (int c = 0; c < headerIdx.Length; c++)
                sb.AppendLine($@"      <c r=""{ColName(c)}1"" t=""s"" s=""1""><v>{headerIdx[c]}</v></c>");
            sb.AppendLine("    </row>");

            // データ行（スタイル 2 = 罫線のみ）
            for (int r = 0; r < dataIdx.Length; r++)
            {
                int rowNum = r + 2;
                sb.AppendLine($@"    <row r=""{rowNum}"">");
                for (int c = 0; c < dataIdx[r].Length; c++)
                    sb.AppendLine($@"      <c r=""{ColName(c)}{rowNum}"" t=""s"" s=""2""><v>{dataIdx[r][c]}</v></c>");
                sb.AppendLine("    </row>");
            }

            sb.AppendLine("  </sheetData>");
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        // ---- ユーティリティ ----

        private static string ColName(int index)
        {
            // 0→A, 1→B, ... 25→Z, 26→AA ...
            var name = string.Empty;
            index++;
            while (index > 0)
            {
                index--;
                name = (char)('A' + index % 26) + name;
                index /= 26;
            }
            return name;
        }

        private static string EscapeXml(string s) =>
            s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
             .Replace("\"", "&quot;").Replace("'", "&apos;");
    }
}
