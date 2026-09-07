- [x] v2.17.3: Toolbar FlowLayoutPanel (controlli ammassati, niente spazi colonne) — completed
- [x] v2.17.2: Icona app da icon2.png — completed
- [x] v2.17.1: Fix resize finestra (stretch DX, CUDA/CPU non ridisegnavano) — completed
- [x] v2.17.0: Colore palette gamma (allineamento Python) — completed
- [x] v2.16.0: Zoom Real Time sulla finestra principale — completed
- [x] v2.16.0: Menu Genera principale (sposta Esporta da File) — completed
- [x] Domanda: CUDA vs DX, colpa di ILGPU? (analisi) — completed (nessun cambio codice)
- [x] v2.15.10: Fix poll DX stretto (non V-Sync) + storici — completed
- [x] Discrepanza bench CPU CLI vs UI: era Debug vs Release, avvia resta Debug — completed
- [x] v2.15.9: Benchmark standard a AA1x + ricalcolo storici — completed
- [x] Test AA1x su Radeon: regge a 4,8 MPixel/s (fuori standard), revert — completed
- [x] Test AA4x su Radeon: TDR anche a AA4x, revert, resta lo skip — completed
- [x] Domanda: bench AA4x per Radeon? (analisi) — completed (nessun cambio codice)
- [x] v2.15.8: Bench DX skip iGPU + misura DX 4070 SUPER — completed
- [x] v2.15.7: Errori benchmark DX nel log/diagnostica — completed
- [x] Pubblicazione v2.15.6 + riverifica Radeon — completed
- [x] v2.15.6: Benchmark DX anti-blocco (VRAM check + timeout 60s + device-removed) — completed
- [ ] Crash app (dialog eccezione, solo coda assembly visibile) — pending
- [x] v2.15.5: Ricalcolo storici benchmark via CLI + tabella nel codice — completed
- [x] v2.15.4: Benchmark rinomina pulsante CSV + Chiudi sempre a destra — completed
- [x] v2.15.3: Benchmark mi calcolata via auto (non fissa) = 10915 — completed
- [x] half=5.226737155905588e-05 -> scala? (info) — completed (nessun cambio codice)
- [x] v2.15.2: Zona benchmark cx=-0.7499302568795561 cy=-0.015139113925433963 half=5.226737155905588e-05 mi=10915 — completed
- [x] Zona del benchmark (info) — completed (nessun cambio codice)
- [x] v2.15.1: Formula iter auto 2000*(1+log10(1.5/half)) clamp 50-50000 + valore benchmark — completed

# TODO — MandelC#

Ogni richiesta dell'utente diventa una voce qui. Stato: `pending` → `completed`/`cancelled`.

- [x] v2.5.23: Revisione SPECIFICHE.md su specifiche reali, pulizia note extra dagli .md — `completed`

- [x] Pubblicazione v2.5.22 + commit — `completed`
- [x] v2.13.2: Icona dell'exe e delle finestre (render Mandelbrot in .ico) — `completed`

- [x] v2.14.1: Palette a 2-3 colori base (Foresta marrone→verde) — `completed`
- [x] Commit v2.14.0 nel repo annidato (6492e75, solo locale) — `completed`
- [x] v2.14.0: Fix encode video (exit -542398533, "no packets": lati dispari) — `completed`
- [x] v2.15.0: Submenu Esporta (Screenshot + Video) + selettore AA proprio nel video — `completed`
- [x] v2.14.0: Export PNG con preset, AA selezionabile e custom validato — `completed`
- [x] Pubblicazione v2.13.1 — `completed`

- [x] v2.6.0: Pan con frecce direzionali (Shift = passo fine) — `completed`
- [x] v2.7.0: Quattro palette fisse aggiuntive — `completed`
- [x] v2.8.0: --bench-cpu con nome modello CPU negli storici — `completed`
- [x] v2.9.0: Export CSV dei risultati benchmark — `completed`
- [x] v2.10.0: Cronologia zone (avanti/indietro) + preferiti nominati — `completed`
- [x] v2.11.0: Export PNG ad alta risoluzione — `completed`
- [x] v2.12.0: Modalità Julia — `completed`
- [x] v2.13.1: Hang export video + direzione invertita + centro proporzionale allo zoom + transizione ease-out — `completed`

- [x] Installazione ffmpeg 7.1 in %USERPROFILE%\ffmpeg + PATH utente (via imageio-ffmpeg/PyPI: gyan.dev andava a 100 KB/s) — `completed`

- [x] v2.13.0: Video zoom MP4 via ffmpeg — `completed`

