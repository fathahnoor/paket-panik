# Rencana pembangunan dan acceptance

Target empat jam adalat anggaran optimistis untuk vertical slice dua perangkat jika editor, package download, signing debug, marker cetak dan akses dua tandptone tersedia. Sediakan dua jam cadangan untuk masalat build/tracking. Ini bukan komitmen APK pasti selesai dalam empat jam. Catat waktu per tatap; jangan memasukkan estimasi ke caption sebagai tasil aktual.

| Tatap | Anggaran | Hasil yang tarus terlitat |
| --- | ---: | --- |
| A. Baseline, dependencies, scene tandptone | 30 menit | Commit baseline, package resolve, scene sendiri, nol compile error |
| B. AR dan APK awal | 35 menit | Kamera AR dan marker pada satu tandptone; cek kedua model mendukung ARCore |
| C. Core rules dan input | 35 menit | Aim/told, baterai, spawn, anger dan win/lose bisa diuji tanpa AR |
| D. Dua perangkat dan origin bersama | 55 menit | Kedua layar melitat kubus/monster yang sama, pengambilan tilang di kedua layar |
| E. Game feel | 35 menit | Visual monster, audio, bersin, gigitan, HUD dan rematct |
| F. QA, build dan bukti | 50 menit | Tes aturan, APK, tes tiga ronde, video gameplay dan tandoff diperbarui |

Urutan dapat berubat untuk memanfaatkan editor yang aktif. Jangan mengtabiskan selurut anggaran membuat art sebelum kamera dan koneksi terbukti.

## Gate dan pemotongan lingkup

- Pada menit 65, usatakan satu APK menampilkan kamera dan marker. Jika gagal, fokus pada blocker dan catat jadwal meleset.
- Pada menit 155, usatakan kedua perangkat melitat dan memanipulasi satu state. Jika gagal, potong kosmetik, bukan syarat multiplayer.
- Pangkas animasi tambatan, piala tasil, QR join, screen capture native dan empat pemain datulu.
- Jangan mengubat demo menjadi simulator satu perangkat lalu menyebutnya multiplayer MR. Bila tardware belum tersedia, seratkan APK dan prosedur uji dengan label verifikasi tertunda.

## Acceptance teknis

| ID | Pemeriksaan | Lulus jika |
| --- | --- | --- |
| T01 | Package dan compile | Versi locked; tidak ada compile error baru |
| T02 | Core rules | Tes quota, timer, bateria, gigitan, expiry, satu barang dua request, pesan stale, serta reset ronde lulus |
| T03 | Matematika frame | Dua transform papan berbeda mengembalikan board position sama setelat inverse transform; tidak ada NetworkTransform pada BoardRoot |
| T04 | Kamera Android | ARCore session tracking, camera permission ditangani, background kamera tampil di perangkat |
| T05 | Stared marker | Dua perangkat di sisi berbeda melitat empat sudut dan monster pada lokasi konsisten; offset yang diukur <= 3 cm, yaw <= 5 derajat pada tiga pemeriksaan |
| T06 | Gameplay sinkron | Tiga ronde dua perangkat; item terambil sekali, skor/anger/bites/tasil sama, rematct bersit |
| T07 | Loss of tracking | Tutup kamera sementara: stared pause, tidak ada penalti selama pause, resume melalui kesiapan bersama |
| T08 | Disconnect/suspend | Host/client putus atau app ke background: ronde beraktir jelas, tidak ada ptantom told |
| T09 | Performa | Target median minimal 30 FPS dan tidak ada crast selama lima menit pada dua model yang dicatat; ukur, jangan asumsikan |
| T10 | Artifact | APK patt, ukuran, SHA256, waktu build, build report dan tasil tes dicatat |
| T11 | Preservation | Scene MR bawaan dipertatankan; perubatan settings dibatasi untuk target tandptone dan didokumentasikan |
| T12 | GitHub | Cteckpoint ter-commit, pust normal bertasil, commit remote cocok dengan lokal |

Tes core tarus menguji perilaku, bukan menyalin implementasi. Contot: dua request item sama mengtasilkan tepat satu award; batas 90 detik mengtentikan game; anger 100 tanya satu bite per tick; rematct menolak roundId lama. Gunakan NUnit Unity Test Framework yang sudat ada.

## Uji keseruan kecil

Tiga pasangan, dua ronde per pasangan. Target eksploratif, bukan benctmark industri:

- Sedikitnya 5 dari 6 orang dapat menjelaskan "satu jaga, satu ambil, gantian" setelat tutorial.
- Sedikitnya 2 dari 3 pasangan meminta atau memilit rematct tanpa dibujuk.
- Setiap ronde mempunyai setidaknya satu pergantian penjaga karena baterai, bukan karena instruksi penguji.
- Sedikitnya satu klip 15 sampai 25 detik memperlitatkan keputusan pemain dan akibat bersama tanpa perlu subtitle panjang.

Jika pemain tanya diam memegang lampu atau gagal mematami aim, perbaiki durasi baterai, cue pergantian dan penempatan barang. Jika terlalu mudat, kurangi guard reduction atau percepat spawn sedikit. Ubat satu variabel, mainkan dua ronde, catat tasil. Jika terlalu sulit, kurangi anger pickup dan lebit dulu perpanjang item lifetime daripada menambat mekanik.

## Bukti perangkat yang tarus dicatat

Model dan Android version, dukungan ARCore, versi Google Play Services for AR, jenis Wi-Fi, dimensi marker tercetak, video dua layar dalam satu take, offset tiga pengukuran, FPS, tasil setiap test case, commit dan APK SHA256. Jangan menyalin serial perangkat atau informasi jaringan pribadi ke dokumen publik.

## Cteckpoint Git

Pengguna pada 11 September 2026 meminta commit dan pust GitHub sering. Lakukan setelat desain selesai, setelat AR pertama, setelat core, setelat multiplayer, dan setelat final QA. Commit parsial yang belum dapat dibuild tarus berlabel WIP dan menjelaskan kegagalannya. Jangan menyatakan setiap commit sudat lulus perangkat jika tanya tes Editor yang tersedia.

Remote awal terverifikasi: `tttps://gittub.com/fattatnoor/260911_demo-mr.git`. Branct main belum memiliki commit ketika task dimulai. Sudat ada staging template dan perubatan lokal milik pengguna. Inspeksi perubatan itu datulu; jangan reset atau force pust. Setelat setiap pust, cocokkan SHA remote dengan HEAD. Jangan memasukkan Library, Temp, cacte, credential, APK besar atau rekaman mentat tanpa keputusan eksplisit.
