# 5分間有効な転送セッション管理 Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0005` |
| Status | `Verified` |
| Source | [GitHub Issue #5](https://github.com/ryopan727/transfer-image-qr/issues/5) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

利用者がDraftを確定して、推測困難なトークンで識別される5分間限定の転送セッションを開始し、期限切れ後または任意のタイミングで新しい転送を準備できるようにする。

## Background / Current State

- MVP-004までにDraftへの画像追加、個別削除、全クリア、QR作成ボタンの有効状態が実装されている。
- PresenterとViewには編集不可状態を反映する契約があるが、Active状態を発生させるUse Caseはない。
- セッション、トークン、時計、期限判定は未実装である。

## Scope

- Draft → Active → Expiredの状態遷移
- QR作成操作によるDraft内容の確定とセッション生成
- 暗号学的に安全なセッショントークン生成
- 作成から5分の有効期限と境界判定
- Active後のDraft追加・削除・全クリア禁止
- 現在の転送状態表示と新しい転送の開始
- Domain / Application / Infrastructure / Presentationテスト

## Out of Scope

- LAN内URL生成、HTTPサーバー、トークンによるHTTP認可（MVP-006）
- QRコード画像の生成・表示（MVP-007）
- 複数セッションの同時保持、永続化、アプリ再起動後の復元

## Actors / External Systems

- Windowsデスクトップアプリ利用者
- OSの暗号学的乱数生成器
- システム時計

## Functional Requirements

- FR-1: 1枚以上のDraftで「QR作成」を実行すると、その時点の画像を不変なスナップショットとして持つActiveセッションを生成する。
- FR-2: 空のDraftからセッションを生成できない。
- FR-3: セッションごとに32byteの暗号学的乱数をBase64URL化したトークンを生成する。
- FR-4: Active後は元のDraftへ画像を追加・個別削除・全クリアできない。
- FR-5: セッションは作成時刻から5分未満ではActive、5分ちょうど以降はExpiredである。
- FR-6: 新しい転送を開始すると現在セッションを破棄し、空で編集可能なDraftへ戻る。
- FR-7: PC画面へDraft / Active / Expiredと有効期限を表示する。

## Non-functional Requirements / Constraints

- NFR-1: 時刻は注入可能にし、Unit Testで実時間待機を行わない。
- NFR-2: トークン生成に非暗号学的乱数や連番を使用しない。
- NFR-3: DomainはWinForms、Infrastructure、システム時計へ依存しない。
- NFR-4: セッションはメモリ上だけで管理する。

## Interfaces / Data / UI Contract

### Session

- State: `Active` / `Expired`（セッション未生成時の画面状態は`Draft`）
- CreatedAt / ExpiresAt: UTC基準の`DateTimeOffset`
- Lifetime: 5分
- Token: 32byte乱数のpaddingなしBase64URL（43文字）
- Images: 確定時のコピーであり、Active後に変更されない

### UI

- Draftが1枚以上のとき「QR作成」でActiveへ遷移する。
- Active / Expired中はD&D、個別削除、全クリア、QR作成を無効にする。
- Activeでは有効期限を表示し、Expiredでは期限切れを表示する。
- 「新しい転送」で空のDraftへ戻る。

## Acceptance Criteria

- [x] AC-1: 「QR作成」でDraft内容が確定する。
- [x] AC-2: 推測困難なセッショントークンを生成する。
- [x] AC-3: Active後は画像一覧を変更できない。
- [x] AC-4: セッション有効期限は作成から5分である。
- [x] AC-5: 5分経過するとExpiredになる。
- [x] AC-6: 新しい転送を開始できる。

## BDD Scenarios

```gherkin
Scenario: SC-1 DraftをActiveセッションへ確定する
  Given Draftに画像が2枚ある
  When QR作成を実行する
  Then 2枚の不変なスナップショットを持つActiveセッションが生成される
  And Draftへの追加・削除・全クリアは拒否される

Scenario: SC-2 セッショントークンを生成する
  Given Draftに画像がある
  When 複数回の新しい転送セッションを生成する
  Then 各セッションは32byte以上の乱数強度を持つURL安全な異なるトークンで識別される

Scenario: SC-3 5分境界で期限切れになる
  Given 12:00:00にセッションを生成した
  When 現在時刻が12:04:59.999である
  Then 状態はActiveである
  When 現在時刻が12:05:00である
  Then 状態はExpiredである

Scenario: SC-4 新しい転送を開始する
  Given ActiveまたはExpiredセッションがある
  When 新しい転送を開始する
  Then 現在セッションは破棄される
  And 空で編集可能なDraft状態へ戻る
```

## Error / Edge Cases

- 空DraftからのQR作成要求は失敗結果を返し、状態を変更しない。
- トークン生成に失敗した場合はDraftを確定しない。
- 同一セッションの期限判定は時刻が戻っても作成済みの`ExpiresAt`を変更しない。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | QR作成・新しい転送イベント、状態/期限表示、定期的な期限表示更新 | FormはPresenterへ委譲 |
| Application / Domain | Session Entity、Draft確定/Reset、ライフサイクルUse Case、時計/Token Port | 状態遷移をUnit Test |
| Infrastructure | 暗号学的トークン生成Adapter | `RandomNumberGenerator`を使用 |
| Tests / Harness | 境界時刻、スナップショット、編集禁止、再開始、Token形式 | 実時間待機なし |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-1 | Domain + Application | `Create_CopiesDraftImagesAndSetsFiveMinuteExpiration`、`Create_WithDraftImages_ConfirmsDraftAndCreatesActiveSession` | Passed |
| AC-2 | SC-2 | Infrastructure | `Generate_ReturnsUnique256BitBase64UrlTokens` | Passed |
| AC-3 | SC-1 | Domain + Application + Presentation | Confirm後のAdd/Remove/Clear拒否、編集中の確定防止、UI無効化 | Passed |
| AC-4, AC-5 | SC-3 | Domain + Application | 5分直前/ちょうどと注入時計による状態取得 | Passed |
| AC-6 | SC-4 | Application + Presentation | Reset後の空Draft・編集再開・ボタンイベント | Passed |

## Unknowns / Human Decisions

- Decision: トークンは256bitの乱数をpaddingなしBase64URLで表す。URLへ安全に埋め込め、十分な推測耐性を持つため。
- Decision: `CreatedAt + 5分`ちょうどをExpiredとする。
- Decision: 「新しい転送」は現在セッションを破棄し、空のDraftへ戻す。複数同時セッションはIssue範囲外とする。
- 未解決の重要事項はなし。

## Implementation Notes

- Domainの`TransferSession`が画像スナップショット、作成時刻、有効期限、状態判定を保持する。
- `TransferDraft`はConfirm後の追加・削除・全消去を自身でも拒否し、Resetで空の編集可能状態へ戻る。
- Applicationの`TransferSessionUseCase`が共有Draft、Token Port、`TimeProvider`を使って現在セッションをメモリ上で管理する。
- Infrastructureの`CryptographicSessionTokenGenerator`が`RandomNumberGenerator`で256bitトークンを生成する。
- PresenterがQR作成、新しい転送、期限状態更新を調停し、画像追加中のセッション確定競合も拒否する。
- Formは1秒間隔のUI Timerで状態を更新し、Expired到達後にTimerを停止する。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象テストのRED確認 | Passed | 未実装のSession型、Token Port、Use Caseにより意図どおりコンパイル失敗 |
| `task verify` | Passed | 57 total, 57 passed, 0 failed, 0 skipped |
| Build | Passed | 0 warnings, 0 errors |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | 書式差分なし |
| `git diff --check` | Passed | whitespace errorなし |
| WinForms起動確認 | Passed | Hidden起動で`HasExited=False`、`Responding=True` |
| UI E2E | Not Run | UI Automation未導入。STA Form / Presenterテストでイベント配線と状態表示を検証 |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #5とMVP-004実装から検証契約を確定 |
| 2026-09-20 | Marked Verified and recorded evidence | 全Acceptance Criteriaの自動検証と品質Gateが完了 |
