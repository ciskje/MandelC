# SPECIFICHE — Mandelbrot Viewer (MandelC#)

WinForms app (.NET 8, `net8.0-windows`) that renders the Mandelbrot set
(`z = z² + c`) with consistent smooth coloring across all engines: the tint
depends on the escape iterations smoothed with the final module of `z`, mapped
on 5 stops per palette with curve `t × 1.35 + 0.03` (saturated at 1, interior
black). Palettes: Fire, Ice, Thermal, Ocean, Purple, Desert, Forest (stops in
`PaletteColors`, single source for CPU and DirectX; CUDA receives them as
`GpuPaletteParams`).

## Interaction

- Left/right click: 2× zoom on the point; wheel: 0.7×/1.43× zoom on the
  cursor; drag or arrow keys: pan (click/drag threshold 5 px,
  throttle 80 ms, half-resolution preview without AA with bilinear upscale,
  full on release; arrow keys = 1/10 of the view, Shift = 1/100, not active on
  the iterations count). Window resize: immediate preview redraw
  (half resolution) on all engines, as DirectX; `pictureBox` uses
  `SizeMode.Zoom` to keep the aspect ratio during the transition.
- `R` = reset view, `S` = save PNG, `+`/`-` = ±50 iterations (50…50000).
- Auto checkbox: iterations 2000*(1+log10(1.5/half)) (half = scale/2, clamp 50-50000; ~1944 at the initial view scale 9.36, 10915 at the benchmark zone); the number shows the value used while staying disabled. Central formula in Mandelbrot.AutoIterForScale, also used by the zoom video. The benchmark uses the iterations computed with the auto formula at its scale (10915) (BenchmarkStandard.MaxIter)
- AA dropdown 1x/2x/4x/8x (1x = off): supersampling k×k and RGB averaging (cost ~k²)
  on CPU and CUDA; in DirectX inside the shader.
- File menu: load/save zone JSON (Ctrl+O / Ctrl+S: center, scale,
  iterations), save image (Ctrl+Shift+S), benchmark (Ctrl+B), Exit (Alt+F4).
- Generate menu: PNG screenshot
  (Ctrl+Shift+E: dialog from current view with View/Full HD/2K/4K/8K/
  Double 4K/Custom presets, validated dimensions 320…16384, selectable AA,
  offscreen render with the active engine, AA auto-reduced beyond 128 MPixel of
  samples), zoom video MP4 (Ctrl+Shift+V:
  from the current view to the set (error if already at the set; center
  proportional to the zoom so the starting point stays in frame;
  ease-out transition: fast at the start, slow at the end), 60…480 frames
  at 24/30/60 fps, selected AA (auto-reduced beyond 128 MPixel of samples),
  auto iterations per frame, ffmpeg H.264 (on worker, async stderr, kill on
  cancel, pad to even dimensions because the view often has odd sides) or
  PNG sequence if absent (Open button for the result)), Back to set (RealTime) (Ctrl+Shift+R: animation on the main view from the current zone to the set, 120 frames at 30 fps, Esc to stop)

