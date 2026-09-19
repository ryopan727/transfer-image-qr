# iPhone Safari向け画像一覧 Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0008` |
| Status | `Verified` |
| Source | [GitHub Issue #8](https://github.com/ryopan727/transfer-image-qr/issues/8) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

iPhone利用者がQRコードのURLをSafariで開き、現在のActiveセッションに含まれる画像を、専用アプリなしで見やすく選択できるようにする。

## Background / Current State

- MVP-006で`GET /transfer/{token}`と元画像配信Routeが実装されている。
- MVP-007で現在のActiveセッションを指すURLがQRコードとして表示される。
- 現在のSession Routeは`text/plain`のみを返し、画像一覧やモバイル向けUIを持たない。
- DomainのDraft上限は20枚で、Active Sessionは確定時の画像一覧を保持する。

## Scope

- Active Session RouteからUTF-8 HTML画像一覧を返す
- iPhone画面幅に対応するviewportとresponsive grid
- 最大20枚の画像カード、ファイル名、件数表示
- 各カードから既存の元画像Routeへ移動できるリンク
- ファイル名など動的値のHTML encoding
- 実Kestrelを使用したHTTP Integration Test

## Out of Scope

- サーバー側でのサムネイル生成、画像変換、キャッシュファイル作成
- 元画像表示・保存操作の詳細な受け入れ確認（MVP-009）
- Expired専用HTML画面（MVP-010）
- 複数NIC選択やネットワーク診断（MVP-011 / MVP-015）
- JavaScript、画像選択状態、一括保存、ZIP生成

## Functional Requirements

- FR-1: `GET /transfer/{token}`は、Tokenが現在のActive Sessionに一致する場合、`200 text/html; charset=utf-8`を返す。
- FR-2: HTMLにはSession内の全画像を、Session順序を維持して最大20枚表示する。
- FR-3: 各画像は既存の`/transfer/{token}/images/{imageId}`を`img src`とタップ先`href`に使用する。
- FR-4: 各カードにファイル名を表示し、一覧の画像件数を表示する。
- FR-5: HTMLは`viewport`を指定し、小さい画面で横スクロールを要求しないresponsive gridを使用する。
- FR-6: 画像カードのリンクは十分な面積を持ち、キーボードFocusも視認できる。
- FR-7: 無効TokenまたはActiveでないSessionは従来どおり404を返す。

## Non-functional Requirements / Constraints

- NFR-1: 専用iPhoneアプリ、JavaScript、外部CDN、Web Font、インターネット接続を要求しない。
- NFR-2: 一覧表示のために元画像をコピー、再圧縮、永続キャッシュしない。ブラウザのCSSでサムネイル寸法へ表示する。
- NFR-3: ファイル名とURL属性はHTML encodingし、利用者由来の文字列をMarkupとして解釈しない。
- NFR-4: 画像には遅延読込属性を付け、20枚時の初期読込負荷を抑える。
- NFR-5: Domain/ApplicationへASP.NET CoreやHTML生成の依存を持ち込まない。

## HTTP / UI Contract

### Session response

| Request | Success | Failure |
| --- | --- | --- |
| `GET /transfer/{token}` | `200 text/html; charset=utf-8` | `404` |

HTMLは次を含む。

- `lang="ja"`
- `meta charset="utf-8"`
- `meta name="viewport" content="width=device-width, initial-scale=1"`
- 画像件数
- 各画像のリンク、`img`、ファイル名
- CSS Gridによるresponsive layout

### Image card

- `href`と`img src`は同じ既存元画像Routeを指す。
- `img`は`object-fit: cover`でカード内のサムネイルとして表示する。
- `loading="lazy"`と`decoding="async"`を指定する。
- `alt`はファイル名を含む。

## Acceptance Criteria

- [x] AC-1: QR URLをSafariで開くと画像一覧が表示される。
- [x] AC-2: 最大20枚を扱える。
- [x] AC-3: iPhone画面幅で操作しやすい。
- [x] AC-4: 各画像をタップできる。
- [x] AC-5: 専用iPhoneアプリを必要としない。

## BDD Scenarios

```gherkin
Scenario: SC-1 Active Sessionの画像一覧を開く
  Given 3枚の画像を含むActive Sessionがある
  When 正しいTokenでSession URLをGETする
  Then Statusは200でContent-TypeはUTF-8 HTMLである
  And 3枚のカードがSession順に表示される
  And 各カードは対応する元画像Routeへリンクする

Scenario: SC-2 最大枚数を一覧表示する
  Given 20枚の画像を含むActive Sessionがある
  When 正しいTokenでSession URLをGETする
  Then 画像カードが20枚表示される
  And 21枚目は存在しない

Scenario: SC-3 iPhone幅で利用できるMarkupを返す
  Given Active Sessionがある
  When Session URLをGETする
  Then viewportがdevice-widthに設定される
  And Gridは画面幅に追従する
  And 各画像リンクはカード全体をタップ領域とする

Scenario: SC-4 動的値を安全に表示する
  Given HTML特殊文字を含むファイル名の画像がある
  When Session URLをGETする
  Then ファイル名は文字として表示できるようencodingされる
  And 任意のMarkupとして挿入されない

Scenario: SC-5 無効Sessionを拒否する
  Given Active Sessionがある
  When 異なるTokenでSession URLをGETする
  Then Statusは404で一覧HTMLを返さない
```

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Domain / Application | 変更なし | Sessionと画像の既存契約を利用 |
| Infrastructure | Session RouteのHTML化、HTML renderer追加 | Kestrel内で完結 |
| Presentation | 変更なし | QR URL契約は維持 |
| Tests | HTTP Integration Testを拡張 | 実Kestrelと一時ファイルを使用 |
| Documentation | READMEと本Specification | Safari一覧の現在機能を明記 |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-1, SC-5 | Integration | `GetSession_WithActiveSession_ReturnsResponsiveImageGallery`、無効Tokenの404 | Passed |
| AC-2 | SC-2 | Integration | `GetSession_WithMaximumDraft_ReturnsTwentyImageCards` | Passed |
| AC-3 | SC-3 | Integration + Markup review | viewport、auto-fit grid、狭幅2-column、safe-area、card全体link | Passed（実iPhone未実行） |
| AC-4 | SC-1, SC-3 | Integration | 各画像のanchor hrefとimg srcが同じ元画像Route | Passed |
| AC-5 | SC-1 | Integration + dependency review | self-contained HTML/CSS、JavaScript／外部Assetなし | Passed |
| Security | SC-4 | Integration | `rock & roll's.png`のHTML encodingとraw値不在 | Passed |

## Unknowns / Human Decisions

- Decision: Issueの「サムネイル」は、既存元画像RouteをCSSで縮小表示する画像カードと解釈する。サーバー側の派生画像生成は要求されておらず、不要なコピーを避けるプロダクト方針を優先する。
- Decision: 各カードはMVP-006で存在する元画像Routeへリンクする。MVP-009では元画像の非変換・保存導線を独立して検証する。
- Decision: 外部Assetsを使わず、HTML内のCSSのみでiPhone向けlayoutを提供する。
- Residual risk: 実iPhone Safariでの視覚・タップ操作は自動環境では検証できないため、responsive contractのIntegration TestとMarkup reviewで代替し、実機確認を残す。

## Implementation Notes

- `TransferGalleryPageRenderer`がActive Sessionをself-containedな日本語HTMLへ変換し、KestrelのSession Routeが`text/html; charset=utf-8`で返す。
- 画像カードはSession順を維持し、既存の元画像Routeを`href`と`img src`へ設定する。
- CSS Grid、viewport、safe-area、狭幅media query、カード全体のanchorによりiPhone向け操作領域を確保する。
- `loading="lazy"`と`decoding="async"`を指定し、一覧用の画像コピーやサーバー側変換を追加しない。
- TokenはURL path segment用にescapeし、URL属性とファイル名はHTML encodeする。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象Integration TestのRED確認 | Passed | 既存の`text/plain`応答に対し対象2件が意図どおり失敗 |
| `task verify` | Passed | Build: 0 warnings / 0 errors、Unit: 70 passed |
| `task test:integration` | Passed | Integration: 6 passed |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | Formatting差分なし |
| `git diff --check` | Passed | Whitespace errorなし（Gitの改行コード通知のみ） |
| WinForms executable smoke test | Passed | Process継続動作、Kestrel動的Port listenを確認 |
| 実iPhone Safari | Not Run | 実端末なし。HTTP contract、responsive markup、既存画像RouteのIntegration Testで代替 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #8、MVP-006/007実装、MVP-009/010境界から受け入れ契約を確定 |
| 2026-09-20 | Marked Verified | HTML gallery実装、Integration、build、format、起動確認を完了 |
