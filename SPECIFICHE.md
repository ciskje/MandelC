# SPECIFICHE — Visualizzatore Mandelbrot (MandelC#)

## Descrizione

App WinForms (.NET 8) che renderizza l'insieme di Mandelbrot (`z = z² + c`)
con smooth coloring. Interattiva: trascinamento = zoom su rettangolo, rotella =
zoom sul cursore, tasto destro = allontana 2x, `R` = reset, `S` = salva PNG,
`+`/`-` = iterazioni. Rendering parallelo (`Parallel.For` + `LockBits`),
asincrono con cancellazione, anti-rimbalzo sul resize.

## File

- `MandelbrotViewer/Mandelbrot.cs` — calcolo + palette HSV con smooth coloring.
- `MandelbrotViewer/Form1.cs` / `Form1.Designer.cs` — UI, zoom, salvataggio PNG.
- `MandelbrotViewer/Program.cs` — entry point WinForms standard.
- `MandelbrotViewer/AppVersion.cs` — versione X.Y.Z (`Display` = X.Y se Z=0).
- `avvia.bat` / `avvia.ps1` — lancio: usa `pubblicato\` se presente, altrimenti
  `bin\Debug`, altrimenti `dotnet run`.
- `pubblicato/` — build self-contained single-file (~150 MB), rigenerabile,
  esclusa da git.

## Changelog (versione nel titolo finestra)

- **v1.0** — Primo visualizzatore: rendering parallelo, zoom mouse/rotella,
  iterazioni configurabili, salvataggio PNG. File: `Form1.*`, `Mandelbrot.cs`.
- **v1.1** — Script di lancio (`avvia.bat`/`avvia.ps1` con fallback dotnet
  user-level); publish self-contained in `pubblicato/` così non serve installare
  il Desktop Runtime; versione in sorgente (`AppVersion.cs` + `<Version>` nel
  csproj, mostrata nel titolo); setup progetto (AGENTS.md, TODO.md,
  SPECIFICHE.md, `.gitignore`, tracking nel repo `test/`).

## Note tecniche

- `dotnet` solo in `~\.dotnet`, non nel PATH: usare percorso completo.
- `pubblicato/`, `bin/`, `obj/` esclusi da git (rigenerabili).
