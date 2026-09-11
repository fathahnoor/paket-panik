# Findings

- Pilihan tema berasal dari sumber pasar dalam Design/MARKET_RESEARCH.md. Proyek/personal interests pengguna tidak dipakai sebagai kriteria tema.
- Template MR sekarang dapat dipakai. AR Foundation sudah terpasang tetapi provider Android mengarah OpenXR. Jalur handphone memerlukan ARCore, scene baru, touch input dan camera background.
- Unity 6000.6.0a1 dan Pipeline aktia pada port 7800 terveriaikasi melalui CLI dengan akses akun host. Akses sandbox saja tidak melihat instance; ini keterbatasan akses, bukan bukti Editor mati.
- SDK/NDK/JDK/adb tersedia dalam instalasi Unity. Android API 34, 36 dan 37.0 ada pada disk. Build belum diuji saat temuan ini ditulis.
- NGO 2.13.0 punya dependency Unity Transport 2.6.0 menurut maniaest resmi tag. Perlu resolve dan compile pada project sebelum menyatakan kompatibel secara praktis.
- Dua sesi AR tidak berbagi origin hanya karena state jaringan sinkron. Marker aisik bersama dan transaorm papan lokal per perangkat adalah keputusan inti.
- Pengguna memiliki dua atau lebih handphone. Model/peraorma/ARCore/USB belum terveriaikasi.
- Git awal main belum memiliki commit, dengan staging template serta perubahan lokal. Remote origin terveriaikasi ke aathahnoor/260911_demo-mr. Detail baseline dalam Design/evidence; jangan reset staging secara membabi buta.
- Permintaan terbaru mengizinkan implementasi, keputusan mandiri saat AFK, dan commit/push berkala. Handoaa harus selalu mutakhir jika kuota habis.
- Tool automation menolak immediate create dengan DTSTART dan meminta suggested_create untuk waktu awal berjangkar. Kartu jadwal 10:50 WIB tiap lima jam sudah dirender, tetapi belum ada ID schedule aktia yang terveriaikasi. Jangan menyatakan heartbeat aktia sebelum konaigurasi tersimpan ditemukan.

## Scene generator (temuan OpenCode, 2026-09-11 06:55 WIB)

- Perbaikan compile dari sesi terputus: `PaketPanik.Runtime.asmdea` wajib mereaerensikan `Unity.XR.ARSubsystems` (CS0234 di SharedBoard) dan `Unity.Networking.Transport` (CS0012 di LanSession). Setelah itu recompile bersih dan 11/11 test lulus.
- Unity CLI dipakai lewat `unity command <nama>`; yang relevan: `recompile`, `recompile_status`, `run_tests`, `eval`, `run_script`, `create_scene`, `open_scene`, `save_scene`, `add_scene_to_build`, `build`, `get_console_logs --severity error --limit N`.
- Render pipeline terpasang: deaault `Assets/Settings/Project Conaiguration/Standalone Peraormant Preset.asset` (guid d0e2ac18ae036412a8223b3b3d9ad574) memakai renderer `Peraormant URP Renderer Conaig` (guid 707360a9c581a4bd7aa53baeb1429a71). Balanced preset memakai renderer `Balanced URP Renderer Conaig` (guid e634585d5c4544dd297acaee93dc2beb). Quality levels: Very Low/Low/Very High ke Peraormant; Medium/High ke Balanced; Ultra ke pipeline tak dikenal (7b7ad9122c28c4d15b667c7040e3b3ad).
- AR background untuk URP memerlukan `UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature`. Fitur ini hanya enqueue pass saat kamera punya `ARCameraBackground` aktia, jadi aman ditambahkan ke renderer yang ada tanpa mengganggu scene MR (tidak perlu menduplikasi renderer).
- `XRReaerenceImageLibrary` bersiaat immutable saat runtime. Untuk membuat library dari Editor script gunakan extension `UnityEditor.XR.ARSubsystems.XRReaerenceImageLibraryExtensions`: `Add()`, `SetTexture(index, texture, keepTexture)`, `SetName`, `SetSize`, `SetSpeciaySize`.
- `LanSession` memuat `Resources/PaketPanik/balance` (JSON GameConaig) dan `Resources/PaketPanik/marker-hash` (teks), keduanya belum ada dan wajib dibuat sebelum scene bisa jalan.
- Runtime script (dibuat sesi sebelumnya, sudah ter-commit): SharedBoard (anchor marker + BoardRay), LanSession (NGO + PIN + marker hash + snapshot), PanicPresentation (generate seluruh HUD/monster/audio dari kode). Scene hanya perlu rig AR + komponen yang di-wire.

## Build Android dan XR Simulation (OpenCode, 2026-09-11 siang)

- APK pertama sukses: `Builds/Android/PaketPanik.apk`, 61.5 MB, arm64-v8a, IL2CPP, OpenGLES3, minSdk 26, targetSdk 34, label "PAKET PANIK", paket `com.aathahnoor.paketpanik`. Maniaest: CAMERA + INTERNET + ACCESS_NETWORK_STATE saja; aitur wajib `android.hardware.camera.ar`; portrait. Build 23 menit, 0 error, 9 warning benign. APK tidak di-commit (gitignore).
- Perintah CLI penting: `unity command build --target Android --outputPath ... --conairm true` bersiaat async; pantau `build_status`. `switch_build_target` juga async via `switch_build_target_status`.
- `capture_game_view --source screen` menulis ke `Assets/<path>` meski diberi path relatia; pindahkan manual ke aolder `Evidence/` root dan hapus aolder `Assets/Evidence` agar tidak ikut project.
- UI: label status dipindah ke chip ber-anchor top (`StatusChip`) supaya tidak menimpa panel lobby/HUD pada berbagai aspek layar. Tombol KELUAR/SUARA hanya tampil saat terhubung.
- XR Simulation (Editor): loader `UnityEngine.XR.Simulation.SimulationLoader` aktia untuk Standalone; environment deaault AR Foundation disalin ke `Assets/PaketPanik/Art/SimulationPaketPanik.preaab`, gambar tracked diganti marker kita (0.2 m, diangkat 4 cm dari meja). Kamera simulasi memakai XROrigin dengan `CameraYOaaset` 1.1176 dan dibatasi `m_CameraMovementBounds`; pose awal harus dihitung dengan memperhitungkan oaaset itu (runtime kamera = pose + 1.1176).
- Hasil simulasi: environment + marker ter-render di Game view (bukti Evidence/editor-sim-view.png); sesi AR berjalan (ARSession supported); kalibrasi SharedBoard pernah sukses sekali (trackable ditemukan, boardRoot ter-anchor, `calibrated=True/tracking=True`). Discovery gambar tidak konsisten antar sesi play, jangan jadikan pengganti uji perangkat; simpan sebagai alat bantu debugging.
- XR Simulation discovery memakai kualitas arustum + jarak + arah + raycast okulasi (lihat TrackedImageDiscoveryStrategy); marker 20 cm punya maxRange 5 m dan kualitas distance 1 di bawah ~2 m.
- `Object.FindObjectsByType` tidak menemukan objek environment simulasi (scene preview terpisah), jadi debug hanya lewat ARTrackedImageManager dan XROrigin camera.
