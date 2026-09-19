# 複数画像のドラッグ＆ドロップによるDraft作成 Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0002` |
| Status | `Verified` |
| Source | [GitHub Issue #2](https://github.com/ryopan727/transfer-image-qr/issues/2) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

Windows上の複数画像をアプリへドラッグ＆ドロップし、QR作成前の転送候補としてサムネイル一覧で確認できるようにする。

## Background / Current State

- MVP-001により、C# / .NET 8 Windows FormsアプリとApplication、Domain、Infrastructure、Unit Testの各プロジェクトが存在する。
- メイン画面は静的な案内だけを表示し、D&Dイベント、Draft状態、画像読み込み、サムネイル表示は未実装である。
- Issue #2の依存先MVP-001はPR #17のsquash mergeにより`main`へ取り込み済みである。

## Scope

- Windows Explorer等から複数ファイルをD&Dできる領域
- JPEG（`.jpg` / `.jpeg`）、PNG（`.png`）、WebP（`.webp`）のDraft追加
- 追加済み画像のサムネイルとファイル名の一覧表示
- 同じDraftへの複数回の追加操作
- Formから分離したDraft状態、Use Case、画像サムネイル処理、Presenter

## Out of Scope

- 10MB制限、20枚制限、画像内容の厳密な検証、拒否理由表示（MVP-003）
- 個別削除、全クリア、0枚時のQR作成制御（MVP-004）
- Active状態、QRコード、HTTP配信
- 重複画像の排除（Issueに指定がないため、同じファイルも各D&D操作として追加する）

## Actors / External Systems

- Windowsデスクトップユーザー
- Windows Explorer等のファイルD&D元
- ローカルファイルシステム

## Functional Requirements

- FR-1: メイン画面は複数ファイルのD&Dを受け付ける。
- FR-2: 拡張子がJPEG、PNG、WebPのファイルを現在のDraftへ追加する。
- FR-3: 各追加画像について、元ファイルを保持せず、元パスを参照したサムネイル表示用データを生成する。
- FR-4: Draft内の各画像をサムネイルとファイル名で一覧表示する。
- FR-5: QR作成前は追加のD&Dを受け付け、既存一覧へ追記する。
- FR-6: 複数ファイル中に未対応拡張子または読み込めない画像があっても、読み込める対応画像は追加する。

## Non-functional Requirements / Constraints

- NFR-1: DomainとApplicationはWindows Forms、System.Drawing、SkiaSharpへ依存しない。
- NFR-2: サムネイル作成はInfrastructureで行い、SkiaSharp固有型を外側へ漏らさない。
- NFR-3: サムネイル作成時に元画像ファイルをロックし続けない。
- NFR-4: D&D後の画像読み込み中もUIイベントを長時間同期ブロックしない。
- NFR-5: 元画像をコピー、再圧縮、変更しない。

## Interfaces / Data / UI Contract

### UI

- D&D領域には「画像をここにドロップ」と対応形式を表示する。
- ファイルをドラッグ中、受け付け可能な場合はCopy効果を表示する。
- 一覧にはサムネイルとファイル名を表示する。
- 0枚時は空状態の案内を表示し、追加後は画像件数を表示する。

### Data

- Draft画像は一意なID、元ファイルの絶対パス、ファイル名を保持する。
- サムネイルはUI表示用のPNGバイト列として境界を越す。
- 元画像自体のコピーはDraftへ保持しない。

## Acceptance Criteria

- [x] AC-1: 複数画像を一度にD&Dできる。
- [x] AC-2: JPEG、PNG、WebPの読み込み可能な画像がDraftへ追加される。
- [x] AC-3: 追加した各画像をサムネイルとファイル名の一覧で確認できる。
- [x] AC-4: QR作成前に再度D&Dすると、既存Draftを維持したまま画像が追加される。
- [x] AC-5: 混在入力で1ファイルの読み込みに失敗しても、他の正常な対応画像は追加される。

## BDD Scenarios

```gherkin
Scenario: SC-1 複数の対応画像を一度に追加する
  Given Draftが空である
  When JPEG、PNG、WebPをまとめてD&Dする
  Then 読み込めた3枚がDraftへ追加される
  And 3枚のサムネイルとファイル名が一覧表示される

Scenario: SC-2 Draftへ追加操作を繰り返す
  Given Draftに画像が1枚ある
  When 別の対応画像2枚をD&Dする
  Then Draftには既存画像を含む3枚が保持される
  And 一覧には3枚が表示される

Scenario: SC-3 未対応形式と読み込み失敗を分離する
  Given Draftが空である
  When 正常なPNG、未対応のTXT、読み込めないJPEGをまとめてD&Dする
  Then 正常なPNGだけがDraftへ追加される
  And 失敗したファイルによって正常なPNGは拒否されない

Scenario: SC-4 ファイル以外のD&Dを拒否する
  Given メイン画面が表示されている
  When ファイル一覧を含まないデータをドラッグする
  Then Drop効果はNoneになる
```

## Error / Edge Cases

- 空のファイル一覧ではDraftと表示を変更しない。
- 拡張子判定は大文字小文字を区別しない。
- 存在しない、アクセス不能、破損した画像はそのファイルだけ追加しない。
- サイズと最大枚数、詳細な拒否理由はMVP-003で扱う。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | D&D領域、一覧、空状態、Presenter接続を追加 | 現在のFormは静的表示のみ |
| Application | Draft追加Use CaseとサムネイルPortを追加 | File I/O型やWinForms型を持ち込まない |
| Domain | DraftとDraft画像を追加 | Draft状態は後続のActive遷移へ拡張予定だが先取りしない |
| Infrastructure | SkiaSharpによるJPEG/PNG/WebPサムネイル生成 | MITライセンス、バージョン4.152.1 |
| Tests / Harness | Domain、Application、Presenter、InfrastructureのUnit/Integration相当テストを追加 | 実一時ファイルで3形式のデコードも確認する |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1, AC-2 | SC-1 | Application + Infrastructure | `AddImagesToDraftUseCaseTests`, `SkiaImageThumbnailProviderTests` | Passed |
| AC-3 | SC-1 | Presentation | `MainPresenterTests`, `Form1Tests.PresenterResult_RendersThumbnailFileNameAndCount` | Passed |
| AC-4 | SC-2 | Domain + Presentation | `TransferDraftTests`, `MainPresenterTests` | Passed |
| AC-5 | SC-3 | Application | `ExecuteAsync_WithMixedInput_AddsValidImagesWithoutRejectingTheBatch` | Passed |
| AC-1 | SC-4 | Presentation / wiring evidence | `Form1Tests.MainForm_ExposesEnabledFileDropAreaAndEmptyDraftState`およびDragEnter/DragDrop配線確認 | Passed |

## Unknowns / Human Decisions

- Decision: ユーザーの既承認方針に従いWinFormsで実装する。
- Decision: WebP対応のためSkiaSharp 4.152.1をInfrastructureに限定して利用する。標準`System.Drawing`のみではWebPを安定してデコードできないため。
- 未解決の重要事項はなし。

## Implementation Notes

- Domainに元ファイルパスを保持する`TransferDraft`と`DraftImage`を追加した。元画像データはコピーしない。
- Application Use Caseが対応拡張子を個別処理し、サムネイル生成に失敗したファイルだけを除外する。
- InfrastructureのSkiaSharp ProviderがJPEG、PNG、WebPをデコードし、アスペクト比を維持した最大160x120のPNGサムネイルを生成する。
- Presenterが非同期追加中のD&Dを抑止し、追加分とDraft総数をViewへ反映する。
- FormはファイルD&D受付とListView描画に限定し、業務ルールや画像デコードを持たない。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象テストのRED確認 | Passed | 未実装のDraft、Use Case、Presenter、Provider型により意図どおりコンパイル失敗 |
| `dotnet restore TransferImageQR.sln` | Passed | SkiaSharp 4.152.1を含む全Projectを復元 |
| `dotnet build TransferImageQR.sln --no-restore` | Passed | 0 warnings, 0 errors |
| `dotnet test TransferImageQR.sln --no-build` | Passed | 16 total, 16 passed, 0 failed, 0 skipped |
| JPEG / PNG / WebP実ファイル試験 | Passed | 各形式からPNGサムネイルを生成し、元ファイルを直後に削除できることを確認 |
| WinForms起動確認 | Passed | `MainWindowTitle=TransferImageQR`、非ゼロHandle、`Responding=True` |
| OSポインターによる手動D&D | Not Run | native computer-use bridgeが無効。Form STAテスト、Presenterテスト、D&Dイベント配線確認を代替Evidenceとした |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #2とRepository Discoveryから実装・検証契約を確定 |
| 2026-09-20 | Marked Verified and recorded evidence | 全自動テスト、build、起動確認が完了 |
