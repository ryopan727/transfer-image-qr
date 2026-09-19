# TransferImageQR

Windows PC上の画像を、クラウドやNASを経由せず、同一LAN上のiPhoneへ渡すためのC# / .NET 8 Windows Formsアプリです。

現在はMVP-006まで実装しており、JPEG、PNG、WebPをドラッグ＆ドロップしてDraftを編集し、「QR作成」で5分間有効な転送セッションを確定できます。アプリ内KestrelがActiveセッションの元画像をLAN内HTTPで直接配信します。QRコード画像とSafari向け一覧は後続Issueで追加します。

## Requirements

- Windows
- .NET 8 SDK

## Build and test

Task CLIが利用できる場合:

```powershell
task verify
task test:integration
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

起動後、「画像をここにドロップ」と表示された領域へ画像ファイルをドロップします。複数ファイルを同時に追加でき、追加操作を繰り返すと同じDraftへ追記されます。一覧で画像を選択すると個別削除でき、「全クリア」でDraftを空にできます。「QR作成」でDraftを確定すると状態がActiveになり、5分後にExpiredへ変わります。「新しい転送」で空のDraftからやり直せます。

Kestrelはアプリ起動時に全ネットワークインターフェースの動的Portで開始し、終了時に停止します。HTTP Routeは`/transfer/{token}`と`/transfer/{token}/images/{imageId}`です。到達用LAN IPとURLの画面表示・QRコード化は後続Issueで追加します。

## Project structure

- `TransferImageQR`: Windows Forms presentation and composition root
- `TransferImageQR.Application`: use cases and ports
- `TransferImageQR.Domain`: entities and business rules
- `TransferImageQR.Infrastructure`: file, network, HTTP, and OS adapters
- `tests/TransferImageQR.UnitTests`: fast unit and architecture tests
- `tests/TransferImageQR.IntegrationTests`: real Kestrel and file-delivery integration tests
- `specs`: Issue-derived specifications and verification evidence
