# TransferImageQR

Windows PC上の画像を、クラウドやNASを経由せず、同一LAN上のiPhoneへ渡すためのC# / .NET 8 Windows Formsアプリです。

MVP-001〜016を実装しており、JPEG、PNG、WebPをドラッグ＆ドロップしてDraftを編集し、「QR作成」で5分間有効な転送セッションを確定できます。アプリ内KestrelがActiveセッションの元画像をLAN内HTTPで直接配信し、現在のセッションURLをQRコードとして表示します。QRをiPhoneで読み取ると、Safari向けの画像一覧から元画像を1枚ずつ開けます。背景、Tray常駐、Windows Login時の自動起動、配信Endpointと接続診断もMain Windowから利用できます。

## Requirements

- Windows
- .NET 8 SDK

## Build and test

Task CLIが利用できる場合:

```powershell
task verify
task test:integration
task test:acceptance
task test:all
```

Task CLIがない場合:

```powershell
dotnet restore TransferImageQR.sln
dotnet build TransferImageQR.sln --no-restore
dotnet test TransferImageQR.sln --no-build
```

## Run

```powershell
dotnet run --project TransferImageQR/TransferImageQR.csproj
```

起動後、「画像をここにドロップ」と表示された領域へ画像ファイルをドロップします。複数ファイルを同時に追加でき、追加操作を繰り返すと同じDraftへ追記されます。一覧で画像を選択すると個別削除でき、「全クリア」でDraftを空にできます。「QR作成」でDraftを確定すると状態がActiveになり、同一LANから到達できるURLとQRコードが表示されます。iPhoneの標準カメラで読み取ると、Safariに最大20枚のresponsive画像一覧が表示されます。画像カードをタップすると、変換・再圧縮されていない元画像がインライン表示され、iOSの標準操作を利用できます。セッションは5分後にExpiredへ変わり、QRコードは非表示になります。期限切れURLをSafariで開くと再作成を促す案内が表示され、「新しい転送」でPC側を空のDraftへ戻せます。

Kestrelはアプリ起動時に全ネットワークインターフェースの動的Portで開始し、終了時に停止します。HTTP Routeは`/transfer/{token}`と`/transfer/{token}/images/{imageId}`です。QRコードには、起動中のKestrel Port、選択したプライベートIPv4、現在のセッショントークンから構築したURLを埋め込みます。複数NICがある場合はMain Windowで配信に使うAddressを選択できます。

## MVP手動受入確認

自動Suiteは実File、画像Decode、QR生成、Kestrel、HTTP配信、入力制約、Token、5分失効を検証します。iPhone Camera/Safari、写真保存、実Firewall、Windows再Loginは次の手順で確認します。

### 事前条件

- Windows PCとiPhoneを同じLANへ接続する。
- JPEG、PNG、WebPを各1枚用意する。各Fileは10MB以下にする。
- 必要に応じてWindows FirewallでTransferImageQRのPrivate network通信を許可する。
- `task verify`、`task test:integration`、`task test:acceptance`がすべて成功していることを確認する。

### 主要転送フロー

1. `dotnet run --project TransferImageQR/TransferImageQR.csproj`で起動する。
2. Main Windowの診断欄に`http://<選択IP>:<Port>`、同一LAN、Windows Firewallの確認案内が表示されることを確認する。
3. JPEG、PNG、WebPをD&Dし、3枚が順番どおりDraftへ表示されることを確認する。
4. 「QR作成」を選び、状態がActiveになり、Draftの追加・削除・全Clearができなくなることを確認する。
5. iPhone標準CameraでQRを読み、Safariに3枚の一覧が表示されることを確認する。
6. 各画像を開き、元のJPEG、PNG、WebPが表示され、iOS標準操作で写真へ保存できることを確認する。
7. Session作成から5分後に同じ一覧URLと画像URLを再読込し、期限切れ案内が表示されることを確認する。
8. PCで「新しい転送」を選び、空の編集可能Draftへ戻ることを確認する。

### Error・設定フロー

1. Active化後に元画像を移動または削除し、該当URLが404の「元画像が見つかりません」Pageになることを確認する。
2. iPhoneを別LANへ切り替えた場合は接続できず、PC診断欄の同一LAN・Windows Firewall案内から復旧確認できることを確認する。
3. 背景画像を選択・調整して再起動し、表示設定が復元されることを確認する。
4. 「閉じたときトレイに格納」をONにしてWindowを閉じ、Trayから再表示・終了できることを確認する。
5. 「Windowsログイン時に起動」をONにしてWindowsへ再Loginし、TransferImageQRが起動することを確認する。確認後にOFFへ戻し、次回Loginでは起動しないことを確認する。

実施日、Windows/iOS version、Network種別、各手順の結果をIssueまたはRelease checklistへ記録してください。未実施の実機項目を自動Test成功で代替したものとして扱わないでください。

## Project structure

- `TransferImageQR`: Windows Forms presentation and composition root
- `TransferImageQR.Application`: use cases and ports
- `TransferImageQR.Domain`: entities and business rules
- `TransferImageQR.Infrastructure`: file, network, HTTP, and OS adapters
- `tests/TransferImageQR.UnitTests`: fast unit and architecture tests
- `tests/TransferImageQR.IntegrationTests`: real Kestrel and file-delivery integration tests
- `tests/TransferImageQR.AcceptanceTests`: production adaptersを横断するMVP acceptance tests
- `specs`: Issue-derived specifications and verification evidence
