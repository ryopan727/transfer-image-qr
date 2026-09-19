# TransferImageQR BDD Specification

この文書は、Repositoryが現在提供する外部から観測可能な振る舞いの正本である。Issue別Specificationは変更差分と作業Evidenceを記録し、変更後の最終的な振る舞いは本ファイルへ統合する。

## Metadata

| Item | Value |
| --- | --- |
| Status | `Current` |
| Updated | `2026-09-20` |
| Owners | TransferImageQR maintainers |

## Scope

- .NET 8 Windows Formsデスクトップアプリの起動とDraft編集
- JPEG、PNG、WebP画像の入力検証
- 5分間のメモリ内転送SessionとQRコード生成
- Kestrelによる同一LAN向け画像一覧・元画像配信
- iPhone Safari向け表示、Token検証、期限切れ拒否
- MVP-001〜MVP-011で実装済みの振る舞い

システムトレイ、自動起動、背景画像設定など、未実装Issueの振る舞いは本書の対象外とする。

## Actors / External Systems

- Windows利用者: 画像を選択し、転送を開始・再開する
- iPhone利用者: 標準CameraでQRを読み、Safariで画像を表示・保存する
- Windows filesystem: 選択された元画像を保持する
- Windows network stack: LAN IPv4 AddressとHTTP通信を提供する
- iPhone Camera / Safari: QR読取、一覧表示、元画像表示、写真保存操作を提供する

## Scenario ID規則

- Scenario IDは `BDD-<AREA>-NNN` 形式のRepository内で一意な安定IDとする。
- 一度MergeしたIDを別の振る舞いへ再利用しない。
- Scenarioを廃止する場合はIssue別Change Specificationに理由を残し、本ファイルから削除する。
- Test名、Test Plan、またはTest MappingからScenario IDを追跡できるようにする。

## Feature: Windowsデスクトップアプリ

### Rule: 利用者は転送作業を開始できる

```gherkin
Scenario: BDD-APP-001 アプリを起動する
  Given Windowsに.NET 8 Desktop Runtimeがある
  When 利用者がTransferImageQRを起動する
  Then Main Windowが表示される
  And Draftへ画像を追加できる状態になる
```

## Feature: Draft画像入力

### Rule: 対応画像だけをDraftへ追加する

```gherkin
Scenario: BDD-DRAFT-001 対応画像をまとめて追加する
  Given 編集可能な空のDraftがある
  When 利用者がJPEG、PNG、WebPをWindowへDrag and Dropする
  Then すべての有効画像がDraft一覧へ追加される
  And 各画像のThumbnailとFile情報が表示される

Scenario: BDD-DRAFT-002 複数回のDropを追記する
  Given Draftに有効画像がある
  When 利用者が別の有効画像をDrag and Dropする
  Then 既存画像を維持して新しい画像が末尾へ追加される

Scenario: BDD-DRAFT-003 不正Fileだけを除外する
  Given 編集可能なDraftがある
  When 有効画像と非対応、破損、またはFile以外の入力が混在する
  Then 有効画像だけがDraftへ追加される
  And 拒否された入力と理由が利用者へ表示される

Scenario: BDD-DRAFT-004 1Fileの10MB制限を適用する
  Given 編集可能なDraftがある
  When 利用者が10MB以下と10MBを超える画像を追加する
  Then 10MB以下の画像は追加される
  And 10MBを超える画像はSize超過として拒否される

Scenario: BDD-DRAFT-005 最大20枚の制限を適用する
  Given Draftの画像数が20枚未満である
  When 利用者が合計20枚を超える画像を追加する
  Then 先頭から20枚までの有効画像が保持される
  And 21枚目以降はCapacity超過として拒否される
```

### Rule: Draft中だけ転送対象を編集できる

