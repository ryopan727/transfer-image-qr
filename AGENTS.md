# TransferImageQR development guide

This repository uses the workflow and quality gates in `matsupy-steering-dotnet/`.

## Technology

- C# / .NET 8
- Windows Forms desktop UI
- ASP.NET Core / Kestrel for LAN image delivery
- xUnit.net for automated tests

The upstream steering text refers to VB.NET in some places. For this repository, apply its architecture, workflow, testing, and quality principles while using C# syntax and project settings.

## Architecture

- `TransferImageQR`: Windows Forms presentation and composition root
- `TransferImageQR.Application`: use cases and ports
- `TransferImageQR.Domain`: entities and business rules
- `TransferImageQR.Infrastructure`: file, network, HTTP, and operating-system adapters
- `tests/TransferImageQR.UnitTests`: fast unit and architecture tests
- `tests/TransferImageQR.IntegrationTests`: real Kestrel and file-delivery integration tests

Dependencies point inward: Desktop -> Application/Infrastructure, Infrastructure -> Application/Domain, Application -> Domain, and Domain -> no product project.

## Commands

- `task restore`: restore dependencies
- `task build`: build production and test code
- `task test` / `task test:unit`: run the fast unit suite
- `task test:integration`: run the real Kestrel integration suite
- `task test:all`: run unit, presentation, and integration suites
- `task verify`: restore, build, and test

If Task CLI is unavailable, run the equivalent `dotnet restore`, `dotnet build --no-restore`, and `dotnet test --no-build` commands shown in `Taskfile.yml`.

## Issue workflow

Use one issue branch per logical change, create or update the matching file under `specs/` before production code, and report build/test evidence. Do not mix unrelated refactors into an issue.
