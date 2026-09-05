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