```gherkin
Scenario: BDD-DRAFT-006 個別画像を削除する
  Given Draftに複数の画像がある
  When 利用者が1枚を選択して削除する
  Then 選択した画像だけがDraftから削除される

Scenario: BDD-DRAFT-007 DraftをすべてClearする
  Given Draftに画像がある
  When 利用者がすべてClearを選択する
  Then Draftは空になる
  And QR作成操作は無効になる

Scenario: BDD-DRAFT-008 Active Sessionを編集しない
  Given DraftからActive Sessionが作成されている
  When 利用者が画像の追加、個別削除、または全Clearを試みる
  Then 転送対象は変更されない
  And 編集操作は利用できない
```

## Feature: 転送Session

### Rule: QR作成時に転送対象を確定する

```gherkin
Scenario: BDD-SESSION-001 DraftをActive Sessionへ確定する
  Given Draftに1枚以上の有効画像がある
  When 利用者がQR作成を選択する
  Then 現在の画像集合を持つActive Sessionが作成される
  And Sessionの画像集合は変更できない

Scenario: BDD-SESSION-002 推測困難なSession Tokenを発行する
  Given QR作成可能なDraftがある
  When Active Sessionを作成する
  Then 256bit以上のRandomnessを持つURL-safe Tokenが発行される
  And 別のSessionには別のTokenが発行される

Scenario: BDD-SESSION-003 5分境界でSessionを期限切れにする
  Given Active Sessionが作成されている
  When 作成時刻から5分未満で状態を確認する
  Then SessionはActiveである
  When 現在時刻がExpiresAtへ到達する
  Then SessionはExpiredである

Scenario: BDD-SESSION-004 新しい転送を開始する
  Given PC側にActiveまたはExpired Sessionがある
  When 利用者が新しい転送を選択する
  Then 現在Sessionは破棄される
  And 空の編集可能Draftへ戻る
  And 以前のTokenは利用できない
```

## Feature: LAN転送の開始

### Rule: 転送に使うLAN IPv4 Addressを選択できる

```gherkin
Scenario: BDD-NETWORK-001 利用可能なLAN IPv4 Address候補を表示する
  Given Windows PCにUp状態の複数Network Interfaceがある
  When Main WindowでLAN Address候補を確認する
  Then Up状態のPrivate IPv4 Addressが重複なく表示される
  And Loopback、link-local、Public IPv4、IPv6、Down InterfaceのAddressは表示されない

Scenario: BDD-NETWORK-002 転送に使うAddressを選択する
  Given 利用可能なLAN IPv4 Address候補が複数表示されている
  When 利用者が1件のAddressを選択してQR作成を選択する
  Then 選択したAddressが転送URLとQR payloadのHostになる
  And Active中はAddressを変更できない

Scenario: BDD-NETWORK-003 次の転送でも選択を保持する
  Given 利用者がLAN IPv4 Addressを選択して転送を開始している
  When 新しい転送を選択してDraftへ戻る
  Then 同じAddressが選択されたまま表示される
  And 利用者は別の候補へ変更できる
```

### Rule: 現在SessionのLAN URLをQRで提示する

```gherkin
Scenario: BDD-TRANSFER-001 現在の転送URLをQR表示する
  Given Draftに画像があり利用可能なLAN IPv4 Addressを選択している
  When 利用者がQR作成を選択する
  Then Kestrelが全Network InterfaceでHTTP待受を開始する
  And 選択したAddressと現在のSession Tokenを含むLAN URLのQRコードが表示される
  And 過去のTokenはQRコードに含まれない

Scenario: BDD-TRANSFER-002 LAN IPv4 Addressがない場合に案内する
  Given 利用可能なLAN IPv4 Address候補がない
  When 利用者がQR作成を選択する
  Then 読み取り不能なQRコードは表示されない
  And Networkを確認する案内が表示される

Scenario: BDD-TRANSFER-003 アプリ終了時にHTTP Serverを停止する
  Given Kestrelが転送要求を待ち受けている
  When 利用者がアプリを終了する
  Then HTTP Serverと使用Resourceが停止される
```

