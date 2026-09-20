# システムトレイ常駐 Change Specification

この文書はIssueによる変更差分と検証Evidenceを記録する。変更後の外部仕様の正本は `specs/bdd.md` とし、本ファイルを現行仕様の正本として扱わない。

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0012` |
| Status | `Verified` |
| Source | [GitHub Issue #12](https://github.com/ryopan727/transfer-image-qr/issues/12) |
| Canonical BDD | [`specs/bdd.md`](bdd.md) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

Windows利用者が画像転送Serverを必要な間だけ常駐させ、Main Windowを閉じてもシステムトレイから再表示または明示終了できるようにする。

## Background / Current State

- Main Windowを閉じるとApplication Runが終了し、Kestrelも停止する。
- システムトレイIcon、再表示Menu、明示終了Menuは存在しない。
- 背景設定はLocal App Dataへ永続化されるが、常駐設定は保持されない。

## Scope

- 「閉じたときトレイに格納」設定のON/OFF
- 設定ON時のNotifyIcon表示とWindow CloseのHide化
- Tray IconのDouble-clickおよびContext MenuからのMain Window再表示
- Tray Context Menuからの明示的なApplication終了
- 常駐設定のLocal App Dataへの永続化と起動時復元
- Unit、Presentation、Integration Test

## Out of Scope

- Windows Login時の自動起動（Issue #14）
- Balloon通知
- 複数Instance制御
- Tray Icon用の独自画像Asset
- Application終了時の確認Dialog

## Actors / External Systems

- Windows利用者: 常駐設定を変更し、Windowを閉じ、Trayから再表示・終了する
- Windows Shell: Notification AreaへNotifyIconとContext Menuを表示する
- Windows filesystem: Local App Data内のJSON設定を保持する

## Functional Requirements

- FR-1: 利用者はMain Windowで「閉じたときトレイに格納」をON/OFFできる。
- FR-2: ONではNotifyIconを表示し、WindowのClose要求をCancelしてWindowだけを非表示にする。
- FR-3: OFFではNotifyIconを表示せず、WindowのClose要求でApplicationを終了する。
- FR-4: Tray IconのDouble-clickまたは「表示」MenuでMain Windowを通常表示し、前面へ復帰する。
- FR-5: Tray Menuの「終了」では常駐設定にかかわらずApplicationを終了する。
- FR-6: 常駐設定は変更時に保存し、再起動時に復元する。
- FR-7: 設定保存失敗は技術例外を露出せず、Main Window内へ利用者向け文言を表示する。

## Non-functional Requirements / Constraints

- NFR-1: 設定判断はPresenter/Application、JSON File I/OはInfrastructure、NotifyIcon操作はFormへ分離する。
- NFR-2: Windowを隠している間も既存KestrelとSession lifecycleを維持する。
- NFR-3: NotifyIcon、ContextMenuStrip、MenuItemをForm破棄時に確実にDisposeする。
- NFR-4: 設定Fileが存在しない、破損している、または未知Versionの場合はOFFへFallbackする。

## Interfaces / Data / UI Contract

### UI

- Main Window上部にAccessible Name付きの「閉じたときトレイに格納」CheckBoxを表示する。
- 設定ON時はNotification Areaへ「TransferImageQR」Iconを表示する。
- Tray Menuは「表示」と「終了」を提供する。
- 設定保存ErrorはCheckBox近傍へ簡潔に表示する。

### Data

- `%LocalAppData%/TransferImageQR/tray-settings.json`へVersionと`MinimizeToTray`をJSON保存する。
- 既定値は`MinimizeToTray = false`とし、従来のClose終了動作を維持する。

## Acceptance Criteria

- [x] AC-1: システムトレイへ常駐できる。
- [x] AC-2: Windowを閉じても設定に従い常駐できる。
- [x] AC-3: Tray IconからMain Windowを再表示できる。
- [x] AC-4: Tray Menuから明示的にApplicationを終了できる。
- [x] AC-5: 常駐設定を再起動後も維持できる。

## BDD Delta

| Change | Target Scenario | Before | After / Reason |
| --- | --- | --- | --- |
| Add | `BDD-TRAY-001` | なし | 設定ONでClose時にWindowを隠して常駐する |
| Add | `BDD-TRAY-002` | なし | Tray IconからMain Windowを再表示する |
| Add | `BDD-TRAY-003` | なし | Tray Menuから明示終了する |
| Add | `BDD-TRAY-004` | なし | 設定OFFでは従来どおりClose終了する |
| Add | `BDD-TRAY-005` | なし | 再起動後に常駐設定を復元する |

### Proposed Canonical Scenarios

```gherkin
Scenario: BDD-TRAY-001 Close時にシステムトレイへ格納する
  Given 「閉じたときトレイに格納」がONでMain Windowが表示されている
  When 利用者がMain Windowを閉じる
  Then Main Windowは非表示になる
  And TransferImageQRはNotifyIconを表示して動作を継続する

