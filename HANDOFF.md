# Project Handover

Handoff status: READY
Updated: 2026-09-11T17:42:00+07:00 Asia/Jakarta
Project root: C:\DevPath\260911_demo-mr
Source: GPT-6 Astra in Codex (design + first runtime), continued by OpenCode / DeepSeek V4.1 Flash (scene, APK, simulation, QA evidence), next session by Codex or any agent

## Resume here

CURRENT TASK OVERRIDE: user returned after OpenCode implementation and requested a substantial UI/UX improvement. Commit locally per checkpoint, then push ONCE after finishing; do not follow the old push-first advice below. Read Evidence/UIUX/WORKLOG.md and TASK_STATE.md first. New PanicInterface/PanicSurface UI compiles and 22 EditMode tests pass; portrait welcome/join have been visually checked. Next: complete UI state/interaction and compact-layout review, rebuild Android, update evidence/docs, checkpoint, one push. Preserve the three pre-existing changes listed in TASK_STATE.md. APK and real-device evidence from before this refresh do not validate the new UI.

Read TASK_STATE.md, progress.md and findings.md first, then Design/BUILD_PLAN.md. Inspect actual Git/Unity state: local `main` may have commits ahead of origin because pushes were deferred to ~10:45 WIB at the user's request; push them first if `git status` shows ahead. Next real work: run the two-phone QA from Evidence/DEVICE-QA-CHECKLIST.md with the built APK, then fix whatever the device run reveals. The vertical slice (scene, marker, LAN, APK) is otherwise complete and verified on the Editor side.

Safe initial commands: `Get-Content -LiteralPath .\TASK_STATE.md -Raw`; `unity status --json`; `unity command editor_status --json`; `git status --short --branch`; `git log --oneline -8`; `git push`. Unity CLI lives at `C:\Users\fathahnoor\AppData\Local\Unity\bin\unity.exe`. Use `unity command <nama>` to reach Editor tools. Use host permissions when the tool sandbox cannot reach the Editor or write Git metadata.

## Goal and scope

Design and implement PAKET PANIK, a two-phone Android handheld AR game with LAN multiplayer, preserving work for any coding agent. User initially requested next-session execution, then explicitly authorized implementation after design, autonomous decisions while AFK, frequent commits/pushes and durable progress records. Acceptance is in Design/BUILD_PLAN.md. A playable APK and genuine two-device evidence are separate deliverables.

## Current state

Vertical slice complete on the Editor side. Runtime (core rules, SharedBoard, LanSession, PanicPresentation) compiles clean with 11/11 EditMode tests. Scene `Assets/PaketPanik/Scenes/PaketPanik.unity` generated and reference-verified; marker PNG + `XRReferenceImageLibrary` (0.20 m) exist; Resources balance/hash load. Android APK built successfully at `Builds/Android/PaketPanik.apk` (61.5 MB, arm64-v8a, minSdk 26, IL2CPP, OpenGLES3, AR Required, CAMERA+INTERNET only); build evidence in Evidence/build-android.json. Editor play smoke passes (menu, lobby, Host() with PIN). XR Simulation renders the marker environment; calibration succeeded once but discovery is flaky. Device QA NOT RUN (no phone attached). Check progress.md for subsequent changes rather than trusting this paragraph after time has passed.

## Worktree and files

Git repository on main. Remote origin: https://github.com/fathahnoor/paket-panik.git. Detailed baseline: Design/evidence/git-baseline.txt. Local commits after `bcef19a`: scene/builder, APK build, UI fixes, simulation setup and docs; push deferred to ~10:45 WIB at user request. One pre-existing local modification remains unstaged by choice: `Assets/XR/UserSimulationSettings/SimulationEnvironmentAssetsManager.asset` (adds AR Foundation default simulation environment). Preserve user-owned changes.