## Feature: iPhone Safari画像一覧

### Rule: Active Sessionの画像をMobile向けに表示する

```gherkin
Scenario: BDD-WEB-001 QRから画像一覧を開く
  Given Active Sessionの正しいTokenを含むQRコードが表示されている
  When iPhone利用者が標準CameraでQRを読みSafariでURLを開く
  Then HTTP 200の画像一覧が表示される
  And Session内の各画像が選択可能なCardとして表示される

Scenario: BDD-WEB-002 最大20枚をResponsive表示する
  Given Active Sessionに20枚の画像がある
  When iPhone Safariで画像一覧を開く
  Then 20枚すべてが重複なく表示される
  And Mobile viewport、safe area、画面幅に応じたGridが適用される

Scenario: BDD-WEB-003 File名を安全に表示する
  Given HTML特殊文字を含むFile名の画像がActive Sessionにある
  When 画像一覧を開く
  Then File名はHTML encodeされる
  And MarkupやScriptとして実行されない
```

## Feature: 元画像の表示

### Rule: 選択画像を元形式のまま配信する

```gherkin
Scenario: BDD-IMAGE-001 Cardから元画像を開く
  Given Active Sessionの画像一覧が表示されている
  When iPhone利用者が画像Cardを選択する
  Then Safariで対応する元画像がinline表示される
  And JPEG、PNG、WebPのContent-Typeと元File byteが維持される

Scenario: BDD-IMAGE-002 Range Requestへ応答する
  Given Active Sessionの元画像URLがある
  When Safariが有効なRangeを指定して画像を要求する
  Then HTTP 206と正しいContent-Rangeが返る
  And 指定範囲の元File byteだけが返る

Scenario: BDD-IMAGE-003 Unicode File名をHTTP Headerへ安全に含める
  Given Unicode文字を含むFile名の画像がある
  When 元画像を要求する
  Then Content-Dispositionはinlineである
  And UTF-8のfilename* Parameterが返る
```

## Feature: 転送Access制御

### Rule: 正しいActive Tokenだけが画像へAccessできる

```gherkin
Scenario: BDD-ACCESS-001 不正Tokenへの情報開示を拒否する
  Given ActiveまたはExpired Sessionがある
  When 異なる、または存在しないTokenで一覧か画像を要求する
  Then HTTP 404が返る
  And Sessionや期限切れの情報は開示されない

Scenario: BDD-ACCESS-002 期限切れ一覧を拒否する
  Given Session作成から5分が経過している
  When 正しいTokenで画像一覧を要求する
  Then HTTP 410とUTF-8 HTMLの期限切れ案内が返る
  And 画像一覧、File名、Tokenは含まれない

Scenario: BDD-ACCESS-003 期限切れ元画像を拒否する
  Given Session作成から5分が経過している
  When 正しいTokenとSession内画像IDで元画像を要求する
  Then HTTP 410とUTF-8 HTMLの期限切れ案内が返る
  And 元画像byteは返らない

Scenario: BDD-ACCESS-004 Browser cacheによる再利用を抑止する
  Given ActiveまたはExpired SessionへHTTP Accessする
  When Serverが一覧、元画像、または期限切れ画面を返す
  Then ResponseにCache-Control no-storeが含まれる
```

## Feature: 主要End-to-End Journey

### Rule: WindowsからiPhoneへ画像を転送できる

```gherkin
Scenario: BDD-E2E-001 画像を選択してiPhoneへ保存する
  Given Windows PCとiPhoneが同一LANに接続されている
  And TransferImageQRを起動している
  When 利用者が複数の対応画像をDrag and Dropする
  And QR作成を選択する
  And iPhone標準CameraでQRを読みSafariで画像一覧を開く
  And 1枚の画像を選択する
  Then 選択した元画像がSafariで表示される
  And iOS標準操作で写真へ保存できる

Scenario: BDD-E2E-002 期限切れ後に新しい転送を開始する
  Given iPhone SafariでActive Sessionの画像一覧を開ける
  When Session作成から5分が経過して同じURLを再読込する
  Then Safariに期限切れ案内が表示され画像へAccessできない
  When PCで新しい転送を開始して別の画像からQRを作成する
  Then 古いURLは利用できない
  And 新しいQRから新しい画像一覧を開ける
```