- [ ] Upgrade a .NET 10 LTS (piano pronto, da eseguire: SDK side-by-side, retarget, riverifica motori+bench, bump v2.6.0) — `pending`

- [x] Audit aggiornamenti sicuri (SDK/runtime/NuGet): tutto già all'ultimo stabile, nessun cambio — `completed`

- [x] v2.5.22: AppStarting (freccia+clessidra) al posto di Wait dove l'UI resta interattiva — `completed`

- [x] v2.5.21: Cursore wait non visibile durante i render lunghi se il mouse è sull'immagine (pictureBox fuori dalla lista di SetBusyCursor) — `completed`

- [x] v2.5.20: Benchmark DirectX offscreen senza Present (headless + event query) così le schede senza monitor non pagano la copia inter-GPU — `completed`

- [x] v2.5.19: Test CUDA (8sx3) e inserimento risultati negli storici del benchmark — `completed`

- [x] v2.5.18: Triplo test DirectX per ogni scheda (3×8 s, best per scheda) via `--bench-dx` e storico per scheda nel grafico benchmark — `completed`

- [x] v2.5.17: DXGI: AdapterNames restituiva elenco vuoto (overflow su GPU >4GB via PointerUSize→uint); elencare davvero le schede DirectX — `completed`

- [x] v2.5.16: DirectX: applicare davvero la scheda scelta dal dropdown (creare il device sull'adapter DXGI selezionato) — `completed`

- [x] v2.5.15: Revisione e pulizia del codice (codice morto, deduplicazione, riordino file) — `completed`

- [x] v2.5.14: Confronto teorico prestazioni RTX 4070 SUPER vs RTX 5070 Ti (tensor, memoria, FP32/FP64) — `completed`


- [x] v2.5.13: Fix benchmark DirectX: risultati irrealistici (resize swapchain non avvenuto) e preview nera (CopyResource invertito) — `completed`

- [x] v2.5.12: Preview colorata del benchmark anche per DirectX e pannello DX nascosto durante il test — `completed`
- [x] v2.5.11: Mostrare il primo frame del benchmark anche in CUDA (e CPU) — `completed`
- [x] v2.5.10: Standardizzare il benchmark tra i motori (960x540 AA8x senza media dei campioni, DirectX senza v-sync) — `completed`
- [x] v2.5.9: Aggiornare i riferimenti storici del grafico benchmark — `completed`
- [x] v2.5.8: Mostrare subito 0% e aggiornare lo stato ogni secondo — `completed`
- [x] v2.5.7: Spostare benchmark DirectX su worker e mostrare MPixel/s intermedi — `completed`
- [x] v2.5.6: Rendere visibili gli aggiornamenti intermedi del benchmark su tutti i motori — `completed`
- [x] v2.5.5: Rimuovere la barra di avanzamento dal benchmark — `completed`
- [x] v2.5.4: Mostrare percentuale, correggere asse grafico e consentire il primo ridisegno — `completed`
- [x] v2.5.3: Sistemare valore benchmark tagliato e usare barre orizzontali — `completed`
- [x] v2.5.2: Aggiungere launcher BAT per provare la versione corrente senza publish — `completed`
- [x] v2.5.1: Correggere il riferimento CUDA RTX 5070 Ti nel grafico benchmark — `completed`
- [x] v2.5.0: Avviare subito il benchmark e mostrare il grafico dei riferimenti prestazionali — `completed`
- [x] v2.4.1: Correggere le variazioni cromatiche CUDA ad alte iterazioni — `completed`
- [x] v2.4.0: Ripristinare lo smooth coloring coerente su CPU, CUDA e DirectX — `completed`
- [x] v2.3.15: Rimuovere tutti i launcher `avvia.*` — `completed`
- [x] v2.3.14: Correggere la selezione iniziale blu nella finestra log — `completed`
- [x] v2.3.13: Schiarire la luminosità delle palette dopo la rimozione dello smooth coloring — `completed`
- [x] v2.3.12: Aggiungere benchmark reale del motore DirectX — `completed`
- [x] v2.3.11: Correggere testo benchmark DirectX fallback CPU e indicazione precisione — `completed`
- [x] v2.3.10: Uniformare la colorazione CPU a CUDA/DirectX usando le iterazioni — `completed`
- [x] v2.3.9: CUDA/DX: allineare la colorazione e mantenere AA/downsampling GPU; fix runtime — `completed`
- [x] v2.3.6: Ottimizzare al massimo il render GPU e il benchmark CUDA — `completed`
- [x] v2.3.5: Fix DirectX: usare `DriverType.Unknown` quando D3D11CreateDevice riceve un adapter DXGI — `completed`
- [x] v2.3.4: DirectX: log con lo step esatto che fallisce; swapchain FlipDiscard→FlipSequential + SampleDescription esplicito — `completed`
- [x] v2.3.3: voce di menù Aiuto → "Mostra log / diagnostica..." con stato motori (DirectX/CUDA), schede, errori e impostazioni — `completed`
- [x] v2.3.2: all'avvio, se un motore GPU (DirectX/CUDA) non è disponibile, mostrare il motivo nella barra di stato — `completed`
- [x] DirectX: verificare dal log lo step esatto dopo FlipSequential; se ancora fallisce, valutare render su texture + blit a bitmap (fallback) — `cancelled` (obsoleto: DirectX operativo in modo stabile dalla v2.3.5)
- [x] v2.3.1: nascondere il dropdown GPU (e la label) quando è selezionato il motore CPU — `completed`
- [x] v2.3: radio "32"/"64" per la precisione del motore CUDA (float/double) — `completed`
- [x] v2.2: ToolTip sui controlli e sulle voci del menu che ne spiegano la funzione — `completed`
- [x] Fix: cursore wait visibile anche col mouse sopra un controllo (es. dropdown AA) durante il render — `completed`
- [x] Dropdown GPU disabilitato se è selezionato il motore CPU — `completed`
- [x] v2.1: più schede video, dropdown per scegliere quale usare (CUDA + DirectX) — `completed`
- [x] Messaggi commit che partono col numero di versione (regola in AGENTS.md) — `completed`
- [x] Parti sempre dall'insieme (non memorizzare la zona tra un lancio e l'altro) — `completed`
- [x] Limite massimo iterazioni a 50000 — `completed`
- [x] Iter auto: almeno 2000 iterazioni a scala 1,95e-4 — `completed`
- [x] Fix: radio Manuale e CPU nello stesso gruppo (servono contenitori separati) — `completed`
- [x] v2.0 motore DirectX realtime (shader HLSL, 60fps) — `completed`
- [x] Mantenere i parametri cambiati tra un lancio e l'altro (settings.json) — `completed`
- [x] Togliere le iterazioni totali dal benchmark — `completed`
- [x] Velocizzare CUDA (il 3x sulla CPU è poco) — `completed`
- [x] Sostituire il tasto "Salva PNG" (usato poco) con il Benchmark — `completed`
- [x] Benchmark: aggiornamento UI ogni 3 secondi per non falsare il test — `completed`
- [x] Benchmark in AA 8x — `completed`
- [x] Risultato benchmark in pixel/s — `completed`
- [x] Benchmark standard 8s ad alte iterazioni con iter/s in grande — `completed`
- [x] AA senza checkbox: solo dropdown (1x = disabilitato) — `completed`
- [x] Durante il pan: niente AA e rendering a 1/4 di risoluzione (full al rilascio) — `completed`
- [x] Iter auto: almeno 4500 iterazioni a scala 1e-5 — `completed`
- [x] Antialias 1x/2x/4x/8x con checkbox (supersampling + media dei pixel) — `completed`
- [x] Iterazioni automatiche da radio a checkbox — `completed`
- [x] Roadmap v1.4: backend CUDA con ILGPU, float poi double, auto + fallback CPU — `completed`
- [x] Radio button per selezionare il motore di rendering — `completed`
- [x] Rinominare Form1 in MandelbrotForm — `completed`
- [x] Numero iterazioni aggiornato anche in modalità auto (se disabilitato) — `completed`
- [x] CHANGELOG.md separato + Esci nel menu File — `completed`
- [x] Dropdown palette (fuoco, ghiaccio, termico) — `completed`
- [x] Allineare numero iterazioni con label + radio iterazioni auto da zoom — `completed`
- [x] Menu File (carica/salva zona JSON, salva immagine con nome), menu Help con info, zoom con rotella sul mouse — `completed`
- [x] Zoom solo col click del mouse, tenendo schiacciato si fa pan/scroll — `completed`
- [x] Creare in C# un visualizzatore semplice dell'insieme di Mandelbrot — `completed`
- [x] Spiegare come lanciarlo — `completed`
- [x] Creare uno script per il lancio (`avvia.bat` + `avvia.ps1`) — `completed`
- [x] Risolvere richiesta di installazione .NET Desktop Runtime (publish self-contained in `pubblicato/`) — `completed`
- [x] Chiarire se AGENTS.md viene letto a sessione vuota — `completed`
- [x] Setup progetto: git + versione in sorgente X.Y.Z + AGENTS.md + TODO.md + SPECIFICHE.md — `completed`

