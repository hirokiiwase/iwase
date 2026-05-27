# Reports — Plugin + Excel 帳票生成

Plugin 内で ClosedXML を使い、取引先レコードの情報を Excel に出力して  
添付ファイル（Annotation）としてレコードに保存するサンプルです。

## ファイル構成

```
Reports/
├── Reports.csproj          # プロジェクト定義（ClosedXML を参照）
├── AccountExcelReport.cs   # Plugin 本体
└── README.md               # このファイル
```

## 動作の流れ

```
取引先レコード 作成 / 更新
        ↓
Post-Operation Plugin 起動（非同期）
        ↓
svc.Retrieve で最新データ取得
        ↓
ClosedXML で Excel ファイルをメモリ上で生成
        ↓
Annotation エンティティとして添付ファイルに保存
        ↓
レコードの「タイムライン」に Excel が添付される
```

## セットアップ

### 1. ビルド

```bash
dotnet build csharp/Reports/Reports.csproj
```

### 2. ILMerge（ClosedXML を DLL に結合）

Dataverse Plugin サンドボックスは外部 DLL を個別に読めないため、  
`ILRepack` で依存ライブラリを 1 つの DLL にまとめる必要があります。

```bash
# ILRepack のインストール（初回のみ）
dotnet tool install -g dotnet-ilrepack

# マージ実行
ilrepack /out:D365.Reports.Merged.dll \
         bin/Debug/net462/D365.Reports.dll \
         bin/Debug/net462/ClosedXML.dll \
         bin/Debug/net462/DocumentFormat.OpenXml.dll
```

### 3. Plugin Registration Tool でアップロード

1. `D365.Reports.Merged.dll` を登録
2. Step を追加:
   - **Message**: Create
   - **Primary Entity**: account
   - **Stage**: Post-operation (40)
   - **Execution Mode**: Asynchronous

## 確認方法

取引先レコードを作成後、レコードの **タイムライン** を開くと  
`取引先レポート YYYY-MM-DD.xlsx` が添付されていれば成功です。

## 注意事項

- Plugin サンドボックスは **外部ネットワーク接続不可**（Dataverse Online の場合）
- ClosedXML はメモリ内で完結するため問題なく動作する
- 大量レコードを一度に処理するとタイムアウトする可能性があるため Asynchronous 推奨
