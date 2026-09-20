# TransferImageQR

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4?logo=windows)
![UI](https://img.shields.io/badge/UI-Windows%20Forms-5C2D91)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Windows PCにある画像を、クラウドへアップロードせず、同じLAN上のiPhoneへQRコードで渡すデスクトップアプリです。

画像をドラッグ＆ドロップしてQRコードを作成すると、アプリ内のHTTPサーバーが5分間だけ画像を配信します。iPhoneの標準カメラでQRコードを読み取り、Safariから元画像を表示・保存できます。

> [!IMPORTANT]
> 現在はソースから実行するMVPです。インストーラーや署名済みバイナリはまだ提供していません。

## Features

- JPEG / PNG / WebPをドラッグ＆ドロップで追加
- 最大20枚、1ファイル10MBまでの入力検証
- Draft内の個別削除・全クリア
- 5分間有効な推測困難トークン付き転送URLとQRコード
- ASP.NET Core Kestrelによる同一LAN内の直接配信
- iPhone Safari向けのレスポンシブ画像一覧
- 変換・再圧縮を行わない元画像配信とHTTP Range対応
- 複数LANインターフェースから配信IPアドレスを選択
- 背景画像、不透明度、拡大率、表示位置のカスタマイズ
- Windows通知領域への常駐とWindowsログイン時の自動起動
- HTTPサーバー起動失敗、元画像消失、期限切れの利用者向け案内

### Current limits

| Item | Limit |
| --- | --- |
| Supported OS | Windows |
| Image formats | JPEG, PNG, WebP |
| Images per transfer | 20 |
| File size | 10MB per image |
| Session lifetime | 5 minutes |
| Network | PCとiPhoneが同一LANに接続されていること |
| Persistence | 転送Sessionはメモリ内のみ。アプリ終了後は復元しない |

## How it works

1. Windowsアプリへ画像をドラッグ＆ドロップします。
2. アプリが画像を検証し、5分間有効な転送Sessionを作成します。
3. 選択したLAN IPv4アドレス、Kestrelの動的Port、Session TokenからURLとQRコードを生成します。
4. iPhoneがQRコードを読み取り、PCから元画像を直接取得します。

クラウド、NAS、外部APIは転送経路に使用しません。

## Requirements

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PCと転送先端末が接続された同一LAN
- 必要に応じてWindows FirewallでPrivate network通信を許可できること
- 任意: [Task](https://taskfile.dev/) CLI

## Quick start

```powershell
git clone https://github.com/ryopan727/transfer-image-qr.git
cd transfer-image-qr
dotnet restore TransferImageQR.sln
dotnet run --project TransferImageQR/TransferImageQR.csproj
```

起動後、「画像をここにドロップ」へ画像を追加して「QR作成」を選択します。表示されたQRコードをiPhoneの標準カメラで読み取ってください。

## Usage

### Transfer images

1. JPEG、PNG、WebPをドロップ領域へ追加します。追加操作は複数回行えます。
2. 必要に応じて一覧から画像を削除するか、「全クリア」でDraftを空にします。
3. 複数のLANアドレスがある場合、iPhoneと同じLANのアドレスを選択します。
4. 「QR作成」を選択します。SessionがActiveになるとDraftは編集できません。
5. iPhoneでQRコードを読み、Safariの一覧から画像を開いて保存します。
6. 続けて転送する場合は「新しい転送」を選択します。

Session作成から5分が経過すると、一覧と画像URLは`410 Gone`になり、再作成を案内します。Active化後に元画像が移動・削除された場合は、安全な`404 Not Found`ページを表示します。

### Desktop settings

- 背景画像と表示方法はLocal App Dataへ保存されます。
- 「閉じたときトレイに格納」を有効にすると、Windowを閉じても通知領域で動作を継続します。
- 「Windowsログイン時に起動」はCurrent UserのRegistry Run設定を使用し、管理者権限を要求しません。

## Tech stack

| Area | Technology | Role |
| --- | --- | --- |
| Language / Runtime | C# / .NET 8 | Application全体 |
| Desktop UI | Windows Forms | Drag & drop、Draft、QR、設定画面 |
| HTTP server | ASP.NET Core / Kestrel | LAN内の画像一覧・元画像配信 |
| Image processing | SkiaSharp 4.152.1 | 画像形式検出、Decode、Thumbnail生成 |
| QR code | QRCoder 1.8.0 | 転送URLのPNG QRコード生成 |
| Settings | System.Text.Json / Windows Registry | 背景・Tray設定、自動起動 |
| Tests | xUnit.net v3 | Unit、Presentation、Integration、Acceptance tests |
| Task runner | Taskfile | Restore、Build、Testの品質Gate |

## Build and test

Task CLIを使用する場合:

```powershell
task verify           # Restore、Build、全自動Suite
task test:unit        # Unit・Presentation
task test:integration # 実Kestrel・File配信
task test:acceptance  # 実FileからHTTP配信までの主要Flow
task test:all         # 全Test Suite
```

Task CLIを使用しない場合:

```powershell
dotnet restore TransferImageQR.sln
dotnet build TransferImageQR.sln --no-restore
dotnet test TransferImageQR.sln --no-build
```

現在の自動Suiteは次を含みます。

- Unit / Presentation: 104 tests
- Integration: 17 tests
- Acceptance: 2 tests

Test数は機能追加により増える場合があります。Merge前の品質Gateは`task verify`です。

## Security and privacy

- 画像をクラウドや第三者サービスへアップロードしません。
- 元画像はPC上の元ファイルから直接配信し、不要なCopyを作成しません。
- URLには暗号学的に安全なSession Tokenを含め、5分後に無効化します。
- 不正Tokenは`404`、期限切れSessionは`410`で拒否します。
- HTTP responseへ`no-store`を設定します。
- 通信は暗号化されていないLAN内HTTPです。信頼できるPrivate networkで使用してください。
- Application終了時にSessionは破棄されます。

## Troubleshooting

| Symptom | Check |
| --- | --- |
| iPhoneから開けない | PCとiPhoneが同じLANか、選択したIPv4が正しいか確認する |
| QRは読めるが接続できない | Windows FirewallでPrivate network通信が許可されているか確認する |
| HTTPサーバーを起動できない | Portを使用している別ProcessやSecurity softwareを確認する |
| 画像が見つからない | QR作成後に元画像を移動・削除していないか確認し、転送を作り直す |
| 期限切れと表示される | PCで「新しい転送」を選び、新しいQRコードを作成する |
| LANアドレスが複数ある | iPhoneと同じRouter / Wi-Fiに接続されたIPv4を選択する |

## Manual acceptance checklist

<details>
<summary>iPhone、Firewall、Windows再ログインを含む確認手順</summary>

### Preconditions

- Windows PCとiPhoneを同じLANへ接続する。
- 10MB以下のJPEG、PNG、WebPを各1枚用意する。
- `task verify`が成功していることを確認する。

### Main flow

1. アプリを起動し、正常時は画面上部に診断Errorがないことを確認する。
2. JPEG、PNG、WebPをD&Dし、3枚が順番どおりDraftへ表示されることを確認する。
3. 「QR作成」を選び、Active状態、転送URLのIP / Port、Draft編集不可を確認する。
4. iPhone標準CameraでQRを読み、Safariに3枚の一覧が表示されることを確認する。
5. 各画像を開き、元画像が表示され、iOS標準操作で写真へ保存できることを確認する。
6. 5分後に一覧URLと画像URLを再読込し、期限切れ案内を確認する。
7. 「新しい転送」で空の編集可能Draftへ戻ることを確認する。

### Error and settings

1. Active化後に元画像を移動し、画像URLが404の案内ページになることを確認する。
2. iPhoneを別LANへ切り替えると接続できず、同一LANへ戻すと復旧することを確認する。
3. `設定` → `背景設定`で専用画面を開き、背景画像を選択・調整して再起動し、設定が復元されることを確認する。
4. Tray設定をONにし、Windowを閉じた後に通知領域から再表示・終了できることを確認する。
5. 自動起動をONにしてWindowsへ再Loginし、起動を確認後、OFFへ戻す。

実施日、Windows / iOS version、Network種別、各手順の結果をIssueまたはRelease checklistへ記録してください。未実施の実機項目を自動Test成功として扱わないでください。

</details>

## Contributing

変更前にRepository rootの[`AGENTS.md`](AGENTS.md)と[`specs/README.md`](specs/README.md)を確認してください。

- 1つの論理変更につき1つのBranchを使用する。
- Production codeより先に対応する`specs/`の差分仕様とCanonical BDDを更新する。
- Layer間の依存方向を維持する。
- PR前に`task verify`を成功させ、Test evidenceと残存Riskを記録する。

Bug reportやFeature requestは[GitHub Issues](https://github.com/ryopan727/transfer-image-qr/issues)へお願いします。

## License

このProjectは[MIT License](LICENSE)で公開されています。