## Test Mapping

`Not Run`は未実施を表し、下位Layerの自動TestがPassedでも当該E2E Scenario自体をPassedとはみなさない。

| BDD Scenario | Test Layer | Test / Evidence | Last Verified |
| --- | --- | --- | --- |
| BDD-APP-001 | Build / Runtime smoke | `task verify`; WinForms process・Kestrel起動確認 | 2026-09-20 |
| BDD-DRAFT-001 | Unit / Presentation | AddImages use case、Presenter、Form integration tests | 2026-09-20 |
| BDD-DRAFT-002 | Unit / Presentation | 追記順序と再描画のtests | 2026-09-20 |
| BDD-DRAFT-003 | Unit / Presentation | 混在入力、非File入力、拒否理由表示のtests | 2026-09-20 |
| BDD-DRAFT-004 | Unit | 10MB境界とSize超過のtests | 2026-09-20 |
| BDD-DRAFT-005 | Unit | 20枚境界と21枚目拒否のtests | 2026-09-20 |
| BDD-DRAFT-006 | Unit / Presentation | 個別削除UseCase、Presenter、Form tests | 2026-09-20 |
| BDD-DRAFT-007 | Unit / Presentation | ClearとQR作成可否のtests | 2026-09-20 |
| BDD-DRAFT-008 | Unit / Presentation | Active後の変更拒否tests | 2026-09-20 |
| BDD-SESSION-001 | Unit / Presentation | Session確定、snapshot、編集無効化tests | 2026-09-20 |
| BDD-SESSION-002 | Unit | Token entropy、URL-safe、unique tests | 2026-09-20 |
| BDD-SESSION-003 | Unit | `TimeProvider`を使う5分境界tests | 2026-09-20 |
| BDD-SESSION-004 | Unit / Presentation | `StartNewTransfer_AfterActiveSession_ClearsSessionAndResetsDraft` | 2026-09-20 |
| BDD-NETWORK-001 | Unit / Presentation | `SelectUsable_ReturnsPrivateIPv4CandidatesInDeterministicOrder`、Presenter／Form候補表示tests | 2026-09-20 |
| BDD-NETWORK-002 | Unit / Presentation / Integration | 選択Event、Active時無効化、URL Host、QR生成tests | 2026-09-20 |
| BDD-NETWORK-003 | Presentation | `SelectedLanAddress_IsUsedForQrAndRetainedAfterStartingNewTransfer` | 2026-09-20 |
| BDD-TRANSFER-001 | Unit / Presentation / Runtime smoke | 選択AddressのURL／QR反映、Kestrel listen | 2026-09-20 |
| BDD-TRANSFER-002 | Unit / Presentation | 候補なしのPresenter／Form表示tests | 2026-09-20 |
| BDD-TRANSFER-003 | Unit / Runtime smoke | Server lifecycle tests、Process終了確認 | 2026-09-20 |
| BDD-WEB-001 | Integration / E2E | 実Kestrel gallery integration: Passed; iPhone Camera/Safari: Not Run | 2026-09-20 |
| BDD-WEB-002 | Integration / E2E | 20枚、viewport、Grid markup: Passed; iPhone実機visual: Not Run | 2026-09-20 |
| BDD-WEB-003 | Integration | HTML encodeとunsafe markup不在のtests | 2026-09-20 |
| BDD-IMAGE-001 | Integration / E2E | 元byte、Content-Type、inline: Passed; iPhone保存: Not Run | 2026-09-20 |
| BDD-IMAGE-002 | Integration | 実Kestrel Range Request test | 2026-09-20 |
| BDD-IMAGE-003 | Integration | UTF-8 `filename*` test | 2026-09-20 |
| BDD-ACCESS-001 | Unit / Integration | 固定時間Token比較、wrong token 404 tests | 2026-09-20 |
| BDD-ACCESS-002 | Unit / Integration / E2E | 5分境界、gallery 410 HTML: Passed; iPhone実機visual: Not Run | 2026-09-20 |
| BDD-ACCESS-003 | Integration / E2E | image 410、元byte不在: Passed; iPhone実機visual: Not Run | 2026-09-20 |
| BDD-ACCESS-004 | Integration | Active一覧／画像とExpired応答のno-store tests | 2026-09-20 |
| BDD-E2E-001 | E2E | Not Run — GUI E2E harnessと実iPhone環境が未整備 | Not Run |
| BDD-E2E-002 | E2E | Not Run — 時刻制御を含むGUI E2E harnessと実iPhone環境が未整備 | Not Run |

