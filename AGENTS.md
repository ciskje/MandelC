# AGENTS.md — Project instructions (MandelC#)

File automatically read at each new session. Describes toolchain, conventions
and mandatory workflow for this project.

## Project

Interactive Mandelbrot set viewer in C# / WinForms (.NET 8).
Project root: `MandelC#/`. Code: `MandelC#/MandelbrotViewer/`.

## Language (mandatory)

All code (comments, identifiers, enum names), UI strings, documentation
(AGENTS.md, TODO.md, CHANGELOG.md, SPECS.md), and commit messages
must be written in **English** from now on.

## Toolchain .NET

- `dotnet` resolves via PATH (user-level install under `~\.dotnet`, SDK 8.0).
- If PATH ever loses it, use the full path (always quote it, the path contains `#`).
  `& "$env:USERPROFILE\.dotnet\dotnet.exe" <command>`
- Target: `net8.0-windows` + `UseWindowsForms`. Windows only.

## Commands

```powershell
# Build
dotnet build "MandelbrotViewer\MandelbrotViewer.csproj"
# Debug run
dotnet run --project "MandelbrotViewer"
# Publish self-contained (regenerates published\, ~150 MB, no installed runtime required)
dotnet publish "MandelbrotViewer\MandelbrotViewer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "published"
```

- End-user launch: run the published file in `published\` if present;
  for development use the `dotnet run` command indicated above.

## Versioning (mandatory)

- The version lives in the source: `MandelbrotViewer/AppVersion.cs` (constants
  `Major`, `Minor`, `Patch`) + `<Version>` in the `.csproj`. The two must match.
- Format `X.Y.Z`. The UI shows `AppVersion.Display`: if `Z` is 0 use the
  short notation `X.Y`.
- Bump rules based on the importance of the change:
  - `Z` (patch): fixes, refactors, docs, minor changes.
  - `Y` (minor): new backward-compatible features.
  - `X` (major): breaking change / rewrites.
- Exception: `.md`/docs-only changes (README, TODO, SPECS, CHANGELOG,
  QUESTIONS) do NOT bump the version; they ride along with the next
  code version.
- On each bump: update `AppVersion.cs` + `.csproj` and add an entry in
  `CHANGELOG.md`.

## Workflow for each user request (mandatory)

1. Add the request at the top as a `pending` entry in `TODO.md`.
2. Execute it; mark `completed` (or `cancelled`) as soon as done, no batching.
3. Add a note in `SPECS.md` (what was done, files touched, version if bumped).
4. Verify with build (`dotnet build`) when C# code is touched.

## Git

- Commit scope: only files in `MandelC#/`. Never commit secrets.
- Ignored via `MandelC#/.gitignore`: `bin/`, `obj/`, `published/` (regenerable).
- Concise commit messages in English starting with the version number
  (e.g. `v2.0.1: DirectX realtime, benchmark, ...`).
- Commit/push only on explicit user request.
- On every push: publish the self-contained exe, attach it to the GitHub
  Release of the current version, and update the README download section
  (version, asset name, size) plus the SPECS.md release line.
