# 開発メモ

## 拡張機能

### VS Code（インストール済み）
- **Power Platform Tools** — 環境接続・pac CLI・ソリューション管理
- **C# Dev Kit** — Plugin / CWA の開発・ビルド
- **ESLint** — JS/TS のリアルタイム品質チェック
- **Dataverse DevTools** — エンティティ型定義の自動生成
- **PCF Builder** — PCF コンポーネントの雛形生成・デプロイ

### XrmToolBox（別途インストール推奨）
- **FetchXML Builder** — クエリを GUI で組み立て・検証
- **Plugin Trace Viewer** — Plugin 実行ログの確認・デバッグ
- **Metadata Browser** — テーブル・列定義をブラウズ
- **Ribbon Workbench** — リボンボタンを GUI で編集
- **WebResources Manager** — Web リソースの一覧・デプロイ

### ブラウザ拡張機能
- **Level up for Dynamics 365/Power Apps**（Chrome）
- **Pascalcase Metadata Browser**（Edge）

---

## 帳票出力の練習メニュー

### 1. Word テンプレート（コード不要）
- 設定 → テンプレート → ドキュメントテンプレート からアップロード
- 差し込みフィールドで取引先名・住所などを埋め込む

### 2. SSRS レポート
- Visual Studio で `.rdl` ファイルを作成
- FetchXML でデータ取得 → レイアウト設定 → Dynamics 365 にインポート

### 3. SLERT（アドオン）
- 30日無料トライアルあり: https://sourcelink.jp/products/slert/
- Excel テンプレートをアップロードするだけで帳票出力できる
- マクロ・PDF 変換対応

### 4. カスタム実装（Plugin + PDF 生成）← 練習予定
- Plugin 内で PDF ライブラリを使って帳票を生成し、添付ファイルとして保存する
- ライブラリ候補:
  - **QuestPDF** — OSS、.NET 向け、コードで PDF レイアウトを定義
  - **iTextSharp** — 老舗ライブラリ、商用利用は要ライセンス確認
- 大まかな流れ:
  1. Plugin の Execute メソッド内でデータ取得（FetchXML / LINQ）
  2. PDF ライブラリでバイト配列を生成
  3. `Annotation`（添付ファイル）エンティティとしてレコードに紐付けて保存

---

## Tips

<!-- ここに学習メモを追記していく -->

---

## Plugin + Excel 帳票実装（完了済み）

### 作成ファイル
| ファイル | 役割 |
|---------|------|
| `csharp/Reports/SimpleXlsxWriter.cs` | ゼロ依存 xlsx 生成（System.IO.Compression のみ） |
| `csharp/Reports/MitsumoriBuilder.cs` | 見積書レイアウトの xlsx 生成（セル結合・カンマ書式） |
| `csharp/Reports/MitsumoriReport.cs` | Plugin 本体。Account 作成時に見積書を Annotation に保存 |
| `csharp/Reports/AccountExcelReport.cs` | 取引先情報を xlsx で Annotation に保存 |

### 重要な注意点

#### ライブラリ選定の失敗と解決策
- **ClosedXML** → ILRepack で System.IO.Packaging バージョン衝突でクラッシュ
- **NPOI** → ILRepack でスタックオーバーフロー
- **解決**: xlsx = ZIP + XML なので `System.IO.Compression`（.NET 標準）で手書き生成
- 外部 DLL なし → ILRepack 不要 → Plugin DLL の署名のみでデプロイ可能

#### ビルドと署名
```powershell
# 1. snk ファイル生成（初回のみ）
sn.exe -k D365Reports.snk

# 2. ビルド
dotnet build

# 3. ILRepack（外部 DLL がある場合のみ必要）
ilrepack /keyfile:D365Reports.snk /out:D365.Reports.Merged.dll D365.Reports.dll ...

# 今回は外部 DLL なしなので D365.Reports.dll を直接登録
```

#### Plugin Registration Tool での登録手順
1. 接続 → 「Register New Assembly」
2. `D365.Reports.dll`（または Merged.dll）を選択
3. 「Register New Step」で以下を設定：
   - Message: `Create`
   - Primary Entity: `account`
   - Execution Mode: `Asynchronous`
   - Stage: `PostOperation (40)`

#### デバッグ方法
- XrmToolBox → Plugin Trace Viewer でログ確認
- `tracing.Trace("メッセージ")` でログ出力
- Plugin の Execution Mode は必ず **Asynchronous**（Synchronous は重い処理に不向き）

---

## Plugin + PDF 帳票実装（Azure Functions + QuestPDF）

