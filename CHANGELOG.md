# CHANGELOG — MandelC#

Versionamento `X.Y.Z` (se `Z` è 0, notazione breve `X.Y`). Regole di bump in
`AGENTS.md`. La versione è mostrata nel titolo della finestra.

- **v2.0.1** — Limite massimo iterazioni portato a 50000 (manuale e zone; il
  benchmark resta standard a 5000). L'app parte sempre dall'insieme completo:
  la vista precedente (centro/larghezza) non viene più memorizzata in
  `settings.json`. File: `MandelbrotForm.Designer.cs`, `Settings.cs`,
  `MandelbrotForm.cs`.

- **v2.0** — Motore DirectX 11 realtime: pixel shader HLSL che calcola il
  frattale a ogni frame (float, triangolo fullscreen), loop ~60 fps con
  pan/zoom immediati, AA 2x/4x/8x come supersampling dentro lo shader,
  salvataggio PNG dal backbuffer, fallback CPU automatico in caso di errore.
  Radio DirectX abilitata all'avvio. File: `DxMandelbrot.cs` (nuovo),
  `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs` (`dxPanel`, `radioDx`),
  `.csproj` (dipendenze Vortice 3.8.3).

- **v1.10** — Impostazioni persistite tra un lancio e l'altro
  (`%APPDATA%\MandelbrotViewer\settings.json`): iterazioni auto/manuale e valore,
  palette, AA, motore preferito, ultima vista e posizione finestra; preferenza
  GPU rispettata all'avvio. File: `Settings.cs` (nuovo), `MandelbrotForm.cs`.

- **v1.9** — Dettaglio benchmark senza iterazioni totali. Benchmark GPU più
  veloce: kernel solo-iterazioni (niente buffer |z|²) + buffer riusati per tutti
  i frame (niente alloc/transfer extra). File: `BenchmarkForm.cs`,
  `GpuMandelbrot.cs`.

- **v1.8.1** — Tasto "Salva PNG" sostituito da "Benchmark" (il PNG resta in
  File → Salva immagine con nome). File: `MandelbrotForm.Designer.cs`.

- **v1.8** — Benchmark in AA 8x (64x pixel per frame, contati nel risultato) e
  aggiornamento UI limitato a ogni 3 s + report finale, così gli Invoke non
  falsano la misura (`BenchmarkProgress.ReportInterval`). File: `Mandelbrot.cs`,
  `GpuMandelbrot.cs`, `BenchmarkForm.cs`.

- **v1.7.1** — Risultato benchmark in pixel/s (live e finale, con G/M/k suffissi);
  le iterazioni totali restano nel dettaglio. `BenchmarkProgress` riporta anche
  i frame. File: `BenchmarkForm.cs`, `Mandelbrot.cs`, `GpuMandelbrot.cs`.

- **v1.7** — Benchmark standard (menu File, Ctrl+B): zona fissa 800x600 a 5000
  iterazioni max per 8 secondi, misura iterazioni/s del motore selezionato
  (fallback CPU), risultato in grande (Giter/s, Miter/s…) con barra di
  avanzamento e annullamento. File: `BenchmarkForm.cs` + `.Designer.cs` (nuovi),
  `Mandelbrot.cs` (`BenchmarkCpu`), `GpuMandelbrot.cs` (`BenchmarkGpu`),
  `MandelbrotForm.*` (voce menu).

- **v1.6.1** — Antialias senza checkbox: solo dropdown (1x = off, 2x/4x/8x = on).
  File: `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`.
- **v1.6** — Anteprima veloce durante il pan: niente AA e 1/4 dei pixel (metà
  per lato) con upscale bilineare, render completo al rilascio; stato marcato
  "(anteprima)". File: `MandelbrotForm.cs` (`RenderAsync(preview)`, `Upscale`).

- **v1.5** — Antialias con checkbox + dropdown 1x/2x/4x/8x: supersampling a
  risoluzione k volte maggiore + media RGB di ogni blocco kxk (stessa logica su
  CPU con `AverageBlock` e su GPU); iterazioni automatiche da radio a checkbox
  (default manuale); finestra default 1152x720. Verificato con smoke test
  (CPU 1x vs 2x, GPU 2x vs CPU 2x). File: `Mandelbrot.cs`, `GpuMandelbrot.cs`,
  `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`. Formula iter auto
  `200 + 790·log10(zoom)` (~2000 a scala 1,95e-4, ~4550 a scala 1e-5).

