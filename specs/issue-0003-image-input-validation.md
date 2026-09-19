# 転送画像の入力バリデーション Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0003` |
| Status | `Verified` |
| Source | [GitHub Issue #3](https://github.com/ryopan727/transfer-image-qr/issues/3) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

Draftへ追加する画像を対応形式、ファイルサイズ、最大枚数で検証し、不正ファイルだけを除外して利用者へファイル名と拒否理由を示す。

## Background / Current State

- MVP-002のPR #18で複数画像D&D、Draft、サムネイル一覧が実装されている。
- 現在は対応拡張子とサムネイル生成成功だけを判定し、10MB制限、20枚制限、拒否理由の表示はない。
- 本作業はMVP-002ブランチを起点とする積み上げブランチで進める。

## Scope

- JPEG、PNG、WebPの拡張子と実デコード形式の検証
- 1ファイル10MB以下の制限
- Draft全体20枚以下の制限
- 複数入力のファイル単位検証
- 拒否したファイル名と理由の画面表示
- 境界値と混在入力のUnit / Presentation / Infrastructureテスト

## Out of Scope

- Draft画像の削除・全クリア（MVP-004）
- Active状態とActive後の変更禁止（MVP-005）
- 画像のウイルス検査やメタデータ除去
- ファイル内容の修復

## Functional Requirements

- FR-1: `.jpg`、`.jpeg`、`.png`、`.webp`以外を拒否する。
- FR-2: 拡張子と実際にデコードされた画像形式が一致しない画像を拒否する。
- FR-3: 10MB（10 * 1024 * 1024 bytes）以下を許可し、1byteでも超過した画像を拒否する。
- FR-4: Draftへ保持できる画像は20枚までとする。
- FR-5: 複数入力は入力順に検証し、空き枠まで正常画像を追加し、残りを拒否する。
- FR-6: 1ファイルの失敗でバッチ全体を拒否しない。
- FR-7: 拒否結果ごとにファイル名と利用者向け理由を表示する。

## Non-functional Requirements / Constraints

- NFR-1: サイズ・枚数・形式ルールはWinFormsへ実装しない。
- NFR-2: DomainはInfrastructure、FileInfo、SkiaSharpへ依存しない。
- NFR-3: 技術例外の詳細を一般UIへ表示しない。
- NFR-4: 最大枚数判定後は不要な画像デコードを行わない。

## Interfaces / Data / UI Contract

### Validation results

- `UnsupportedFormat`: 対応外の拡張子
- `FileTooLarge`: 10MB超
- `DraftLimitReached`: 20枚の上限超過
- `UnreadableImage`: 存在しない、アクセス不能、破損などで読めない
- `FileFormatMismatch`: 拡張子と実画像形式の不一致

### UI messages

- `ファイル名: 対応していない形式です。`
- `ファイル名: ファイルサイズが10MBを超えています。`
- `ファイル名: Draftは最大20枚です。`
- `ファイル名: 画像を読み込めません。`
- `ファイル名: 拡張子と画像形式が一致しません。`

## Acceptance Criteria

- [x] AC-1: 10MBちょうどの画像は追加され、10MBを1byteでも超える画像は追加されない。
- [x] AC-2: 非対応拡張子、読み込み不能画像、拡張子と実形式が異なる画像は追加されない。
- [x] AC-3: Draftは最大20枚で、21枚目以降は追加されない。
- [x] AC-4: 複数D&D時、正常ファイルは追加し、不正ファイルのみ拒否する。
- [x] AC-5: 拒否した各ファイルのファイル名と理由が画面へ表示される。

## BDD Scenarios

```gherkin
Scenario: SC-1 10MB境界を判定する
  Given Draftが空である
  When 10MBちょうどと10MB+1byteの対応画像を追加する
  Then 10MBちょうどの画像だけが追加される
  And 超過画像にはサイズ超過理由が返る

Scenario: SC-2 最大20枚まで追加する
  Given Draftに19枚ある
  When 正常画像2枚を追加する
  Then 入力順の先頭1枚が追加されDraftは20枚になる
  And 2枚目には最大枚数理由が返る

Scenario: SC-3 形式の不正を分離する
  Given Draftが空である
  When 正常PNG、TXT、破損JPEG、PNG内容のJPEG拡張子ファイルを追加する
  Then 正常PNGだけが追加される
  And 他の各ファイルに対応する拒否理由が返る

Scenario: SC-4 拒否結果を表示する
  Given メイン画面が表示されている
  When 拒否結果を含む追加処理が完了する
  Then 拒否一覧にファイル名と一般利用者向け理由が表示される
  And 技術例外詳細は表示されない
```

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | 拒否結果ViewModel、Presenter変換、拒否一覧を追加 | Formはメッセージ描画のみ |
| Application | ファイルサイズPort、検証順序、拒否結果を追加 | 個別失敗を結果として返す |
| Domain | Draft最大20枚の不変条件を追加 | 21枚目をDomainでも防止 |
| Infrastructure | FileInfoによるサイズ取得、Skia実形式返却 | 技術例外は失敗結果へ変換 |
| Tests | 10MB、20/21枚、形式、混在、表示文言を追加 | 境界値をUnit中心に検証 |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-1 | Application | `ExecuteAsync_AtTenMegabyteBoundary_AcceptsExactSizeAndRejectsOneByteOver` | Passed |
| AC-2 | SC-3 | Application + Infrastructure | 混在入力テスト、Skia実形式テスト、File metadataテスト | Passed |
| AC-3 | SC-2 | Domain + Application | `TryAdd_WhenDraftAlreadyContainsTwentyImages_RejectsTheTwentyFirst`および19+2入力テスト | Passed |
| AC-4 | SC-1, SC-3 | Application | `ExecuteAsync_WithMixedInvalidInput_AddsOnlyValidImagesAndReturnsEachReason` | Passed |
| AC-5 | SC-4 | Presentation | Presenter理由変換Theory、`RejectedImages_AreRenderedWithFileNameAndReason` | Passed |

## Unknowns / Human Decisions

- Decision: 10MBは10 * 1024 * 1024 bytesとする。
- Decision: 複数入力は入力順に処理し、20枚へ達した後の対応画像を最大枚数超過として拒否する。
- Decision: 拡張子とデコード形式の不一致は、不正・誤配信防止のため拒否する。
- 未解決の重要事項はなし。

## Implementation Notes

- Domainの`TransferDraft`が最大20枚を不変条件として保持し、`TryAdd`でも上限を越えない。
- Applicationは拡張子、Draft容量、ファイル取得、10MB、画像デコード、実形式一致の順で検証し、不要なデコードを避ける。
- 各拒否を`RejectedDraftImage`と理由enumで返し、Presenterが日本語の一般利用者向け文言へ変換する。
- Infrastructureは`FileInfo`の技術例外を失敗結果へ変換し、SkiaSharpの実デコード形式をDomainの形式へ変換する。
- Formは拒否一覧の描画だけを担当し、例外詳細や検証ロジックを持たない。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象テストのRED確認 | Passed | 未実装のmetadata Port、拒否結果、最大枚数API、表示ViewModelにより意図どおりコンパイル失敗 |
| `dotnet restore TransferImageQR.sln` | Passed | 全Project最新 |
| `dotnet build TransferImageQR.sln --no-restore` | Passed | 0 warnings, 0 errors |
| `dotnet test TransferImageQR.sln --no-build` | Passed | 27 total, 27 passed, 0 failed, 0 skipped |
| 境界値 | Passed | 10MB / 10MB+1byte、20枚 / 21枚目を検証 |
| UI表示 | Passed | 5種類の理由変換とForm拒否一覧をSTA Presentation Testで検証 |
| WinForms起動確認 | Passed | `MainWindowTitle=TransferImageQR`、非ゼロHandle、`Responding=True` |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #3とMVP-002実装から検証契約を確定 |
| 2026-09-20 | Marked Verified and recorded evidence | 全Acceptance Criteriaの自動検証と品質Gateが完了 |
