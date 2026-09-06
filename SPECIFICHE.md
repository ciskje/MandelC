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
disponibile, CUDA se c'è una GPU NVIDIA (radio "32"/"64" per la precisione:
float 32-bit o double 64-bit, v2.3; prima auto per scala; altrimenti fallback
CPU); DirectX realtime (v2.0, shader HLSL, float, loop
~60 fps, pan/zoom immediati, fallback CPU in caso di errore; se un motore GPU
non è disponibile all'avvio, il motivo è mostrato nella barra di stato (v2.3.2)
e nel log con lo step esatto dell'inizializzazione (v2.3.4). Dropdown GPU
(v2.1): con più schede video si sceglie quale usare — "Auto" = la più potente,
altrimenti la scheda nominata; unisce le schede DirectX (DXGI) e i device
CUDA (ILGPU); la scelta è persistita e applicata al cambio di motore; il
dropdown è nascosto quando il motore attivo è la CPU (v2.3.1: compare solo con
CUDA/DirectX). Menu File: carica zona
(Ctrl+O) e salva zona (Ctrl+S) in formato JSON (centro, scala, iterazioni),
salva immagine con nome (Ctrl+Shift+S), benchmark standard 8s (Ctrl+B) con
pixel/s in grande, Esci (Alt+F4).
Menu Aiuto: finestra Informazioni con versione e riepilogo comandi, e
"Mostra log / diagnostica" (v2.3.3) con stato dei motori, schede, errori e
impostazioni (utile per capire perché un motore è disabilitato). Barra di
stato in basso con centro, larghezza, iterazioni e palette. Ogni controllo della
barra e le voci dei menu hanno un ToolTip esplicativo (v2.2, si mostra al
passaggio del mouse); durante il calcolo il cursore "atteso" è imposto su form
e controlli di input (v2.2), così è visibile anche sopra un dropdown.
Rendering parallelo (`Parallel.For` + `LockBits`), asincrono con cancellazione,
anteprima in pan (1/4 risoluzione, no AA, upscale bilineare, full al rilascio),
throttle del pan (80 ms) e anti-rimbalzo sul resize.

Benchmark standardizzato (v2.5.10): tutti i motori misurano lo stesso lavoro —
zona fissa 960x540, supersampling 8x inteso come griglia di **campioni elementari
senza media** (7680x4320 pixel · 33,18 MPixel per frame), solo conteggio delle
iterazioni di fuga (niente colorazione/smooth né downsampling). Il benchmark
DirectX usa uno shader dedicato solo-iterazioni e presenta con `Present(0)`
(senza v-sync) per non restare tappato al refresh del monitor; la swapchain è
ingrandita temporaneamente alla griglia di campioni e ripristinata al termine.
Il metro finale è `frame × 960 × 540 × 8 × 8 / secondi` (campioni/s), identico
per CPU, CUDA e DirectX.

Il benchmark mostra il primo frame della zona testata (v2.5.11/2.5.12: anteprima
colorata 960x540 AA1x mostrata nel box della finestra Benchmark per tutti i
motori: CUDA/CPU renderizzano un Bitmap col motore attivo, DirectX rende
offscreen con lo shader normale via `RenderPreviewToBitmap`); è renderizzata
prima del loop per non interferire con la misura. Durante il test DirectX il
pannello DX della finestra principale viene nascosto (non mostra più il frame
grigio dei campioni) e ripristinato alla chiusura del benchmark.

Fix v2.5.13: il benchmark DirectX dava risultati irrealistici perché
`BeginBenchmark` impostava `_benchmarking` prima di chiamare `Resize`, che con
il flag attivo ignora la richiesta — la swapchain restava a dimensione pannello
mentre il metro creditava 33,18 MPixel/frame (misura gonfiata ~30-40×); ora il
resize avviene prima del flag. La preview DirectX usciva nera perché
`ReadTextureToBitmap` invertiva gli argomenti di `CopyResource` (copiava lo
staging vuoto sulla texture renderizzata): corretto in `CopyResource(staging,
source)` (ripristina anche Salva PNG) e texture di preview in `B8G8R8A8_UNorm`
per evitare canali R/B invertiti rispetto a `Format32bppArgb`.

Confronto teorico (v2.5.14, fonti TechPowerUp/Wikipedia/NVIDIA): RTX 4070
SUPER (AD104: 7168 core CUDA, 224 tensor 4ª gen, 56 RT core, boost 2,475 GHz,
12 GB GDDR6X 192-bit, 504 GB/s, 220 W) vs RTX 5070 Ti (GB203: 8960 core CUDA,
70 SM, 280 tensor 5ª gen, 70 RT core, boost 2,452 GHz, 16 GB GDDR7 256-bit,
896 GB/s, 300 W). Differenze: FP32 35,5 vs 43,9 TFLOPS (+24%), FP64 0,55 vs
0,69 TFLOPS (+24%, entrambe 1/64), banda memoria +78% ma irrilevante per il
benchmark (scrive ~132 MB/frame, kernel compute-bound in registri), tensor
core: AI TOPS marketing 616 (FP8 sparse) vs 1406 (FP4 sparse) — formati
diversi, a parità di formato il guadagno è ~+25%; il kernel Mandelbrot non
usa tensor core. Atteso sul benchmark di questa app ~+20-25%; raster gaming
(relative performance TechPowerUp) ~+37% perché lì contano anche ROP (96 vs
80) e fillrate.
Refactor v2.5.15 (nessun cambiamento funzionale): palette e gradienti spostati
da `Mandelbrot.cs` in `Palette.cs` (`PaletteColors`: fonte unica degli stop per
CPU/DirectX, ricevuti da CUDA tramite `GpuPaletteParams`) con commenti di
allineamento nelle tre implementazioni della colorazione (CPU/CUDA/HLSL);
`BenchmarkProgress` in file proprio; diagnostica CLI `--diag-dx`/`--diag-gpu`
spostata da `Program.cs` a `Diagnostics.cs`; in `DxMandelbrot.cs` estratti
`BuildParams`/`DrawFrame` condivisi da Render/RenderBenchmark/
RenderPreviewToBitmap, indentazione sistemata e `System.Drawing.Bitmap` →
`Bitmap`; rimossa la variabile `total` morta nel benchmark CUDA (il metro è a
frame, `TotalIters` = 0 come per DirectX); rinominato l'handler del menu "Salva
immagine" in `SaveImageItem_Click`; rimossi wrapper `RenderGpu`, doppio dispose
del bitmap precedente, usings ridondanti e commenti obsoleti (`RenderEngine.cs`).

Fix v2.5.16: il dropdown GPU mostrava le schede DirectX ma `TryInitialize`
creava sempre il device sull'adapter predefinito (`D3D11CreateDevice` con
adapter null): la scelta non aveva alcun effetto. Ora con una scheda richiesta
si enumera l'adapter DXGI per nome dallo stesso factory della swapchain e il
device viene creato su quell'adapter con `DriverType.Unknown` (obbligatorio
quando si passa un adapter); il motivo dell'eventuale fallimento finisce in
`LastError` con lo step esatto ("Scheda video non trovata: …" se il nome non
corrisponde). `Diagnostics.DiagDx` accetta il nome scheda come secondo
argomento CLI per verificarla senza UI (`--diag-dx "Nome Scheda"`).



