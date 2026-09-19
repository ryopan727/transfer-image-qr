# Specifications

This directory stores the version-controlled requirements and verification contract for each feature or bug fix.

## Rules

- Create or update the corresponding specification before changing production or test code.
- Keep the source Issue, acceptance criteria, BDD scenarios, test mapping, and verification evidence traceable.
- Set a specification to `Ready` before implementation and to `Verified` only after verification and self-review.
- Use `issue-NNNN-short-slug.md` for GitHub Issues.

## Index

| Spec | Status | Source | Summary |
| --- | --- | --- | --- |
| [issue-0001-winforms-app-foundation.md](issue-0001-winforms-app-foundation.md) | Verified | [Issue #1](https://github.com/ryopan727/transfer-image-qr/issues/1) | C# / .NET 8 WinForms application foundation |
| [issue-0002-draft-drag-drop.md](issue-0002-draft-drag-drop.md) | Verified | [Issue #2](https://github.com/ryopan727/transfer-image-qr/issues/2) | Add JPEG, PNG, and WebP files to a Draft by drag and drop |
| [issue-0003-image-input-validation.md](issue-0003-image-input-validation.md) | Verified | [Issue #3](https://github.com/ryopan727/transfer-image-qr/issues/3) | Validate image format, size, and Draft capacity |
| [issue-0004-draft-editing.md](issue-0004-draft-editing.md) | Verified | [Issue #4](https://github.com/ryopan727/transfer-image-qr/issues/4) | Remove individual Draft images, clear the Draft, and control editing actions |
| [issue-0005-transfer-session-lifecycle.md](issue-0005-transfer-session-lifecycle.md) | Verified | [Issue #5](https://github.com/ryopan727/transfer-image-qr/issues/5) | Confirm a Draft as a five-minute transfer session with a secure token |
| [issue-0006-lan-http-server.md](issue-0006-lan-http-server.md) | Verified | [Issue #6](https://github.com/ryopan727/transfer-image-qr/issues/6) | Serve active-session image files over LAN HTTP with Kestrel |
