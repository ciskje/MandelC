# TODO — MandelC#

Ogni richiesta dell'utente diventa una voce qui. Stato: `pending` → `completed`/`cancelled`.

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
