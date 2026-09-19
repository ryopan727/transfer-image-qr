# LAN画像配信用HTTPサーバー Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0006` |
| Status | `Verified` |
| Source | [GitHub Issue #6](https://github.com/ryopan727/transfer-image-qr/issues/6) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

同一LAN上のiPhoneが、クラウドやNASを介さず、Activeな転送セッションの元画像へ安全にHTTPアクセスできる配信基盤をデスクトップアプリ内へ構築する。

## Background / Current State

- MVP-005で現在の転送セッション、推測困難なToken、5分期限、Active / Expired判定が実装されている。
- HTTP Host、LANバインド、Route、Content-Type、サーバーLifecycleは未実装である。
- DesktopはWinFormsだが、Issue本文のWPF表記はリポジトリ方針に従いWinFormsとして適用する。

## Scope

- WinFormsプロセス内のASP.NET Core / Kestrel起動・停止
- 全NICで待ち受ける動的HTTPポート
- ActiveセッションTokenによるアクセス制御
- セッション確認Endpointと画像Endpoint
- JPEG / PNG / WebPの元ファイル直接配信
- 実Kestrelと一時ファイルを使用するIntegration Test Project
- Taskfile / READMEのIntegration Test入口

## Out of Scope

- iPhone向けHTML画像一覧（MVP-008）
- QRコードとLAN URL生成（MVP-007）
- LANインターフェース選択（MVP-011）
- Windows Firewall自動設定、TLS、外部公開
- 期限切れ専用HTML画面（MVP-010）
- 詳細なネットワーク診断表示（MVP-015）

## Actors / External Systems

- WinFormsデスクトップアプリ
- ASP.NET Core / Kestrel
- 同一LAN上のiPhone Safari
- ローカルファイルシステム

## Functional Requirements

- FR-1: アプリプロセス内でHTTPサーバーを開始・停止できる。
- FR-2: サーバーはループバック限定ではなく全ネットワークインターフェースで待ち受ける。
- FR-3: `GET /transfer/{token}`はTokenが現在のActiveセッションと一致すると200を返す。
- FR-4: `GET /transfer/{token}/images/{imageId}`はActiveセッションに含まれる画像だけを返す。
- FR-5: Token欠落、不一致、対象外の画像IDは404を返す。正しいExpired TokenはMVP-010の契約に従い410期限切れ画面を返す。
- FR-6: JPEGは`image/jpeg`、PNGは`image/png`、WebPは`image/webp`で返す。
- FR-7: 画像は変換・再圧縮・中間コピーを行わず、元ファイルから返す。
- FR-8: アプリ起動中にKestrelを開始し、アプリ終了時に停止・破棄する。

## Non-functional Requirements / Constraints

- NFR-1: NAS、クラウド、インターネット接続を要求しない。
- NFR-2: 無効Tokenと存在しないSessionの差を外部へ露出しない。
- NFR-3: ServerはApplication Portに依存し、Domain/ApplicationはASP.NET Coreへ依存しない。
- NFR-4: Testは固定Portを使用せず、OSが割り当てる一時Portを使用する。
- NFR-5: Server、HttpClient、一時ファイルはTest終了時に確実に破棄する。

## Interfaces / Data / UI Contract

### HTTP

| Method / Path | Success | Failure |
| --- | --- | --- |
| `GET /transfer/{token}` | `200 text/html` | 不正Token: `404`、Expired: `410 text/html` |
| `GET /transfer/{token}/images/{imageId:guid}` | `200` + image Content-Type + original bytes | 不正Token/ID: `404`、Expired: `410 text/html` |

- TokenはPath segmentに含める。
- Tokenなしの`/transfer`にはRouteを設けず404とする。
- 画像IDはセッション確定時の`DraftImage.Id`を使用する。

### Server lifecycle

- `StartAsync`は起動済みの場合no-op。
- `StopAsync`は停止済みの場合no-op。
- 起動後は割り当て済みPortと待受Addressを取得できる。

## Acceptance Criteria

- [x] AC-1: WinFormsアプリ内でHTTPサーバーを開始・停止できる。
- [x] AC-2: Activeなセッションのみアクセス可能である。
- [x] AC-3: セッショントークンなしでは画像へアクセスできない。
- [x] AC-4: JPEG / PNG / WebPを適切なContent-Typeで返却できる。
- [x] AC-5: 同一LAN上の別端末からアクセス可能な全NICバインドで待ち受ける。

## BDD Scenarios

```gherkin
Scenario: SC-1 Activeセッションの画像を取得する
  Given JPEG、PNG、WebPを含むActiveセッションがある
  And Kestrelが全NICの一時Portで起動している
  When 正しいTokenと各画像IDでHTTP GETする
  Then 元ファイルと同一Byte列が対応する画像Content-Typeで返る

Scenario: SC-2 Tokenなしまたは不一致を拒否する
  Given Activeセッションがある
  When Tokenなしまたは異なるTokenでアクセスする
  Then 404が返り画像Byte列は返らない

Scenario: SC-3 Expiredセッションを拒否する
  Given 5分を経過したセッションがある
  When 正しいTokenでセッションまたは画像へアクセスする
  Then 410と利用者向け期限切れHTMLが返る

Scenario: SC-4 Serverを開始・停止する
  Given Serverが停止している
  When StartAsyncを実行する
  Then 全NICの動的PortでHTTP要求を受けられる
  When StopAsyncを実行する
  Then Serverは停止状態になりResourceが解放される
```

## Error / Edge Cases

- 元画像が削除・移動されている場合は404とする。利用者向け診断はMVP-015で追加する。
- 対応外拡張子はセッション入力時に除外済みだが、HTTP層でも配信対象外として404にする。
- Server起動失敗のPC向け表示はMVP-015で追加する。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | Composition RootでServerを生成しApplication lifecycleに合わせて破棄 | Form/PresenterからHTTPへ直接依存しない |
| Application / Domain | Active Session検索Portを追加 | ASP.NET Core型は持ち込まない |
| Infrastructure | Kestrel Host、Route、File配信、MIME判定 | `Microsoft.AspNetCore.App` FrameworkReference |
| Tests / Harness | Integration Test Project、Taskfile入口 | 実Kestrel + localhost +一時Directory |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-4 | Integration | `StartAndStopAsync_BindsAllInterfacesOnDynamicPort`で開始・冪等開始・停止・冪等停止・再開始 | Passed |
| AC-2 | SC-1, SC-3 | Unit + Integration | `GetAccess_DistinguishesActiveExpiredAndNotFoundWithoutExposingExpiredSession`、Expired時のSession/Image GET | Passed |
| AC-3 | SC-2 | Integration | Token欠落・不一致・画像ID不一致が404 | Passed |
| AC-4 | SC-1 | Integration | JPEG / PNG / WebPのContent-Typeと元Byte一致 | Passed |
| AC-5 | SC-4 | Integration + OS evidence | `[::]`全NIC待受とloopback HTTP、製品ProcessのListen確認 | Passed（実iPhoneは未実行） |

## Unknowns / Human Decisions

- Decision: HTTP契約は後続のSafari一覧とQR URLが拡張できる`/transfer/{token}`配下に置く。
- Decision: 無効・欠落Tokenは404とし、正しいExpired TokenのみMVP-010で410期限切れ画面へ拡張した。
- Decision: Port競合を避けるためKestrelは起動時に動的Portを取得する。固定Port設定は要求されていない。
- Decision: HTTPSは同一LAN・インターネット不要・証明書配布なしのMVP制約から対象外とする。
- Residual risk: Windows FirewallやAP isolationにより実端末から到達できない可能性があり、MVP-011/015で利用者向け選択・診断を追加する。

## Implementation Notes

- Applicationの`ITransferSessionAccessProvider`がToken一致を固定時間比較し、Active / Expired / NotFoundを区別する。
- Infrastructureの`LanImageHttpServer`がSlim ASP.NET Core Hostを構築し、`ListenAnyIP(0)`で全NICの動的Portへバインドする。
- Session確認と画像配信の両Routeが同じActive Session Portを使用し、認可失敗を404へ統一する。
- 画像配信は`Results.File`で元Pathを直接返し、Range Requestを有効にする。
- Composition RootがWinForms開始前にServerを起動し、Message Loop終了時に`IAsyncDisposable`で停止・破棄する。
- Unit/Presentationと分離したIntegration Test Projectを追加し、実Kestrel・localhost HTTP・一時ファイルで検証する。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象テストのRED確認 | Passed | Restore後、未実装の`LanImageHttpServer`型で意図どおりコンパイル失敗 |
| `task verify` | Passed | Unit / Presentation 58 total, 58 passed, 0 failed, 0 skipped |
| `task test:integration` | Passed | Integration 3 total, 3 passed, 0 failed, 0 skipped |
| `task test:all` | Passed | Unit / Presentation 58 + Integration 3 |
| Build | Passed | 0 warnings, 0 errors |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | 書式差分なし |
| `git diff --check` | Passed | whitespace errorなし |
| 製品EXE起動 / OS Listen確認 | Passed | `HasExited=False`、`Responding=True`、`LocalAddress=::`、動的PortでListen |
| 実iPhone接続 | Not Run | 利用可能な実端末・Firewall環境がない。全NICバインドと実HTTPで代替し、MVP-011/015でも確認する |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #6とMVP-005実装からHTTP契約と検証境界を確定 |
| 2026-09-20 | Marked Verified and recorded evidence | 全Acceptance Criteriaの自動検証とOS待受確認が完了 |
| 2026-09-20 | Updated expiration contract | MVP-010で正しいExpired Tokenを410期限切れ画面へ拡張 |
