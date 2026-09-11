# UI and UX refresh, 2026-09-11

Owner request: improve the completed OpenCode game's UI and usability; commit locally at meaningful checkpoints, then push exactly once when finished.

Baseline: main = origin/main at 7c7e51f. Unity 6000.6.0f1 running on port 7800. The Editor initially had SampleScene open; opened the saved PaketPanik scene after checking the previous scene was not dirty.

Pre-existing changes to preserve and exclude from commits: Assets/PaketPanik/Art/PaketPanikMarkerLibrary.asset; Assets/XR/UserSimulationSettings/SimulationEnvironmentAssetsManager.asset; ProjectSettings/ProjectSettings.asset.

Observed source problems: welcome combines connection forms with all guidance; fixed-size panels; low contrast secondary buttons; crowded HUD; incorrect minute rounding; loot percentage is seconds times 100; no leave confirmation; no connection busy state; help is an unscrollable wall of text; network interface enumeration runs every frame.

Direction: parcel-label visual identity, original procedural cardboard mascot, Inter type, separate welcome/join, readable connection and calibration steps, compact camera HUD, thumb-friendly hold controls, normalized progress, dedicated results, scrollable help, clear errors and retry paths. Keep core rules and host authority intact.

Next: implement presentation, compile, inspect portrait and compact layouts, exercise UI states, run regression tests, rebuild APK, finish portable handoff, local checkpoint commits, one push. Editor screenshots are UI evidence only. Real two-phone AR/LAN and independent-player QA remain required for itch.io readiness.

## Presentation checkpoint, 17:38 WIB

- Added PanicInterface (welcome/join/lobby/HUD/results/scrolling guide), PanicSurface (rounded vector UI), original parcel mascot, and Inter font reference through Unity scene APIs plus builder.
- Kept world/rules separate; added LanSession.Connecting read-only property. Corrected timer and normalized loot progress; added multi-touch ownership and modal hold cancellation.
- Current compilation passed. Welcome and join inspected at actual 1080x1920 Game View resolution, text overflow count 0. Screenshots now use ScreenCapture after configuring Game View; the baseline CLI capture stretched the old Game View.
- Fixed missing CanvasRenderer on the custom Graphic during first Play smoke. Initial tests: 21/22; the remaining test wrongly expected Play Mode OnDisable callbacks in EditMode. Explicit cancellation is tested in EditMode; hide/disable behavior will be checked in the actual Play smoke.
- No push yet. Next: full state/interaction review, compact and inset layouts, final tests, Android build.
