# Draft画像編集 Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0004` |
| Status | `Verified` |
| Source | [GitHub Issue #4](https://github.com/ryopan727/transfer-image-qr/issues/4) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

転送セッションを確定する前に、利用者がDraft内の不要な画像を個別に削除または全消去し、転送対象を整えられるようにする。

## Background / Current State

- MVP-002/003により、対応画像をDraftへ追加し、サムネイル一覧と件数を表示できる。
- `TransferDraft`は追加と20枚上限のみを提供し、削除・全消去操作はない。
- メイン画面にはDraft編集ボタンとQR作成ボタンがない。
- Active状態と転送セッションはMVP-005で導入予定である。

## Scope

- Draft画像の選択と個別削除
- Draft画像の全クリア
- Draft件数に応じた編集操作と「QR作成」ボタンの有効状態
- Domain、Application、Presenter、WinForms表示の自動テスト

## Out of Scope

- QR作成操作による転送セッション生成、Active状態、5分失効（MVP-005）
- QRコード画像の生成と表示（MVP-007）
- 削除確認ダイアログ、Undo、並べ替え

## Actors / External Systems

- Windowsデスクトップアプリ利用者

## Functional Requirements

- FR-1: 選択したDraft画像をIDで個別削除できる。
- FR-2: Draft内の全画像を一括削除できる。
- FR-3: 削除後の件数と空状態を画面へ反映する。
- FR-4: Draftが0枚の間は「QR作成」を無効にし、1枚以上なら有効にする。
- FR-5: 個別削除は画像未選択時に実行できない。
- FR-6: 後続のActive状態から編集処理を呼ばないよう、編集可否を画面へ反映できる契約を設ける。Active遷移そのものはMVP-005で実装する。

## Non-functional Requirements / Constraints

- NFR-1: 削除と全消去の業務状態変更はFormへ実装しない。
- NFR-2: Domain/ApplicationはWinForms固有型へ依存しない。
- NFR-3: サムネイルの`Image`リソースは一覧から削除するときに破棄する。

## Interfaces / Data / UI Contract

### UI

- Draft一覧で1画像を選択すると「選択画像を削除」を有効にする。
- 「全クリア」はDraftが1枚以上のときだけ有効にする。
- 「QR作成」はDraftが1枚以上かつ編集可能なときだけ有効にする。
- 個別削除および全クリア後、一覧、件数、空状態を同期する。

## Acceptance Criteria

- [x] AC-1: Draftから画像を個別削除できる。
- [x] AC-2: Draftを全クリアできる。
- [x] AC-3: 0枚の場合「QR作成」は実行できない。
- [x] AC-4: Active後は画像追加・削除できない。MVP-004では編集不可通知後の防御とUI契約を検証し、Active遷移との結合は依存先MVP-005で検証する。

## BDD Scenarios

```gherkin
Scenario: SC-1 選択した画像を個別削除する
  Given Draftに2枚の画像がある
  When 1枚を選択して削除する
  Then 選択した画像だけがDraftと画面一覧から削除される
  And Draft件数は1枚になる

Scenario: SC-2 Draftを全クリアする
  Given Draftに複数の画像がある
  When 全クリアする
  Then Draftと画面一覧が空になる
  And 空状態が表示される

Scenario: SC-3 空のDraftではQR作成できない
  Given Draftが空である
  Then QR作成ボタンは無効である
  When 画像を1枚追加する
  Then QR作成ボタンは有効になる

Scenario: SC-4 編集不可状態を画面へ反映する
  Given Draftに画像があり編集可能である
  When 後続の転送セッション処理が編集不可を通知する
  Then 追加、個別削除、全クリア、QR作成の操作は無効になる
```

## Error / Edge Cases

- 存在しない画像IDの削除はDraftを変更せず失敗として返す。
- 画像未選択時は個別削除を実行しない。
- 空Draftの全クリアは安全なno-opとする。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | 編集ボタン、QR作成ボタン、Presenterイベント、View更新契約 | Formはイベント委譲と描画・リソース解放のみ |
| Application / Domain | ID指定削除、全消去のUse CaseとDomain操作 | WinForms型へ依存しない |
| Infrastructure | なし | ファイル自体は削除しない |
| Tests / Harness | Domain、Application、Presenter、STA Formテスト | UI E2Eは未導入 |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-1 | Domain + Application + Presentation | `Remove_WithExistingImageId_RemovesOnlyTheSelectedImage`、Use Case/Presenter/Formテスト | Passed |
| AC-2 | SC-2 | Domain + Application + Presentation | `Clear_WithImages_RemovesEveryImage`、Use Case/Presenter/Formテスト | Passed |
| AC-3 | SC-3 | Presentation | `DraftActions_ReflectImageCountAndEditingState` | Passed |
| AC-4 | SC-4 | Presentation | `DraftEditingCommands_WhenEditingIsDisabled_DoNotChangeDraft`とForm状態テスト | Passed（Active遷移結合はMVP-005） |

## Unknowns / Human Decisions

- Decision: Issue #4のActive後編集禁止は、MVP-005がActive状態を導入するため、本Issueでは編集可否を反映するUI契約を用意し、状態遷移との結合試験はMVP-005で完了する。
- Decision: 個別削除は一覧の単一選択を維持し、複数選択削除は追加しない。
- 未解決の重要事項はなし。

## Implementation Notes

- `TransferDraft`がID指定削除と全消去を担当し、元画像ファイル自体は変更しない。
- `EditDraftUseCase`がDomain操作と更新後件数をPresentationへ返す。
- Presenterは編集不可通知後の追加・削除・全消去を拒否し、ViewへボタンとD&Dの状態を通知する。
- Formは選択状態に応じた個別削除ボタン、全クリア、QR作成ボタンを表示し、削除したサムネイルの`Image`を破棄する。
- QR作成ボタンの実処理とActive遷移はMVP-005で接続する。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象テストのRED確認 | Passed | 未実装の編集Use Case、Domain API、View契約により意図どおりコンパイル失敗 |
| `task verify` | Passed | 38 total, 38 passed, 0 failed, 0 skipped |
| Build | Passed | 0 warnings, 0 errors |
| UI状態 | Passed | STA Formテストで空Draft、操作有効化、編集不可表示、サムネイル削除を検証 |
| E2E / 手動操作 | Not Run | UI Automation未導入。イベント委譲はPresentation/Formテストで代替 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #4と現在のMVP-003実装から検証契約を確定 |
| 2026-09-20 | Marked Verified and recorded evidence | 全Acceptance Criteriaの自動検証と品質Gateが完了 |
