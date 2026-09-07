# CHANGELOG — MandelC#

Versionamento `X.Y.Z` (se `Z` è 0, notazione breve `X.Y`). Regole di bump in
`AGENTS.md`. La versione è mostrata nel titolo della finestra.
- **v2.15.10** — Fix throughput DX a AA1x: poll event-query di nuovo stretto (lo Sleep/quanto deprimeva i frame da ~1 ms: 5070 Ti 67,4→292,4); controlli anti-blocco ogni 1024 poll. Storici DX aggiornati. File: `DxMandelbrot.cs`, `BenchmarkForm.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.15.9** — Benchmark standard a AA1x (griglia 960x540): la metrica resta MPixel/s e la Radeon rientra (niente piu TDR ne skip iGPU); storici ricalcolati via CLI: CUDA 5070 Ti 276,3/6,7 e 4070 SUPER 220,7/5,3, DirectX 5070 Ti 65,9, 4070 SUPER 56,8 e Radeon 4,8, CPU 9900X 4,8 (9 barre). File: `BenchmarkStandard.cs`, `DxMandelbrot.cs`, `BenchmarkForm.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.15.8** — Benchmark DX: skip preventivo schede con <1 GB dedicati (niente piu TDR/popup AMD), `QuerySignaled` sicura su device removed (niente piu HRESULT grezzo nel log), log evento unico via GUI; storici: DirectX 5070 Ti 436,3 + 4070 SUPER 355,2 (8 barre). File: `DxMandelbrot.cs`, `BenchmarkForm.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.15.7** — Log/diagnostica (menu Aiuto): nuova sezione Log eventi con errori dei benchmark (AppLog in memoria, ultime 200 righe); gli errori DX (device removed, timeout, VRAM) e della GUI ora restano nel log. File: `AppLog.cs`, `MandelbrotForm.cs`, `DxMandelbrot.cs`, `BenchmarkForm.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.15.6** — Benchmark DX anti-blocco: check VRAM prima della griglia AA8x, timeout 60 s per frame e rilevazione device-removed in `DrainOne` (la Radeon iGPU andava in TDR e restava appesa senza errori); errori visibili in GUI e `run fallito` in CLI. File: `DxMandelbrot.cs`, `Diagnostics.cs`, `BenchmarkForm.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.15.5** — Storici benchmark ricalcolati via CLI sulla nuova zona (best di 3): CUDA 5070 Ti 490,7/9,0 e 4070 SUPER 384,1/7,0 (32/64-bit), DirectX 5070 Ti 483,0, CPU 9900X 6,1; rimosse le barre DirectX 4070 SUPER (scheda assente in DXGI) e AMD Radeon (bench si blocca). Grafico a 7 barre. File: BenchmarkForm.cs, TODO.md, SPECIFICHE.md, CHANGELOG.md, AppVersion.cs, .csproj.

- **v2.15.4** — Benchmark: pulsante export rinominato CSV… → Esporta CSV e Chiudi sempre a destra (ordine dock: Avvia, Esporta CSV, Chiudi). File: BenchmarkForm.Designer.cs, TODO.md, SPECIFICHE.md, CHANGELOG.md, AppVersion.cs, .csproj.

- **v2.15.3** — Benchmark: iterazioni non piu fisse ma calcolate con la formula auto alla scala del test (BenchmarkStandard.MaxIter => Mandelbrot.AutoIterForScale(Scale) = 10915). File: BenchmarkStandard.cs, BenchmarkForm.cs, TODO.md, SPECIFICHE.md, CHANGELOG.md, AppVersion.cs, .csproj.

- **v2.15.2** — Zona benchmark: centro (-0.7499302568795561, -0.015139113925433963), scala 1.0453474311811176e-04 (half 5.226737155905588e-05), 10915 iter (= auto della zona); w×h 960×540 e budget 8 s invariati. Storici invariati (riferiti alla vecchia zona). File: BenchmarkStandard.cs, TODO.md, SPECIFICHE.md, CHANGELOG.md, AppVersion.cs, .csproj.

- **v2.15.1** — Iter auto 2000*(1+log10(1.5/half)) (half = scala/2, clamp 50-50000) centralizzata in Mandelbrot.AutoIterForScale, usata da vista e video zoom (alla scala benchmark 5e-4 vale 9556, ma il benchmark resta a 5000 iter fisse). File: Mandelbrot.cs, MandelbrotForm.cs, ZoomVideoForm.cs, TODO.md, SPECIFICHE.md, CHANGELOG.md, AppVersion.cs, .csproj.

- **v2.15.0** — Submenu File → Esporta con Screenshot (Ctrl+Shift+E: dialog che
  parte da Vista corrente con preset e AA liberi) e Video zoom (Ctrl+Shift+V)
  con selettore AA proprio (Come vista/1x/2x/4x/8x: il video può usare un AA
  diverso dalla vista); tolte le voci singole. File: `MandelbrotForm.cs`,
  `MandelbrotForm.Designer.cs`, `ExportForm.cs`, `ZoomVideoForm.cs`,
  `ZoomVideoForm.Designer.cs`, `TODO.md`, `SPECIFICHE.md`,
  `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.14.1** — Palette a 2-3 colori base: Foresta ridisegnata (marrone e verde
  agli estremi, interno sempre nero) e Viola con opposti vivaci magenta↔ciano;
  le altre erano già a 2-3 tinte. Vale su tutti i motori (stop generici). File:
  `Palette.cs`, `TODO.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.14.0** — Export PNG con preset (Vista corrente, Full HD, 2K, 4K, 8K,
  Doppio 4K 7680×2160, Personalizzata), AA selezionabile (Come vista/1x/2x/4x/8x) e
  dimensioni custom validate (interi 320…16384, caselle attive solo su
  Personalizzata); tetto campioni e dialog invariati. Fix encode video: la
  vista ha spesso lati dispari e libx264/yuv420p li rifiuta ("Could not open
  encoder" + "no packets", exit 0xDFABA7BB, riprodotto) — pad a dimensioni pari,
  pre-flight sui PNG e più righe stderr negli errori. File: `ExportForm.cs`,
  `ExportForm.Designer.cs`, `ZoomVideoForm.cs`, `TODO.md`, `SPECIFICHE.md`,
  `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.13.2** — Icona dell'exe e delle finestre: render 256 px dell'insieme in
  palette Fuoco, `.ico` multirisoluzione (16/32/48/256) via `ApplicationIcon` +
  risorsa incorporata applicata a tutte le form (`Program.ApplyIcon`). File:
  `app.ico` (nuovo), `MandelbrotViewer.csproj`, `Program.cs`,
  `MandelbrotForm.cs`, `BenchmarkForm.cs`, `ExportForm.cs`, `ZoomVideoForm.cs`,
  `LogForm.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`.