Ottimizzazione v2.3.6: il render CUDA riusa i buffer device e host tra frame;
il benchmark rispetta la precisione CUDA selezionata e misura il kernel senza
trasferire il vettore delle iterazioni alla CPU.

Ottimizzazione v2.3.7: il kernel CUDA calcola anche colorazione palette e
downsampling AA; alla CPU viene trasferito solo il bitmap finale. CUDA e
DirectX usano la stessa interpolazione della palette basata sulle iterazioni.

Fix v2.3.8: verificato il kernel CUDA a runtime su RTX 5070 Ti (`Ready: True`);
DirectX usa l’adapter hardware predefinito senza leggere la descrizione DXGI,
evitando l’overflow del wrapper Vortice.

Fix v2.3.10: CPU, CUDA e DirectX usano la stessa colorazione basata sulle
iterazioni e la stessa interpolazione delle palette.

Fix v2.3.11: nel benchmark DirectX, non misurabile direttamente, il testo
indica correttamente il fallback CPU senza mostrare una precisione CUDA.

Fix v2.3.12: il benchmark DirectX esegue direttamente lo shader realtime per
otto secondi e misura frame/s e pixel/s; usa CPU solo se DirectX non è pronto.

Fix v2.3.13: le palette usano una curva di luminosità comune, `t × 1,35 + 0,03`,
per compensare la rimozione dello smooth coloring e mantenere i motori coerenti.

