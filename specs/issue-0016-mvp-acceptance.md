# MVP E2E・受入試験 Change Specification

この文書はIssueによる検証差分とEvidenceを記録する。外部仕様の正本は `specs/bdd.md` とする。

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0016` |
| Status | `Verified` |
| Source | [GitHub Issue #16](https://github.com/ryopan727/transfer-image-qr/issues/16) |
| Owner | Codex |
| Created / Updated | 2026-09-20 |

## Goal

MVPの入力から配信・失効までをProduction adapterを含む自動受入試験で横断し、iPhone実機部分を誰でも再現できる手動確認手順として固定する。

## Scope

- 独立したAcceptance Test projectとTask CLI command
- 実File、Skia decode、Application use case、Session、実Kestrel、HTTP responseを結ぶ主要フロー
- 10MB超、非対応形式、21枚目、Active後変更、無効Token、期限切れ、JPEG/PNG/WebPの受入検証
- READMEのMVP手動確認手順と期待結果
- Solution / `task test:all` / `task verify`へのAcceptance suite統合

## Out of Scope

- iPhone Camera/Safariの自動操作
- Windows Firewall dialogの自動操作
- GUI D&D automation harnessの新規採用

## Acceptance Criteria

- [x] AC-1: 正常系主要Flowを自動または再現可能な受入試験として整備する。
- [x] AC-2: 10MB超・非対応形式・21枚目を検証する。
- [x] AC-3: Active後に画像を変更できないことを検証する。
- [x] AC-4: 無効Tokenを拒否する。
- [x] AC-5: 期限切れSessionを拒否する。
- [x] AC-6: JPEG / PNG / WebPの配信を検証する。
- [x] AC-7: 全Unit/Integration/Acceptance Testが成功する。
- [x] AC-8: MVP手動確認手順をREADMEへ記載する。

## BDD Delta

外部挙動は変更しない。既存の `BDD-DRAFT-*`、`BDD-SESSION-*`、`BDD-WEB-*`、`BDD-IMAGE-*`、`BDD-ACCESS-*`、`BDD-E2E-*` にAcceptance evidenceを追加する。

## Test Design

- `MvpMainFlowAcceptanceTests`: 実JPEG/PNG/WebPを追加し、Session確定、実Kestrel一覧、元Byte配信、Active後拒否、無効Token、5分失効を一連で検証する。
- `MvpInputValidationAcceptanceTests`: 実File metadataとThumbnail adapterを使い、10MB超、非対応形式、21枚目を検証する。
- Test dataはOS temporary directoryに生成し、終了時に削除する。
- 時刻とTokenだけを決定的Test doubleとし、File/Image/HTTPはProduction adapterを使用する。

## Manual Acceptance

READMEへWindows PCと同一LAN上のiPhoneを使うPrecondition、D&D、QR、Safari一覧、元画像保存、期限切れ、Tray、自動起動、診断の確認手順を記載する。

## Residual Risk

- iPhone Camera、Safari visual、写真保存、実Firewall、Windows再Loginは手動確認を必要とする。
- GUI操作は既存のPresentation testsと手動手順で補い、未検証を自動E2E成功として扱わない。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| `task verify` | Passed | Build警告0、Unit 104件、Integration 17件、Acceptance 2件成功 |
| `task test:integration` | Passed | 実Kestrel Integration 17件成功（`task verify`内） |
| `task test:acceptance` | Passed | 実File・Skia・QR・Kestrel・HTTPを横断する2件成功 |
| `task test:all` | Passed | 3 Suite合計123件成功 |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | Format差分なし |
| Manual iPhone / login cycle | Not Run | READMEに再現手順を記録 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #16の自動・手動受入境界を確定 |
| 2026-09-20 | Marked Verified | Acceptance project、Task統合、README手順、全Suite成功 |
