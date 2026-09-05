# TODO — MandelC#

Ogni richiesta dell'utente diventa una voce qui. Stato: `pending` → `completed`/`cancelled`.

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
