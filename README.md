<div align="center">

# 📦 PAKET PANIK

### 🎁 Jaga paketnya. Curi isinya. Salahkan temanmu.

**Dua HP. Satu meja. Satu kardus yang punya gigi.** 👹
Monster paket terlihat oleh kedua pemain lewat kamera AR masing-masing, satu pemain menenangkannya dengan lampu, satu lagi mencuri camilannya. Lampu habis, permen emas menggoda, lalu paketnya bersin. 😱🍬

[![Unity](https://img.shields.io/badge/Unity-6000.6.0f1-000000?logo=unity&logoColor=white)](https://unity.com)
[![ARCore](https://img.shields.io/badge/AR-ARCore%20%2B%20AR%20Foundation-4285F4?logo=google&logoColor=white)](https://developers.google.com/ar)
[![Netcode](https://img.shields.io/badge/LAN-Netcode%20for%20GameObjects-2196F3)](https://docs-multiplayer.unity3d.com)
[![Android](https://img.shields.io/badge/Android-ARM64%20%7C%20minSdk%2026-3DDC84?logo=android&logoColor=white)]()
[![Tests](https://img.shields.io/badge/Tests-22%2F22%20lulus-brightgreen)]()
[![PRs](https://img.shields.io/badge/PRs-welcome-brightgreen)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Made in Indonesia](https://img.shields.io/badge/Made%20in-Indonesia-red)]()

</div>

> ⚠️ **Status jujur:** vertical slice sudah terbangun dan teruji di Editor (kompilasi bersih, 22/22 tes lulus, APK berhasil dibuild). **Uji dua handphone nyata belum dijalankan**, checklistnya ada di [`Evidence/DEVICE-QA-CHECKLIST.md`](Evidence/DEVICE-QA-CHECKLIST.md).
> 📣 **Punya HP Android yang mendukung ARCore? Kami butuh laporan pengujianmu!** Cukup ikuti checklist dan kirim hasilnya lewat issue. 🙏

---

## 📸 Cuplikan

| 📦 Beranda baru | 🎮 HUD permainan | 🧾 Hasil ronde |
|:-:|:-:|:-:|
| <img src="Evidence/UIUX/after-menu.png" width="230" alt="Beranda dengan ilustrasi paket"> | <img src="Evidence/UIUX/after-playing-editor.png" width="230" alt="HUD permainan dalam fixture Editor"> | <img src="Evidence/UIUX/after-won-editor.png" width="230" alt="Struk hasil dalam fixture Editor"> |

<p align="center"><i>UI dirender di Unity. Gambar gameplay dan hasil memakai state uji Editor, bukan bukti permainan dua HP. Karakter digambar dari kode.</i></p>

UI baru memisahkan beranda dan formulir gabung, memberi petunjuk kesiapan kedua pemain, menyediakan panduan yang bisa digulir, serta memperjelas timer, emosi, lampu, dan progres tahan. Bukti pengujian ada di [Evidence/UIUX/WORKLOG.md](Evidence/UIUX/WORKLOG.md).

## ✨ Kenapa proyek ini menarik

- 🎯 **Puzzle sosial asli**: satu jaga, satu ambil, dan kalian harus percaya pada orang yang salah. 😅
- 📸 **AR bersama tanpa Cloud Anchor**: satu kartu marker 20 cm menjadi koordinat papan yang sama untuk dua HP, tanpa server, tanpa akun, tanpa internet.
- 🧠 **Aturan deterministik**: host menghitung 20 tick/detik; klien hanya mengirim intent yang divalidasi secara geometri. Anti-cheat dasar sudah ada.
- 🎨 **Nol aset biner karakter**: monster kardus, permen, efek suara, dan marker seluruhnya digenerate dari kode.
- 🧪 **Test-first gameplay**: 11 tes NUnit mengunci perilaku aturan (quota, baterai, gigitan, lock barang, rematch), ditambah 11 kasus presentasi untuk timer, progres, dan input tahan.
- 📶 **LAN murni**: host + PIN 6 digit, jalan di Wi-Fi biasa, cocok untuk demo meja kafe. ☕

## 🎮 Cara main (30 detik)

1. 🃏 Cetak kartu marker 20 cm ([`Marker_PaketPanik_Print.html`](Assets/PaketPanik/Art/Marker_PaketPanik_Print.html)), **skala 100%**, sisi gambar harus 20,0 cm, letakkan datar di meja terang.
2. 📲 Pasang APK di dua HP Android 8+ ARM64 yang mendukung [ARCore dan Depth API](https://developers.google.com/ar/devices). Build saat ini mewajibkan fitur depth di manifest.
3. 📡 Kedua HP ke Wi-Fi yang sama. HP A tekan **Buat meja baru** dan bagikan IP + PIN. HP B pilih **Gabung meja teman**, isi IP + PIN, lalu tekan **Gabung sekarang**.
4. 📸 Keduanya pindai kartu yang sama, tekan **Posisi cocok. Saya siap**, lalu host menekan **Mulai ronde**.
5. 🔄 Satu pemain menahan **TAHAN UNTUK MENJAGA** (bidik paket), satu lagi menahan **TAHAN UNTUK MENGAMBIL** (bidik camilan). Gantian sebelum lampu habis!
6. 🏆 Kumpulkan 12 poin lalu **SEGEL BERSAMA**, atau ambil risiko demi skor lebih tinggi. Tiga gigitan = kalah. 💀

## 🏗️ Arsitektur

```mermaid
flowchart TB
  H["📱 Host (HP A)<br/>GameRules authoritative (20 Hz)"]
  C["📱 Client (HP B)"]
  M["🃏 Marker 20 cm<br/>(koordinat papan bersama)"]
  H -- "snapshot state (10 Hz, reliable)" --> C
  C -- "intent tervalidasi (10 Hz)" --> H
  H -. "anchor" .- M
  C -. "anchor" .- M
```

- 🗺️ **Koordinat bersama**: tiap HP meng-anchor `BoardRoot` lokalnya ke marker fisik yang sama. Posisi dunia AR **tidak pernah** dikirim lewat jaringan.
- 🔐 **Authority**: skor, kemarahan, baterai, dan jadwal barang hanya ada di host. Klien tidak bisa curang soal angka.
- 🧩 **Modular**: `Core` (aturan murni, tanpa AR) → `AR` (marker/alignment) → `Network` (LAN) → `Presentation` (HUD/monster/audio).

## 🛠️ Stack

| Bagian | Teknologi |
|---|---|
| 🎮 Engine | Unity **6000.6.0f1** + URP |
| 📸 AR | AR Foundation 6.6.2 + ARCore XR Plugin |
| 🌐 Jaringan | Netcode for GameObjects 2.13 + Unity Transport (UDP LAN, port 7777) |
| 🖱️ Input | Input System + TrackedPoseDriver |
| 🧪 Tes | Unity Test Framework (EditMode, 22 kasus) |

## 🚀 Build dari sumber

```bash
# 1. Kebutuhan: Unity Hub dengan Unity 6000.6.0f1 + Android Build Support (SDK/NDK/JDK)
git clone https://github.com/fathahnoor/paket-panik.git
# 2. Buka folder dengan Unity 6000.6.0f1
# 3. Scene utama: Assets/PaketPanik/Scenes/PaketPanik.unity
```

- 🏗️ **Rebuild APK** (Windows): `pwsh Tools/Build-AndroidApk.ps1` → hasil di `Builds/Android/PaketPanik.apk` + SHA256 otomatis.
- 🔁 **Regenerate scene/marker/settings** (idempotent): menu Unity **PaketPanik → Build Project Assets**.
- 🧪 **Jalankan tes**: Test Runner → EditMode → `PaketPanik.Tests`, atau via CLI: `unity command run_tests --mode editor --filter PaketPanik.Tests --filter_type assembly`.
- 🕶️ **Pratinjau tanpa HP**: Play Mode menyediakan **XR Simulation**, tekan **Buat meja baru**. Penemuan marker simulasi masih tidak konsisten; gunakan HP untuk menguji AR sesungguhnya.

## 📁 Struktur repo

```text
Assets/PaketPanik/
├── Runtime/Core/          🧠 aturan game deterministik (tanpa AR)
├── Runtime/AR/            📸 SharedBoard: anchor marker & koordinat papan
├── Runtime/Network/       🌐 LanSession: host, PIN, intent, snapshot
├── Runtime/Presentation/  🎨 HUD, monster, loot, audio (semua dari kode)
├── Editor/                🏗️ generator scene/marker/settings yang idempotent
├── Tests/Editor/          🧪 11 tes aturan + 11 kasus presentasi
├── Art/                   🃏 marker PNG + library + versi cetak
├── Resources/PaketPanik/  ⚙️ balance.json + marker-hash
└── Scenes/PaketPanik.unity
Design/                    📐 GAME_DESIGN, TECH_SPEC, MARKET_RESEARCH, dll.
Evidence/                  ✅ bukti tes, build report, screenshot, checklist QA
Tools/                     🤖 helper Unity CLI + build APK (PowerShell)
```

## 🗺️ Roadmap

- [x] 🧠 Aturan inti + 11 tes deterministik
- [x] 📸 Marker AR bersama + check drift
- [x] 🌐 LAN host/client dengan PIN
- [x] 📦 APK Android pertama (61,5 MB, ARM64)
- [x] 🎨 UI paket, panduan bertahap, HUD dan struk hasil; 26 pemeriksaan UI/koneksi lokal
- [ ] 📱 **Uji dua HP nyata** ← kontribusi paling dibutuhkan sekarang!
- [ ] 👥 Mode 4 pemain (balancing disiapkan di desain)
- [ ] 🎨 Model & animasi kardus yang lebih hidup
- [ ] 🔊 Sound design orisinal (hum lampu, stapler gigitan, fanfare)
- [ ] 🌏 Dukungan bahasa Inggris
- [ ] 🔗 Join tanpa mengetik IP (QR / mDNS)
- [ ] 🚀 Rilis publik itch.io

## 🤝 Kontribusi

PR, issue, dan laporan HP sangat diterima! 💚

- 🐛 **Laporkan bug / hasil uji perangkat**, pakai template sederhana: model HP, versi Android, versi ARCore, apa yang terjadi, screenshot/video.
- 💡 **Good first issues**: sound design, animasi bersin/gigit, terjemahan, polesan HUD, tes tambahan, model kardus.
- 🔧 **Alur standar**: fork → branch fitur → jalankan `GameRulesTests` → PR dengan deskripsi jelas.
- 🧭 **Aturan emas proyek**: jangan sinkronkan transform dunia AR lewat jaringan (pakai papan/marker!), jangan kirim skor dari klien, dan jangan tambah aset berhak cipta.

## 🤖 Dibangun dengan kolaborasi manusia + AI

- 🧠 Riset pasar & desain game: **GPT-6 Astra** (Codex)
- 🛠️ Implementasi, build APK, dan bukti QA: **OpenCode** dengan **DeepSeek V4.1 Flash**
- 🎨 Perbaikan UI/UX, tes presentasi dan build lanjut: **GPT-6 Astra** (Codex)
- 🧑💻 Arah produk, keputusan, dan pengujian perangkat: **@fathahnoor**
- 🎨 Seluruh karakter/audio orisinal dan digenerate; tidak ada meme atau IP pihak ketiga.

## 📚 Dokumen desain

- 🎮 [GAME_DESIGN.md](Design/GAME_DESIGN.md): aturan lengkap, balancing, batas MVP
- 📐 [TECH_SPEC.md](Design/TECH_SPEC.md): detail AR, sinkronisasi, struktur kode
- 📊 [MARKET_RESEARCH.md](Design/MARKET_RESEARCH.md): sumber bertanggal & alasan pemilihan tema
- 🧪 [BUILD_PLAN.md](Design/BUILD_PLAN.md): acceptance T01-T12 & rencana uji keseruan
- 🎬 [LAUNCH_PLAN.md](Design/LAUNCH_PLAN.md): rencana video peluncuran 20 detik
- 🤝 [HANDOFF.md](HANDOFF.md): peta status untuk kontributor baru

## 📜 Lisensi

Proyek ini dirilis di bawah [MIT License](LICENSE). Kamu bebas memakai, memodifikasi, dan mendistribusikan proyek ini, termasuk untuk keperluan komersial, dengan tetap mencantumkan atribusi lisensi aslinya. 😊

---

<div align="center">

📦 **PAKET PANIK**, dibuat untuk dimainkan berdua, di satu meja, sambil tertawa. 😂

*Jika kamu membaca ini dan tersenyum, bintang ⭐ atau issue pertamamu sangat berarti!*

</div>
