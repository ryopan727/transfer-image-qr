# LANインターフェース選択 Change Specification

この文書はIssueによる変更差分と検証Evidenceを記録する。変更後の外部仕様の正本は `specs/bdd.md` とし、本ファイルを現行仕様の正本として扱わない。

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0011` |
| Status | `Verified` |
| Source | [GitHub Issue #11](https://github.com/ryopan727/transfer-image-qr/issues/11) |
| Canonical BDD | [`specs/bdd.md`](bdd.md) |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

複数のNetwork Interfaceを持つWindows PCで、利用者がiPhoneから到達可能なLAN IPv4 Addressを確認・選択し、そのAddressを転送用QRへ確実に反映できるようにする。

## Background / Current State

- `SystemLanAddressProvider`はUp状態のPrivate IPv4から1件を自動選択し、候補一覧を公開しない。
- Physical LANを優先するが、VPN、Hyper-V、Docker等が存在する環境で自動選択が到達可能とは限らない。
- `TransferUrlProvider`は自動選択されたAddressをURLへ使用する。
- Main Viewには利用中Addressの選択UIがなく、利用者はQR作成前に確認・変更できない。
- Loopback、link-local、Public IPv4、IPv6は既存Filterで選択されない。

## Scope

- Up状態のPrivate IPv4候補の列挙
- Loopback、link-local、Public IPv4、IPv6、Down Interfaceの除外
- Interface名とIPv4 Addressを表示する選択UI
- QR作成前の候補選択
- 選択Addressの転送URL／QRへの反映
- アプリ実行中および「新しい転送」後の選択保持
- 候補なし、単一候補、複数候補のUnit／Presentation Test

## Out of Scope

- iPhoneからPCへの実到達性Probe
- Windows Firewallの自動設定または診断
- Network変更Eventの常時監視
- 選択内容のApplication再起動を跨ぐ永続化
- IPv6 URL
- VPN、Hyper-V、Docker Adapterの自動判別・自動除外

## Actors / External Systems

- Windows利用者: 候補を確認し、転送に使うAddressを選択する
- Windows network stack: Interface情報、Operational Status、Unicast Addressを提供する
- iPhone: 選択Addressを含むQR URLへ同一LANからAccessする

## Functional Requirements

- FR-1: Up状態のInterfaceからPrivate IPv4 Addressを重複なく候補として列挙する。
- FR-2: Loopback、link-local、Public IPv4、IPv6、Down Interfaceを候補から除外する。
- FR-3: 候補はPhysical LANを優先し、Interface index、Addressの順で決定的に表示する。
- FR-4: 候補には利用者が識別できるInterface名とIPv4 Addressを表示する。
- FR-5: 複数候補から利用者が1件を選択できる。初期値は候補順の先頭とする。
- FR-6: QR作成時は選択中Addressを転送URLへ使用する。
- FR-7: 選択はActive中に変更できず、「新しい転送」でDraftへ戻っても同一Process内で保持する。
- FR-8: 候補がない場合は選択UIを無効にし、既存の「LAN用IPv4アドレスを取得できません」案内を維持する。

## Non-functional Requirements / Constraints

- NFR-1: OS依存のInterface列挙はInfrastructureへ置き、PresenterとApplicationは`System.Net.NetworkInformation`へ依存しない。
- NFR-2: FormはControl値の受け渡しに限定し、選択可否と保持の判断はPresenterへ置く。
- NFR-3: 候補列挙中に個別Interfaceが利用不能になった場合、そのInterfaceだけを無視する。
- NFR-4: Address選択はQR payloadを変えるだけで、Kestrelの全Interface待受契約を変更しない。

## Interfaces / UI Contract

- Main Windowへ「転送に使うLANアドレス」のDrop-downを表示する。
- 各項目は`<Interface名> — <IPv4 Address>`として表示する。
- Drop-downはDraft中だけ操作可能とし、Active／Expired中は無効にする。
- 候補が0件なら「利用可能なLAN IPv4アドレスがありません」を表示して無効にする。
- 「新しい転送」でDraftへ戻ると、以前選択した候補を選択したまま再び操作可能にする。

## Acceptance Criteria

- [x] AC-1: 利用可能なLAN向けIPv4 Address候補を検出する。
- [x] AC-2: 明らかなLoopback等を転送URL候補から除外する。
- [x] AC-3: 複数候補がある場合、利用者が選択できる。
- [x] AC-4: 選択したAddressをQR用URLへ反映する。
- [x] AC-5: 選択内容を同一Process内の次の転送まで保持する。

## BDD Delta

| Change | Target Scenario | Before | After / Reason |
| --- | --- | --- | --- |
| Add | `BDD-NETWORK-001` | なし | 利用可能なPrivate IPv4候補を列挙し、明らかに利用不能なAddressを除外する |
| Add | `BDD-NETWORK-002` | なし | 複数候補から利用者が1件を選択する |
| Add | `BDD-NETWORK-003` | なし | 「新しい転送」後も同一Process内で選択を保持する |
| Change | `BDD-TRANSFER-001` | 自動選択されたLAN IPv4 AddressでQRを作成 | 利用者が選択したLAN IPv4 AddressでQRを作成 |
| Change | `BDD-TRANSFER-002` | LAN IPv4 Addressがない場合に案内 | 候補一覧を空で表示し、QRを表示せず案内する |

### Proposed Canonical Scenarios

```gherkin
Scenario: BDD-NETWORK-001 利用可能なLAN IPv4 Address候補を表示する
  Given Windows PCにUp状態の複数Network Interfaceがある
  When Main WindowでLAN Address候補を確認する
  Then Up状態のPrivate IPv4 Addressが重複なく表示される
  And Loopback、link-local、Public IPv4、IPv6、Down InterfaceのAddressは表示されない