Scenario: BDD-TRAY-002 TrayからMain Windowを再表示する
  Given TransferImageQRがMain Windowを隠してTray常駐している
  When 利用者がTray IconをDouble-clickするか「表示」を選択する
  Then Main Windowが通常状態で前面へ表示される

Scenario: BDD-TRAY-003 TrayからApplicationを終了する
  Given TransferImageQRがTray常駐している
  When 利用者がTray Menuの「終了」を選択する
  Then Main WindowとNotifyIconが閉じる
  And HTTP Serverを含むApplication processが終了する

Scenario: BDD-TRAY-004 設定OFFではClose時に終了する
  Given 「閉じたときトレイに格納」がOFFでMain Windowが表示されている
  When 利用者がMain Windowを閉じる
  Then Main Windowが閉じる
  And TransferImageQR processが終了する

Scenario: BDD-TRAY-005 再起動後に常駐設定を復元する
  Given 「閉じたときトレイに格納」をONへ変更して保存している
  When TransferImageQRを終了して再起動する
  Then 常駐設定がONで表示される
  And Close時にTrayへ格納される
```

## Error / Edge Cases

- 設定保存に失敗した場合も現在Processでは選択状態を反映し、次回復元できない可能性を表示する。
- Windowが最小化状態でTrayから再表示された場合はNormalへ戻す。
- 明示終了時はClose要求をTray格納へ変換しない。
- Trayから複数回表示操作を行ってもWindowを重複生成しない。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | CheckBox、NotifyIcon、Context Menu、Close/Show/Exit配線 | FormはOS Control操作に限定 |
| Application | Tray設定Model、Store Port、Use Caseを追加 | WinForms型に依存しない |
| Infrastructure | Versioned JSON設定Storeを追加 | Local App Data File I/O |
| Domain | 変更なし | 転送Domainとは独立したApplication設定 |
| Tests / Harness | Use Case、JSON round-trip、Presenter、Form lifecycle tests | Shell上の実Tray表示は手動確認を残す |

## Test Strategy / Traceability

| AC | BDD Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1, AC-2 | BDD-TRAY-001 | Unit / Presentation | 設定反映、FormClosing cancel、NotifyIcon状態 | Passed |
| AC-3 | BDD-TRAY-002 | Presentation | Hide後のShow/Normal/Activate | Passed |
| AC-4 | BDD-TRAY-003 | Unit / Presentation / Runtime smoke | 明示Exit bypassとProcess終了 | Passed |
| AC-2 | BDD-TRAY-004 | Presentation | OFF時のClose完了 | Passed |
| AC-5 | BDD-TRAY-005 | Unit / Integration / Presentation | JSON round-tripとPresenter初期復元 | Passed |

## Unknowns / Human Decisions

- Decision: 既存利用者のClose終了動作を変えないため、常駐設定の既定値はOFFとする。
- Decision: 設定ON時だけNotifyIconを表示し、OFFへ戻した時点でIconを非表示にする。
- Residual risk: Windows Shell上のIcon描画やTaskbar挙動はUnit/Presentation Testだけでは完全に保証できない。

## Implementation Notes

- `TraySettingsUseCase`が常駐設定のProcess内状態と保存結果を扱う。
- `JsonTraySettingsStore`がLocal App Dataへversioned JSONをatomic保存し、不正DocumentをOFFへFallbackする。
- `Form1`がNotifyIcon、Context Menu、CheckBox、FormClosingをWinForms固有処理として実装し、判断はPresenterへ委譲する。
- Windows shutdown、Task Manager終了、Application ExitではTray格納を行わない。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| `task verify` | Passed | Build 0 warnings / 0 errors、Unit／Presentation 94 passed / 0 failed / 0 skipped |
| `task test:integration` | Passed | 13 passed / 0 failed / 0 skipped |
| Process runtime smoke | Passed | Main Window process起動継続と明示停止を確認 |
| Form lifecycle automation | Passed | NotifyIcon Visible、Close時Hide、Menu再表示、明示Exit、OFF時Disposeを確認 |
| Windows Notification Area手動確認 | Not Run | Windows UI制御サービス未構成のため実Shell目視は未実施 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #12、Canonical BDD、現行実装のGapを整理 |
| 2026-09-20 | Marked Verified | 実装、Unit／Integration／Form lifecycle／runtime smoke Evidenceを反映 |
