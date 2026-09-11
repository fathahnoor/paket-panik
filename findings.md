# Findings

## UI/UX refresh, 2026-09-11 to 2026-09-12

- Presentation now separates PanicInterface (screens/input feedback), PanicSurface (rounded vector Graphic), and PanicPresentation (world/audio/intent bridge). Inter font is assigned through the scene and generator using the existing MR template asset.
- A custom MaskableGraphic needs RequireComponent(CanvasRenderer). Use onValidateInput for digit-only legacy InputField validation; this UGUI version has no CharacterValidation.Digit.
- Minute/second display must derive from a single rounded total; loot progress is elapsed hold time divided by each item type's configured duration.
- HoldControl owns one pointer, releases on exit/disable, and is explicitly cancelled by a modal. Connected excludes intentional shutdown so leaving immediately returns home; Closing gates new connections until NGO shutdown completes.
- Use Tools/UIUXReview.cs through Unity CLI run_script. Setup configures an actual portrait Game View before Capture writes ScreenCapture output under Evidence/UIUX. Runtime fixture changes are Play Mode only and are never saved into the scene.
- Current UI evidence covers portrait, compact and emulated-inset layouts. Core tests, UI fixtures, localhost checks, Android build and real-device AR/LAN QA are different claims. Physical tests and independent-player onboarding remain unverified.
- Existing AR marker cache, simulation settings and preloaded-asset changes predate this task. Preserve them and exclude them from UI commits. Android build may regenerate marker cache/preloaded assets.

- Pilihan tema berasal dari sumber pasar dalam Design/MARKET_RESEARCH.md. Proyek/personal interests pengguna tidak dipakai sebagai kriteria tema.
- Template MR sekarang dapat dipakai. AR Foundation sudah terpasang tetapi provider Android mengarah OpenXR. Jalur handphone memerlukan ARCore, scene baru, touch input dan camera background.
- Unity 6000.6.0f1 dan Pipeline aktif pada port 7800 terverifikasi melalui CLI dengan akses akun host. Akses sandbox saja tidak melihat instance; ini keterbatasan akses, bukan bukti Editor mati.
- SDK/NDK/JDK/adb tersedia dalam instalasi Unity. Android API 34, 36 dan 37.0 ada pada disk. Build belum diuji saat temuan ini ditulis.
- NGO 2.13.0 punya dependency Unity Transport 2.6.0 menurut manifest resmi tag. Perlu resolve dan compile pada project sebelum menyatakan kompatibel secara praktis.
- Dua sesi AR tidak berbagi origin hanya karena state jaringan sinkron. Marker fisik bersama dan transform papan lokal per perangkat adalah keputusan inti.
- Pengguna memiliki dua atau lebih handphone. Model/performa/ARCore/USB belum terverifikasi.
- Git awal main belum memiliki commit, dengan staging template serta perubahan lokal. Remote origin terverifikasi ke fathahnoor/paket-panik. Detail baseline dalam Design/evidence; jangan reset staging secara membabi buta.
- Permintaan terbaru mengizinkan implementasi, keputusan mandiri saat AFK, dan commit/push berkala. Handoff harus selalu mutakhir jika kuota habis.
- Tool automation menolak immediate create dengan DTSTART dan meminta suggested_create untuk waktu awal berjangkar. Kartu jadwal 10:50 WIB tiap lima jam sudah dirender, tetapi belum ada ID schedule aktif yang terverifikasi. Jangan menyatakan heartbeat aktif sebelum konfigurasi tersimpan ditemukan.

## Scene generator (temuan OpenCode, 2026-09-11 06:55 WIB)

