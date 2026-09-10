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