Funzionalità v2.4.0: ripristinato lo smooth coloring su CPU, CUDA float/double
e DirectX usando il modulo finale di `z`. CUDA usa `ILGPU.Algorithms` con
`EnableAlgorithms()` nel context builder per rendere disponibile `XMath.Log2`
al compilatore PTX.

Fix v2.4.1: il colorizer CUDA limita la coordinata della palette a `[0,1]`,
come CPU e DirectX, evitando l'extrapolazione dei canali RGB oltre 255.

Funzionalità v2.5.0: la finestra Benchmark avvia il test automaticamente in
`Shown` e visualizza un grafico a barre con il risultato misurato e i riferimenti
5070 Ti CUDA a 5880 MPixel/s e AMD 9900X a 80 MPixel/s.

Fix v2.5.1: il riferimento della 5070 Ti CUDA nel grafico è 5880 MPixel/s.

Strumento v2.5.2: `avvia.bat` esegue la versione corrente tramite `dotnet run`
senza richiedere una nuova pubblicazione.

Fix v2.5.3: il valore principale del benchmark usa una dimensione leggibile e
il grafico prestazionale usa barre orizzontali.

Fix v2.5.4: durante gli aggiornamenti del benchmark viene mostrata solo la
percentuale di completamento; una pausa iniziale consente il primo ridisegno
della finestra e il grafico riserva spazio alle etichette dell’asse.

Fix v2.5.5: rimossa la barra di avanzamento grafica; il benchmark mostra solo
la percentuale testuale.

Fix v2.5.6: la percentuale intermedia viene aggiornata e ridisegnata su CPU,
CUDA e DirectX; DirectX usa una callback sincrona per non posticipare il testo
fino al completamento.

Fix v2.5.7: il benchmark DirectX viene eseguito su un worker separato mentre
il timer DirectX principale è sospeso; la UI mostra durante il test percentuale
e MPixel/s intermedi.

Fix v2.5.8: il benchmark mostra subito `0%` prima dell’avvio effettivo e aggiorna
lo stato ogni secondo.

Riferimenti storici v2.5.9: CUDA RTX 5070 Ti a 5940 MPixel/s, DirectX RTX 5070
Ti a 1750 MPixel/s e CPU a 30 MPixel/s.

Fix v2.3.14: la finestra log apre senza selezione automatica del testo e con il
focus sul pulsante di chiusura.

Fix v2.3.5: la creazione del device DirectX usa `DriverType.Unknown` quando
viene passato un adapter DXGI esplicito; `DriverType.Hardware` causava
`E_INVALIDARG` e lasciava il motore DirectX disabilitato.

## File

- `MandelbrotViewer/Mandelbrot.cs` — calcolo + enum `Palette` (Fuoco, Ghiaccio,
  Termico) con gradienti e smooth coloring.
- `MandelbrotViewer/MandelbrotForm.cs` / `MandelbrotForm.Designer.cs` — UI: zoom/pan,
  menu File (zone, PNG, benchmark, Esci) e Aiuto, dropdown palette e AA,
  checkbox iterazioni auto, radio motore, dropdown GPU (scelta scheda video),
  radio precisione CUDA 32/64 (v2.3),
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
   float/double (un thread per pixel, solo fuga), precisione a scelta dell'utente (radio 32/64, v2.3; prima auto per scala < 1e-3),
  fallback CPU, `LastError` diagnostico. `DeviceNames()` elenca i device CUDA;
  `TryInitialize(deviceName)` usa la scheda scelta (auto = la più capiente);
  `ResetAccelerator` riusa il contesto CUDA al cambio di scheda.
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
