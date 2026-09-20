# カスタム背景画像 Change Specification

この文書はIssueによる変更差分と検証Evidenceを記録する。変更後の外部仕様の正本は `specs/bdd.md` とし、本ファイルを現行仕様の正本として扱わない。

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0013` |
| Status | `Verified` |
| Source | [GitHub Issue #13](https://github.com/ryopan727/transfer-image-qr/issues/13) |
| Canonical BDD | [`specs/bdd.md`](bdd.md) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

Windows利用者がローカルの推しメン画像をMain Windowの背景へ設定し、前景の操作性を保ったまま好みの見え方へ調整できるようにする。

## Background / Current State

- `Form1`は単色の既定背景だけを表示し、背景画像の選択または調整UIを持たない。
- Application終了時に復元する設定保存機構はない。
- D&D領域とQR領域は不透明なPanelであり、背景描画を追加しても前景面として維持できる。

## Scope

- ローカルJPEG、PNG、WebP画像の背景選択
- 元画像のAlphaを維持した背景描画
- 0〜100%の背景不透明度、25〜300%の拡大率、X/Yオフセットの調整
- 選択Pathと表示設定のLocal App Dataへの永続化
- Application起動時の設定復元
- カスタム背景のClearと既定背景への復帰
- D&D領域、QR、主要文字の視認性維持
- 正常系、破損・消失File、範囲外設定、再起動相当のUnit／Integration／Presentation Test

## Out of Scope

- 背景画像File自体のApplication管理領域へのCopy
- Crop、回転、色補正、ぼかし等の画像編集
- 複数背景のPresetまたは自動切替
- Cloud同期、別PCとの設定共有
- Windows themeに応じた自動画質補正

## Actors / External Systems

- Windows利用者: 背景画像を選択し、表示を調整またはClearする
- Windows filesystem: 元画像とLocal App Data内のJSON設定を保持する

## Functional Requirements

- FR-1: 利用者はFile選択DialogからJPEG、PNG、WebPを背景へ選択できる。
- FR-2: PNG等が持つAlphaを維持し、Application指定の不透明度を画像Alphaへ乗算して描画する。
- FR-3: 利用者は不透明度を0〜100%、拡大率を25〜300%、X/Y位置を中央基準のPixel offsetで調整できる。
- FR-4: 選択Path、不透明度、拡大率、X/Y offsetを変更の都度永続化する。
- FR-5: Application起動時に設定と画像を復元する。
- FR-6: 背景をClearすると画像選択と表示調整を既定値へ戻し、その状態を永続化する。
- FR-7: 保存画像が消失または読取不能の場合はApplicationを起動でき、既定背景と利用者向けErrorを表示する。

## Non-functional Requirements / Constraints

- NFR-1: 設定File I/Oと画像DecodeはInfrastructureへ置き、FormまたはPresenterから直接Fileを読み書きしない。
- NFR-2: 背景画像はWindow内へContainする基準Sizeに拡大率を適用し、Aspect比を維持する。
- NFR-3: D&D領域とQR領域は不透明な前景面を維持し、Form直下の主要文字には可読性を確保する背景を設ける。
- NFR-4: 設定JSONが存在しない、破損している、または範囲外の値を持つ場合は安全な既定値へ正規化する。
- NFR-5: 背景Image、Stream、描画Resourceを確実に破棄し、選択変更でFile lockを残さない。

## Interfaces / Data / UI Contract

### UI

- Main Windowに「背景を選択」「背景をクリア」、不透明度、サイズ、横位置、縦位置のControlを表示する。
- Slider/数値は不透明度0〜100%、サイズ25〜300%、位置-1000〜1000pxを示す。
- Controlには安定したName、Accessible Name、Tab Orderを設定する。
- 背景選択または復元に失敗した場合は設定欄へErrorを表示し、転送操作は継続できる。

### Data

- `%LocalAppData%/TransferImageQR/background-settings.json`へVersion、画像Path、不透明度、拡大率、X/Y offsetをJSONで保存する。
- 元画像は参照Pathだけを保存し、Copyしない。
- 既定値は画像なし、不透明度35%、拡大率100%、X/Y offset 0pxとする。

## Acceptance Criteria

- [x] AC-1: ローカル画像を背景として選択できる。
- [x] AC-2: 透過PNGを元の透過状態を維持して表示できる。
- [x] AC-3: 背景の不透明度を変更できる。
- [x] AC-4: 表示位置とサイズを調整できる。
- [x] AC-5: Application再起動後も背景設定が維持される。
- [x] AC-6: カスタム背景をClearして既定背景へ戻せる。
- [x] AC-7: D&D領域、QR、文字の視認性を損なわない。

## BDD Delta

| Change | Target Scenario | Before | After / Reason |
| --- | --- | --- | --- |
| Add | `BDD-BACKGROUND-001` | なし | ローカル画像をAlphaを維持して背景表示する |
| Add | `BDD-BACKGROUND-002` | なし | 不透明度、拡大率、X/Y位置を調整する |
| Add | `BDD-BACKGROUND-003` | なし | 再起動後に設定を復元する |
| Add | `BDD-BACKGROUND-004` | なし | 背景をClearして既定表示へ戻す |
| Add | `BDD-BACKGROUND-005` | なし | 読取不能な保存画像から安全に復旧する |

### Proposed Canonical Scenarios

```gherkin
Scenario: BDD-BACKGROUND-001 透過画像を背景へ設定する
  Given Main Windowが既定背景で表示されている
  When 利用者がAlphaを持つローカルPNGを背景として選択する
  Then PNGの透明部分を維持してMain Window背景に表示される
  And D&D領域、QR領域、主要文字は読み取れる

