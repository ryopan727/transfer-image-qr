# Specifications

このディレクトリは、現在の外部仕様と、Issueまたは依頼ごとの変更差分を分けてGit管理する。

## Source of Truth

- `bdd.md`: 現在の外部から観測可能な振る舞いを表す正本
- `issue-NNNN-*.md`: `bdd.md`に対する変更差分、判断、検証Evidence
- Default Branchの`bdd.md`は実装済み仕様と一致させる。Issue Branchでは、同じPRで実装する変更後の状態を先に記述してよい。

## Rules

- Issueまたは依頼を受けたら、実装前に変更差分Specificationを作成する。
- 変更差分Specificationで追加・変更・削除するBDD Scenarioを特定し、変更後の内容を`bdd.md`へ反映する。
- `Status: Ready`になるまでProduction CodeとTest Codeの実装へ進まない。
- 完了時はAC、BDD Scenario、Test Evidence、残存Riskを変更差分Specificationへ反映し、`bdd.md`のTest Mappingも更新する。
- GitHub Issueには`issue-NNNN-short-slug.md`を使用する。

## Index

| Spec | Status | Source | Summary |
| --- | --- | --- | --- |
| [Canonical BDD](bdd.md) | Current | Repository | 現在の外部仕様とE2E Test項目の正本 |
| [issue-0001-winforms-app-foundation.md](issue-0001-winforms-app-foundation.md) | Verified | [Issue #1](https://github.com/ryopan727/transfer-image-qr/issues/1) | C# / .NET 8 WinForms application foundation |
| [issue-0002-draft-drag-drop.md](issue-0002-draft-drag-drop.md) | Verified | [Issue #2](https://github.com/ryopan727/transfer-image-qr/issues/2) | Add JPEG, PNG, and WebP files to a Draft by drag and drop |
| [issue-0003-image-input-validation.md](issue-0003-image-input-validation.md) | Verified | [Issue #3](https://github.com/ryopan727/transfer-image-qr/issues/3) | Validate image format, size, and Draft capacity |
| [issue-0004-draft-editing.md](issue-0004-draft-editing.md) | Verified | [Issue #4](https://github.com/ryopan727/transfer-image-qr/issues/4) | Remove individual Draft images, clear the Draft, and control editing actions |
| [issue-0005-transfer-session-lifecycle.md](issue-0005-transfer-session-lifecycle.md) | Verified | [Issue #5](https://github.com/ryopan727/transfer-image-qr/issues/5) | Confirm a Draft as a five-minute transfer session with a secure token |
| [issue-0006-lan-http-server.md](issue-0006-lan-http-server.md) | Verified | [Issue #6](https://github.com/ryopan727/transfer-image-qr/issues/6) | Serve active-session image files over LAN HTTP with Kestrel |
| [issue-0007-transfer-url-qr-code.md](issue-0007-transfer-url-qr-code.md) | Verified | [Issue #7](https://github.com/ryopan727/transfer-image-qr/issues/7) | Generate and display a QR code for the active LAN transfer URL |
| [issue-0008-safari-image-gallery.md](issue-0008-safari-image-gallery.md) | Verified | [Issue #8](https://github.com/ryopan727/transfer-image-qr/issues/8) | Provide a responsive image gallery for iPhone Safari |
| [issue-0009-safari-original-image.md](issue-0009-safari-original-image.md) | Verified | [Issue #9](https://github.com/ryopan727/transfer-image-qr/issues/9) | Open original images individually in Safari for standard save operations |
| [issue-0010-expired-session-page.md](issue-0010-expired-session-page.md) | Verified | [Issue #10](https://github.com/ryopan727/transfer-image-qr/issues/10) | Reject expired sessions and show a user-facing expiration page |
| [issue-0011-lan-interface-selection.md](issue-0011-lan-interface-selection.md) | Verified | [Issue #11](https://github.com/ryopan727/transfer-image-qr/issues/11) | Select the LAN IPv4 address used in the transfer QR code |
| [issue-0013-custom-background.md](issue-0013-custom-background.md) | Verified | [Issue #13](https://github.com/ryopan727/transfer-image-qr/issues/13) | Customize the desktop background image, appearance, persistence, and reset |
