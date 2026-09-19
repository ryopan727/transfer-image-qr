# 転送URLのQRコード生成・表示 Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0007` |
| Status | `Verified` |
| Source | [GitHub Issue #7](https://github.com/ryopan727/transfer-image-qr/issues/7) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

利用者が「QR作成」を押すだけで、現在のActive転送セッションを指すLAN内URLをQRコードとしてPC画面へ表示し、iPhone標準カメラから転送ページを開けるようにする。

## Background / Current State

- MVP-005で「QR作成」によるActiveセッションとTokenが生成される。
- MVP-006でKestrelが全NICの動的Portへバインドし、`/transfer/{token}`を提供する。
- 現在はLAN IPv4選択、転送URL構築、QR画像生成、QR表示領域がない。
- Issue本文のWPF表記はリポジトリ方針に従いWinFormsとして適用する。

## Scope

- ActiveセッションToken、選択LAN IPv4、Kestrel PortからのHTTP URL生成
- QRCoderによるURLのPNG QRコード生成
- WinForms上のQRコードとURL表示
- Active / Expired / 新しい転送に合わせたQR表示Lifecycle
- URL生成、IPv4選択、QR PNG、Presenter、Formの自動テスト

## Out of Scope

- 複数NIC候補のユーザー選択と設定保持（MVP-011）
- Safari画像一覧HTML（MVP-008）
- QRコードのデザイン変更、ロゴ合成、色変更
- Server/Firewall失敗の詳細診断（MVP-015）

## Actors / External Systems

- Windowsデスクトップアプリ利用者
- iPhone標準カメラ / Safari
- QRCoder 1.8.0
- Windowsネットワークインターフェース

## Functional Requirements

- FR-1: ActiveセッションのURLを`http://{LAN-IPv4}:{Kestrel-Port}/transfer/{token}`形式で生成する。
- FR-2: URLのTokenは現在作成したActiveセッションのTokenと一致する。
- FR-3: URL全体をQRCoderでError Correction Level QのPNG QRコードへ変換する。
- FR-4: Active中はQRコードと転送URLを画面に表示する。
- FR-5: Expired時はQR画像を隠して期限切れ表示にし、新しい転送開始時はQRとURLを破棄する。
- FR-6: LAN IPv4が取得できない場合、Active状態は維持し、QRを表示せず一般利用者向け案内を表示する。

## Non-functional Requirements / Constraints

- NFR-1: QRアルゴリズムを自前実装せず、QRCoder 1.8.0の`PngByteQRCode`を使用する。
- NFR-2: ApplicationはWinForms、QRCoder、NetworkInterfaceの具体型へ依存しない。
- NFR-3: QR画像の`Image`リソースは置換・新しい転送・Form破棄時に解放する。
- NFR-4: URLはHTTPのみとし、インターネット接続やDNSを要求しない。

## Interfaces / Data / UI Contract

### URL

```text
http://192.168.x.x:{dynamic-port}/transfer/{base64url-token}
```

- Scheme: `http`
- Host: 選択されたプライベートIPv4
- Port: 起動中Kestrelの割当Port
- Path: `/transfer/{URL-escaped-token}`

### 暫定LAN IPv4選択

1. OperationalStatusがUpで、EthernetまたはWireless80211のプライベートIPv4
2. その他のUp状態インターフェースのプライベートIPv4
3. 各Group内はInterface Index、Addressの順で決定
4. Loopback、IPv6、169.254/16、Public IPv4は除外

MVP-011で候補一覧とユーザー選択へ置き換える。

### UI

- Active: 余白を維持した正方形QR、読み取り先URL、新しい転送ボタン
- Expired: QRを隠し「QRコードの有効期限が切れました」を表示
- URL生成不可: QRを隠し「LAN用IPv4アドレスを取得できません」を表示
- Draft: QR領域を隠してDraft一覧を表示

## Acceptance Criteria

- [x] AC-1: ActiveセッションのLAN内URLを生成する。
- [x] AC-2: URLをQRコード化する。
- [x] AC-3: QRをWinForms画面に表示する。
- [x] AC-4: iPhone標準カメラで読み取れる形式・余白・コントラストのQRである。
- [x] AC-5: QRは現在のActiveセッションを指す。

## BDD Scenarios

```gherkin
Scenario: SC-1 ActiveセッションのQRを表示する
  Given KestrelがPort 5000で起動しLAN IPv4が192.168.1.20である
  And Tokenがabc_123のActiveセッションを作成する
  When QR作成処理が完了する
  Then URLはhttp://192.168.1.20:5000/transfer/abc_123である
  And そのURLを表すPNG QRコードが画面へ表示される

Scenario: SC-2 QRが現在のセッションを指す
  Given 以前の転送を終了して新しいDraftを作成した
  When 新しいActiveセッションを作成する
  Then QR URLは新しいTokenを含む
  And 以前のTokenを含まない

Scenario: SC-3 セッション期限切れを表示する
  Given QRを表示中のセッションがActiveである
  When 5分経過してExpiredになる
  Then QR画像は隠れる
  And 期限切れ案内と新しい転送ボタンが表示される

Scenario: SC-4 LAN IPv4を取得できない
  Given Kestrelは起動している
  And 利用可能なプライベートIPv4がない
  When Activeセッションを作成する
  Then アプリは異常終了しない
  And QRを表示せずLAN IPv4取得不可を案内する
```

## Error / Edge Cases

- Kestrelが未起動またはPortが無効な場合はURL生成失敗として扱う。
- QR生成例外の詳細は一般UIへ露出しない。詳細診断はMVP-015で拡張する。
- 画面更新Timerでは同じQR画像を毎秒再生成しない。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | QR/URL ViewModel、Presenter調停、PictureBox/案内Panel | Timer更新時は既存QRを再利用 |
| Application / Domain | Server Endpoint / LAN Address / Transfer URL / QR Generator Port | Domain変更なし |
| Infrastructure | NetworkInterface列挙、QRCoder Adapter | QRCoder 1.8.0 PackageReference |
| Tests / Harness | URL/選択Unit、QR PNG Integration、Presenter/Form | 実iPhone cameraは手動未実行 |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-1, SC-4 | Application + Infrastructure | `TransferUrlProviderTests`、`SystemLanAddressProviderTests` | Passed |
| AC-2 | SC-1 | Infrastructure | `QrCoderPngGeneratorTests`でPNG signature、正方形、300px以上を確認 | Passed |
| AC-3 | SC-1, SC-3 | Presentation | `Form1Tests`でPictureBox、URL、期限切れ、LAN IP取得不可を確認 | Passed |
| AC-4 | SC-1 | Library contract + Presentation | ECC Q、標準黒白、quiet zone、12 pixels/module | Passed（実iPhone未実行） |
| AC-5 | SC-2 | Application + Presentation | `TransferQrCodeServiceTests`、`MainPresenterTests`で現在TokenとQR入力の一致を確認 | Passed |

## Unknowns / Human Decisions

- Decision: QRライブラリは公式NuGetのQRCoder 1.8.0を使用し、platform-independentな`PngByteQRCode`を採用する。
- Decision: ECC Level Q、pixels-per-module 12、quiet zone有効、黒/白の標準配色を使用する。
- Decision: MVP-011までの暫定IPv4選択は物理Ethernet/Wi-Fiを優先する決定的規則とする。
- Residual risk: 実iPhoneカメラでの読み取りと複数NIC環境の最適性は自動環境で検証できず、MVP-011/016で再確認する。

## Implementation Notes

- `IHttpServerEndpoint`、`ILanAddressProvider`、`ITransferUrlProvider`、`IQrCodeGenerator`をApplication Portとして定義し、WinFormsと具体ライブラリを分離した。
- `SystemLanAddressProvider`はMVP-011までの暫定規則でプライベートIPv4を決定し、取得不能時は例外終了させずQR非表示の案内へ切り替える。
- `QrCoderPngGenerator`はQRCoder 1.8.0の`PngByteQRCode`でECC Q、12 pixels/module、quiet zone有効のPNGを生成する。
- PresenterはActive化直後のSession Tokenから一度だけQRを生成し、Timer更新では同じPNGを再生成しない。
- FormはQR画像の置換、Draft復帰、破棄時に以前の`Image`をDisposeする。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| `task verify` | Passed | Build: 0 warnings / 0 errors、Unit: 70 passed |
| `task test:integration` | Passed | Integration: 4 passed |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | Formatting差分なし |
| `git diff --check` | Passed | Whitespace errorなし（Gitの改行コード通知のみ） |
| WinForms executable smoke test | Passed | Process継続動作、Kestrel動的Port listenを確認 |

## References

- [NuGet: QRCoder 1.8.0](https://www.nuget.org/packages/QRCoder/1.8.0)
- [QRCoder: PngByteQRCode renderer](https://github.com/codebude/QRCoder/wiki/advanced-usage---qr-code-renderers#25-pngbyteqrcode-renderer-in-detail)

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #7、MVP-005/006、Library公式資料から受け入れ契約を確定 |
| 2026-09-20 | Marked Verified | MVP-007実装、Unit/Integration、build、format、起動確認を完了 |
