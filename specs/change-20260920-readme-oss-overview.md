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

- OSS READMEとして一般的なOverview、Features、Quick start、Usage、簡潔なTech stack表を追加
- Test、Security / Privacy、Troubleshooting、Contributing、License statusを明示
- 詳細なMVP手動受入手順を折りたたみ領域へ整理
- 現在の実装とRepository構成だけを記載し、未提供のInstaller、Release、CIを装わない
- RepositoryへMIT License本文を追加し、READMEから明示する

## Acceptance Criteria

- [x] 冒頭でアプリの価値と対象Platformが分かる。
- [x] 主要機能と制約が一覧で分かる。
- [x] C#/.NET、WinForms、Kestrel、SkiaSharp、QRCoder、xUnitの役割が分かる。
- [x] Cloneから実行・TestまでのCommandが再現可能である。
- [x] 技術説明はTech stack表だけとし、Architecture図やProject責務一覧を重複掲載しない。
- [x] LAN内HTTP、Token、有効期限を誤解なく明示する。
- [x] MIT License本文、badge、READMEリンクが一致する。

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
| 2026-09-20 | Reopened to simplify technical details per user feedback |
| 2026-09-20 | Marked Verified after retaining only the Tech stack table |
| 2026-09-20 | Reopened to publish the MIT License requested by the owner |
| 2026-09-20 | Marked Verified with LICENSE file, badge, and README link |
