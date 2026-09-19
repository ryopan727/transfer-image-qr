# 期限切れ転送セッション拒否 Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0010` |
| Status | `Verified` |
| Source | [GitHub Issue #10](https://github.com/ryopan727/transfer-image-qr/issues/10) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

転送セッション作成から5分経過した後は一覧・元画像を配信せず、正しい期限切れURLを開いた利用者にはSafariで理解できる期限切れ案内を表示し、PC側から次の転送を開始できる状態を維持する。

## Background / Current State

- DomainのSession lifetimeは5分で、境界時刻から`Expired`になる。
- Applicationの`GetActive`はActive Sessionだけを返し、期限切れと不正Tokenをどちらも`null`にする。
- Kestrelは一覧・画像とも`GetActive`が`null`なら404を返すため、期限切れ利用者へ案内を表示できない。
- PC側は期限切れを表示し、「新しい転送」からSessionとDraftをresetできる。
- Active中のHTMLと画像応答には明示的なcache禁止Headerがない。

## Scope

- Token不一致、Active、Expiredを区別するApplication Port
- 正しいExpired Tokenへの`410 Gone`とUTF-8期限切れHTML
- Expired Sessionの一覧・元画像の配信拒否
- 不正Tokenは従来どおり404
- Active／Expired応答の`Cache-Control: no-store`
- PC側の新しい転送操作に関する既存Unit／Presentation Test確認
- Unit Testと実Kestrel Integration Test

## Out of Scope

- Session lifetimeの設定変更
- 期限延長、再有効化、同じTokenの再利用
- SafariからPC側の新規転送を遠隔操作する機能
- Background cleanup、永続Session、DB
- Windows Firewallやネットワーク診断

## Functional Requirements

- FR-1: Sessionは作成時刻から5分未満だけActiveとして一覧・元画像へアクセスできる。
- FR-2: 正しいTokenがExpiredの場合、一覧Routeは`410 text/html; charset=utf-8`の期限切れ画面を返す。
- FR-3: 正しいTokenがExpiredの場合、画像Routeも元画像を返さず`410`の期限切れ画面を返す。
- FR-4: 不正Token、存在しないSession、画像ID不一致は404を返し、期限切れかどうかを開示しない。
- FR-5: 期限切れ画面は「この転送は期限切れ」「PCで新しい転送を作成してQRを読み直す」旨を表示する。
- FR-6: PC側の「新しい転送」は現在Sessionを破棄し、空の編集可能Draftへ戻す。

## Non-functional Requirements / Constraints

- NFR-1: Token比較は固定時間比較を維持する。
- NFR-2: Active一覧・Active画像・Expired画面は`Cache-Control: no-store`を返し、ブラウザや中間cacheへ保存しないよう指示する。
- NFR-3: 期限切れ画面はJavaScript、外部Asset、インターネット接続を要求しないresponsive HTMLとする。
- NFR-4: Expired判定は注入された`TimeProvider`を使用し、実時刻待機なしで検証できる。
- NFR-5: Expired結果にSession内画像情報を含めない。

## HTTP Contract

| Request state | Session route | Image route |
| --- | --- | --- |
| Active + correct Token | `200 text/html` gallery | `200/206 image/*` |
| Expired + correct Token | `410 text/html` expiration page | `410 text/html` expiration page |
| Wrong / unknown Token | `404` | `404` |
| Active + unknown imageId | N/A | `404` |

Active／Expired response:

```http
Cache-Control: no-store
```

期限切れHTMLはToken、ファイル名、画像数、内部時刻などを表示しない。

## Acceptance Criteria

- [x] AC-1: セッション作成から5分経過すると画像一覧へアクセスできない。
- [x] AC-2: 期限切れセッションから元画像を取得できない。
- [x] AC-3: 期限切れ時はHTTPエラーだけでなくユーザー向け期限切れ画面を表示する。
- [x] AC-4: PC側から新しい転送セッションを開始できる。

## BDD Scenarios

```gherkin
Scenario: SC-1 5分境界で一覧を拒否する
  Given Session作成から5分未満で一覧へアクセスできる
  When 現在時刻がExpiresAtへ到達する
  And 同じ正しいTokenで一覧URLをGETする
  Then Statusは410でContent-TypeはUTF-8 HTMLである
  And 画像一覧は含まれない
  And 利用者向け期限切れ案内が表示される

Scenario: SC-2 期限切れ元画像を拒否する
  Given Session作成から5分が経過している
  When 正しいTokenとSession内画像IDで元画像をGETする
  Then Statusは410である
  And Content-Typeは元画像形式ではなくUTF-8 HTMLである
  And 元画像バイトは返らない

Scenario: SC-3 不正Tokenを期限切れとして扱わない
  Given Expired Sessionがある
  When 異なるTokenで一覧または画像へアクセスする
  Then Statusは404である
  And 期限切れ画面は返らない

Scenario: SC-4 PCで新しい転送を開始する
  Given PC側にExpired Sessionが表示されている
  When 利用者が新しい転送を選択する
  Then 現在Sessionは破棄される
  And 空の編集可能Draftが表示される
  And 以前のTokenはNotFoundになる
```

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Domain | 変更なし | 5分境界とStateは実装済み |
| Application | Session access結果をActive / Expired / NotFoundで表現 | Expired時はSession情報を返さない |
| Infrastructure | 410期限切れ画面、Route分岐、no-store Header | Kestrel Integrationで検証 |
| Presentation | Production変更なし | 新しい転送の既存Presenter/Form Testを再実行 |
| Documentation | READMEと本Specification | HTTP状態と利用者導線を記録 |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-1 | Unit + Integration | 5分境界のAccess結果、`GetTransferRoutes_DistinguishNotFoundAndExpiredSessions` | Passed |
| AC-2 | SC-2 | Integration | 画像410、HTML Content-Type、元バイト不在 | Passed |
| AC-3 | SC-1, SC-2 | Integration + Markup review | 日本語期限切れ画面、viewport、safe-area | Passed |
| AC-4 | SC-4 | Unit + Presentation | `StartNewTransfer_AfterActiveSession_ClearsSessionAndResetsDraft`、Presenter/Form tests | Passed |
| Security | SC-3 | Unit + Integration | 不正Tokenは404、期限切れ本文なし | Passed |
| Cache | SC-1, SC-2 | Integration | Active一覧／画像とExpired画面のno-store | Passed |

## Unknowns / Human Decisions

- Decision: 正しいExpired TokenにはHTTP semanticsとして`410 Gone`を使用し、同時に利用者向けHTMLを返す。不正Tokenは404のまま区別する。
- Decision: 画像URLをSafariで直接開いたまま期限切れた場合にも案内へ到達できるよう、Expired画像Routeも同じ410 HTMLを返す。
- Decision: Browser backや既存Tabによる期限後再利用を抑えるため、Active時から`Cache-Control: no-store`を付ける。
- Decision: 期限切れ画面からPC操作はできないため、利用者へPCで新しい転送を作るよう案内する。
- Residual risk: 期限前に端末へ表示・保存済みの画像データを期限到達時に端末から消去することはできない。

## Implementation Notes

- `ITransferSessionAccessProvider`と`TransferSessionAccess`を追加し、ApplicationでActive / Expired / NotFoundを区別する。
- Active結果だけがSession参照を持てるfactory構造とし、Expired / NotFoundから画像情報を取得できないようにした。
- Token一致は既存の固定時間比較を維持し、現在Sessionがない場合や異なるTokenはNotFoundとする。
- Kestrelは正しいExpired Tokenを`410 Gone`のself-contained HTMLへ変換し、一覧・画像の両Routeで元データを返さない。
- Active／Expired応答へ`Cache-Control: no-store`を設定する。
- PC側の新しい転送は既存UseCase／Presenter／Formのreset導線を維持し、以前のTokenはNotFoundになることを追加確認した。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象TestのRED確認 | Passed | Access契約未実装でUnit compile失敗、Expired HTTPが404のためIntegration失敗 |
| `task verify` | Passed | Build: 0 warnings / 0 errors、Unit: 70 passed |
| `task test:integration` | Passed | Integration: 7 passed |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | Formatting差分なし |
| `git diff --check` | Passed | Whitespace errorなし（Gitの改行コード通知のみ） |
| WinForms executable smoke test | Passed | Process継続動作、Kestrel動的Port listenを確認 |
| 実iPhone Safari | Not Run | 実端末なし。410 HTMLとresponsive markupを実Kestrelで検証 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #10とMVP-005/006/008/009の差分から期限切れHTTP契約を確定 |
| 2026-09-20 | Marked Verified | Access状態、410期限切れ画面、no-store、全品質ゲートを確認 |