## Operational Constraints

- Production targetはWindows上のC# / .NET 8 Windows Formsである。
- PCとiPhoneは同一LANを使用し、NAS、Cloud、Internet接続に依存しない。
- HTTP ServerはKestrelを使用し、LAN到達性のため全Network Interfaceで待ち受ける。
- SessionはProcess memoryだけで保持し、Application終了後に復元しない。
- 対応形式はJPEG、PNG、WebP、最大20枚、1File最大10MBである。
- 元画像は不要にCopyせず、可能な限り元Fileから直接配信する。
- Session lifetimeは5分であり、期限延長や再有効化は行わない。
- 複数NIC、VPN、Hyper-V、Docker等ではPrivate IPv4候補から利用者が選択し、選択は同一Application Process内で保持する。
- PC上の候補検出だけではiPhoneからの実到達性を保証できず、Network構成とFirewallに依存する。
- GUI E2E自動化を導入する場合はFlaUI等のToolをPoCで3回連続成功させてから採用する。
- iPhone Camera、Safari layout、iOS写真保存は実端末での手動E2E確認を別途必要とする。

## Change Sources

| Change Specification | Affected Scenarios | Summary |
| --- | --- | --- |
| `specs/issue-0001-winforms-app-foundation.md` | BDD-APP-001 | .NET 8 WinForms基盤とLayer依存方向 |
| `specs/issue-0002-draft-drag-drop.md` | BDD-DRAFT-001〜003 | Drag and DropとThumbnail一覧 |
| `specs/issue-0003-image-input-validation.md` | BDD-DRAFT-003〜005 | 形式、Size、枚数の入力制約 |
| `specs/issue-0004-draft-editing.md` | BDD-DRAFT-006〜008 | 個別削除、全Clear、編集可否 |
| `specs/issue-0005-transfer-session-lifecycle.md` | BDD-SESSION-001〜004 | Active確定、Token、5分期限、reset |
| `specs/issue-0006-lan-http-server.md` | BDD-TRANSFER-003, BDD-ACCESS-001 | Kestrel lifecycleとLAN HTTP配信 |
| `specs/issue-0007-transfer-url-qr-code.md` | BDD-TRANSFER-001〜002 | LAN URLとQRコード表示 |
| `specs/issue-0008-safari-image-gallery.md` | BDD-WEB-001〜003 | iPhone Safari向け画像一覧 |
| `specs/issue-0009-safari-original-image.md` | BDD-IMAGE-001〜003 | 元画像inline配信とRange応答 |
| `specs/issue-0010-expired-session-page.md` | BDD-SESSION-004, BDD-ACCESS-001〜004, BDD-E2E-002 | 期限切れ拒否、案内画面、再転送 |
| `specs/issue-0011-lan-interface-selection.md` | BDD-NETWORK-001〜003, BDD-TRANSFER-001〜002 | LAN IPv4候補の表示・選択・保持とQR反映 |

