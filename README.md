# PAKET PANIK

**Jaga paketnya. Curi isinya. Salahkan temanmu.**

Rancangan game horor komedi untuk dua handphone Android di meja yang sama. Monster kardus terlihat melalui kamera kedua pemain. Satu pemain menenangkannya dengan lampu, satu lagi mengambil camilan. Lampu habis, isi emas menggoda, lalu paketnya bersin.

Status 11 September 2026: vertical slice terbangun di Unity 6000.6.0f1. Scene handphone, marker 20 cm, aturan inti (11/11 tes lulus), LAN host/client dengan PIN, HUD dan monster sudah ada. APK debug berhasil dibuild (61,5 MB, arm64-v8a, ARCore) dengan bukti di [Evidence/build-android.json](Evidence/build-android.json). Pengujian dua handphone nyata belum dijalankan; checklistnya di [Evidence/DEVICE-QA-CHECKLIST.md](Evidence/DEVICE-QA-CHECKLIST.md). Template MR di folder ini tetap menjadi basis project.

## Coba cepat (dua HP)

1. Cetak kartu marker: buka `Assets/PaketPanik/Art/Marker_PaketPanik_Print.html`, cetak skala 100%, pastikan sisi gambar 20,0 cm, letakkan datar di meja terang.
2. Pasang `Builds/Android/PaketPanik.apk` di dua HP Android 8+ yang mendukung ARCore (`adb install -r Builds/Android/PaketPanik.apk`).
3. Kedua HP ke Wi-Fi yang sama. HP A: **BUAT MEJA**, bagikan IP + PIN. HP B: isi IP + PIN, **GABUNG MEJA**.
4. Keduanya pindai kartu, tekan **POSISI COCOK / SIAP**, host menekan **MULAI PAKET**. Satu menjaga, satu mengambil, gantian sebelum lampu habis.
5. Rebuild APK: `pwsh Tools/Build-AndroidApk.ps1` (Editor project harus terbuka).

## Mulai dari sini

- [Lihat konsep secara interaktif](Design/preview.html), storyboard lokal tanpa kamera atau koneksi multiplayer.
- [Desain gameplay](Design/GAME_DESIGN.md), aturan, kontrol, balancing awal, visual, dan batas MVP.
- [Riset pasar](Design/MARKET_RESEARCH.md), sumber bertanggal dan alasan pemilihan tema.
- [Spesifikasi implementasi](Design/TECH_SPEC.md), adaptasi template MR, ARCore, sinkronisasi, dan struktur kode.
- [Rencana implementasi dan pengujian](Design/BUILD_PLAN.md), target empat jam dengan batas bukti yang jelas.
- [Paket video peluncuran](Design/LAUNCH_PLAN.md), storyboard rekaman dan caption berbasis hasil nyata.
- [Prompt untuk agent berikutnya](NEXT_SESSION_PROMPT.md).
- [HANDOFF.md](HANDOFF.md), baca pertama ketika berpindah sesi atau harness.

Keputusan inti: pertahankan Unity 6000.6.0f1 dan template yang ada, buat scene handphone baru, gunakan AR Foundation + ARCore, dan mulai dari multiplayer LAN dua pemain. Nama dan karakter adalah konsep orisinal dalam paket ini. Potensi dibagikan adalah hipotesis yang harus diuji, bukan jaminan viral.
