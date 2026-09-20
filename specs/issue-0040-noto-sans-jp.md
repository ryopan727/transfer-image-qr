# Issue #40 デスクトップUIのNoto Sans JP統一

Status: Verified

GitHub Issue: https://github.com/ryopan727/transfer-image-qr/issues/40

## Goal

Windows FormsのMain Windowと背景設定FormをNoto Sans JPで統一し、日本語表示の一貫性と可読性を向上する。利用者のWindowsへFontをInstallすることは要求しない。

## Current State

- Application既定Fontは.NET 8 Windows Formsの既定値に依存する。
- Main Windowの見出し、状態、案内等は`Segoe UI`を明示している。
- 利用者のWindowsにNoto Sans JPがInstallされている保証はない。

## Scope

- 公式Google Fontsの`NotoSansJP[wght].ttf`をAssembly Resourceとして同梱する。
- SIL Open Font License 1.1をRepositoryへ同梱する。
- Application起動中だけPrivate Fontとして読み込み、OSへInstallしない。
- Main Window、背景設定Form、Menu、子ControlへNoto Sans JPを適用する。
- 既存のFont sizeとRegular／Bold styleを維持する。
- Resourceの欠落・破損・読込失敗時はSystem既定FontへFallbackし、Application起動を継続する。

## Out of Scope

- Safari向けHTMLへのWeb Font追加
- 利用者によるFont選択
- ThemeやFont size設定
- OSへのFont Install

## Functional Requirements

1. Main Windowと背景設定Formの主要Controlが`Noto Sans JP`で描画されること。
2. 見出しと強調LabelのBold、既存ControlのFont sizeを維持すること。
3. Font ResourceをAssembly内から読み込み、外部Font fileやOS Installへ依存しないこと。
4. Font load失敗時は例外でApplicationを終了せず、System既定FontへFallbackすること。
5. Font dataのUnmanaged memoryとPrivate Font resourceをApplication終了時に解放すること。
6. Font licenseと取得元をRepository内で確認できること。

## Acceptance Criteria

1. Main Windowと背景設定FormのForm／主要ControlのFont familyが`Noto Sans JP`になる。
2. Main見出しは24pt Bold、既存の強調表示もBoldを維持する。
3. OSにNoto Sans JPが未Installでも同梱Resourceから利用できる。
4. 無効なFont dataを渡した場合はSystem既定FontへFallbackする。
5. `dotnet publish`成果物のAssemblyにFont Resourceが含まれる。
6. Unit／Presentation／既存Integration／Acceptance testsが成功する。

## BDD Delta

- `BDD-FONT-001`を追加し、同梱Noto Sans JPを全Desktop Formへ適用する振る舞いを定義する。
- `BDD-FONT-002`を追加し、Font load失敗時の安全なFallbackを定義する。

## Test Design

- Unit testで埋め込みResourceから`Noto Sans JP`を読み込めることを検証する。
- Unit testで無効なFont bytesがSystem既定FontへFallbackすることを検証する。
- Presentation testでMain Windowと背景設定Formの主要ControlがNoto Sans JPを使用し、Size／Boldを維持することを検証する。
- `dotnet publish`後のAssembly Resource名を検証する。
- `task verify`で既存Suiteを回帰実行する。

## Risks and Decisions

- Font Resourceは約9.6MBであり、Application／publish Sizeが増加する。OS Install不要という再現性を優先する。
- 配布FontはWeight 100〜900のVariable Fontである。GDI+上でRegular／Boldの生成可否を自動Testする。
- Font load失敗はUIの致命的障害とせず、System既定FontへFallbackする。

## Manual Visual Check

1. Main Windowを起動し、日本語、英数字、記号がNoto Sans JPで欠けずに表示されることを確認する。
2. Main見出し、Drop案内、Draft数、QR案内のBoldと文字Sizeが不自然に変わっていないことを確認する。
3. `設定` → `背景設定`を開き、Preview、Button、Label、数値入力の文字が切れずに表示されることを確認する。
4. WindowsへNoto Sans JPをInstallしていない環境でpublish成果物を起動し、同じ表示になることを確認する。

## Verification Evidence

- RED: `ApplicationFonts`と`ApplicationFontManager`が存在せず、新規3 testsがCompile errorになることを確認。
- GREEN: Embedded Noto Sans JP、Regular／Bold、破損Font fallback、両Formへの適用を検証する3 testsが成功。
- Parallel regression: 複数STA Form testsで検出した共有Font cache競合を同期化し、全Suiteで再検証。
- `task verify`: Build warning 0 / error 0、Unit 110、Integration 17、Acceptance 2、全129 tests成功。
- Release publish: `TransferImageQR.exe`、Assembly内`TransferImageQR.Assets.Fonts.NotoSansJP.ttf`、`NotoSansJP-OFL.txt`を確認。
- Font SHA-256: `C2F3B4D463500A2DDCD3849CDED1FCEEB9FD6D1C32E6CBECD568453BA50FC68F`。
- Manual Visual Check: Not Run。上記手順で実機確認が必要。
