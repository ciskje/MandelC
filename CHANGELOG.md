# CHANGELOG — MandelC#

Versionamento `X.Y.Z` (se `Z` è 0, notazione breve `X.Y`). Regole di bump in
`AGENTS.md`. La versione è mostrata nel titolo della finestra.
- **v2.5.15** — Refactor di riordino e pulizia (nessun cambiamento funzionale):
  palette/gradienti estratti in `Palette.cs` (`PaletteColors`, fonte unica per
  CPU/DirectX; CUDA li riceve via `GpuPaletteParams`) con commenti di
  allineamento tra le tre implementazioni della colorazione (CPU/CUDA/HLSL);
  `BenchmarkProgress` in file proprio; diagnostica CLI `--diag-dx`/`--diag-gpu`
  spostata da `Program.cs` a `Diagnostics.cs`; in `DxMandelbrot.cs` estratti
  `BuildParams`/`DrawFrame` condivisi da Render/RenderBenchmark/
  RenderPreviewToBitmap, indentazione uniformata e `System.Drawing.Bitmap` →
  `Bitmap`; rimosso il campo `total` morto nel benchmark CUDA (il metro è a
  frame, `TotalIters` = 0 come per DirectX); handler del menu "Salva immagine"
  rinominato `SaveImageItem_Click`; rimossi wrapper `RenderGpu`, doppio dispose
  del bitmap, usings ridondanti e commenti obsoleti. File: `Palette.cs`,
  `BenchmarkProgress.cs`, `Diagnostics.cs` (nuovi), `Program.cs`,
  `Mandelbrot.cs`, `GpuMandelbrot.cs`, `DxMandelbrot.cs`, `MandelbrotForm.cs`,
  `MandelbrotForm.Designer.cs`, `RenderEngine.cs`, `LogForm.cs`, `AppVersion.cs`,
  `.csproj`, `TODO.md`, `SPECIFICHE.md`.


- **v2.5.14** — Nota documentale: confronto teorico RTX 4070 SUPER vs RTX 5070 Ti.
  FP32 35,5 → 43,9 TFLOPS (+24%), FP64 0,55 → 0,69 TFLOPS (+24%), memoria
  504 → 896 GB/s (+78%, irrilevante per il benchmark compute-bound), tensor
  core 224 (4ª gen) → 280 (5ª gen) ma AI TOPS di marketing su formati diversi
  (616 FP8 sparse vs 1406 FP4 sparse; a parità di formato ~+25%). Per il
  benchmark di questa app (solo iterazioni FP32/FP64, nessun tensor core)
  atteso ~+20-25%; raster gaming (TechPowerUp) ~+37%. File: `SPECIFICHE.md`,
  `TODO.md`, `AppVersion.cs`, `.csproj`.


- **v2.5.13** — Fix benchmark DirectX: la misura dava risultati irrealistici perché
  `BeginBenchmark` impostava il flag `_benchmarking` **prima** di chiamare `Resize`,
  che con il flag attivo ignora la richiesta: la swapchain restava a dimensione
  pannello (~1 MPixel/frame) mentre il metro creditava 33,18 MPixel/frame
  (risultati ~30-40× gonfiati). Ora il resize avviene prima del flag. La preview
  DirectX era nera perché `ReadTextureToBitmap` chiamava `CopyResource` con gli
  argomenti invertiti (copiava lo staging vuoto sopra la texture renderizzata);
  corretto in `CopyResource(staging, source)` (fix che ripristina anche Salva PNG)
  e la texture di preview ora usa `B8G8R8A8_UNorm`, stesso layout di byte di
  `Format32bppArgb`, per canali non invertiti. File: `DxMandelbrot.cs`,
  `AppVersion.cs`, `.csproj`.


