# Issue #37 背景設定専用Form

Status: Implemented

GitHub Issue: https://github.com/ryopan727/transfer-image-qr/issues/37

## Goal

Main Windowから背景設定Controlを取り除き、`設定` → `背景設定`から開く専用Formへ既存の背景操作を移す。設定変更はMain Windowへ即時反映し、既存の永続化とDraft／Session状態を維持する。

## Current State

- Main Window内のGroupBoxに背景選択、Clear、不透明度、Size、X／Y位置のControlがある。
- `MainPresenter`が既存`IBackgroundCustomizationUseCase`を呼び、Main Windowへ背景とErrorを反映する。
- 保存済み設定はApplication起動時に読み込まれる。

## Scope

- Main Windowへ`設定` Menuと`背景設定` Menu itemを追加する。
- 背景設定専用Formへ既存の選択、Clear、不透明度、Size、X／Y位置を移す。
- 専用Formに現在の背景Previewと現在値を表示する。
- 専用Formの操作を既存Use Case経由で保存し、Main Windowへ即時反映する。
- 既にFormが開いている場合は新規作成せず、既存Formを前面へ戻す。
- Main Windowから背景設定GroupBoxを削除し、空いた領域をDraft表示へ割り当てる。

## Out of Scope

- 背景表示や永続化方式の再実装
- Tray設定、自動起動設定の移動
- Theme、Animation、汎用設定Framework
- Domain、Infrastructureの変更

## Functional Requirements

1. Main WindowにAccessibleな`設定` Menuと`背景設定` itemを表示すること。
2. `背景設定` itemから専用FormをModelessで開けること。
3. 開いた時点で現在の背景Preview、不透明度、Size、X／Y位置、Clear可否を表示すること。
4. 専用Formから画像選択、Appearance変更、Clearを実行できること。
5. すべての変更を既存`IBackgroundCustomizationUseCase`で検証・保存すること。
6. 変更結果をMain Window背景と専用Formへ同じPresenterから反映すること。
7. 専用Formを重複表示しないこと。
8. 専用Formを閉じてもDraft／Active／Expired状態を変更しないこと。
9. 保存済み設定の起動時復元を維持すること。

## Architecture

- `MainForm`のMenu eventはFormの作成、既存Formの再表示、前面化だけを担当する。
- `BackgroundSettingsForm`は`IBackgroundSettingsView`を実装し、UI eventを`IBackgroundSettingsPresenter`へ通知する。
- `MainPresenter`は既存Use CaseをSource of Truthとして、Main Windowと接続中の設定Formへ同じ`BackgroundViewModel`を配信する。
- FormはFile dialog以外のFile／設定Storeへ直接Accessしない。

## Acceptance Criteria

1. `設定` → `背景設定`で専用Formが開く。
2. Menu itemと主要Controlに安定した`Name`とAccessible Nameがある。
3. 現在の背景Previewと各Adjustment値が表示される。
4. 画像選択、Opacity、Size、X／Y位置、Clearが既存Use Case経由で動作する。
5. 変更がMain Windowへ即時反映され、再起動後も復元される。
6. Menuを繰り返し選んでも専用Formは1つだけ表示される。
7. 専用Formを閉じてもMain WindowのDraft／Session状態を失わない。
8. Presenter／Form tests、既存Integration／Acceptance testsが成功する。

## BDD Delta

- `BDD-BACKGROUND-002`と`BDD-BACKGROUND-004`は専用Form上で操作することを明記する。
- `BDD-BACKGROUND-007`を追加し、Menu遷移、現在値表示、重複防止、Main状態維持を検証する。

## Test Design

- Presenter testで設定ViewのAttach時に現在値を反映し、変更結果をMain／設定の両Viewへ配信することを検証する。
- BackgroundSettingsForm testでControl contract、現在値、Select／Appearance／Clear eventを検証する。
- Main Form testでMenu contract、専用FormのOpen、重複防止、Close後のDraft状態維持を検証する。
- 既存の背景永続化、Draft、Session、Integration、Acceptance suitesを回帰実行する。

## Manual Screen Transition Check

1. Main Windowで`設定` → `背景設定`を選び、専用Formが1つ開くことを確認する。
2. 同じMenuを再度選び、新しいFormが増えず既存Formが前面へ移ることを確認する。
3. 背景を選択し、Opacity、Size、X／Y位置を変更してMain Windowへ即時反映されることを確認する。
4. 背景をClearし、Main Windowが既定背景へ戻ることを確認する。
5. Draft画像を保持した状態とActive状態で専用Formを開閉し、それぞれの状態が維持されることを確認する。
6. Applicationを再起動し、最後に保存した背景設定が復元されることを確認する。

## Verification Evidence

- RED: 専用FormとPresenter／View契約が存在しないため、新規testsがCompile errorになることを確認。
- GREEN: Menu遷移・重複防止、専用Form操作、Presenterの二重View反映の3 testsが成功。
- `task verify`: Build warning 0 / error 0、Unit 107、Integration 17、Acceptance 2、全126 tests成功。
