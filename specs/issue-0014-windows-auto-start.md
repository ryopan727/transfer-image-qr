# Windows自動起動 Change Specification

この文書はIssueによる変更差分と検証Evidenceを記録する。変更後の外部仕様の正本は `specs/bdd.md` とし、本ファイルを現行仕様の正本として扱わない。

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0014` |
| Status | `Verified` |
| Source | [GitHub Issue #14](https://github.com/ryopan727/transfer-image-qr/issues/14) |
| Canonical BDD | [`specs/bdd.md`](bdd.md) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

Windows利用者がTransferImageQRをLogin時に自動起動するか選択し、その登録をMain Windowから安全に追加・解除できるようにする。

## Background / Current State

- Applicationは手動起動だけを提供し、Windows Login時の起動登録を持たない。
- Issue #12でTray常駐設定と明示終了を提供している。
- Main Windowには自動起動の状態表示・変更UIがない。

## Scope

- Windows Login時の自動起動ON/OFF CheckBox
- Current UserのRun Registry value追加・照合・削除
- Application起動時の登録状態読込
- Pathに空白を含むExecutableの安全な引用
- Registry Access失敗の利用者向け表示
- Application、Infrastructure、Presentation Test

## Out of Scope

- 全User向け登録、管理者権限要求
- Task SchedulerまたはWindows Service
- Startup時にMain Windowを自動的に隠す追加Option
- Registry以外のStartup Folder方式
- Installerによる登録

## Actors / External Systems

- Windows利用者: 自動起動をON/OFFする
- Windows Registry (HKCU): Login時起動Commandを永続化する
- Windows Shell: Login後にRun valueのCommandを起動する

## Functional Requirements

- FR-1: Main Windowで「Windowsログイン時に起動」をON/OFFできる。
- FR-2: ONでは`HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run`へ現在ExecutableのCommandを登録する。
- FR-3: OFFではTransferImageQRのRun valueを削除する。
- FR-4: 起動時に現在Executableと一致する登録状態を読み込み、CheckBoxへ反映する。
- FR-5: Executable PathをDouble quoteで囲み、空白を含むPathを正しく起動できるCommandにする。
- FR-6: Registry Access失敗は技術例外を露出せず、現在状態を保って利用者向けErrorを表示する。

## Non-functional Requirements / Constraints

- NFR-1: Registry I/OはInfrastructureへ置き、Presenter/Applicationは`Microsoft.Win32`へ依存しない。
- NFR-2: Current User scopeだけを使用し、管理者権限を要求しない。
- NFR-3: TransferImageQR固有valueだけを追加・削除し、他ApplicationのRun valueへ触れない。
- NFR-4: 登録済みCommandが現在Executableと異なる場合はOFFとして表示し、ON操作で更新する。

## Interfaces / Data / UI Contract

### UI

- Main Window上部にAccessible Name付きの「Windowsログイン時に起動」CheckBoxを表示する。
- Registry操作失敗はCheckBox近傍へ簡潔に表示する。

### Registry

- Key: `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run`
- Value name: `TransferImageQR`
- Value data: `"<absolute executable path>"`

## Acceptance Criteria

- [x] AC-1: 自動起動をON/OFFできる。
- [x] AC-2: 設定をWindows Loginを跨いで永続化できる。
- [x] AC-3: ONの場合Windows Login後にApplicationが起動するCommandを登録する。
- [x] AC-4: OFFへ戻すと自動起動登録を解除する。
- [x] AC-5: Registry Access失敗時もApplicationを継続利用できる。

## BDD Delta

| Change | Target Scenario | Before | After / Reason |
| --- | --- | --- | --- |
| Add | `BDD-AUTOSTART-001` | なし | ONでCurrent User Run登録を追加する |
| Add | `BDD-AUTOSTART-002` | なし | OFFで固有Run valueを削除する |
| Add | `BDD-AUTOSTART-003` | なし | 起動時に現在Executableの登録状態を復元する |
| Add | `BDD-AUTOSTART-004` | なし | Registry失敗を一般向けErrorとして表示する |

### Proposed Canonical Scenarios

```gherkin
Scenario: BDD-AUTOSTART-001 Windows Login時の起動をONにする
  Given 自動起動がOFFである
  When 利用者が「Windowsログイン時に起動」をONにする
  Then Current UserのRunへ引用済みExecutable Pathが登録される
  And 次回Windows Login後にTransferImageQRを起動できる

