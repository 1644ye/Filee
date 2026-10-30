# PR #815 Windows animation comparison

Recorded on Windows on 2026-10-30. Left: upstream `79b227d`; right: PR `eaaea17`.

[Download / play the 24-second recording](windows-before-after.mp4)

The harness compiles the actual DonutMenu partials and DonutGeometry from each commit into separate namespaces. Both run side by side in one native Avalonia 12.1.3 window, with identical nine-slice labels and the same scheduled calls. No animation code was reimplemented. Source is included in `harness/`.

Sequence: opening, a flip, two flips 80 ms apart, closing, closing 100 ms into opening, and reduced-motion opening/flip. The sequence loops, so the recording may start mid-cycle.

Both use Skia software rendering because GDI window capture returned black with GPU rendering. Capture requested 60 fps; encoded average is approximately 52.67 fps. This is visual evidence, **not** a GPU performance benchmark. It exercises the controls directly, **not** the global Ctrl-drag hook or RadialController window-show integration. macOS was not available.

Run: `dotnet run --project harness/Comparison.csproj` (Windows, .NET 10). Close the window after viewing.

## Validation

Head under test: `110e904` (the recording uses `eaaea17`; the only subsequent change updates existing flip assertions for the intentional zero-width edge).

- `dotnet format Filee.slnx --verify-no-changes --no-restore`: exit 0. Workspace loading emitted a warning; no formatting changes were requested.
- Focused `DonutAnimationTests`, `DonutFlipTests`, and `MotionTests`: **55 passed, 0 failed**.
- Full `Filee.App.Tests`: **1,627 passed, 0 failed, 3 skipped**, 1,630 total (12.97 minutes). Two Explorer menu checks require the optional native package build; the symlink test is skipped because this machine does not allow creating its symbolic link. See `test-results.json`.

The first focused run exposed 10 legacy assertions requiring a nonzero minimum width at the edge-on handoff. The updated assertions require exact zero at the seam, visible expansion on the next sampled frame, and completion at the original deadline. No production code was changed during this validation.

