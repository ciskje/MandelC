# QUESTIONS — MandelC#

Generic questions and answers that do not concern the program source.
Moved out of `TODO.md` so the todo stays strictly about the program.
Program-related analysis (AA dithering, ILGPU vs DirectX, Radeon bench,
zone constants) stays in `TODO.md` with its verdicts in `SPECS.md`.

- **What SMs and warps are** — answered. SM (Streaming Multiprocessor) is the
  GPU's compute unit (the 5070 Ti has 70, each with 128 CUDA cores, scheduler,
  registers and L1); a warp is the indivisible crew of 32 threads that always
  executes the same instruction in lock-step. Slowest thread of each warp sets
  the warp cost (divergence); each SM hosts a limited number of resident warps,
  so a frame runs in successive waves.

- **How the benchmark splits across the 5070 Ti compute units** — answered.
  One frame is 960x540 = 518,400 independent samples (one thread / pixel-shader
  invocation each), i.e. 16,200 warps of 32 over 70 SMs (~5 waves per SM).
  Tensor cores, RT cores and ROPs stay idle; FP64 units stay idle in 32-bit
  mode, which is why the 64-bit kernel measures ~41x less. The test is pure
  ALU and compute-bound; memory traffic is negligible.

- **Theoretical performance comparison RTX 4070 SUPER vs RTX 5070 Ti** —
  answered (tensor, memory, FP32/FP64). Note only; the full text was removed
  from `SPECS.md` in v2.5.23.

- **ffmpeg 7.1 installation in %USERPROFILE%\ffmpeg + user PATH** —
  done (via imageio-ffmpeg/PyPI, gyan.dev was at 100 KB/s). Environment setup
  for the zoom-video MP4 export, no source change.

- **Safe updates audit (SDK/runtime/NuGet)** — done, all already at latest
  stable, no changes.

- **Explain how to launch the app** — answered (published exe in `published/`
  or `dotnet run`; see README).

- **Clarify if AGENTS.md is read on empty session** — answered.

- **Generic questions in .md files not about the program source** — answered;
  this move itself (generic entries collected here).

- **Where the blocks/warps split is in the program** — answered: nowhere, it
  is implicit. Our code only decides the thread count (one per sample); the
  blocks/warps slicing is done by the ILGPU runtime (CUDA) and by the
  D3D11 driver + rasterizer (DirectX). CUDA: `LoadAutoGroupedStreamKernel`
  in `GpuMandelbrot.cs` ("auto grouped" = ILGPU picks the block size),
  launched with `count = W*H` threads, one `Index1D` per sample. DirectX:
  `Draw(3, 0)` of the fullscreen triangle in `DxMandelbrot.cs` generates one
  pixel-shader invocation per pixel; the hardware groups them into
  32-thread warps. The only parallelism knobs we control are the frame-level
  queue depths (CUDA adaptive batch, DirectX in-flight ring), not the
  intra-frame blocks/warps.