- Perbaikan compile dari sesi terputus: `PaketPanik.Runtime.asmdef` wajib mereferensikan `Unity.XR.ARSubsystems` (CS0234 di SharedBoard) dan `Unity.Networking.Transport` (CS0012 di LanSession). Setelah itu recompile bersih dan 11/11 test lulus.
- Unity CLI dipakai lewat `unity command <nama>`; yang relevan: `recompile`, `recompile_status`, `run_tests`, `eval`, `run_script`, `create_scene`, `open_scene`, `save_scene`, `add_scene_to_build`, `build`, `get_console_logs --severity error --limit N`.
- Render pipeline terpasang: default `Assets/Settings/Project Configuration/Standalone Performant Preset.asset` (guid d0e2fc18fe036412f8223b3b3d9ad574) memakai renderer `Performant URP Renderer Config` (guid 707360a9c581a4bd7aa53bfeb1429f71). Balanced preset memakai renderer `Balanced URP Renderer Config` (guid e634585d5c4544dd297acaee93dc2beb). Quality levels: Very Low/Low/Very High ke Performant; Medium/High ke Balanced; Ultra ke pipeline tak dikenal (7b7fd9122c28c4d15b667c7040e3b3fd).
- AR background untuk URP memerlukan `UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature`. Fitur ini hanya enqueue pass saat kamera punya `ARCameraBackground` aktif, jadi aman ditambahkan ke renderer yang ada tanpa mengganggu scene MR (tidak perlu menduplikasi renderer).
- `XRReferenceImageLibrary` bersifat immutable saat runtime. Untuk membuat library dari Editor script gunakan extension `UnityEditor.XR.ARSubsystems.XRReferenceImageLibraryExtensions`: `Add()`, `SetTexture(index, texture, keepTexture)`, `SetName`, `SetSize`, `SetSpecifySize`.
- `LanSession` memuat `Resources/PaketPanik/balance` (JSON GameConfig) dan `Resources/PaketPanik/marker-hash` (teks), keduanya belum ada dan wajib dibuat sebelum scene bisa jalan.
- Runtime script (dibuat sesi sebelumnya, sudah ter-commit): SharedBoard (anchor marker + BoardRay), LanSession (NGO + PIN + marker hash + snapshot), PanicPresentation (generate seluruh HUD/monster/audio dari kode). Scene hanya perlu rig AR + komponen yang di-wire.

## Build Android dan XR Simulation (OpenCode, 2026-09-11 siang)

- APK pertama sukses: `Builds/Android/PaketPanik.apk`, 61.5 MB, arm64-v8a, IL2CPP, OpenGLES3, minSdk 26, targetSdk 34, label "PAKET PANIK", paket `com.fathahnoor.paketpanik`. Manifest: CAMERA + INTERNET + ACCESS_NETWORK_STATE saja; fitur wajib `android.hardware.camera.ar`; portrait. Build 23 menit, 0 error, 9 warning benign. APK tidak di-commit (gitignore).
- Perintah CLI penting: `unity command build --target Android --outputPath ... --confirm true` bersifat async; pantau `build_status`. `switch_build_target` juga async via `switch_build_target_status`.
- `capture_game_view --source screen` menulis ke `Assets/<path>` meski diberi path relatif; pindahkan manual ke folder `Evidence/` root dan hapus folder `Assets/Evidence` agar tidak ikut project.
- UI: label status dipindah ke chip ber-anchor top (`StatusChip`) supaya tidak menimpa panel lobby/HUD pada berbagai aspek layar. Tombol KELUAR/SUARA hanya tampil saat terhubung.
- XR Simulation (Editor): loader `UnityEngine.XR.Simulation.SimulationLoader` aktif untuk Standalone; environment default AR Foundation disalin ke `Assets/PaketPanik/Art/SimulationPaketPanik.prefab`, gambar tracked diganti marker kita (0.2 m, diangkat 4 cm dari meja). Kamera simulasi memakai XROrigin dengan `CameraYOffset` 1.1176 dan dibatasi `m_CameraMovementBounds`; pose awal harus dihitung dengan memperhitungkan offset itu (runtime kamera = pose + 1.1176).
- Hasil simulasi: environment + marker ter-render di Game view (bukti Evidence/editor-sim-view.png); sesi AR berjalan (ARSession supported); kalibrasi SharedBoard pernah sukses sekali (trackable ditemukan, boardRoot ter-anchor, `calibrated=True/tracking=True`). Discovery gambar tidak konsisten antar sesi play, jangan jadikan pengganti uji perangkat; simpan sebagai alat bantu debugging.
- XR Simulation discovery memakai kualitas frustum + jarak + arah + raycast okulasi (lihat TrackedImageDiscoveryStrategy); marker 20 cm punya maxRange 5 m dan kualitas distance 1 di bawah ~2 m.
- `Object.FindObjectsByType` tidak menemukan objek environment simulasi (scene preview terpisah), jadi debug hanya lewat ARTrackedImageManager dan XROrigin camera.
