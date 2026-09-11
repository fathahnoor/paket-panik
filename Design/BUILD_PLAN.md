# Rencana pembangunan dan acceptance

Target empat jam adalah anggaran optimistis untuk vertical slice dua perangkat jika editor, package download, signing debug, marker cetak dan akses dua handphone tersedia. Sediakan dua jam cadangan untuk masalah build/tracking. Ini bukan komitmen APK pasti selesai dalam empat jam. Catat waktu per tahap; jangan memasukkan estimasi ke caption sebagai hasil aktual.

| Tahap | Anggaran | Hasil yang harus terlihat |
| --- | ---: | --- |
| A. Baseline, dependencies, scene handphone | 30 menit | Commit baseline, package resolve, scene sendiri, nol compile error |
| B. AR dan APK awal | 35 menit | Kamera AR dan marker pada satu handphone; cek kedua model mendukung ARCore |
| C. Core rules dan input | 35 menit | Aim/hold, baterai, spawn, anger dan win/lose bisa diuji tanpa AR |
| D. Dua perangkat dan origin bersama | 55 menit | Kedua layar melihat kubus/monster yang sama, pengambilan hilang di kedua layar |
| E. Game feel | 35 menit | Visual monster, audio, bersin, gigitan, HUD dan rematch |
| F. QA, build dan bukti | 50 menit | Tes aturan, APK, tes tiga ronde, video gameplay dan handoff diperbarui |

Urutan dapat berubah untuk memanfaatkan editor yang aktif. Jangan menghabiskan seluruh anggaran membuat art sebelum kamera dan koneksi terbukti.

## Gate dan pemotongan lingkup

- Pada menit 65, usahakan satu APK menampilkan kamera dan marker. Jika gagal, fokus pada blocker dan catat jadwal meleset.
- Pada menit 155, usahakan kedua perangkat melihat dan memanipulasi satu state. Jika gagal, potong kosmetik, bukan syarat multiplayer.
- Pangkas animasi tambahan, piala hasil, QR join, screen capture native dan empat pemain dahulu.
- Jangan mengubah demo menjadi simulator satu perangkat lalu menyebutnya multiplayer MR. Bila hardware belum tersedia, serahkan APK dan prosedur uji dengan label verifikasi tertunda.

## Acceptance teknis

| ID | Pemeriksaan | Lulus jika |
| --- | --- | --- |
| T01 | Package dan compile | Versi locked; tidak ada compile error baru |
| T02 | Core rules | Tes quota, timer, bateria, gigitan, expiry, satu barang dua request, pesan stale, serta reset ronde lulus |
| T03 | Matematika frame | Dua transform papan berbeda mengembalikan board position sama setelah inverse transform; tidak ada NetworkTransform pada BoardRoot |
| T04 | Kamera Android | ARCore session tracking, camera permission ditangani, background kamera tampil di perangkat |
| T05 | Shared marker | Dua perangkat di sisi berbeda melihat empat sudut dan monster pada lokasi konsisten; offset yang diukur <= 3 cm, yaw <= 5 derajat pada tiga pemeriksaan |
| T06 | Gameplay sinkron | Tiga ronde dua perangkat; item terambil sekali, skor/anger/bites/hasil sama, rematch bersih |
| T07 | Loss of tracking | Tutup kamera sementara: shared pause, tidak ada penalti selama pause, resume melalui kesiapan bersama |
| T08 | Disconnect/suspend | Host/client putus atau app ke background: ronde berakhir jelas, tidak ada phantom hold |
| T09 | Performa | Target median minimal 30 FPS dan tidak ada crash selama lima menit pada dua model yang dicatat; ukur, jangan asumsikan |
| T10 | Artifact | APK path, ukuran, SHA256, waktu build, build report dan hasil tes dicatat |
| T11 | Preservation | Scene MR bawaan dipertahankan; perubahan settings dibatasi untuk target handphone dan didokumentasikan |
| T12 | GitHub | Checkpoint ter-commit, push normal berhasil, commit remote cocok dengan lokal |

Tes core harus menguji perilaku, bukan menyalin implementasi. Contoh: dua request item sama menghasilkan tepat satu award; batas 90 detik menghentikan game; anger 100 hanya satu bite per tick; rematch menolak roundId lama. Gunakan NUnit Unity Test Framework yang sudah ada.

## Uji keseruan kecil

Tiga pasangan, dua ronde per pasangan. Target eksploratif, bukan benchmark industri:

- Sedikitnya 5 dari 6 orang dapat menjelaskan "satu jaga, satu ambil, gantian" setelah tutorial.
- Sedikitnya 2 dari 3 pasangan meminta atau memilih rematch tanpa dibujuk.
- Setiap ronde mempunyai setidaknya satu pergantian penjaga karena baterai, bukan karena instruksi penguji.
- Sedikitnya satu klip 15 sampai 25 detik memperlihatkan keputusan pemain dan akibat bersama tanpa perlu subtitle panjang.

Jika pemain hanya diam memegang lampu atau gagal memahami aim, perbaiki durasi baterai, cue pergantian dan penempatan barang. Jika terlalu mudah, kurangi guard reduction atau percepat spawn sedikit. Ubah satu variabel, mainkan dua ronde, catat hasil. Jika terlalu sulit, kurangi anger pickup dan lebih dulu perpanjang item lifetime daripada menambah mekanik.

## Bukti perangkat yang harus dicatat

Model dan Android version, dukungan ARCore, versi Google Play Services for AR, jenis Wi-Fi, dimensi marker tercetak, video dua layar dalam satu take, offset tiga pengukuran, FPS, hasil setiap test case, commit dan APK SHA256. Jangan menyalin serial perangkat atau informasi jaringan pribadi ke dokumen publik.

## Checkpoint Git

Pengguna pada 11 September 2026 meminta commit dan push GitHub sering. Lakukan setelah desain selesai, setelah AR pertama, setelah core, setelah multiplayer, dan setelah final QA. Commit parsial yang belum dapat dibuild harus berlabel WIP dan menjelaskan kegagalannya. Jangan menyatakan setiap commit sudah lulus perangkat jika hanya tes Editor yang tersedia.

Remote awal terverifikasi: `https://github.com/fathahnoor/paket-panik.git`. Branch main belum memiliki commit ketika task dimulai. Sudah ada staging template dan perubahan lokal milik pengguna. Inspeksi perubahan itu dahulu; jangan reset atau force push. Setelah setiap push, cocokkan SHA remote dengan HEAD. Jangan memasukkan Library, Temp, cache, credential, APK besar atau rekaman mentah tanpa keputusan eksplisit.