Scenario: BDD-NETWORK-002 転送に使うAddressを選択する
  Given 利用可能なLAN IPv4 Address候補が複数表示されている
  When 利用者が1件のAddressを選択してQR作成を選択する
  Then 選択したAddressが転送URLとQR payloadのHostになる
  And Active中はAddressを変更できない

Scenario: BDD-NETWORK-003 次の転送でも選択を保持する
  Given 利用者がLAN IPv4 Addressを選択して転送を開始している
  When 新しい転送を選択してDraftへ戻る
  Then 同じAddressが選択されたまま表示される
  And 利用者は別の候補へ変更できる
```

## Error / Edge Cases

- 候補が0件の場合、選択UIを無効化し、QRを表示しない。
- 同じIPv4 Addressが複数回取得された場合、1件だけ表示する。
- 選択要求が現在の候補に存在しない場合、選択を変更しない。
- Active／Expired中の選択変更要求は無視する。
- Interfaceが列挙中に利用不能になった場合、そのInterfaceだけを除外する。

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | 選択View Model、Drop-down、Presenterの選択状態を追加 | Form eventはPresenterへ委譲する |
| Application | Address候補PortとURL／QR生成引数を候補選択対応へ変更 | OS固有型は`IPAddress`までに限定する |
| Infrastructure | System Interfaceから候補一覧を生成 | 実Network依存の列挙と純粋なFilter／sortを分離する |
| Domain | 変更なし | Session ruleへNetwork設定を持ち込まない |
| Tests / Harness | Infrastructure filter、URL、Presenter、Form testsを追加 | 実iPhone到達性は手動E2Eとして未実施予定 |

## Test Strategy / Traceability

| AC | BDD Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | BDD-NETWORK-001 | Unit / Presentation | 候補Filter・sort、Presenter候補表示、Form Drop-down | Passed |
| AC-2 | BDD-NETWORK-001 | Unit | Loopback、link-local、Public、IPv6、Down除外 | Passed |
| AC-3 | BDD-NETWORK-002 | Presentation | 選択Event、Active時無効化 | Passed |
| AC-4 | BDD-NETWORK-002, BDD-TRANSFER-001 | Unit / Presentation / Integration | URL Host、QR service引数、QR生成 | Passed |
| AC-5 | BDD-NETWORK-003 | Presentation | 新しい転送後の選択保持 | Passed |

## Unknowns / Human Decisions

- Decision: Issueの「適切に保持」は、MVP-011では同一Application Process内と解釈する。再起動を跨ぐ設定永続化は保存先・Lifecycleの指定がないため追加しない。
- Decision: 到達性はPCだけでは確定できないため、VPNやVirtual Adapterを一律除外せず、Private IPv4候補としてInterface名とともに提示して利用者が選択する。
- Residual risk: 選択したPrivate IPv4が実際にiPhoneから到達可能かは、Network構成とFirewallに依存する。

## Implementation Notes

- `ILanAddressProvider`は単一の推奨Addressではなく、Interface名付き候補一覧を返す。
- `SystemLanAddressProvider`はOS列挙と純粋なFilter／sortを分離し、重複Addressを除外する。
- Presenterが候補、現在選択、Draft／Activeに応じた操作可否を管理する。
- URL／QR生成は選択Addressを明示的な引数として受け取り、隠れた自動選択へ依存しない。
- Main WindowのDrop-downはAutomationId相当の`Name`とAccessible Nameを持つ。

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| 対象TestのRED確認 | Passed | 候補型、選択UI契約、Address引数が未実装のためcompile failure |
| `task verify` | Passed | Build: 0 warnings / 0 errors、Unit／Presentation: 76 passed |
| `task test:integration` | Passed | Integration: 7 passed |
| `dotnet format TransferImageQR.sln --verify-no-changes --no-restore` | Passed | Formatting差分なし |
| `git diff --check` | Passed | Whitespace errorなし（改行コード通知のみ） |
| WinForms executable smoke test | Passed | Process継続動作、Kestrel dynamic port 49694 listenを確認 |
| 実iPhoneからの到達確認 | Not Run | 実端末環境なし |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Issue #11、Canonical BDD、現行実装のGapを整理 |
| 2026-09-20 | Marked Verified | 候補検出、選択、QR反映、保持と全自動品質Gateを確認 |