Scenario: BDD-AUTOSTART-002 Windows Login時の起動をOFFにする
  Given TransferImageQRの自動起動がONである
  When 利用者が「Windowsログイン時に起動」をOFFにする
  Then TransferImageQRのRun valueが削除される
  And 他ApplicationのRun valueは変更されない

Scenario: BDD-AUTOSTART-003 自動起動状態を復元する
  Given Current UserのRunへ現在Executableが登録されている
  When TransferImageQRを起動する
  Then 自動起動CheckBoxがONで表示される
  When 登録Commandが現在Executableと異なる
  Then 自動起動CheckBoxはOFFで表示される

Scenario: BDD-AUTOSTART-004 Registry Access失敗を案内する
  Given Current UserのRunへAccessできない
  When 自動起動状態を読み込むか変更する
  Then 技術例外を表示せず自動起動設定を変更できない旨を表示する
  And Draftと転送の操作は継続できる
```

## Error / Edge Cases

- `Environment.ProcessPath`を取得できない場合は設定を無効化し、Error表示する。
- Registry valueがない場合はOFFとして扱う。
- Registry valueが別Path、追加引数、別型の場合はOFFとして扱う。
- OFF操作はvalueが存在しなくても成功とする。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | CheckBox、Presenter command、Error表示 | Registry型を公開しない |
| Application | Registration PortとUse Caseを追加 | Executable PathとON/OFFを調整 |
| Infrastructure | HKCU Registry adapterを追加 | Current Userの固有valueだけを操作 |
| Domain | 変更なし | OS設定であり転送Domain外 |
| Tests / Harness | Use Case、Command生成、隔離Registry key、Presenter、Form tests | 実Login cycleは手動確認を残す |

## Test Strategy / Traceability

| AC | BDD Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1, AC-3 | BDD-AUTOSTART-001 | Unit / Integration / Presentation | Enable、引用Command、CheckBox wiring | Passed |
| AC-1, AC-4 | BDD-AUTOSTART-002 | Unit / Integration / Presentation | Disable、固有value削除 | Passed |
| AC-2 | BDD-AUTOSTART-003 | Unit / Integration / Presentation | IsEnabledと起動時反映 | Passed（実Login cycleを除く） |
| AC-5 | BDD-AUTOSTART-004 | Unit / Presentation | Port failureとfriendly error | Passed |

## Unknowns / Human Decisions

- Decision: 標準的で管理者権限不要なHKCU Run valueを採用する。
- Decision: 自動起動時も通常表示で起動し、Trayへ自動Hideする追加挙動は導入しない。
- Residual risk: 実Windows Login cycleと組織PolicyによるRegistry制限は自動Test環境では保証できない。

## Implementation Notes

- `IAutoStartRegistration`をApplication Portとし、HKCU Run操作をInfrastructureへ隔離した。
- 現在Executable Pathと登録Commandを照合し、異なるPathや引数付きCommandはOFFとして扱う。
- UI更新中のEvent再入を抑止し、登録失敗時は直前状態へ戻して一般向けErrorを表示する。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| `task verify` | Passed | Build警告0、Unit 101件成功 |
| `task test:integration` | Passed | Integration 15件成功。一時HKCU keyで登録・照合・対象valueのみ削除を確認 |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | Format差分なし |
| Windows Login cycle | Not Run | 実Login/Logoff環境が必要なためResidual manual check |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #14、Canonical BDD、Windows Current User制約を整理 |
| 2026-09-20 | Marked Verified | 実装、Unit/Integration/Presentation tests、品質Gate完了 |