- **v2.5.12** — Preview colorata della zona di benchmark anche per DirectX: il
  benchmark renderizza un frame offscreen con lo shader normale (palette Fuoco) e
  lo mostra nel box del BenchmarkForm, come per CUDA/CPU. Durante il test la
  finestra principale non mostra più il frame grigio dei campioni: il pannello DX
  viene nascosto e ripristinato alla chiusura. Aggiunto `DxMandelbrot.
  RenderPreviewToBitmap` + helper `ReadTextureToBitmap` (estratto da `Capture`).
  File: `DxMandelbrot.cs`, `BenchmarkForm.cs`, `MandelbrotForm.cs`,
  `AppVersion.cs`, `.csproj`.

- **v2.5.11** — Il benchmark mostra il primo frame della zona testata anche per
  CUDA e CPU: anteprima 960x540 (AA1x) renderizzata col motore attivo in un box
  dedicato della finestra Benchmark, prima di avviare la misura. DirectX mostrava
  già il primo frame nella swapchain. File: `BenchmarkForm.cs`,
  `BenchmarkForm.Designer.cs`, `AppVersion.cs`, `.csproj`.

- **v2.5.10** — Benchmark standardizzato tra i motori: zona 960x540 AA8x senza
  media dei campioni (griglia di 7680x4320 campioni elementari, stesso lavoro per
  CPU, CUDA e DirectX). Il benchmark DirectX usa uno shader solo-iterazioni e
  presenta senza v-sync (`Present(0)`), eliminando il limite del refresh del
  monitor che tappava la misura a ~60 fps. La swapchain viene ingrandita alla
  griglia durante il test e ripristinata alla fine. File: `DxMandelbrot.cs`
  (`BenchPsSource`, `BeginBenchmark`/`EndBenchmark`/`RenderBenchmark`),
  `BenchmarkForm.cs`, `AppVersion.cs`, `.csproj`.

- **v2.5.9** — Aggiornati i riferimenti storici del grafico: CUDA RTX 5070 Ti
  5940 MPixel/s, DirectX RTX 5070 Ti 1750 MPixel/s, CPU 30 MPixel/s.

- **v2.5.8** — Il benchmark mostra subito `0%` e aggiorna percentuale e
  MPixel/s ogni secondo.

- **v2.5.7** — Il benchmark DirectX gira su un worker separato; la UI resta
  responsiva e mostra gli aggiornamenti intermedi `percentuale | MPixel/s`.

- **v2.5.6** — Resi visibili gli aggiornamenti intermedi della percentuale del
  benchmark su CPU, CUDA e DirectX, incluso il repaint della label.

- **v2.5.5** — Rimossa la barra di avanzamento dal benchmark; resta visibile
  solo la percentuale testuale.

- **v2.5.4** — Durante il benchmark viene mostrata solo la percentuale; aggiunto
  tempo per il primo ridisegno e corretto lo spazio delle etichette dell’asse.

- **v2.5.3** — Ridimensionato il risultato principale del benchmark e convertito
  il grafico in barre orizzontali per evitare testo tagliato.

- **v2.5.2** — Aggiunto `avvia.bat` per provare la versione corrente con
  `dotnet run`, senza usare il publish self-contained.

- **v2.5.1** — Corretto il riferimento della RTX 5070 Ti CUDA nel grafico a
  5880 MPixel/s.

- **v2.5.0** — Il benchmark parte automaticamente all’apertura e mostra un
  grafico a barre con il risultato misurato e i riferimenti 5070 Ti CUDA (5880
  MPixel/s) e AMD 9900X (80 MPixel/s).

- **v2.4.1** — Corretto il colorizer CUDA: il valore della palette viene
  limitato a `[0,1]`, evitando variazioni magenta ad alte iterazioni.

- **v2.4.0** — Ripristinato lo smooth coloring su CPU, CUDA float/double e
  DirectX usando il modulo finale di `z`; CUDA abilita `ILGPU.Algorithms` per
  compilare `XMath.Log2` nei kernel.

- **v2.3.15** — Rimossi i launcher `avvia.bat` e `avvia.ps1`; l'avvio avviene
  tramite il file pubblicato o i comandi .NET documentati.

