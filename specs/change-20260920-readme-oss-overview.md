# OSS向けREADME再構成 Change Specification

## Metadata

| Item | Value |
| --- | --- |
| Status | `Verified` |
| Source | User request (2026-09-20) |
| Owner | Codex |

## Goal

初見の利用者・開発者が、アプリの目的、主要機能、技術スタック、制約、実行方法、品質保証をREADMEだけで短時間に把握できるようにする。

## Scope

- OSS READMEとして一般的なOverview、Features、Quick start、Usage、Tech stack、Architectureを追加
- Test、Security / Privacy、Troubleshooting、Contributing、License statusを明示
- 詳細なMVP手動受入手順を折りたたみ領域へ整理
- 現在の実装とRepository構成だけを記載し、未提供のInstaller、Release、CI、Licenseを装わない

## Acceptance Criteria

- [x] 冒頭でアプリの価値と対象Platformが分かる。
- [x] 主要機能と制約が一覧で分かる。
- [x] C#/.NET、WinForms、Kestrel、SkiaSharp、QRCoder、xUnitの役割が分かる。
- [x] Cloneから実行・TestまでのCommandが再現可能である。
- [x] ArchitectureとProject layoutがRepository実体に一致する。
- [x] LAN内HTTP、Token、有効期限、License未設定を誤解なく明示する。

## Verification Evidence

| Check | Result |
| --- | --- |
| README links / referenced paths | Passed — local link targets and project paths confirmed |
| `task verify` | Passed — Build警告0、Unit 104件、Integration 17件、Acceptance 2件 |

## Change History

| Date | Change |
| --- | --- |
| 2026-09-20 | Initial Ready specification |
| 2026-09-20 | Marked Verified after README review and full quality gate |