### 構成図
```
Account レコード作成（D365）
    ↓
Plugin（Post-Create, Async）
    ↓ HTTP POST（JSON）
Azure Functions（.NET 8 isolated）
    ↓ QuestPDF で PDF 生成
    ↓ PDF バイト返却
Plugin → Annotation に保存（タイムラインに表示）
```

### 作成済みリソース
| リソース | 値 |
|---------|---|
| リソースグループ | `rg-d365-practice` |
| 関数アプリ名 | `iwase-pdf-func` |
| リージョン | Japan West（Japan East はクォータ 0 で失敗） |
| プラン | 従量課金 (Windows) |
| ランタイム | .NET 8 isolated |
| URL | `https://iwase-pdf-func-asg5aughcmgvcmck.japanwest-01.azurewebsites.net/api/generatepdf` |
| ローカルソース | `c:\Users\work\Desktop\iwase\azure\PdfFunc\` |

### 作成ファイル
| ファイル | 役割 |
|---------|------|
| `azure/PdfFunc/GeneratePdf.cs` | HTTP トリガー関数。JSON 受け取り → QuestPDF で PDF 生成 |
| `azure/PdfFunc/Program.cs` | 最小構成（OpenTelemetry は削除済み） |
| `azure/PdfFunc/local.settings.json` | ローカル実行設定（`UseDevelopmentStorage=true`） |

### リクエスト JSON 仕様
```json
{
  "clientName": "株式会社テスト",
  "quoteNumber": "QT-TEST-20260528",
  "issueDate": "2026-05-28",
  "items": [
    { "name": "コンサルティング", "spec": "月額", "quantity": 3, "unitPrice": 150000 }
  ]
}
```

---

## 次回最短実装ガイド（攻略方法）

### Azure Functions プロジェクトの最速セットアップ

```powershell
# 前提: func --version が 4.x を返すこと
# 前提: Az PowerShell モジュールがインストール済みであること

# 1. プロジェクト作成
mkdir azure\PdfFunc
cd azure\PdfFunc
func init --worker-runtime dotnet-isolated --target-framework net8.0

# 2. 関数追加
func new --name GeneratePdf --template "HTTP trigger" --authlevel anonymous

# 3. QuestPDF 追加
dotnet add package QuestPDF

# 4. Program.cs を最小構成に書き換える（OpenTelemetry 削除）
# → UseAzureMonitorExporter() があるとローカル起動でクラッシュする

# 5. ローカルテスト（Azurite を先に起動すること）
# VS Code: Ctrl+Shift+P → "Azurite: Start"
dotnet run

# 6. デプロイ
Connect-AzAccount -UseDeviceAuthentication
func azure functionapp publish <関数アプリ名>
```

### ハマりポイントと対処法

| 問題 | 原因 | 対処 |
|------|------|------|
| `func` が認識されない | PATH未設定またはインストール未完 | VS Code を再起動、またはターミナルを新規開 |
| `ERR_REQUIRE_ESM`（npm） | Node.js v20 と非互換 | MSI インストーラーを使う |
| `A connection string was not found` | Azurite が未起動 または Program.cs に `UseAzureMonitorExporter()` あり | Azurite 起動 + Program.cs を最小構成に |
| `GenerateBytes()` が見つからない | QuestPDF v2023以降でメソッド名変更 | `GeneratePdf()` に変更 |
| Japan East でクォータエラー | 会社サブスクリプションの地域制限 | Japan West に変更 |
| `SubscriptionIsOverQuotaForSku` | リージョンの VM クォータが 0 | 別リージョンを試す |
| デプロイ後 404 | コードが未デプロイ（App だけ作成） | `func azure functionapp publish` で再デプロイ |
| `UseAzureMonitorExporter()` エラー | Application Insights 接続文字列が未設定 | Program.cs から削除（ローカル開発時） |

### 日本語フォントについて
- QuestPDF はデフォルトで `Yu Gothic`（Windows 標準）を使用可能
- Azure Functions (Windows) でも Yu Gothic は利用可能
- Linux プランの場合は別途フォントファイルを埋め込む必要あり

### ライセンス設定（必須）
```csharp
// Run メソッドの先頭に必ず記述
QuestPDF.Settings.License = LicenseType.Community;
```

### Azure Functions Core Tools インストール方法
```powershell
# winget が使えない場合
# npm が使える場合 → Node.js v20 では ERR_REQUIRE_ESM が出るため NG
# → GitHub Releases から MSI を直接ダウンロードする
# https://github.com/Azure/azure-functions-core-tools/releases
# ファイル名: func-cli-x64.msi

# インストール後、VS Code を再起動して PATH を反映
func --version  # 4.x.x が表示されれば OK
```