Task additions: README.md, Design/, TASK_STATE.md, HANDOFF.md, task_plan.md, findings.md, progress.md, NEXT_SESSION_PROMPT.md, Evidence/ (tests, build report, screenshots, QA checklist), Assets/PaketPanik/ (Runtime Core/AR/Network/Presentation + Editor builder + Tests + Art marker + Scenes + Resources), Assets/XR ARCore/Simulation assets, Assets/DefaultNetworkPrefabs.asset, Tools/ (Unity CLI helper scripts). Input hashes: Design/evidence/baseline-hashes.json.

## Decisions and constraints

- Retain MR template and Unity 6000.6.0f1. Create Assets/PaketPanik scene and source, use ARCore for Android handphone.
- Two actual players, LAN first, shared printed image marker, host authority and local board coordinate mapping.
- Assets original/simple; no cloud backend, runtime AI, internet multiplayer, voice chat, four-player requirement, Play Store publication or social posting in MVP.
- Use official Unity CLI. No raw scene/prefab/meta editing. No force push, deleting user assets, or replacing unrelated work.
- Game design is a hypothesis; do not claim viral success, hours spent or physical QA without evidence.

## Verification

- `unity status --json`: PASS, one ready instance port 7800, project path matches.
- `unity command recompile` + `recompile_status`: PASS, failed=false, console errors 0.
- `unity command run_tests --mode editor --filter GameRulesTests`: PASS 11/11; evidence Evidence/core-tests-cli.json.
- Scene/references: PASS via run_script (boardRoot, camera, managers, presentation, library 1 image @0.20 m, Resources load, product name, first build scene).
- Android build: PASS (result Succeeded, 0 errors, 9 benign warnings); APK manifest verified with aapt (CAMERA/INTERNET only, `android.hardware.camera.ar` required, portrait, arm64).
- Editor Play smoke: PASS (menu/lobby render, Host() opens table with PIN, 0 errors). Evidence screenshots in Evidence/.
- XR Simulation: environment + marker render; `SharedBoard` calibration succeeded once (trackables=1, boardRoot anchored); repeated discovery flaky. Not a substitute for device QA.
- Device tests: NOT RUN, no Android device attached. APK install, ARCore support, tracking, FPS all unverified.
- Handoff and design validation: PASS earlier (14-file check, zero warnings) per progress.md.

## Risks and blockers

Device ARCore support and the printed 20 cm marker must be verified on the two real phones; shared alignment and LAN behavior are the main technical gates. XR Simulation discovery instability is an Editor-only quirk; do not claim device success from it. The pre-existing local modification to `SimulationEnvironmentAssetsManager.asset` is intentionally left unstaged; do not discard it. The `Builds/` folder is gitignored, so the APK must be rebuilt or shared manually.

Native automation immediate-create refused anchored DTSTART; suggested_create rendered the exact requested schedule card. No active schedule ID verified yet. See TASK_STATE.md for latest activation state; do not create duplicates or a standalone cron substitute.

## Environment and commands

- Unity CLI: C:\Users\fathahnoor\AppData\Local\Unity\bin\unity.exe
- Editor: C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe
- adb: C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe
- GitHub CLI: C:\Program Files\GitHub CLI\gh.exe
- Discover command syntax with `unity list --json` and command help. Select only the instance whose project path matches this project.
- No credential values needed in handoff. LAN gameplay does not require a cloud account.

## Detailed continuity

- [TASK_STATE.md](TASK_STATE.md): running checkpoint, authorization and scheduler state.
- [task_plan.md](task_plan.md): worklist.
- [findings.md](findings.md): decisions and verified discoveries.
- [progress.md](progress.md): dated execution evidence.
- [NEXT_SESSION_PROMPT.md](NEXT_SESSION_PROMPT.md): harness-neutral restart instruction.

## Handoff checklist

- [x] Scope and first next action explicit.
- [x] Existing worktree distinguished from task additions.
- [x] Runtime and hardware checks explicitly unverified.
- [x] Risks, scheduling limit and authorization visible.
- [x] No credential values or dependency on old chat.
- [x] Relevant paths inspected; later commands require live help verification.
