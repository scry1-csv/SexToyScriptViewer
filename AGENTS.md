# エージェントAI 指示書（リポジトリ固有）

このプロジェクトで即戦力となるために、コードベースから直接発見できる設計・ワークフロー・慣習を簡潔にまとめます。

- **目的と概要**: WPF を使ったデスクトップアプリで、スクリプト（funscript, CSV 系、coyotescript 等）を読み込み、OxyPlot を用いて時系列チャート表示・メディア同期を行います。起点は [App.xaml](App.xaml) と [MainWindow.xaml.cs](MainWindow.xaml.cs) です。

- **主要コンポーネントと責務**
  - UI / アプリ開始: [App.xaml](App.xaml) と [MainWindow.xaml.cs](MainWindow.xaml.cs)。ファイル開閉、ドラッグ&ドロップ、メディア制御（MediaElement）とタイマー同期ロジックがここにあります（例: DispatcherTimer を使った注釈同期、Seekbar 更新）。
  - 表示コンポーネント: [Control/ChartControl.xaml.cs](Control/ChartControl.xaml.cs) がプロットの描画・ズーム同期・注釈更新を担当します。
  - スクリプト抽象化: `Script` フォルダ内の `IScript` インターフェースと各実装（`Funscript`, `UFOTW`, `Vorze_SA`, `TimeRoter`, `CoyoteScript` 等）。読み込みロジックは [Script/ScriptUtil.cs](Script/ScriptUtil.cs#L1-L120) を参照（拡張子で切替え、順次試行するパターン）。

- **データフロー（高レベル）**
  1. ファイルを開く（UI の Open or Drag&Drop）。
  2. `ScriptUtil.LoadScript(path)` が適切なパーサを選択して `IScript` 実装を返す。([Script/ScriptUtil.cs](Script/ScriptUtil.cs#L1-L60))
  3. `ChartControl` を生成して `_chartControls` に追加、OxyPlot のデータ点に変換して表示。
  4. メディア再生時は `DispatcherTimer` で再生位置を取得し、全ての `ChartControl` に注釈同期を指示する（`MovePlayingAnnotation` 等）。

- **プロジェクト固有のパターン/注意点**
  - `IScript` は C# の抽象/インターフェース設計を活かし、`ToPlot()` や `MillisecondsToInternalTime()` を各実装が提供します。`IScript` の定義は [Script/IScript.cs](Script/IScript.cs#L1-L120) を参照。
  - スクリプト読み込み失敗時の振る舞い: `ScriptUtil.LoadScript` はデバッグ時に例外を投げますが、リリースでは `MessageBox` 表示して `null` を返す。例外ハンドリングの挙動はファイル内で分岐しています（デバッグ用ログとリリース用ユーザ通知）。
  - チャート同期: 複数の `ChartControl` 間でズーム/範囲を共有する実装があり、呼び出し元は `SyncChartsRange` を通じて範囲を配布します（`MainWindow` 側で管理）。

- **ビルド / 実行 / 発行（ファイルで判明する手順）**
  - SDK: `UseWPF=true`、ターゲットは `net10.0-windows7.0`（[SexToyScriptViewer.csproj](SexToyScriptViewer.csproj)）。
  - 開発ビルド（復元・ビルド）:
    - `dotnet restore`（通常不要だが CI で明示的に実行可）
    - `dotnet build` または Visual Studio でビルド
  - 実行: Visual Studio のデバッグ起動、または `dotnet run --project SexToyScriptViewer.csproj`（WPF のため VS の方が安定）。
  - 発行（リリース用 単一実行ファイル self-contained）:
    - 例: `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`
    - プロファイルがある場合は `dotnet publish /p:PublishProfile=FolderProfile` で Visual Studio の公開設定を利用可能（[Properties/PublishProfiles/FolderProfile.pubxml](Properties/PublishProfiles/FolderProfile.pubxml) を確認）。

- **外部依存とインテグレーション**
  - `OxyPlot.Wpf` を利用している（グラフ表示）。パッケージ参照は `.csproj` に記載。
  - 出力バイナリやリリースは `github-releases/` にヒントがあるが、CI スクリプトは見当たりません — 自動化が必要な場合は `dotnet publish` を使って下さい。

- **すぐに役立つ探索ポイント（具体例ファイル参照）**
  - UI/イベント処理: [MainWindow.xaml.cs](MainWindow.xaml.cs)
  - ビュー・ViewModel: [MainViewModel.cs](MainViewModel.cs)
  - スクリプトロード/パーサ: [Script/ScriptUtil.cs](Script/ScriptUtil.cs#L1-L120)
  - スクリプト抽象: [Script/IScript.cs](Script/IScript.cs#L1-L120)
  - チャート制御: [Control/ChartControl.xaml.cs](Control/ChartControl.xaml.cs)

- **未解決／要確認事項（レビュー時に尋ねてください）**
  - どのスクリプト仕様（CSV 列構成や funscript のバージョン）を最優先でサポートすべきか。
  - 自動ビルド/リリース（CI）を追加する意向があるか（現状リポジトリに CI 設定は見当たりません）。

フィードバック: ここをベースに更新・追加します。不明瞭な部分や深掘りしてほしい箇所を教えてください。