- View menu: history Back/Forward (Alt+Left/Right, max 200 views: every
  zoom, pan, reset and load is committed) and named favorite zones
  (Ctrl+D, JSON in `%APPDATA%\MandelbrotViewer\zone\`, with jump and
  delete from the submenu); Julia mode (Ctrl+J, `z = z² + c` with c
  fixed, default −0.7 + 0.27015i persisted: click = fix c, R resets also
  c; zones include mode and c).
- Help menu: About (version, commands) and log/diagnostics (engine status,
  cards, errors with exact step, settings, events log with benchmark errors).
- Status bar: center, width, iterations, palette, engine (+AA/preview);
  fixed commands guide. Tooltips on controls and menu items.
- AppStarting cursor (arrow+clock) during async renders — the UI stays
  interactive — assigned recursively to all controls; Wait only for
  the synchronous CUDA init with frozen UI.
- Window 1152×720 (position/size persisted), version in the title.

## Engines

- CPU (always available): double, `Parallel.For` + `LockBits`, async with
  cancellation.
- CUDA (NVIDIA GPU, otherwise CPU fallback): float 32-bit or double
  64-bit kernel at choice (radio 32/64, default 64); coloring and AA downsampling
  computed on the GPU (only the final bitmap reaches the CPU); device and host
  buffers reused between frames; renders serialized (`RenderGate`). CPU always double,
  DirectX always float.
- DirectX 11 (Vortice, float, realtime ~60 fps with a 16 ms timer, immediate
  pan/zoom, CPU fallback on error): fullscreen triangle + pixel
  shader; Save PNG from the backbuffer. Beyond scale ~1e-3 float is not enough (state
  "[beyond float!]"): use CUDA double or CPU.
- GPU dropdown (visible only with GPU engines): unifies DXGI cards and CUDA devices;
  "Auto" = most capable CUDA device / default DirectX adapter; choice
  persisted and applied on engine change. Engine unavailable → reason
  shown in the status bar.

## Settings

`%APPDATA%\MandelbrotViewer\settings.json`: iterations (auto/manual + value),
palette, AA, engine, GPU, CUDA precision, window. The view is NOT saved
(always start from the full set). Tolerant load, validated save
(iterations clamped 50…50000).

## Benchmark

Standardized test identical for the engines (`BenchmarkStandard`): fixed zone
960×540, AA 1× meaning a grid of elementary samples without averaging
(960x540 = 0.52 MPixel per frame), iterations computed with the auto formula at the test scale (10915), center (-0.7499302568795561, -0.015139113925433963), scale 1.0453474311811176e-04 (half 5.226737155905588e-05), only counting
escape iterations (no coloring nor downsampling), 8 s budget, metric
frames × samples/frame / seconds.
- CPU: accumulates the iterations; CUDA: iterations-only kernel with reused buffers;
  DirectX: iterations-only shader on an in-memory offscreen render target
  (~2 MB), without window nor Present, with 4 event queries in a ring to count
  the frames actually completed (DWM and inter-GPU copy excluded: headless cards
  measure the pure shader). Anti-hang: VRAM estimated before allocation (error if insufficient), 60 s timeout per frame and device-removed detection: the Radeon iGPU goes into TDR (frame too heavy at 10915 iterations) and the run fails with an error instead of hanging).
- The window opens on its own, shows the colored 960×540 AA1x preview of the frame
  (rendered with the active engine before the measurement), percentage + MPixel/s
  intermediate every second, result in large and a 9-bar chart (measurement +
  best-of-3 history on the current zone: CUDA 5070 Ti 276.3/6.7 and 4070 SUPER 220.7/5.3 in 32/64-bit; DirectX 5070 Ti 292.4, 4070 SUPER 239.2 and Radeon 5.1; CPU 9900X 4.8 in double) During the DirectX test the realtime panel is
  hidden and the timer suspended, restored on close.
- CLI: `--bench-dx [card]` (all DXGI or one), `--bench-cuda [device]`
  (all devices, 32 + 64 bit), `--bench-cpu` (with model name),
  `--diag-dx [card]`, `--diag-gpu`; `--csv file` queues one row per run
  (timestamp, engine, device, precision, run, frame, seconds, MPixel/s),
  like the "Export CSV" button of the window. Note: the history is measured in Release (published); run.bat runs in Debug and in CPU renders ~2.7x less

## Files

- `MandelbrotViewer/Mandelbrot.cs` — CPU computation and benchmark.
- `MandelbrotViewer/Palette.cs` — `PaletteColors` (stops/gradients, single source).
- `MandelbrotViewer/GpuMandelbrot.cs` — CUDA backend (ILGPU 1.5.3): render
  kernel float/double + benchmark kernel iterations-only, `DeviceNames()`,
  `TryInitialize(deviceName)`, diagnostic `LastError`.
- `MandelbrotViewer/DxMandelbrot.cs` — DirectX 11 backend (Vortice 3.8.3):
  realtime shader + benchmark shader, swapchain on the panel, `Capture`,
  `RenderPreviewToBitmap`, offscreen headless benchmark (`EnsureDevice`,
  `TryInitializeHeadless`, `BeginBenchmarkOffscreen`,
  `RunBenchmarkFramesOffscreen`, `EndBenchmarkOffscreen`), `AdapterNames()`
  (WARP excluded by name), `TryInitialize(..., adapterName)`,
  `LastError`/`EnumerationError`, `ShortAdapterName`.
- `MandelbrotViewer/MandelbrotForm.cs` / `.Designer.cs` — main UI.
- `MandelbrotViewer/BenchmarkForm.cs` / `.Designer.cs` — benchmark window
  (auto-start, preview, history chart).
- `MandelbrotViewer/BenchmarkStandard.cs` — shared standard parameters GUI/CLI.
- `MandelbrotViewer/BenchmarkProgress.cs` — progress (`ReportInterval` 1 s;
  `TotalIters` meaningful only for CPU).
- `MandelbrotViewer/Diagnostics.cs` — CLI without UI
  (`--diag-dx`, `--diag-gpu`, `--bench-dx`, `--bench-cuda`).
- `MandelbrotViewer/Program.cs` — entry point + CLI dispatch.
- `MandelbrotViewer/RenderEngine.cs` — engines enum + display names.
- `MandelbrotViewer/Settings.cs` — `AppSettings` in JSON.
- `MandelbrotViewer/LogForm.cs` — log/diagnostics window.
- AppLog.cs - in-memory events log (last 200 lines, benchmark errors).
- `MandelbrotViewer/AppVersion.cs` — X.Y.Z version (short `Display` if Z=0).
- `MandelbrotViewer/app.ico` — exe + windows icon (from `icon2.png`, 16/32/48/256).
- Toolbar: two `FlowLayoutPanel` (row 0: buttons + iterations + palette + AA;
  row 1: engine + precision + GPU), controls grouped to the left.
- Root: `run.bat` (runs the current version via `dotnet run`, forwards the
  arguments), `publish.bat` / `publish.ps1` (self-contained publish
  single-file in `pubblicato/`, ~158 MB), `AGENTS.md`, `TODO.md`,
  `CHANGELOG.md`, `.gitignore` (excludes `bin/`, `obj/`, `pubblicato/`, `*.user`).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Technical notes

- `dotnet` only in `~\.dotnet`, not in PATH: use the full path.
- Audit 2026-09-06: SDK 8.0.424 + runtime 8.0.30 (latest), ILGPU 1.5.3 and
  Vortice 3.8.3 (latest stable) — no safe updates available;
  target stays `net8.0-windows` (LTS until 2026-11-10, then evaluate migration
  to .NET 10 LTS).
- The CUDA backend requires an NVIDIA GPU + driver on the target machine (the
  CUDA toolkit is not needed at runtime); without a GPU the app uses the CPU automatically.
