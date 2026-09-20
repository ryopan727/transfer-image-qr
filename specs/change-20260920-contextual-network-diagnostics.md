# ネットワーク診断の常時表示削除 Change Specification

## Metadata

| Item | Value |
| --- | --- |
| Status | `Verified` |
| Source | User request (2026-09-20) |
| Canonical BDD | [`specs/bdd.md`](bdd.md) |

## Goal

正常時に画面上部へ常時表示される配信先・接続確認文言を削除し、転送画面の重複情報と視覚的なノイズを減らす。

## Requirements

- 正常稼働中は「配信先」「同一LAN」「Windows Firewall」の診断文言を画面上部へ表示しない。
- Active時の配信URL欄は維持し、利用中のIP AddressとPortをそこで確認できる。
- HTTP Server起動失敗、Server停止、LAN IPv4 Address取得不能のError表示は維持する。
- Error表示へ例外詳細を露出しない。

## BDD Delta

- `BDD-DIAGNOSTICS-001`を、正常時の常時案内を表示せずActive転送URLでEndpointを確認する振る舞いへ変更する。
- `BDD-DIAGNOSTICS-002`、`BDD-DIAGNOSTICS-003`は変更しない。

## Acceptance Criteria

- [x] 正常稼働時に画面上部の診断文言が非表示になる。
- [x] Server起動失敗時は一般向けErrorが表示される。
- [x] Unit / Presentation / Acceptance testsが成功する。

## Verification Evidence

| Check | Result |
| --- | --- |
| `task verify` | Passed — Unit 104件、Integration 17件、Acceptance 2件、Build警告0 |
| Format / diff check | Passed |

## Change History

| Date | Change |
| --- | --- |
| 2026-09-20 | Initial Ready specification |
| 2026-09-20 | Marked Verified after implementation and automated verification |
