# SPECIFICHE — Visualizzatore Mandelbrot (MandelC#)

## Descrizione

App WinForms (.NET 8) che renderizza l'insieme di Mandelbrot (`z = z² + c`)
con smooth coloring. Interattiva: click sinistro = zoom avanti 2x sul punto,
click destro = zoom indietro 2x, rotella = zoom sul cursore,
trascinamento (tenendo premuto) = pan della vista, `R` = reset, `S` = salva PNG,
`+`/`-` = iterazioni. Dropdown palette: Fuoco, Ghiaccio, Termico. Radio
Checkbox Auto: in Auto le iterazioni crescono col logaritmo dell'ingrandimento
(`200 + 790·log10(zoom)`, ~2000 a scala 1,95e-4, ~4550 a scala 1e-5) e il numero
mostra comunque il valore usato pur restando disabilitato. Dropdown AA
1x/2x/4x/8x (1x = off): supersampling a risoluzione k volte maggiore e media RGB
di ogni blocco kxk (su CPU e GPU, costo ~k²). Radio motore di rendering: CPU sempre
disponibile, CUDA se c'è una GPU NVIDIA (float sopra scala 1e-3, double sotto,
altrimenti fallback CPU); DirectX realtime (v2.0, shader HLSL, float, loop
~60 fps, pan/zoom immediati, fallback CPU in caso di errore). Dropdown GPU
(v2.1): con più schede video si sceglie quale usare — "Auto" = la più potente,
altrimenti la scheda nominata; unisce le schede DirectX (DXGI) e i device
CUDA (ILGPU); la scelta è persistita e applicata al cambio di motore; il
dropdown è disabilitato quando il motore attivo è la CPU. Menu File: carica zona
(Ctrl+O) e salva zona (Ctrl+S) in formato JSON (centro, scala, iterazioni),
salva immagine con nome (Ctrl+Shift+S), benchmark standard 8s (Ctrl+B) con
pixel/s in grande, Esci (Alt+F4).
Menu Aiuto: finestra Informazioni con versione e riepilogo comandi. Barra di
stato in basso con centro, larghezza, iterazioni e palette.
Rendering parallelo (`Parallel.For` + `LockBits`), asincrono con cancellazione,
anteprima in pan (1/4 risoluzione, no AA, upscale bilineare, full al rilascio),
throttle del pan (80 ms) e anti-rimbalzo sul resize.

## File

- `MandelbrotViewer/Mandelbrot.cs` — calcolo + enum `Palette` (Fuoco, Ghiaccio,
  Termico) con gradienti e smooth coloring.
- `MandelbrotViewer/MandelbrotForm.cs` / `MandelbrotForm.Designer.cs` — UI: zoom/pan,
  menu File (zone, PNG, benchmark, Esci) e Aiuto, dropdown palette e AA,
  checkbox iterazioni auto, radio motore, dropdown GPU (scelta scheda video),
  `TableLayoutPanel`, `StatusStrip`.
- `MandelbrotViewer/BenchmarkForm.cs` / `.Designer.cs` — benchmark standard
  (zona fissa 800x600 AA 8x, 5000 iterazioni, durata minima 8 s) con risultato
  pixel/s in grande, UI aggiornata ogni 3 s, progresso e annullamento.
- `MandelbrotViewer/Program.cs` — entry point WinForms standard.
- `MandelbrotViewer/AppVersion.cs` — versione X.Y.Z (`Display` = X.Y se Z=0).
- `MandelbrotViewer/RenderEngine.cs` — enum `RenderEngine` (Cpu/Cuda/DirectX) +
  disponibilità (CUDA solo se `GpuMandelbrot.IsReady`).
- `MandelbrotViewer/Settings.cs` — `AppSettings` in
  `%APPDATA%\MandelbrotViewer\settings.json`: iterazioni, palette, AA, motore,
  GPU scelta e finestra; la vista NON viene memorizzata (all'avvio si parte
  sempre dall'insieme completo); load tollerante, save validato.
- `MandelbrotViewer/DxMandelbrot.cs` — backend DirectX 11 realtime via
  Vortice 3.8.3: pixel shader HLSL (float) che calcola il frattale a ogni
  frame, swapchain legata al pannello, AA come supersampling in-shader,
  cattura backbuffer per Salva PNG, `LastError` diagnostico. `AdapterNames()`
  elenca le schede hardware (escluso WARP); `TryInitialize(..., adapterName)`
  crea il device sull'adapter scelto (auto = più memoria dedicata).
- `MandelbrotViewer/GpuMandelbrot.cs` — backend CUDA via ILGPU 1.5.3: kernel
  float/double (un thread per pixel, solo fuga), soglia double a scala < 1e-3,
  fallback CPU, `LastError` diagnostico. `DeviceNames()` elenca i device CUDA;
  `TryInitialize(deviceName)` usa la scheda scelta (auto = la più capiente);
  `ResetAccelerator` riusa il contesto CUDA al cambio di scheda.
- `avvia.bat` / `avvia.ps1` — lancio: usa `pubblicato\` se presente, altrimenti
  `bin\Debug`, altrimenti `dotnet run`.
- `pubblicato/` — build self-contained single-file (~150 MB), rigenerabile,
  esclusa da git.

## Changelog

Vedi [CHANGELOG.md](CHANGELOG.md).

## Note tecniche

- `dotnet` solo in `~\.dotnet`, non nel PATH: usare percorso completo.
- `pubblicato/`, `bin/`, `obj/` esclusi da git (rigenerabili).
- Il backend CUDA richiede GPU NVIDIA + driver sulla macchina target (il toolkit
  CUDA non serve a runtime); senza GPU l'app usa la CPU in automatico.
- Il backend DirectX è in float: oltre la scala ~1e-3 perde precisione (lo
  stato lo segnala con "[oltre float!]"); per zoom profondi usare CUDA (double)
  o CPU.
