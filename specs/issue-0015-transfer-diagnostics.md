# ネットワーク・転送診断 Change Specification

この文書はIssueによる変更差分と検証Evidenceを記録する。変更後の外部仕様の正本は `specs/bdd.md` とする。

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0015` |
| Status | `Verified` |
| Source | [GitHub Issue #15](https://github.com/ryopan727/transfer-image-qr/issues/15) |
| Owner | Codex |
| Created / Updated | 2026-09-20 |

## Goal

転送に失敗した利用者が、技術例外を読むことなく、配信Endpointと代表的な確認事項から問題を切り分けられるようにする。

## Scope

- Main Windowで利用中のLAN IPv4 AddressとPortを表示
- 接続不能時の同一LAN、Windows Firewall確認案内
- HTTP Server起動失敗後もMain Windowを表示し、一般向けErrorを案内
- Session確定後に元画像が削除・移動された場合の404案内Page
- Unit、Presentation、実Kestrel Integration tests

## Out of Scope

- Firewall ruleの自動追加、Router設定変更、到達性Probe
- 例外Stack traceや内部Pathの一般UI表示
- 自動再起動、Port固定設定

## Functional Requirements

- FR-1: Server稼働中は選択中IPv4 Addressと実際にBindしたPortをPC側へ表示する。
- FR-2: PC側へ「同一LAN」と「Windows Firewall」を確認する案内を表示する。
- FR-3: Server起動失敗を捕捉し、Draft操作可能なMain Windowと一般向けErrorを表示する。
- FR-4: Active Sessionの元画像が存在しない場合、HTTP 404と再転送を促す日本語HTMLを返す。
- FR-5: 一般UIとHTTP Error Pageへ例外型、Stack trace、内部File Pathを表示しない。

## Non-functional Requirements / Constraints

- NFR-1: Kestrel起動例外はComposition Rootで境界化し、Presentationへ例外Objectを渡さない。
- NFR-2: 既存のToken不正・画像ID不正の404は情報を追加せず維持する。
- NFR-3: 選択LAN Address変更時に診断Endpoint表示も更新する。

## Acceptance Criteria

- [x] AC-1: 元画像が削除・移動された場合に適切なErrorを返す。
- [x] AC-2: HTTP Server起動失敗をPC側へ表示する。
- [x] AC-3: 利用中の配信IP/Portを確認できる。
- [x] AC-4: 接続できない場合に「同一LAN」「Windows Firewall」等の確認ポイントを表示する。
- [x] AC-5: 例外詳細を一般UIへ露出しない。

## BDD Delta

| Change | Target Scenario | Summary |
| --- | --- | --- |
| Add | `BDD-DIAGNOSTICS-001` | 稼働Endpointと接続確認事項を表示 |
| Add | `BDD-DIAGNOSTICS-002` | Server起動失敗を一般向け表示へ変換 |
| Add | `BDD-DIAGNOSTICS-003` | 消失した元画像へ安全な404案内Pageを返す |

## Error / Edge Cases

- LAN IPv4候補がない場合はPortだけを示さず、Address取得不能を案内する。
- Server停止中はQRを生成せず、Serverを起動できない旨を表示する。
- Active Sessionに存在しないimage IDや不正Tokenでは消失案内Pageを返さない。

## Test Strategy / Traceability

| AC | BDD Scenario | Layer | Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-3, AC-4 | BDD-DIAGNOSTICS-001 | Unit / Presentation | Presenter endpoint更新、Form案内表示 | Passed |
| AC-2, AC-5 | BDD-DIAGNOSTICS-002 | Unit / Presentation / Integration | 起動失敗status、一般向け文言、実Port競合 | Passed |
| AC-1, AC-5 | BDD-DIAGNOSTICS-003 | Integration | 実KestrelでFile削除後の404 HTML、Path非露出 | Passed |

## Decisions / Residual Risk

- Decision: 常時見える既存Status領域を診断表示へ利用し、設定画面を追加しない。
- Decision: Port競合等の起動例外はMain Windowを閉じず、QR作成不能状態として表示する。
- Residual risk: iPhoneからの実到達性と実Firewall Policyは自動Testでは保証できない。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| `task verify` | Passed | Build警告0、Unit 104件成功 |
| `task test:integration` | Passed | Integration 17件成功 |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | Format差分なし |
| Runtime smoke | Passed | 通常Processの起動・終了を確認。Port競合は実Kestrel Integrationで確認 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #15の診断境界と検証方針を確定 |
| 2026-09-20 | Marked Verified | 診断UI、安全な404、Port競合、品質Gateを検証 |
