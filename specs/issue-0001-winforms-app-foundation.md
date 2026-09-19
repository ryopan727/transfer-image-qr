# .NET 8 WinForms desktop application foundation Specification

## Metadata

| Item | Value |
| --- | --- |
| Spec ID | `issue-0001` |
| Status | `Verified` |
| Source | [GitHub Issue #1](https://github.com/ryopan727/transfer-image-qr/issues/1), plus the user's 2026-09-20 instruction allowing WinForms |
| Owner | Codex |
| Created | 2026-09-20 |
| Updated | 2026-09-20 |

## Goal

Provide a buildable and testable Windows desktop foundation on which the image-transfer MVP can be implemented issue by issue.

## Background / Current State

- Repository HEAD contains only the initial README commit.
- The working tree contains an untracked C# / .NET 8 WinForms template with one empty `Form1` and no tests or layer projects.
- Issue #1 originally names WPF. The user explicitly confirmed on 2026-09-20 that WinForms is acceptable, so this specification adopts WinForms while preserving the remaining acceptance outcomes.

## Scope

- C# / .NET 8 Windows Forms desktop executable
- Desktop, Application, Domain, and Infrastructure responsibilities represented by separate projects
- Unit test project and dependency-direction tests
- Reproducible restore, build, and test commands
- A minimal main window that can be launched on Windows

## Out of Scope

- Image drag and drop
- Image validation and draft editing
- QR code generation
- HTTP image delivery
- iPhone browser UI
- System tray, startup registration, and custom backgrounds

## Actors / External Systems

- Windows desktop user
- Windows Forms runtime

## Functional Requirements

- FR-1: Starting the desktop executable opens the application's main window.
- FR-2: The solution exposes separate Desktop, Application, Domain, Infrastructure, and Unit Test projects.
- FR-3: A developer can restore, build, and run unit tests from the command line.

## Non-functional Requirements / Constraints

- NFR-1: Target .NET 8 and Windows.
- NFR-2: Keep business and use-case code outside the Form.
- NFR-3: Dependencies point inward; Domain does not reference an outer product layer.
- NFR-4: Do not add implementation for later MVP issues.

## UI Contract

- The main window title is `TransferImageQR`.
- The window displays the product name and a short foundation-state message.

## Acceptance Criteria

- [x] AC-1: On Windows, starting `TransferImageQR.exe` opens a visible main window.
- [x] AC-2: `dotnet build TransferImageQR.sln` succeeds without warnings or errors.
- [x] AC-3: `dotnet test TransferImageQR.sln` discovers at least one test and succeeds.
- [x] AC-4: Project references preserve the documented inward dependency direction.

## BDD Scenarios

```gherkin
Scenario: SC-1 Launch the desktop foundation
  Given the solution has been built on Windows with .NET 8
  When the user starts TransferImageQR.exe
  Then a visible main window titled "TransferImageQR" is shown

Scenario: SC-2 Build the complete solution
  Given the .NET 8 SDK is installed
  When the developer builds TransferImageQR.sln
  Then all production and test projects compile without errors

Scenario: SC-3 Run the foundation test suite
  Given dependencies have been restored and the solution has been built
  When the developer tests TransferImageQR.sln
  Then at least one test is discovered
  And all discovered tests pass

Scenario: SC-4 Preserve layer dependency direction
  Given the four product layers are compiled
  When their assembly references are inspected
  Then Domain references no outer product layer
  And Application references no Desktop or Infrastructure layer
  And Infrastructure references no Desktop layer
```

## Error / Edge Cases

- An unsupported OS is outside the target platform and is not treated as a supported launch environment.
- Dependency restore failures must fail the quality gate rather than being ignored.

## Impact Analysis

| Area | Impact | Evidence / Notes |
| --- | --- | --- |
| Presentation | Replace the generic template form with a minimal named main form | Existing `Form1.*` template files |
| Application / Domain | Add empty foundation assemblies with public assembly markers only | No business requirements exist in MVP-001 |
| Infrastructure | Add a foundation assembly and inward project references | No adapters are implemented in MVP-001 |
| Tests / Harness | Add xUnit architecture tests and canonical commands | No existing test project or Taskfile |

## Test Strategy / Traceability

| AC | Scenario | Test Layer | Test / Evidence | Result |
| --- | --- | --- | --- | --- |
| AC-1 | SC-1 | Runtime evidence | Started the executable; Windows reported title `TransferImageQR`, a non-zero main window handle, and `Responding=True` | Passed |
| AC-2 | SC-2 | Build | `dotnet build TransferImageQR.sln --no-restore` | Passed: 0 warnings, 0 errors |
| AC-3 | SC-3 | Unit | `dotnet test TransferImageQR.sln --no-build` | Passed: 3 total, 3 passed, 0 failed, 0 skipped |
| AC-4 | SC-4 | Unit | `ArchitectureDependencyTests` | Passed: 3 architecture tests |

## Unknowns / Human Decisions

- Decision: Use C# / .NET 8 WinForms instead of the WPF technology named in Issue #1, based on the user's explicit 2026-09-20 instruction.
- No unresolved decision changes the acceptance result.

## Implementation Notes

- Kept the existing C# Windows Forms executable as the Presentation and composition-root project.
- Added separate Application, Domain, and Infrastructure class libraries with inward project references.
- Added xUnit.net v3-generation architecture tests without implementing later MVP behavior.
- Added Taskfile commands and documented equivalent `dotnet` commands because Task CLI is not installed on the verification machine.

## Verification Evidence

| Command / Check | Result | Notes |
| --- | --- | --- |
| `dotnet restore TransferImageQR.sln` | Passed | All five projects restored |
| `dotnet build TransferImageQR.sln --no-restore` | Passed | 0 warnings, 0 errors |
| `dotnet test TransferImageQR.sln --no-build` | Passed | 3 total, 3 passed, 0 failed, 0 skipped |
| Main form runtime check | Passed | `MainWindowTitle=TransferImageQR`, non-zero handle, `Responding=True`; native screenshot bridge was unavailable |
| `task verify` | Not Run | Task CLI is not installed; equivalent dotnet commands passed |

## Change History

| Date | Change | Reason |
| --- | --- | --- |
| 2026-09-20 | Initial Ready specification | Establish the issue contract and record the approved WinForms deviation |
| 2026-09-20 | Marked Verified and recorded evidence | All applicable acceptance criteria passed |
