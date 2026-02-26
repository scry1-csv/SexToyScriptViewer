# エージェントAI 指示書（リポジトリ固有）

このプロジェクトで即戦力となるために、コードベースから直接発見できる設計・ワークフロー・慣習を簡潔にまとめます。

- **目的と概要**: WPF を使ったデスクトップアプリで、スクリプト（funscript, CSV 系、coyotescript 等）を読み込み、OxyPlot を用いて時系列チャート表示・メディア同期を行います。アーキテクチャはMVCパターンを採用しており、起点は [App.xaml](App.xaml) 、[MainWindow.xaml.cs](MainWindow.xaml.cs) および [Controller.cs](Controller.cs) です。

- **主要コンポーネントと責務**
  - View (UI): [MainWindow.xaml.cs](MainWindow.xaml.cs) はアプリウィンドウ、[Control/ChartControl.xaml.cs](Control/ChartControl.xaml.cs) はチャート部分の描画のみに専念する純粋なViewです。イベント発生時はControllerへ通知します。
  - Controller: [Controller.cs](Controller.cs) が主力ロジックを担います。ファイル開閉、メディア制御（MediaElement）、DispatcherTimerを使った注釈同期、ViewModelやViewへの描画指示を集約しています。
  - Model (スクリプト処理): `Script` フォルダ内の各クラス（`Funscript`, `UFOTW`, `Vorze_SA`, `TimeRoter`, `CoyoteScript`）。純粋なデータ保持クラスとして実装されています。読み込みロジックは [Script/ScriptUtil.cs](Script/ScriptUtil.cs) を参照（拡張子で切替え、順次試行するパターン）。

- **データフロー（高レベル）**
  1. ファイルを開く（UI の Open or Drag&Drop イベントを Controller がハンドリング）。
  2. `ScriptUtil.LoadScript(path)` が適切なパーサを選択してモデル（`Funscript`等のオブジェクト）を返す。
  3. Controller がモデルからOxyPlot用データ点群やフォーマッタ等を生成し、`ChartControl` の初期化メソッドへ引き渡して表示。
  4. メディア再生時は Controller が `DispatcherTimer` で再生位置を取得し、全ての `ChartControl` に注釈同期とUI更新の指示を出す。

- **プロジェクト固有のパターン/注意点**
  - スクリプト読み込み失敗時の振る舞い: `ScriptUtil.LoadScript` は内部的にエラーを返しますが、最終的にControllerからUIを介した `MessageBox` 等でのユーザ通知へ連携します。
  - チャート同期: Controller によって複数の `ChartControl` 間のズーム/範囲の共有や同期処理が集中管理されています（`SyncChartsRange`メソッド等）。

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
  - コントローラー層: [Controller.cs](Controller.cs)
  - UI/ビュー層: [MainWindow.xaml.cs](MainWindow.xaml.cs), [Control/ChartControl.xaml.cs](Control/ChartControl.xaml.cs)
  - ViewModel: [MainViewModel.cs](MainViewModel.cs)
  - スクリプトロード/パーサ: [Script/ScriptUtil.cs](Script/ScriptUtil.cs)

- **未解決／要確認事項（レビュー時に尋ねてください）**
  - どのスクリプト仕様（CSV 列構成や funscript のバージョン）を最優先でサポートすべきか。
  - 自動ビルド/リリース（CI）を追加する意向があるか（現状リポジトリに CI 設定は見当たりません）。

フィードバック: ここをベースに更新・追加します。不明瞭な部分や深掘りしてほしい箇所を教えてください。
