# Findings

- Pilihan tema berasal dari sumber pasar dalam Design/MARKET_RESEARCH.md. Proyek/personal interests pengguna tidak dipakai sebagai kriteria tema.
- Template MR sekarang dapat dipakai. AR Foundation sudah terpasang tetapi provider Android mengarah OpenXR. Jalur handphone memerlukan ARCore, scene baru, touch input dan camera background.
- Unity 6000.6.0f1 dan Pipeline aktif pada port 7800 terverifikasi melalui CLI dengan akses akun host. Akses sandbox saja tidak melihat instance; ini keterbatasan akses, bukan bukti Editor mati.
- SDK/NDK/JDK/adb tersedia dalam instalasi Unity. Android API 34, 36 dan 37.0 ada pada disk. Build belum diuji saat temuan ini ditulis.
- NGO 2.13.0 punya dependency Unity Transport 2.6.0 menurut manifest resmi tag. Perlu resolve dan compile pada project sebelum menyatakan kompatibel secara praktis.
- Dua sesi AR tidak berbagi origin hanya karena state jaringan sinkron. Marker fisik bersama dan transform papan lokal per perangkat adalah keputusan inti.
- Pengguna memiliki dua atau lebih handphone. Model/performa/ARCore/USB belum terverifikasi.
- Git awal main belum memiliki commit, dengan staging template serta perubahan lokal. Remote origin terverifikasi ke fathahnoor/260911_demo-mr. Detail baseline dalam Design/evidence; jangan reset staging secara membabi buta.
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
