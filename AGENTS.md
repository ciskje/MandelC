# AGENTS.md — Istruzioni di progetto (MandelC#)

File letto automaticamente a ogni nuova sessione. Descrive toolchain, convenzioni
e workflow obbligatorio per questo progetto.

## Progetto

Visualizzatore interattivo dell'insieme di Mandelbrot in C# / WinForms (.NET 8).
Root del progetto: `MandelC#/`. Codice: `MandelC#/MandelbrotViewer/`.

## Toolchain .NET

- `dotnet` NON è nel PATH. SDK 8.0 installato user-level in `~\.dotnet\dotnet.exe`.
- In PowerShell usare sempre il percorso completo:
  `& "$env:USERPROFILE\.dotnet\dotnet.exe" <comando>`
- Target: `net8.0-windows` + `UseWindowsForms`. Solo Windows.
- Il percorso contiene `#`: quotare sempre i path negli script/comandi.

## Comandi

```powershell
# Build
& "$env:USERPROFILE\.dotnet\dotnet.exe" build "MandelbrotViewer\MandelbrotViewer.csproj"
# Avvio in debug
& "$env:USERPROFILE\.dotnet\dotnet.exe" run --project "MandelbrotViewer"
# Publish self-contained (rigenera pubblicato\, ~150 MB, non richiede runtime installato)
& "$env:USERPROFILE\.dotnet\dotnet.exe" publish "MandelbrotViewer\MandelbrotViewer.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "pubblicato"
```

- Lancio per l'utente finale: eseguire il file pubblicato in `pubblicato\` se presente;
  per lo sviluppo usare il comando `dotnet run` indicato sopra.

## Versionamento (obbligatorio)

- La versione vive nel sorgente: `MandelbrotViewer/AppVersion.cs` (costanti
  `Major`, `Minor`, `Patch`) + `<Version>` nel `.csproj`. I due devono coincidere.
- Formato `X.Y.Z`. Nella UI si mostra `AppVersion.Display`: se `Z` è 0 si usa
  la notazione breve `X.Y`.
- Regole di bump a seconda dell'importanza della modifica:
  - `Z` (patch): fix, refactor, docs, modifiche minori.
  - `Y` (minor): nuove funzionalità compatibili.
  - `X` (major): breaking change / riscritture.
- Ad ogni bump: aggiornare `AppVersion.cs` + `.csproj` e aggiungere voce in
  `CHANGELOG.md`.

## Workflow per ogni richiesta utente (obbligatorio)

1. Aggiungere la richiesta in cima come voce `pending` in `TODO.md`.
2. Eseguirla; segnare `completed` (o `cancelled`) appena finita, senza batch.
3. Aggiungere una nota in `SPECIFICHE.md` (cosa fatto, file toccati, versione se bumpata).
4. Verificare con build (`dotnet build`) quando si tocca codice C#.

## Git

- Il progetto vive nel repo `test/` (root sopra `MandelC#/`). Niente repo annidato.
- Scope commit: solo file di `MandelC#/`. Mai committare segreti.
- Ignorati via `MandelC#/.gitignore`: `bin/`, `obj/`, `pubblicato/` (rigenerabili).
- Messaggi concisi in italiano che partono col numero di versione
  (es. `v2.0.1: DirectX realtime, benchmark, ...`).
- Commit/push solo su richiesta esplicita dell'utente.
