# TransferImageQR

Windows PC上の画像を、クラウドやNASを経由せず、同一LAN上のiPhoneへ渡すためのC# / .NET 8 Windows Formsアプリです。

現在はMVP-001のアプリケーション基盤までを実装しています。画像のドラッグ＆ドロップ、QRコード生成、HTTP配信は後続Issueで追加します。

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

## Project structure

- `TransferImageQR`: Windows Forms presentation and composition root
- `TransferImageQR.Application`: use cases and ports
- `TransferImageQR.Domain`: entities and business rules
- `TransferImageQR.Infrastructure`: file, network, HTTP, and OS adapters
- `tests/TransferImageQR.UnitTests`: fast unit and architecture tests
- `specs`: Issue-derived specifications and verification evidence