- **v2.13.1** — Fix hang a fine export video: la codifica ffmpeg girava sul
  thread UI e lo stderr veniva letto dopo il `WaitForExit` — con 60+ frame il
  pipe si riempie, ffmpeg si blocca in scrittura e l'app non torna più (file
  presente ma UI morta; riprodotto con watchdog). Ora encode su worker, stderr
  drenato in asincrono durante l'attesa (`BeginErrorReadLine`) e processo ucciso
  su annullamento; direzione invertita (dalla vista corrente all'insieme, con
  errore se si è già all'insieme invece di partire da 100x dentro); centro
  proporzionale allo zoom e non a t (prima il punto di partenza usciva subito
  dall'inquadratura: fino a 46x la semi-vista, ora max 0,17); transizione
  ease-out cubica (veloce all'inizio, lento alla fine); il video usa l'AA
  selezionato (auto-ridotto oltre 128 MPixel di campioni, come l'export);
  tasto Apri per il risultato (MP4 col player, cartella PNG in Explorer).
  File: `ZoomVideoForm.cs`, `ZoomVideoForm.Designer.cs`, `TODO.md`, `SPECIFICHE.md`,
  `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.13.0** — Video zoom MP4 (File → Ctrl+Shift+V): interpola dalla vista
  corrente all'insieme completo (scala logaritmica, centro lineare, iterazioni auto per
  frame come `AutoIter`), rende 60…480 frame a risoluzione vista e AA1x col
  motore attivo (Julia inclusa), codifica con ffmpeg (H.264, CRF 18) o — se
  assente — lascia la sequenza PNG; progress determinata + annullamento. Nuovi
  `ZoomVideoForm.cs`/`.Designer.cs`. File: `ZoomVideoForm.cs`,
  `ZoomVideoForm.Designer.cs` (nuovi), `MandelbrotForm.cs`,
  `MandelbrotForm.Designer.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`,
  `AppVersion.cs`, `.csproj`.

- **v2.12.0** — Modalità Julia (menu Vista, Ctrl+J): `z = z² + c` con c fissata
  (default −0,7 + 0,27015i, persistita in settings), click sinistro = fissa c
  sul punto, zoom/pan/AA/palette invariati, R resetta anche c; kernel CPU, CUDA
  float/double e shader HLSL con branch uniforme (stessa colorazione); zone
  (cronologia, preferiti, file) includono modo e c con default per i file
  vecchi; benchmark sempre Mandelbrot; export HR segue il modo. File:
  `Mandelbrot.cs`, `GpuMandelbrot.cs`, `DxMandelbrot.cs`, `MandelbrotForm.cs`,
  `ExportForm.cs`, `Settings.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`,
  `AppVersion.cs`, `.csproj`.

- **v2.11.0** — Export PNG ad alta risoluzione (File → Ctrl+Shift+E): dialog con
  larghezza 320…16384 px (preset 1280…7680, altezza dall'aspect della vista),
  render offscreen col motore attivo (CPU/CUDA/DirectX via preview offscreen),
  AA ridotto in automatico oltre 128 MPixel di campioni, marquee +
  annullamento, salvataggio PNG. Nuovi `ExportForm.cs`/`.Designer.cs`. File:
  `ExportForm.cs`, `ExportForm.Designer.cs` (nuovi), `MandelbrotForm.cs`,
  `MandelbrotForm.Designer.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`,
  `AppVersion.cs`, `.csproj`.

- **v2.10.0** — Cronologia viste + zone preferite: nuovo menu Vista con
  Indietro/Avanti (Alt+Left/Right, stack in memoria cap 200, ogni zoom/pan/
  reset/caricamento è committed), "Salva zona preferita…" (Ctrl+D, JSON in
  `%APPDATA%\MandelbrotViewer\zone\`), sottomenu di salto ed eliminazione
  (ricostruiti all'apertura, con validazione come Carica zona). `LoadZone`
  rifattorizzato su `TryParseZone`/`ApplyZone` condivisi. File:
  `MandelbrotForm.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`,
  `AppVersion.cs`, `.csproj`.

- **v2.9.0** — Export CSV dei benchmark (una riga per run: timestamp, motore,
  device, precisione, run, frame, secondi, MPixel/s): pulsante "CSV…" nella
  finestra benchmark (accoda la misura) e `--csv file` nei comandi
  `--bench-dx/--bench-cuda/--bench-cpu`. Nuovo `BenchmarkCsv.cs`. File:
  `BenchmarkCsv.cs` (nuovo), `BenchmarkForm.cs`, `BenchmarkForm.Designer.cs`,
  `Diagnostics.cs`, `Program.cs`, `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`,
  `AppVersion.cs`, `.csproj`.

- **v2.8.0** — Benchmark CPU da CLI (`--bench-cpu`: 3 run da 8 s in double) e
  storico con nome modello (`Diagnostics.CpuName` da registro, es. "CPU 9900X"):
  misurato 27,8 MPixel/s su AMD Ryzen 9 9900X (sostituisce il vecchio "CPU"
  30,0). File: `Diagnostics.cs`, `Program.cs`, `BenchmarkForm.cs`, `TODO.md`,
  `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.7.0** — Quattro palette fisse (Oceano, Viola, Deserto, Foresta, 5 stop
  ciascuna come le esistenti): valgono subito su CPU, CUDA e DirectX perché gli
  stop sono generici; enum esteso solo in coda così gli indici persistiti restano
  validi. File: `Palette.cs`, `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`,
  `TODO.md`, `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.6.0** — Pan da tastiera con le frecce direzionali: 1/10 della vista per
  passo (Shift = passo fine 1/100); ignorato quando il focus è sul numero
  iterazioni (che usa già su/giù). Guida comandi aggiornata. File:
  `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`, `TODO.md`,
  `SPECIFICHE.md`, `CHANGELOG.md`, `AppVersion.cs`, `.csproj`.

- **v2.5.23** — Revisione documentazione: `SPECIFICHE.md` riscritto sulle
  specifiche reali e attuali (via la cronistoria versioni duplicata, il
  confronto hardware e le note obsolete su Present/finestra bench; descrizione
  motori, benchmark, CLI, file e note tecniche verificate contro il codice) e
  intestazioni versione ripristinate in `CHANGELOG.md` dove mancavano.
  Nessun cambio di codice. File: `SPECIFICHE.md`, `CHANGELOG.md`, `TODO.md`,
  `AppVersion.cs`, `.csproj`.

- **v2.5.22** — Cursore AppStarting (freccia+clessidra) invece di Wait durante i
  render: il calcolo è async e l'UI resta interattiva, quindi è il cursore
  corretto; assegnato in ricorsione a form e discendenti (copre pictureBox,
  menu, stato; `UseWaitCursor` rimosso perché forza la clessidra piena). Resta
  Wait solo per l'init CUDA sincrona a UI congelata. File: `MandelbrotForm.cs`,
  `TODO.md`, `SPECIFICHE.md`, `AppVersion.cs`, `.csproj`.

- **v2.5.21** — Fix cursore wait: durante i render lunghi (es. zona difficile AA8x
  passando CUDA da 32 a 64 bit) il cursore restava freccia se il mouse era
  sull'immagine, perché `pictureBox` non era nella lista di `SetBusyCursor`.
  Ora la form usa anche `UseWaitCursor`, che copre tutti i controlli e le
  superfici (immagine, menu, stato). File: `MandelbrotForm.cs`, `TODO.md`,
  `SPECIFICHE.md`, `AppVersion.cs`, `.csproj`.

- **v2.5.20** — Benchmark DirectX offscreen senza Present: risolve la misura
  falsata sulle schede senza monitor (la 4070 SUPER, headless su slot PCIe x4,
  pagava la copia di 132 MB/frame verso la 5070 Ti del display: 1655 → 4441
  MPixel/s, ~2,7×). Il test rende su render target in memoria (headless, senza
  finestra né DWM) e conta i frame davvero completati con un anello di event
  query (`QueryType.Event`, `GetData` con pData NULL: S_OK = pronta). Dettaglio
  Vortice: l'overload `GetData(async, flags)` restituisce sempre un DataStream
  non-null, per lo status serve l'overload raw (S_OK vs S_FALSE). Creazione
  device/shader estratta in `EnsureDevice`, riusata da `TryInitialize` (GUI) e
  `TryInitializeHeadless` (CLI). GUI e CLI usano lo stesso percorso; storici DX
  sostituiti con le misure offscreen (best di 3): 5070 Ti 5780,7 (+1%),
  4070 SUPER 4441,6, AMD Radeon 102,2. File: `DxMandelbrot.cs`, `Diagnostics.cs`,
  `BenchmarkForm.cs`, `TODO.md`, `SPECIFICHE.md`, `AppVersion.cs`, `.csproj`.

- **v2.5.19** — Triplo test CUDA per device e storici aggiornati: nuovo comando
  `--bench-cuda [nome device]` che esegue 3 run da 8 s del test standardizzato
  su ogni device CUDA in float 32-bit e double 64-bit e stampa i valori in
  MPixel/s con il migliore (analogo di `--bench-dx`). Misurato ora (best di 3):
  RTX 5070 Ti 5653,6 (32-bit) / 140,7 (64-bit), RTX 4070 SUPER 4667,8 (32-bit) /
  110,3 (64-bit) — il vecchio riferimento unico "CUDA 5070 Ti 5940" è sostituito
  da queste quattro barre. Grafico allargato (9 barre: margine sinistro e altezza
  pannello aumentati). File: `Diagnostics.cs`, `Program.cs`, `BenchmarkForm.cs`,
  `BenchmarkForm.Designer.cs`, `TODO.md`, `SPECIFICHE.md`, `AppVersion.cs`,
  `.csproj`.

- **v2.5.18** — Triplo test DirectX per scheda e storico aggiornato: nuovo comando
  `--bench-dx [nome scheda]` che esegue 3 run da 8 s del test standardizzato su
  ogni scheda DXGI disponibile e stampa i valori in MPixel/s con il migliore
  (finestra visibile minima: con Present(0) su finestra nascosta il compositor
  potrebbe saltare il lavoro GPU). Parametri del test spostati nella classe
  condivisa `BenchmarkStandard` (usata da GUI e CLI); loop di misura estratto in
  `DxMandelbrot.RunBenchmarkFrames` (riusato dal benchmark GUI, ora anche su
  worker thread); `DxMandelbrot.ShortAdapterName` per i nomi compressi. Nel
  grafico del benchmark la barra obsoleta "DirectX 5070 Ti 1750" (test vecchio
  con v-sync, non confrontabile) è sostituita dai riferimenti per scheda
  misurati ora (best di 3 run, vedi SPECIFICHE); asse calcolato dalle barre.
  File: `BenchmarkStandard.cs` (nuovo), `DxMandelbrot.cs`, `BenchmarkForm.cs`,
  `Diagnostics.cs`, `Program.cs`, `TODO.md`, `SPECIFICHE.md`, `AppVersion.cs`,
  `.csproj`.

- **v2.5.17** — Fix: `AdapterNames()` restituiva sempre un elenco vuoto su
  macchine con GPU moderne (≥4 GB): `DedicatedVideoMemory` è un PointerUSize
  (SIZE_T) e il confronto `> 0` passava per la conversione implicita a 32 bit
  di SharpGen (`UIntPtr.ToUInt32`), che lancia OverflowException oltre i 4 GB —
  la RTX 5070 Ti (16 GB, primo adapter) uccideva l'intera enumerazione, con
  l'errore inghiottito dal catch (è l'"overflow del wrapper Vortice" della nota
  v2.3.8). Ora i renderizzatori software si escludono per nome ("Microsoft
  Basic*") e l'elenco riporta davvero le schede. `DiagDx` stampa l'enumerazione
  dettagliata per adapter e l'eventuale errore di enumerazione; il log
  diagnostico della GUI riporta `EnumerationError`. Verificato a runtime:
  elenco completo (RTX 5070 Ti, AMD Radeon iGPU, RTX 4070 SUPER) e selezione
  esplicita operativa su 4070 SUPER e iGPU (`IsReady: True`, `Render: OK`).
  File: `DxMandelbrot.cs`, `Diagnostics.cs`, `MandelbrotForm.cs`, `TODO.md`,
  `SPECIFICHE.md`, `AppVersion.cs`, `.csproj`.

- **v2.5.16** — Fix: il dropdown GPU non applicava la scheda DirectX scelta:
  `TryInitialize` creava sempre il device sull'adapter predefinito
  (`D3D11CreateDevice(null, DriverType.Hardware)`). Ora la scheda richiesta viene
  cercata per nome tra gli adapter DXGI dello stesso factory della swapchain e
  passata esplicitamente a `D3D11CreateDevice` con `DriverType.Unknown`
  (obbligatorio quando si passa un adapter); nome inesistente → errore chiaro in
  `LastError` ("Scheda video non trovata: …"). `--diag-dx` accetta il nome
  scheda come argomento per il test senza UI. File: `DxMandelbrot.cs`,
  `Diagnostics.cs`, `Program.cs`, `TODO.md`, `SPECIFICHE.md`, `AppVersion.cs`,
  `.csproj`.

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

- **v2.3.6** — Ottimizzazione render GPU e benchmark CUDA: serializzazione di
  calcolo e colorazione per evitare race tra render cancellati; il benchmark
  ora rispetta la precisione 32/64 selezionata e non ricopia più il buffer
  completo alla CPU. File: `GpuMandelbrot.cs`, `BenchmarkForm.cs`,
  `MandelbrotForm.cs`, `AppVersion.cs`, `.csproj`.

- **v2.3.5** — Fix DirectX: `D3D11CreateDevice` ora usa `DriverType.Unknown`
  quando riceve l'adapter DXGI selezionato. File: `DxMandelbrot.cs`,
  `AppVersion.cs`, `.csproj`.

- **v2.3.4** — Diagnostica DirectX: log con lo step esatto che fallisce
  l'inizializzazione (es. `[IDXGIFactory2.CreateSwapChainForHwnd] ...`); swapchain
  cambiata da `FlipDiscard` a `FlipSequential` (più compatibile) e `SampleDescription`
  fissato esplicito a (1,0). File: `DxMandelbrot.cs` (`TryInitialize`), `AppVersion.cs`,
  `.csproj`.

- **v2.3.3** — Voce Aiuto → log/diagnostica: una finestra di sola lettura con
  lo stato dei motori (DirectX e CUDA: pronto, scheda in uso, ultimo errore,
  schede disponibili), la selezione GPU, la precisione e le impostazioni
  salvate — utile per capire perché un motore risulta disabilitato. File:
  `MandelbrotForm.cs` (`LogItem_Click`, `BuildDiagnosticLog`),
  `MandelbrotForm.Designer.cs`, nuovo `LogForm.cs`, `AppVersion.cs`, `.csproj`.

- **v2.3.2** — Motore GPU non disponibile: il motivo esatto (`LastError`) è ora
  mostrato nella barra di stato invece di lasciare la radio semplicemente
  grigia. File: `MandelbrotForm.cs` (handler `Shown`), `AppVersion.cs`, `.csproj`.

- **v2.3.1** — Dropdown GPU nascosto (invece che disabilitato) con motore CPU:
  la barra resta pulita e la scelta della scheda video compare solo con CUDA o
  DirectX. File: `MandelbrotForm.cs` (`ApplyEngineVisibility`),
  `MandelbrotForm.Designer.cs` (`lblGpu`/`cmbGpu` `Visible`), `AppVersion.cs`,
  `.csproj`.

- **v2.3** — Precisione CUDA a scelta: radio 32/64 (32 = float 32-bit, più
  veloce; 64 = double 64-bit, più preciso, predefinito). Prima la precisione era
  decisa in automatico dalla scala (`WantsDouble`); ora è a scelta dell'utente.
  I due radio sono abilitati solo con motore CUDA (CPU = sempre double,
  DirectX = sempre float) e la scelta è persistita in settings.json. File:
  `MandelbrotForm.cs`/`MandelbrotForm.Designer.cs` (`precisionPanel`,
  `radPrec32`/`radPrec64`, `UseDoublePrecision`), `GpuMandelbrot.cs`
  (`RenderFrame` con `useDouble`), `Settings.cs` (`Single`), `AppVersion.cs`,
  `.csproj`.

- **v2.2** — ToolTip su controlli File/Aiuto (spiegano funzione e scorciatoia);
  cursore "atteso" ora visibile anche col mouse sopra un controllo (es. il
  dropdown AA) durante il calcolo, perché viene imposto su form e controlli di
  input e non solo sulla form. File: `MandelbrotForm.cs` (`SetBusyCursor`,
  `RenderAsync`), `MandelbrotForm.Designer.cs` (componente `toolTip` +
  `SetToolTip`), `AppVersion.cs`, `.csproj`.

- Dropdown GPU disabilitato con motore CPU (la scheda si sceglie solo con CUDA
  o DirectX). File: `MandelbrotForm.cs` (`ApplyEngineVisibility`),
  `MandelbrotForm.Designer.cs`.

- **v2.1** — Scelta della scheda video (CUDA + DirectX) da dropdown: barra
  (Auto = scheda più potente, altrimenti la scheda nominata). Enumera l'unione
  delle schede DirectX (DXGI) e dei device CUDA (ILGPU) e crea il
  device/accelerator sulla scelta; la preferenza è persistita in
  `settings.json` (`Gpu`) e applicata al cambio di motore. File:
  `DxMandelbrot.cs` (`AdapterNames`, `TryInitialize(..., adapterName)`),
  `GpuMandelbrot.cs` (`DeviceNames`, `TryInitialize(deviceName)`,
  `ResetAccelerator`), `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`
  (`lblGpu`, `cmbGpu`), `Settings.cs`, `AppVersion.cs`, `.csproj`.

- Vista iniziale sempre dall'insieme completo (la vista precedente non è più
  memorizzata in `settings.json`; il benchmark resta standard a 5000). File:
  `MandelbrotForm.Designer.cs`, `Settings.cs`, `MandelbrotForm.cs`.

- **v2.0** — Motore DirectX realtime: frattale a ogni frame (float, triangolo
  fullscreen), loop ~60 fps con pan/zoom immediati, AA 2x/4x/8x come
  supersampling dentro lo shader, salvataggio PNG dal backbuffer, fallback CPU
  automatico in caso di errore. Radio DirectX abilitata all'avvio. File:
  `DxMandelbrot.cs` (nuovo), `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`
  (`dxPanel`, `radioDx`), `.csproj` (dipendenze Vortice 3.8.3).

- Impostazioni persistite (`%APPDATA%\MandelbrotViewer\settings.json`):
  iterazioni auto/manuale e valore, palette, AA, motore preferito, ultima vista
  e posizione finestra; preferenza GPU rispettata all'avvio. File: `Settings.cs`
  (nuovo), `MandelbrotForm.cs`.

- Benchmark CUDA veloce: kernel solo-iterazioni (niente buffer |z|²) + buffer
  riusati per tutti i frame (niente alloc/transfer extra). File:
  `BenchmarkForm.cs`, `GpuMandelbrot.cs`.

- Menu File → Salva immagine con nome. File: `MandelbrotForm.Designer.cs`.

- Benchmark: aggiornamento UI limitato a ogni 3 s + report finale, così gli
  Invoke non falsano la misura (`BenchmarkProgress.ReportInterval`). File:
  `Mandelbrot.cs`, `GpuMandelbrot.cs`, `BenchmarkForm.cs`.

- Iterazioni totali tolte dal risultato del benchmark (restano nel dettaglio);
  `BenchmarkProgress` riporta anche i frame. File: `BenchmarkForm.cs`,
  `Mandelbrot.cs`, `GpuMandelbrot.cs`.

- Benchmark standard (8 s, iterazioni max, misura iter/s del motore selezionato
  con fallback CPU), risultato in grande (Giter/s, Miter/s…) con barra di
  avanzamento e annullamento. File: `BenchmarkForm.cs` + `.Designer.cs` (nuovi),
  `Mandelbrot.cs` (`BenchmarkCpu`), `GpuMandelbrot.cs` (`BenchmarkGpu`),
  `MandelbrotForm.*` (voce menu).

- Anteprima in pan (metà per lato) con upscale bilineare, render completo al
  rilascio; stato marcato "(anteprima)". File: `MandelbrotForm.cs`
  (`RenderAsync(preview)`, `Upscale`).

- Antialias con checkbox 1x/2x/4x/8x: risoluzione k volte maggiore + media RGB
  di ogni blocco kxk (stessa logica su CPU con `AverageBlock` e su GPU);
  iterazioni automatiche da radio a checkbox (default manuale); finestra default
  1152x720. Verificato con smoke test (CPU 1x vs 2x, GPU 2x vs CPU 2x). File:
  `Mandelbrot.cs`, `GpuMandelbrot.cs`, `MandelbrotForm.cs`,
  `MandelbrotForm.Designer.cs`. Formula iter auto `200 + 790·log10(zoom)`
  (~2000 a scala 1,95e-4, ~4550 a scala 1e-5).

- **v1.4** — Backend CUDA con ILGPU (precisione auto per scala < 1e-3), un
  thread per pixel, device più capiente in automatico, fallback CPU se niente
  GPU, radio CUDA abilitata all'avvio se la GPU risponde; stato e Informazioni
  mostrano motore/precisione/device. Refactor: `ColorFromEscape` condiviso tra
  CPU e GPU. Verificato con smoke test su RTX 5070 Ti (iterazioni GPU == CPU).
  Nuova formula iter auto `200 + 430·log10(zoom)` (~2000 a scala 1,95e-4); fix
  gruppi radio Manuale/CPU (pannelli contenitore separati). File:
  `GpuMandelbrot.cs` (nuovo), `Mandelbrot.cs`, `MandelbrotForm.cs`,
  `MandelbrotForm.Designer.cs`, `RenderEngine.cs`, `.csproj` (dipendenza ILGPU).

- Selezione motore con radio: enum `RenderEngine` + `RenderEngineInfo`; CUDA e
  DirectX disabilitati (roadmap v1.4/v2.0). Aiuto spostato nella `StatusStrip`,
  finestra default 1024x680, motore attivo mostrato nello stato. File:
  `RenderEngine.cs` (nuovo), `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`.

- Rinominato `Form1` in `MandelbrotForm` (vista preservata). File:
  `MandelbrotForm.cs`, `MandelbrotForm.Designer.cs`, `Program.cs`.
- Numero iterazioni aggiornato anche in modalità auto: mostra il valore
  automatico usato per il render. File: `Form1.cs`.

- Dropdown palette (Fuoco, Ghiaccio, Termico) con gradienti dedicati in
  `Mandelbrot.cs` (via l'enum `Palette`); barra superiore rifatta con
  `TableLayoutPanel` così numero iterazioni, label, radio e dropdown sono
  allineati verticalmente; radio Auto/Manuale per le iterazioni
  (`AutoIter = 150 + 150·log10(zoom)`, clamp 50–5000, numero disabilitato in
  auto); stato spostato in `StatusStrip` in basso. File: `Form1.cs`,
  `Form1.Designer.cs`, `Mandelbrot.cs`.
- Menu File (zone JSON, immagine con nome Ctrl+Shift+S) + menu Aiuto con
  Informazioni (versione e comandi); zoom con rotella sul cursore (focus
  automatico al passaggio mouse); tasti singoli R/S/+/- ignorati con Ctrl/Alt
  premuti. File: `Form1.cs` (`ViewZone`, `SaveZone`, `LoadZone`, `ShowAbout`),
  `Form1.Designer.cs` (`MenuStrip`).
- Pan con trascinamento a pulsante premuto (throttle 80 ms, soglia
  click/trascinamento 5 px). Rimossi zoom su rettangolo e zoom con rotella.
  File: `Form1.cs`, `Form1.Designer.cs` (testo aiuto aggiornato).
- Setup progetto (SDK user-level; publish self-contained in `pubblicato/` così
  non serve installare il Desktop Runtime; versione in sorgente (`AppVersion.cs`
  + `<Version>` nel csproj, mostrata nel titolo); setup progetto (AGENTS.md,
  TODO.md, SPECIFICHE.md, `.gitignore`, tracking nel repo `test/`).
- Visualizzatore iniziale in C# (WinForms): iterazioni configurabili,
  salvataggio PNG. File: `Form1.*`, `Mandelbrot.cs`.