- **v2.3.14** — La finestra log non mostra più tutto il testo selezionato in blu
  all’apertura; il focus iniziale va al pulsante `Chiudi`.

- **v2.3.13** — Schiarite le palette con una curva comune a CPU, CUDA e
  DirectX (`t × 1,35 + 0,03`, saturata a 1), mantenendo nero l’interno.

- **v2.3.12** — Aggiunto il benchmark reale DirectX: esegue lo shader sulla
  swapchain e conta i frame presentati, mostrando pixel/s e frame completati.

- **v2.3.11** — Corretto il testo del benchmark: il fallback DirectX ora mostra
  `precisione CPU`; `CUDA 32/64-bit` compare solo quando CUDA è realmente usata.

- **v2.3.10** — Uniformata la colorazione CPU, CUDA e DirectX: stessa mappa
  basata sulle iterazioni e stessi stop di palette.

- **v2.3.9** — Allineata la colorazione CUDA/DirectX sulla stessa interpolazione
  della palette; mantenuti su GPU il calcolo e il downsampling AA. Verificati
  entrambi i motori a runtime.

- **v2.3.8** — Corretto il kernel CUDA per la compilazione runtime ILGPU e
  verificato il render sulla RTX 5070 Ti; DirectX ora inizializza l’adapter
  hardware senza il marshalling difettoso della descrizione DXGI.

