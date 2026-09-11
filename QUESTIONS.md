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

- **What PTX is** — answered. PTX (Parallel Thread Execution) is NVIDIA's
  virtual GPU assembly: an intermediate, forward-compatible instruction set
  between CUDA C++ (`.cu`) and the real per-architecture machine code (SASS).
  The chain is `.cu` → PTX (via `nvcc` or NVRTC, sharing the same backend)
  → SASS (via the driver's JIT compiler at module load, cached on disk).
  One PTX file runs on every GPU because the driver translates it for the
  actual chip (sm_89, sm_120, ...); SASS would need one binary per chip.
  Think of it as the GPU equivalent of .NET IL or Java bytecode. This app
  embeds one `compute_75` PTX (`Cuda/mandelbrot.ptx`, built from
  `Cuda/mandelbrot.cu`) and loads it with `cuModuleLoadDataEx`; it is
  compiled with `--fmad=false` so the rational arithmetic matches the CPU
  bit for bit.

- **What --fmad=false means** — answered. FMA (fused multiply-add) is one
  GPU instruction computing `a*b+c` with a single rounding instead of two
  (round after mul, round after add). With fusion on (`nvcc` default, and our
  first PTX), the compiler silently merges separate `*` and `+` operations
  of the escape loop (e.g. `2*zx*zy+ccy`) into FMAs: faster (one instruction
  instead of two) and in isolation more accurate, but bit-different from the
  CPU, where the .NET JIT evaluates mul and add separately. `--fmad=false`
  forbids that contraction, so every GPU operation rounds exactly like its
  CPU counterpart and the render comes out pixel-identical (verified: 0 diffs
  on all scenes). Measured cost on the double-precision kernel: ~13%
  throughput (6.7 → 5.8 MPixel/s on the 5070 Ti); the float kernel still
  gained from the explicit block tuning.   Deliberate trade: identity with the
  CPU over raw speed, same guarantee as the CPU SIMD path (v2.19.4).
  Measured per-precision cost on the 5070 Ti (same PTX, flag on/off):
  32-bit 295 → 291.3 MPixel/s (−1.3%), 64-bit 6.7 → 5.8 (−13%).
  So the flag does affect 32-bit, but barely; the double loop pays most.
  Decision v2.22.2: FMA enabled on both precisions (user choice, speed over
  bit-identity). Measured vs-CPU pixel difference with FMA on: ≤0.7% of
  pixels on all scenes except 32-bit deep zoom, where chaos amplification
  reaches ~12% sparse flips; official history bars remeasured accordingly
  (5070 Ti 293.7/6.7, 4070 SUPER 246.2/5.3).

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
