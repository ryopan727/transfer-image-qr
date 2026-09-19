# TransferImageQR

Windows PC上の画像を、クラウドやNASを経由せず、同一LAN上のiPhoneへ渡すためのC# / .NET 8 Windows Formsアプリです。

現在はMVP-003まで実装しており、JPEG、PNG、WebPをドラッグ＆ドロップしてDraftのサムネイル一覧へ追加できます。1ファイル10MB、Draft最大20枚の制限があり、追加できないファイルはファイル名と理由を画面へ表示します。QRコード生成とHTTP配信は後続Issueで追加します。

## Requirements

- Windows
- .NET 8 SDK

## Build and test

Task CLIが利用できる場合:

```powershell
task verify
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

起動後、「画像をここにドロップ」と表示された領域へ画像ファイルをドロップします。複数ファイルを同時に追加でき、追加操作を繰り返すと同じDraftへ追記されます。

## Project structure

- `TransferImageQR`: Windows Forms presentation and composition root
- `TransferImageQR.Application`: use cases and ports
- `TransferImageQR.Domain`: entities and business rules
- `TransferImageQR.Infrastructure`: file, network, HTTP, and OS adapters
- `tests/TransferImageQR.UnitTests`: fast unit and architecture tests
- `specs`: Issue-derived specifications and verification evidence
