# SPECIFICHE — Visualizzatore Mandelbrot (MandelC#)

App WinForms (.NET 8, `net8.0-windows`) che renderizza l'insieme di Mandelbrot
(`z = z² + c`) con smooth coloring coerente su tutti i motori: la tinta dipende
dalle iterazioni di fuga smussate col modulo finale di `z`, mappate su 5 stop
per palette con curva `t × 1,35 + 0,03` (saturata a 1, interno nero). Palette:
Fuoco, Ghiaccio, Termico, Oceano, Viola, Deserto, Foresta (stop in
`PaletteColors`, fonte unica per CPU e DirectX; CUDA li riceve come
`GpuPaletteParams`).

## Interazione

- Click sinistro/destro: zoom 2× sul punto; rotella: zoom 0,7×/1,43× sul
  cursore; trascinamento o frecce: pan (soglia click/trascinamento 5 px,
  throttle 80 ms, anteprima a metà risoluzione senza AA con upscale bilineare,
  full al rilascio; anti-rimbalzo sul resize di 300 ms; frecce = 1/10 della
  vista, Shift = 1/100, non attive sul numero iterazioni).
- `R` = reset vista, `S` = salva PNG, `+`/`-` = ±50 iterazioni (50…50000).
- Checkbox Auto: iterazioni `200 + 790·log10(zoom)` (clamp 50…50000, ~2000 a
  scala 1,95e-4); il numero mostra il valore usato pur restando disabilitato.
- Dropdown AA 1x/2x/4x/8x (1x = off): supersampling k×k e media RGB (costo ~k²)
  su CPU e CUDA; in DirectX dentro lo shader.
- Menu File: carica/salva zona JSON (Ctrl+O / Ctrl+S: centro, scala,
  iterazioni), salva immagine (Ctrl+Shift+S), export PNG ad alta risoluzione
  (Ctrl+Shift+E: preset Vista/Full HD/2K/4K/8K/Doppio 4K/Personalizzata con
  dimensioni validate 320…16384, AA selezionabile, render offscreen col motore
  attivo, AA auto-ridotto oltre 128 MPixel di campioni), video zoom MP4 (Ctrl+Shift+V:
  dalla vista corrente all'insieme (errore se si è già all'insieme; centro
  proporzionale allo zoom così il punto di partenza resta inquadrato;
  transizione ease-out: veloce all'inizio, lento alla fine), 60…480 frame
  a 24/30/60 fps, AA selezionato (auto-ridotto oltre 128 MPixel di campioni),
  iter auto per frame, ffmpeg H.264 (su worker, stderr asincrono, kill su
  annulla) o sequenza PNG se assente (tasto Apri per il risultato)),
  benchmark (Ctrl+B), Esci (Alt+F4).