- **v1.4** — Backend CUDA con ILGPU 1.5.3: kernel float (zoom bassi) e double
  (scala < 1e-3), un thread per pixel, device più capiente in automatico,
  fallback CPU se niente GPU, radio CUDA abilitata all'avvio se la GPU risponde;
  stato e Informazioni mostrano motore/precisione/device. Refactor:
  `ColorFromEscape` condiviso tra CPU e GPU. Verificato con smoke test su
  RTX 5070 Ti (iterazioni GPU == CPU). Nuova formula iter auto
  `200 + 430·log10(zoom)` (~2000 a scala 1,95e-4); fix gruppi radio Manuale/CPU
  (pannelli contenitore separati). File: `GpuMandelbrot.cs` (nuovo),
  `Mandelbrot.cs`, `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`,
  `RenderEngine.cs`, `.csproj` (dipendenza ILGPU).

- **v1.3.3** — Radio di selezione motore di rendering (CPU/CUDA/DirectX) con
  enum `RenderEngine` + `RenderEngineInfo`; CUDA e DirectX disabilitati
  (roadmap v1.4/v2.0). Aiuto spostato nella `StatusStrip`, finestra default
  1024x680, motore attivo mostrato nello stato. File: `RenderEngine.cs`
  (nuovo), `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`.

- **v1.3.2** — Rinomina `Form1` → `MandelbrotForm` (via `git mv`, storia
  preservata). File: `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`,
  `Program.cs`.
- **v1.3.1** — In modalità Auto il numero delle iterazioni resta disabilitato ma
  mostra il valore automatico usato per il render. File: `Form1.cs`.

- **v1.3** — Voce `Esci` (Alt+F4) nel menu File; dropdown palette colori
  (Fuoco, Ghiaccio, Termico) con gradienti dedicati in `Mandelbrot.cs` (via
  l'enum `Palette`); barra superiore rifatta con `TableLayoutPanel` così numero
  iterazioni, label, radio e dropdown sono allineati verticalmente; radio
  Auto/Manuale per le iterazioni (`AutoIter = 150 + 150·log10(zoom)`,
  clamp 50–5000, numero disabilitato in auto); stato spostato in `StatusStrip`
  in basso. File: `Form1.cs`, `Form1.Designer.cs`, `Mandelbrot.cs`.
- **v1.2** — Menu File (carica zona Ctrl+O, salva zona Ctrl+S in JSON, salva
  immagine con nome Ctrl+Shift+S) + menu Aiuto con Informazioni (versione e
  comandi); zoom con rotella sul cursore (focus automatico al passaggio mouse);
  tasti singoli R/S/+/- ignorati con Ctrl/Alt premuti. File: `Form1.cs`
  (`ViewZone`, `SaveZone`, `LoadZone`, `ShowAbout`), `Form1.Designer.cs`
  (`MenuStrip`).
- **v1.1.1** — Zoom solo col click (sx = avanti 2x, dx = indietro 2x centrati sul
  punto); trascinamento con pulsante premuto = pan con throttle 80 ms
  (soglia click/trascinamento 5 px). Rimossi zoom su rettangolo e zoom con
  rotella. File: `Form1.cs`, `Form1.Designer.cs` (testo aiuto aggiornato).
- **v1.1** — Script di lancio (`avvia.bat`/`avvia.ps1` con fallback dotnet
  user-level); publish self-contained in `pubblicato/` così non serve installare
  il Desktop Runtime; versione in sorgente (`AppVersion.cs` + `<Version>` nel
  csproj, mostrata nel titolo); setup progetto (AGENTS.md, TODO.md,
  SPECIFICHE.md, `.gitignore`, tracking nel repo `test/`).
- **v1.0** — Primo visualizzatore: rendering parallelo, zoom mouse/rotella,
  iterazioni configurabili, salvataggio PNG. File: `Form1.*`, `Mandelbrot.cs`.
