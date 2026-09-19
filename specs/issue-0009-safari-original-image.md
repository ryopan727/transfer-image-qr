# Safari元画像表示 Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0009` |
| Status | `Verified` |
| Source | [GitHub Issue #9](https://github.com/ryopan727/transfer-image-qr/issues/9) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

iPhone利用者がSafariの画像一覧から1枚を選び、変換されていない元画像をブラウザで直接開いて、iOS標準操作から写真へ保存できるHTTP応答を提供する。

## Background / Current State

- MVP-006でActive Sessionの画像を`GET /transfer/{token}/images/{imageId}`から元ファイル直接配信している。
- MVP-008で一覧の各画像カードの`href`と`img src`が同じ画像Routeを指している。
- JPEG / PNG / WebPのContent-Type、元バイト一致、Range processingは基盤として実装済みである。
- 一方、Safariでattachment downloadではなく元画像を開く契約、元ファイル名、Range応答、一覧から画像までの導線はMVP-009として明示的に検証されていない。

## Scope

- 一覧カードから対応する元画像Routeへの1対1リンク契約
- JPEG / PNG / WebPのインライン画像応答
- UTF-8元ファイル名を含む`Content-Disposition: inline`応答
- 元ファイルの全バイト一致とContent-Length
- Range Requestに対する206、Content-Range、元ファイル部分バイト
- 実Kestrelと一時ファイルを使用したIntegration Test

## Out of Scope

- 一括保存、ZIP生成、写真アプリへの自動登録
- 専用iPhoneアプリ、JavaScript download処理
- サーバー側の画像変換、リサイズ、再圧縮、透かし、派生ファイル
- Expired専用画面とキャッシュ制御の拡張（MVP-010）
- iOS Safari自体の保存UI変更や自動操作

## Functional Requirements

- FR-1: 一覧の各カードを選択すると、そのSessionと画像IDに対応する元画像Routeへ遷移する。
- FR-2: JPEGは`image/jpeg`、PNGは`image/png`、WebPは`image/webp`として返す。
- FR-3: 画像応答は`Content-Disposition: inline`とUTF-8の元ファイル名を持つ。
- FR-4: 通常GETは元ファイルと同一のバイト列およびContent-Lengthを返す。
- FR-5: 有効な単一Range Requestには206、`Content-Range`、要求範囲と一致する元バイトを返す。
- FR-6: 無効Token、Session外の画像ID、消失ファイルは404を返す。Expired SessionはMVP-010の410期限切れ画面契約に従う。

## Non-functional Requirements / Constraints

- NFR-1: 元画像を復号、再エンコード、縮小、再圧縮しない。
- NFR-2: 中間コピー、永続キャッシュ、派生画像ファイルを作成せず、元ファイルPathをKestrelへ渡す。
- NFR-3: `Content-Disposition`のファイル名はHeaderとして安全なRFC 5987形式で表現する。
- NFR-4: Application / DomainへASP.NET CoreやHTTP Headerの依存を持ち込まない。
- NFR-5: iOS標準操作を妨げる`attachment`やHTMLの`download`属性を使用しない。

## HTTP Contract

### Full image

```http
GET /transfer/{token}/images/{imageId}

200 OK
Content-Type: image/jpeg | image/png | image/webp
Content-Disposition: inline; filename*=utf-8''{encoded-original-file-name}
Content-Length: {original-file-length}
Accept-Ranges: bytes
```

Bodyは元ファイルと同じバイト列である。

### Partial image

```http
GET /transfer/{token}/images/{imageId}
Range: bytes={start}-{end}

206 Partial Content
Content-Range: bytes {start}-{end}/{original-file-length}
```

Bodyは元ファイルの指定範囲と同じバイト列である。

## Acceptance Criteria

- [x] AC-1: 一覧の画像をタップすると元画像を表示する。
- [x] AC-2: JPEG / PNG / WebPの元画像を返す。
- [x] AC-3: 画像の解像度を意図せず縮小・再圧縮しない。
- [x] AC-4: Safariの標準操作で画像を保存できる。

## BDD Scenarios

```gherkin
Scenario: SC-1 一覧から元画像を開く
  Given JPEGを含むActive Sessionの一覧をSafariで表示している
  When JPEGの画像カードを選択する
  Then カードはそのJPEGの元画像Routeへ移動する
  And 応答はimage/jpegかつinlineである
  And attachment downloadを要求しない

Scenario: SC-2 各対応形式の元バイトを返す
  Given JPEG、PNG、WebPを含むActive Sessionがある
  When 各画像RouteをGETする
  Then 対応する画像Content-Typeが返る
  And Content-Lengthは元ファイル長と一致する
  And Bodyは元ファイルと全バイト一致する

Scenario: SC-3 画像を部分取得する
  Given Active Sessionの元画像がある
  When Range Headerで画像の一部を要求する
  Then Statusは206である
  And Content-Rangeは要求範囲と元ファイル長を示す
  And Bodyは元ファイルの同じ範囲と一致する

Scenario: SC-4 元ファイル名をSafariへ渡す
  Given 日本語と空白を含むファイル名の画像がある
  When 画像RouteをGETする
  Then Content-Dispositionはinlineである
  And filename*にUTF-8の元ファイル名が安全に設定される
```

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Domain / Application | 変更なし | Active SessionとDraftImageを再利用 |
| Infrastructure | 画像Routeのinline filename Header | 元Path直接配信は維持 |
| Safari HTML | 変更なし | MVP-008のカードリンクを検証対象にする |
| Tests | HTTP Integration Testを強化 | Full response、Range、Header、gallery link |
| Documentation | READMEと本Specification | 元画像表示・保存導線を記録 |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-1 | Integration | Gallery hrefと`GetImage_WithActiveSession_ReturnsOriginalBytesAndContentTypes` | Passed |
| AC-2 | SC-2 | Integration | JPEG / PNG / WebPのContent-Typeと全バイト一致 | Passed |
| AC-3 | SC-2, SC-3 | Integration | Content-Length、全バイト一致、`GetImage_WithRangeHeader_ReturnsOriginalByteRange` | Passed |
| AC-4 | SC-1, SC-4 | Integration + manual boundary | inline disposition、UTF-8 filename、attachment不使用 | Passed（実iPhone未実行） |

## Unknowns / Human Decisions

- Decision: 「元画像を表示する」は専用Viewer HTMLではなく、画像Content-Typeの元ファイル応答をSafariで直接開くこととする。これによりiOSの画像用標準操作を利用できる。
- Decision: ファイル名は`filename*`へUTF-8で設定し、Dispositionは`inline`とする。`attachment`は使用しない。
- Decision: 解像度・圧縮状態の不変性は、画像を一切decodeせず元Pathから全バイト一致で返すことで保証する。
- Residual risk: iOS Safariの長押し保存操作と写真アプリへの保存結果は実端末が必要であり、自動環境では検証しない。

## Implementation Notes

- 一覧カードはMVP-008の既存元画像Routeを`href`として使用し、画像ごとの直接表示導線を維持した。
- 画像Routeは`Content-DispositionHeaderValue`で`inline`とUTF-8の`filename*`を設定する。
- `Results.File`へ元ファイルPathをそのまま渡し、Content-Type、Content-Length、Range processingをKestrelに委譲する。
- 画像decode、resize、encode処理や派生ファイルを追加せず、通常GETとRange GETの双方で元バイト一致を検証した。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象Integration TestのRED確認 | Passed | 元バイト・Rangeは既存実装で成功し、inline Header未実装の2件が意図どおり失敗 |
| `task verify` | Passed | Build: 0 warnings / 0 errors、Unit: 70 passed |
| `task test:integration` | Passed | Integration: 7 passed |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | Formatting差分なし |
| `git diff --check` | Passed | Whitespace errorなし（Gitの改行コード通知のみ） |
| WinForms executable smoke test | Passed | Process継続動作、Kestrel動的Port listenを確認 |
| 実iPhone Safari save | Not Run | 実端末なし。inline image responseと元バイト不変のHTTP契約で代替 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #9とMVP-006/008の実装差分からinline表示・元画像不変の契約を確定 |
| 2026-09-20 | Marked Verified | inline元画像配信、Range、Integration、build、format、起動確認を完了 |
| 2026-09-20 | Updated expiration contract | MVP-010で正しいExpired Tokenを410期限切れ画面へ拡張 |