- Menu Vista: cronologia Indietro/Avanti (Alt+Left/Right, max 200 viste: ogni
  zoom, pan, reset e caricamento è committed) e zone preferite nominate
  (Ctrl+D, JSON in `%APPDATA%\MandelbrotViewer\zone\`, con salto ed
  eliminazione dal sottomenu); modalità Julia (Ctrl+J, `z = z² + c` con c
  fissata, default −0,7 + 0,27015i persistita: click = fissa c, R resetta anche
  c; zone includono modo e c).
- Menu Aiuto: Informazioni (versione, comandi) e log/diagnostica (stato motori,
  schede, errori con step esatto, impostazioni).
- Barra di stato: centro, larghezza, iterazioni, palette, motore (+AA/anteprima);
  guida comandi fissa. ToolTip su controlli e voci di menu.
- Cursore AppStarting (freccia+clessidra) durante i render async — l'UI resta
  interattiva — assegnato in ricorsione a tutti i controlli; Wait solo per
  l'init CUDA sincrona a UI congelata.
- Finestra 1152×720 (posizione/dimensione persistite), versione nel titolo.

## Motori

- CPU (sempre disponibile): double, `Parallel.For` + `LockBits`, async con
  cancellazione.
- CUDA (GPU NVIDIA, altrimenti fallback CPU): kernel float 32-bit o double
  64-bit a scelta (radio 32/64, default 64); colorazione e downsampling AA
  calcolati sulla GPU (alla CPU arriva solo il bitmap finale); buffer device e
  host riusati tra frame; render serializzati (`RenderGate`). CPU sempre double,
  DirectX sempre float.
- DirectX 11 (Vortice, float, realtime ~60 fps con timer da 16 ms, pan/zoom
  immediati, fallback CPU in caso di errore): triangolo fullscreen + pixel
  shader; Salva PNG dal backbuffer. Oltre scala ~1e-3 il float non basta (stato
  "[oltre float!]"): usare CUDA double o CPU.
- Dropdown GPU (visibile solo con motori GPU): unisce schede DXGI e device CUDA;
  "Auto" = device CUDA più capiente / adapter DirectX predefinito; scelta
  persistita e applicata al cambio motore. Motore non disponibile → motivo
  mostrato in barra di stato.

## Impostazioni

`%APPDATA%\MandelbrotViewer\settings.json`: iterazioni (auto/manuale + valore),
palette, AA, motore, GPU, precisione CUDA, finestra. La vista NON è memorizzata
(si parte sempre dall'insieme completo). Load tollerante, save validato
(iterazioni clampate 50…50000).

## Benchmark

Test standardizzato identico per i motori (`BenchmarkStandard`): zona fissa
960×540, AA 8× inteso come griglia di campioni elementari senza media
(7680×4320 = 33,18 MPixel per frame), 5000 iterazioni, solo conteggio delle
iterazioni di fuga (niente colorazione né downsampling), budget 8 s, metrica
frame × campioni/frame / secondi.
- CPU: accumula le iterazioni; CUDA: kernel solo-iterazioni con buffer riusati;
  DirectX: shader solo-iterazioni su render target offscreen in memoria
  (132 MB), senza finestra né Present, con 4 event query in anello per contare
  i frame davvero completati (DWM e copia inter-GPU esclusi: le schede senza
  monitor misurano il puro shader).
- La finestra parte da sola, mostra l'anteprima colorata 960×540 AA1x del frame
  (resa col motore attivo prima della misura), percentuale + MPixel/s
  intermedi ogni secondo, risultato in grande e grafico a 9 barre (misura +
  storici best-di-3: CUDA 5070 Ti 5653,6/140,7 e 4070 SUPER 4667,8/110,3 in
  32/64-bit; DirectX 5070 Ti 5780,7, 4070 SUPER 4441,6, AMD Radeon 102,2;
  CPU 9900X 27,8 in double). Durante il test DirectX il pannello realtime è
  nascosto e il timer sospeso, ripristinati alla chiusura.
- CLI: `--bench-dx [scheda]` (tutte le DXGI o una), `--bench-cuda [device]`
  (tutti i device, 32 + 64 bit), `--bench-cpu` (con nome modello),
  `--diag-dx [scheda]`, `--diag-gpu`; `--csv file` accoda una riga per run
  (timestamp, motore, device, precisione, run, frame, secondi, MPixel/s),
  come il pulsante "CSV…" della finestra.

## File

- `MandelbrotViewer/Mandelbrot.cs` — calcolo e benchmark CPU.
- `MandelbrotViewer/Palette.cs` — `PaletteColors` (stop/gradienti, fonte unica).
- `MandelbrotViewer/GpuMandelbrot.cs` — backend CUDA (ILGPU 1.5.3): kernel
  render float/double + kernel benchmark solo-iterazioni, `DeviceNames()`,
  `TryInitialize(deviceName)`, `LastError` diagnostico.
- `MandelbrotViewer/DxMandelbrot.cs` — backend DirectX 11 (Vortice 3.8.3):
  shader realtime + shader benchmark, swapchain sul pannello, `Capture`,
  `RenderPreviewToBitmap`, benchmark offscreen headless (`EnsureDevice`,
  `TryInitializeHeadless`, `BeginBenchmarkOffscreen`,
  `RunBenchmarkFramesOffscreen`, `EndBenchmarkOffscreen`), `AdapterNames()`
  (escluso WARP per nome), `TryInitialize(..., adapterName)`,
  `LastError`/`EnumerationError`, `ShortAdapterName`.
- `MandelbrotViewer/MandelbrotForm.cs` / `.Designer.cs` — UI principale.
- `MandelbrotViewer/BenchmarkForm.cs` / `.Designer.cs` — finestra benchmark
  (auto-avvio, anteprima, grafico storici).
- `MandelbrotViewer/BenchmarkStandard.cs` — parametri standard condivisi GUI/CLI.
- `MandelbrotViewer/BenchmarkProgress.cs` — avanzamento (`ReportInterval` 1 s;
  `TotalIters` significativo solo per CPU).
- `MandelbrotViewer/Diagnostics.cs` — CLI senza UI
  (`--diag-dx`, `--diag-gpu`, `--bench-dx`, `--bench-cuda`).
- `MandelbrotViewer/Program.cs` — entry point + smistamento CLI.
- `MandelbrotViewer/RenderEngine.cs` — enum motori + nomi display.
- `MandelbrotViewer/Settings.cs` — `AppSettings` in JSON.
- `MandelbrotViewer/LogForm.cs` — finestra log/diagnostica.
- `MandelbrotViewer/AppVersion.cs` — versione X.Y.Z (`Display` breve se Z=0).
- `MandelbrotViewer/app.ico` — icona exe + finestre (render Fuoco 256 px,
  16/32/48/256).
- Root: `avvia.bat` (esegue la versione corrente via `dotnet run`, passa gli
  argomenti), `pubblica.bat` / `pubblica.ps1` (publish self-contained
  single-file in `pubblicato/`, ~158 MB), `AGENTS.md`, `TODO.md`,
  `CHANGELOG.md`, `.gitignore` (esclude `bin/`, `obj/`, `pubblicato/`, `*.user`).

## Changelog

Vedi [CHANGELOG.md](CHANGELOG.md).

## Note tecniche

- `dotnet` solo in `~\.dotnet`, non nel PATH: usare percorso completo.
- Audit 2026-09-06: SDK 8.0.424 + runtime 8.0.30 (ultimi), ILGPU 1.5.3 e
  Vortice 3.8.3 (ultimi stabili) — nessun aggiornamento sicuro disponibile;
  target resta `net8.0-windows` (LTS fino al 10/11/2026, poi valutare migrazione
  a .NET 10 LTS).
- Il backend CUDA richiede GPU NVIDIA + driver sulla macchina target (il toolkit
  CUDA non serve a runtime); senza GPU l'app usa la CPU in automatico.