Scenario: BDD-BACKGROUND-002 背景の見え方を調整する
  Given カスタム背景が表示されている
  When 利用者が不透明度、拡大率、X位置、Y位置を変更する
  Then Aspect比を維持した背景が指定値で再描画される
  And 変更した設定が保存される

Scenario: BDD-BACKGROUND-003 再起動後に背景設定を復元する
  Given カスタム背景と表示設定が保存されている
  When TransferImageQRを終了して再起動する
  Then 同じ画像、不透明度、拡大率、X位置、Y位置で背景が表示される

Scenario: BDD-BACKGROUND-004 カスタム背景をClearする
  Given カスタム背景が設定されている
  When 利用者が背景をクリアする
  Then Main Windowは既定背景へ戻る
  And 再起動してもカスタム背景は復元されない

Scenario: BDD-BACKGROUND-005 読み込めない保存画像から復旧する
  Given 保存済み背景画像が移動、削除、または破損している
  When TransferImageQRを起動する
  Then Main Windowは既定背景で表示される
  And 背景を読み込めなかったことが表示される
  And Draftと転送の操作は継続できる
```

## Error / Edge Cases

- File選択をCancelした場合は背景と保存設定を変更しない。
- 非対応または破損画像を選択した場合は現在の背景を維持してErrorを表示する。
- 保存画像が消失・破損した場合は既定背景へFallbackし、Application起動を妨げない。
- 範囲外または欠損したJSON値は既定値または許容範囲へ正規化する。
- 画像変更時とForm破棄時に以前のImageを破棄する。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | 背景設定Control、Presenter command、背景描画とError表示を追加 | File Dialogの結果だけをPresenterへ渡す |
| Application | 背景設定Model、Store/Image loader Port、設定Use Caseを追加 | WinForms型に依存しない |
| Infrastructure | JSON設定StoreとSkiaSharp背景画像Loaderを追加 | Local App Dataと画像File I/Oを担当 |
| Domain | 変更なし | 転送業務ルールとは独立したApplication設定 |
| Tests / Harness | Use Case、JSON Store、Alpha描画、Form配線Testを追加 | GUI実画面の視認性は手動確認が必要 |

## Test Strategy / Traceability

| AC | BDD Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | BDD-BACKGROUND-001 | Unit / Presentation | 画像読込、Presenter、Form選択反映 | Passed |
| AC-2 | BDD-BACKGROUND-001 | Unit / Presentation | Alpha付きPNG decode／pixel描画 | Passed |
| AC-3, AC-4 | BDD-BACKGROUND-002 | Unit / Presentation | 値正規化、保存、描画矩形 | Passed |
| AC-5 | BDD-BACKGROUND-003 | Integration / Presentation | JSON round-tripと初期復元 | Passed |
| AC-6 | BDD-BACKGROUND-004 | Unit / Presentation | Clear、既定値保存、Image破棄 | Passed |
| AC-7 | BDD-BACKGROUND-001〜005 | Presentation / Manual | 不透明前景面、Control accessibility、pixel描画: Passed; 実画面: Not Run | Passed with manual residual |

## Unknowns / Human Decisions

- Decision: 「表示位置」は中央配置を基準としたX/Y Pixel offset、「拡大縮小」はWindowへContainしたSizeに対する25〜300%として提供する。
- Decision: 元画像をCopyせずPathを保存するため、移動・削除時は既定背景へFallbackしてError表示する。
- Decision: 前景の視認性はD&D/QR/Listを不透明面に置き、ヘッダーと設定欄にも不透明な背景を設けることで保証する。
- Residual risk: すべての画像での主観的な文字視認性と高DPI表示は自動Pixel Testだけでは保証できないため、手動画面確認を残す。

## Implementation Notes

- `BackgroundCustomizationUseCase`が設定の正規化、画像選択、表示調整、Clearと保存Errorを調整する。
- `JsonBackgroundSettingsStore`がLocal App Dataのversioned JSONをatomicに保存し、欠損・破損設定を既定値へFallbackする。
- `SkiaBackgroundImageLoader`がJPEG、PNG、WebPをPNG byteへDecodeし、Alphaを維持したまま元File lockを解放する。
- `Form1`は背景をContain基準で描画し、元AlphaへApplication opacityを乗算する。前景Panelと設定欄は不透明背景を維持する。
- 背景設定欄の表示領域を確保するためDesignerの既存Control位置とForm最小Sizeだけを更新し、生成Codeへ処理ロジックは追加しない。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| `task verify` | Passed | Restore、Build、Unit 80 passed / 0 failed / 0 skipped |
| `task test:integration` | Passed | 10 passed / 0 failed / 0 skipped |
| `dotnet build TransferImageQR.sln --no-restore` | Passed | 0 warnings / 0 errors |
| `dotnet test tests/TransferImageQR.UnitTests/TransferImageQR.UnitTests.csproj --no-build` | Passed | 80 passed / 0 failed / 0 skipped |
| `dotnet test tests/TransferImageQR.IntegrationTests/TransferImageQR.IntegrationTests.csproj --no-build` | Passed | 10 passed / 0 failed / 0 skipped |
| Process runtime smoke | Passed | Main Window processが起動後2秒間継続し、終了時に停止 |
| WinForms自動描画確認 | Passed | Alpha合成Pixel、不透明なD&D/QR前景、Accessible NameをPresentation Testで確認 |
| WinForms実画面確認 | Not Run | Windows UI制御サービスが未構成のため自動目視確認不可 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #13、Canonical BDD、現行実装のGapを整理 |
| 2026-09-20 | Marked Verified | 実装、Unit／Integration／描画Test、runtime smokeのEvidenceを反映 |