- **v2.3.7** — Spostate su CUDA la colorazione e il downsampling AA;
  alla CPU viene trasferito solo il bitmap finale. File: `GpuMandelbrot.cs`,
  `AppVersion.cs`, `.csproj`.

  serializzando calcolo e colorazione per evitare race tra render cancellati.
  Il benchmark ora rispetta la precisione 32/64 selezionata e non ricopia più
  il buffer completo alla CPU. File: `GpuMandelbrot.cs`, `BenchmarkForm.cs`,
  `MandelbrotForm.cs`, `AppVersion.cs`, `.csproj`.

  `D3D11CreateDevice` ora usa `DriverType.Unknown` quando riceve l'adapter DXGI
  selezionato. File: `DxMandelbrot.cs`, `AppVersion.cs`, `.csproj`.

  l'inizializzazione (es. `[IDXGIFactory2.CreateSwapChainForHwnd] ...`); swapchain
  cambiata da `FlipDiscard` a `FlipSequential` (più compatibile) e `SampleDescription`
  fissato esplicito a (1,0). File: `DxMandelbrot.cs` (`TryInitialize`), `AppVersion.cs`,
  `.csproj`.

  una finestra di sola lettura con lo stato dei motori (DirectX e CUDA: pronto,
  scheda in uso, ultimo errore, schede disponibili), la selezione GPU, la
  precisione e le impostazioni salvate — utile per capire perché un motore risulta
  disabilitato. File: `MandelbrotForm.cs` (`LogItem_Click`, `BuildDiagnosticLog`),
  `MandelbrotForm.Designer.cs`, nuovo `LogForm.cs`, `AppVersion.cs`, `.csproj`.

  il motivo esatto (`LastError`) è ora mostrato nella barra di stato invece di
  lasciare la radio semplicemente grigia. File: `MandelbrotForm.cs`
  (handler `Shown`), `AppVersion.cs`, `.csproj`.

  invece che disabilitato: la barra resta pulita e la scelta della scheda video
  compare solo con CUDA o DirectX. File: `MandelbrotForm.cs`
  (`ApplyEngineVisibility`), `MandelbrotForm.Designer.cs` (`lblGpu`/`cmbGpu`
  `Visible`), `AppVersion.cs`, `.csproj`.

  CUDA: 32 = float 32-bit (più veloce), 64 = double 64-bit (più preciso,
  predefinito). Prima la precisione era decisa in automatico dalla scala
  (`WantsDouble`); ora è a scelta dell'utente. I due radio sono abilitati solo
  con motore CUDA (CPU = sempre double, DirectX = sempre float) e la scelta è
  persistita in settings.json. File: `MandelbrotForm.cs`/
  `MandelbrotForm.Designer.cs` (`precisionPanel`, `radPrec32`/`radPrec64`,
  `UseDoublePrecision`), `GpuMandelbrot.cs` (`RenderFrame` con `useDouble`),
  `Settings.cs` (`Single`), `AppVersion.cs`, `.csproj`.

  File/Aiuto (spiegano funzione e scorciatoia); cursore "atteso" ora visibile
  anche col mouse sopra un controllo (es. il dropdown AA) durante il calcolo,
  perché viene imposto su form e controlli di input e non solo sulla form.
  File: `MandelbrotForm.cs` (`SetBusyCursor`, `RenderAsync`),
  `MandelbrotForm.Designer.cs` (componente `toolTip` + `SetToolTip`),
  `AppVersion.cs`, `.csproj`.

  (la scheda si sceglie solo con CUDA o DirectX). File: `MandelbrotForm.cs`
  (`ApplyEngineVisibility`), `MandelbrotForm.Designer.cs`.

  barra (Auto = scheda più potente, altrimenti la scheda nominata). Enumera
  l'unione delle schede DirectX (DXGI) e dei device CUDA (ILGPU) e crea il
  device/accelerator sulla scelta; la preferenza è persistita in
  `settings.json` (`Gpu`) e applicata al cambio di motore. File: `DxMandelbrot.cs`
  (`AdapterNames`, `TryInitialize(..., adapterName)`), `GpuMandelbrot.cs`
  (`DeviceNames`, `TryInitialize(deviceName)`, `ResetAccelerator`),
  `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs` (`lblGpu`, `cmbGpu`),
  `Settings.cs`, `AppVersion.cs`, `.csproj`.

  benchmark resta standard a 5000). L'app parte sempre dall'insieme completo:
  la vista precedente (centro/larghezza) non viene più memorizzata in
  `settings.json`. File: `MandelbrotForm.Designer.cs`, `Settings.cs`,
  `MandelbrotForm.cs`.

  frattale a ogni frame (float, triangolo fullscreen), loop ~60 fps con
  pan/zoom immediati, AA 2x/4x/8x come supersampling dentro lo shader,
  salvataggio PNG dal backbuffer, fallback CPU automatico in caso di errore.
  Radio DirectX abilitata all'avvio. File: `DxMandelbrot.cs` (nuovo),
  `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs` (`dxPanel`, `radioDx`),
  `.csproj` (dipendenze Vortice 3.8.3).

  (`%APPDATA%\MandelbrotViewer\settings.json`): iterazioni auto/manuale e valore,
  palette, AA, motore preferito, ultima vista e posizione finestra; preferenza
  GPU rispettata all'avvio. File: `Settings.cs` (nuovo), `MandelbrotForm.cs`.

  veloce: kernel solo-iterazioni (niente buffer |z|²) + buffer riusati per tutti
  i frame (niente alloc/transfer extra). File: `BenchmarkForm.cs`,
  `GpuMandelbrot.cs`.

  File → Salva immagine con nome). File: `MandelbrotForm.Designer.cs`.

  aggiornamento UI limitato a ogni 3 s + report finale, così gli Invoke non
  falsano la misura (`BenchmarkProgress.ReportInterval`). File: `Mandelbrot.cs`,
  `GpuMandelbrot.cs`, `BenchmarkForm.cs`.

  le iterazioni totali restano nel dettaglio. `BenchmarkProgress` riporta anche
  i frame. File: `BenchmarkForm.cs`, `Mandelbrot.cs`, `GpuMandelbrot.cs`.

  iterazioni max per 8 secondi, misura iterazioni/s del motore selezionato
  (fallback CPU), risultato in grande (Giter/s, Miter/s…) con barra di
  avanzamento e annullamento. File: `BenchmarkForm.cs` + `.Designer.cs` (nuovi),
  `Mandelbrot.cs` (`BenchmarkCpu`), `GpuMandelbrot.cs` (`BenchmarkGpu`),
  `MandelbrotForm.*` (voce menu).

  File: `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`.
  per lato) con upscale bilineare, render completo al rilascio; stato marcato
  "(anteprima)". File: `MandelbrotForm.cs` (`RenderAsync(preview)`, `Upscale`).

  risoluzione k volte maggiore + media RGB di ogni blocco kxk (stessa logica su
  CPU con `AverageBlock` e su GPU); iterazioni automatiche da radio a checkbox
  (default manuale); finestra default 1152x720. Verificato con smoke test
  (CPU 1x vs 2x, GPU 2x vs CPU 2x). File: `Mandelbrot.cs`, `GpuMandelbrot.cs`,
  `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`. Formula iter auto
  `200 + 790·log10(zoom)` (~2000 a scala 1,95e-4, ~4550 a scala 1e-5).

  (scala < 1e-3), un thread per pixel, device più capiente in automatico,
  fallback CPU se niente GPU, radio CUDA abilitata all'avvio se la GPU risponde;
  stato e Informazioni mostrano motore/precisione/device. Refactor:
  `ColorFromEscape` condiviso tra CPU e GPU. Verificato con smoke test su
  RTX 5070 Ti (iterazioni GPU == CPU). Nuova formula iter auto
  `200 + 430·log10(zoom)` (~2000 a scala 1,95e-4); fix gruppi radio Manuale/CPU
  (pannelli contenitore separati). File: `GpuMandelbrot.cs` (nuovo),
  `Mandelbrot.cs`, `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`,
  `RenderEngine.cs`, `.csproj` (dipendenza ILGPU).

  enum `RenderEngine` + `RenderEngineInfo`; CUDA e DirectX disabilitati
  (roadmap v1.4/v2.0). Aiuto spostato nella `StatusStrip`, finestra default
  1024x680, motore attivo mostrato nello stato. File: `RenderEngine.cs`
  (nuovo), `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`.

  preservata). File: `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`,
  `Program.cs`.
  mostra il valore automatico usato per il render. File: `Form1.cs`.

  (Fuoco, Ghiaccio, Termico) con gradienti dedicati in `Mandelbrot.cs` (via
  l'enum `Palette`); barra superiore rifatta con `TableLayoutPanel` così numero
  iterazioni, label, radio e dropdown sono allineati verticalmente; radio
  Auto/Manuale per le iterazioni (`AutoIter = 150 + 150·log10(zoom)`,
  clamp 50–5000, numero disabilitato in auto); stato spostato in `StatusStrip`
  in basso. File: `Form1.cs`, `Form1.Designer.cs`, `Mandelbrot.cs`.
  immagine con nome Ctrl+Shift+S) + menu Aiuto con Informazioni (versione e
  comandi); zoom con rotella sul cursore (focus automatico al passaggio mouse);
  tasti singoli R/S/+/- ignorati con Ctrl/Alt premuti. File: `Form1.cs`
  (`ViewZone`, `SaveZone`, `LoadZone`, `ShowAbout`), `Form1.Designer.cs`
  (`MenuStrip`).
  punto); trascinamento con pulsante premuto = pan con throttle 80 ms
  (soglia click/trascinamento 5 px). Rimossi zoom su rettangolo e zoom con
  rotella. File: `Form1.cs`, `Form1.Designer.cs` (testo aiuto aggiornato).
  user-level); publish self-contained in `pubblicato/` così non serve installare
  il Desktop Runtime; versione in sorgente (`AppVersion.cs` + `<Version>` nel
  csproj, mostrata nel titolo); setup progetto (AGENTS.md, TODO.md,
  SPECIFICHE.md, `.gitignore`, tracking nel repo `test/`).
  iterazioni configurabili, salvataggio PNG. File: `Form1.*`, `Mandelbrot.cs`.
