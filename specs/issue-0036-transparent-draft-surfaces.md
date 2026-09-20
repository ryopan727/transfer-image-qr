# Issue #36 D&D・Draft一覧の背景透過

Status: Implemented

GitHub Issue: https://github.com/ryopan727/transfer-image-qr/issues/36

## Goal

カスタム背景を設定したとき、D&D領域とDraft画像一覧の背後にも背景が見えるようにしつつ、案内文、Thumbnail、File名、選択状態、操作性を維持する。

## Current State

- Main WindowはAlpha付きPNGの背景表示、不透明度、拡大率、位置の調整に対応している。
- D&D領域とDraft画像一覧は不透明な背景色で描画され、カスタム背景を覆っている。
- QR領域はQRコードの読取性を守るため白色の不透明面になっている。

## Scope

- カスタム背景がある場合、D&D領域を半透明表示にする。
- Draft画像一覧へMain Window背景と同じ位置関係の合成画像を表示し、薄い明色Veilを重ねる。
- 背景設定の変更とWindow size変更にDraft一覧の合成表示を追従させる。
- 背景をクリアした場合はD&D領域とDraft一覧を既定の不透明表示へ戻す。
- QR領域は不透明な白色を維持する。

## Out of Scope

- WinForms標準ListViewそのものへの真のAlpha透過機能追加
- QR領域の透過
- 背景設定UIの別Window化（Issue #37）
- Domain、Application、Infrastructureの変更

## Functional Requirements

1. カスタム背景が表示されている間、D&D領域を通して背景が見えること。
2. カスタム背景が表示されている間、Draft画像一覧を通して背景が見えること。
3. D&D案内、対応形式、Thumbnail、File名、選択状態が読み取れること。
4. 背景の不透明度、拡大率、位置の変更後にD&D領域とDraft一覧へ最新表示を反映すること。
5. Window size変更後にDraft一覧の背景位置とSizeを再計算すること。
6. 背景Clear後はD&D領域とDraft一覧を既定の不透明表示へ戻すこと。
7. 背景表示の更新でDraft画像と編集状態を失わないこと。
8. QR領域は白色かつ不透明なままにすること。

## UI Contract

- D&D領域は明色の半透明面を使い、文字Contrastを維持する。
- ListViewはNative controlのAlpha透過へ依存せず、Main Window背景を同じ座標で切り出したBitmapと明色Veilを合成して表示する。
- カスタム背景がない場合は従来の既定色を使用する。

## Acceptance Criteria

1. 背景設定時、D&D領域とDraft一覧の両方で背景が視認できる。
2. D&D案内と対応形式が読み取れる。
3. DraftのThumbnail、File名、選択状態が読み取れ、選択と削除操作が継続できる。
4. 背景設定値の変更が透過面にも反映される。
5. Window resize後もDraft一覧の背景がListView全体を覆い、Main Windowとの位置関係が維持される。
6. 背景Clear後は既定の不透明面へ戻る。
7. QR領域は不透明な白色を維持する。
8. Unit / Presentation testsと既存Integration / Acceptance testsが成功する。

## BDD Delta

- `BDD-BACKGROUND-001`を、D&D領域とDraft一覧から背景が見え、前景Contentを読める要求へ更新する。
- `BDD-BACKGROUND-006`を追加し、背景Clear時に既定の不透明面へ戻りDraft状態を維持することを検証する。

## Test Design

- Presentation testで背景設定後のD&D surface、Draft List背景、QR surfaceを検証する。
- Presentation testで背景Clear後に既定表示へ戻り、既存Draft itemが維持されることを検証する。
- Presentation testでWindow resizeによりDraft List用Bitmapが再生成され、Client sizeへ一致することを検証する。
- 既存のDraft操作、背景永続化、Integration、Acceptance suitesを回帰実行する。

## Manual Visual Check

1. 明暗差とAlphaを含むPNGを背景へ設定し、D&D領域とDraft一覧の背後に画像が見えることを確認する。
2. JPEG、PNG、WebPをDropし、案内文、Thumbnail、File名、選択Highlightを読み取れることを確認する。
3. 個別削除、全Clear、QR作成を実行し、各状態の操作と表示が従来どおりであることを確認する。
4. Windowを拡大・縮小し、Draft一覧の背景に空白、ずれ、著しいちらつきがないことを確認する。
5. 背景をClearし、D&D領域とDraft一覧が既定色へ戻ることを確認する。
6. Active画面のQR領域が白色で、QR codeを読み取れることを実機で確認する。

## Design Decisions

- WinForms `ListView`は任意Alphaの背景色を安定して扱えないため、背景を切り出した合成Bitmapを使用する。
- 合成Bitmapには明色Veilを重ね、画像や文字の読取性を確保する。
- QR codeのContrastを守るためQR領域は透過しない。

## Verification Evidence

- Presentation RED: 新規3 testsが既存の不透明面とDraft一覧背景未設定により失敗することを確認。
- Presentation GREEN: 背景表示、Clear復帰、resize追従の3 testsが成功。
- `task verify`: Build warning 0 / error 0、Unit 106、Integration 17、Acceptance 2、全125 tests成功。
