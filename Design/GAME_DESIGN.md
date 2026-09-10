# PAKET PANIK: desain game v1

**Jaga paketnya. Curi isinya. Salahkan temanmu.**

Status: spesifikasi untuk implementasi berikutnya. Seluruh angka balancing merupakan nilai awal rancangan, belum hasil playtest. Nilai kanonis tersimpan di [balance.json](balance.json).

## Premis dan pengalaman

Sebuah paket salah kirim mendarat di meja. Isinya camilan biasa dan permen emas. Kardusnya ternyata hidup. Pemain harus mengambil barang sambil menjaga monster tetap tenang, lalu menyegel paket sebelum terlambat.

Target MVP: tepat dua pemain manusia dengan dua handphone Android yang mendukung ARCore, berdampingan atau berseberangan di meja yang sama. Satu perangkat menjadi host, satu client, melalui Wi-Fi lokal. Pengguna sudah mengonfirmasi tersedia dua atau lebih handphone; model dan dukungan ARCore belum diperiksa.

Orientasi portrait. Tidak perlu headset. Interaksi memakai arah kamera dan satu tombol aksi yang ditahan. Pemain boleh tetap duduk; tidak perlu berlari, menyentuh monster dengan tangan nyata, atau mengguncang handphone. Durasi ronde maksimal 90 detik, dengan rematch dari meja yang sama.

## Mengapa bisa seru

Pemain yang menjaga monster tidak bisa mengambil barang pada saat yang sama. Pemain yang mengambil barang mempercepat monster bangun. Lampu penjaga hanya bertahan empat detik, sehingga pergantian diperlukan. Barang emas bernilai tiga tetapi lebih lama ditarik dan menaikkan kemarahan lebih besar.

Konfliknya jelas: aman sekarang atau skor lebih tinggi. Kesalahan terlihat oleh kedua orang. Monster menoleh ke pemain yang memicu gigitan, memuntahkan isi tas, lalu memperlihatkan label pengiriman "DIKEMBALIKAN KE PENGIRIM". Kekalahan dibuat lucu, tanpa kehilangan progres di luar ronde.

## Alur pemain

1. Menu menampilkan **Buat meja**, **Gabung meja**, dan bantuan dua gambar. Host melihat alamat IP lokal dan port; client memasukkannya. Tidak ada room code internet pada MVP.
2. Kedua pemain memindai kartu gambar yang sama, tercetak selebar 20 cm dan diletakkan datar di tengah meja. Masing-masing melihat kubus penanda pada posisi sama. Marker berfungsi sebagai alas paket di dalam cerita.
3. Pemain menekan **Posisi cocok** setelah melihat empat titik sudut dan arah panah konsisten. Host memulai ketika dua peserta siap dan tracking baik.
4. Hitung mundur tiga detik. Tampilkan satu kalimat: **Satu tenangkan, satu ambil. Ganti sebelum lampunya habis.**
5. Ronde berlangsung, barang muncul, pemain bergantian. Saat target 12 poin tercapai, tombol **Segel sekarang** tersedia bagi keduanya.
6. Kedua pemain menahan tombol segel bersama selama 1,5 detik untuk mengakhiri ronde dengan skor saat itu. Mereka bisa melanjutkan mengambil barang demi skor lebih tinggi.
7. Pada detik 90, paket menutup otomatis. Menang jika skor minimal 12 dan gigitan belum mencapai tiga. Jika kurang, paket kembali ke pengirim. Tiga gigitan langsung mengakhiri ronde gagal.
8. Layar hasil: skor bersama, jumlah gigitan, peran tiap pemain, dan tombol **Kirim paket lagi**. Host memulai rematch setelah keduanya siap. Kalibrasi dipakai ulang hanya jika masih valid.

## Aturan aksi

| Aksi | Input | Validasi | Hasil |
| --- | --- | --- | --- |
| Tenangkan | Arahkan reticle ke paket, tahan TENANGKAN | Tracking valid, dalam jarak 0,25 sampai 1,20 m, baterai cukup, tidak sedang menarik/menyegel/kena stun | Lampu berwarna menekan kemarahan; baterai berkurang |
| Ambil | Arahkan reticle ke satu barang, tahan AMBIL | Barang hidup, reticle masih tepat, pemain tidak sedang memakai aksi lain | Setelah durasi penuh, host menghapus barang dan menambah skor sekali |
| Batal ambil | Lepas tombol atau arahkan keluar barang | Toleransi aim 0,15 detik untuk jitter ringan | Progres pengambilan direset; belum ada skor |
| Segel | Tahan SEGEL SEKARANG | Skor minimal target dan kedua pemain menahan bersamaan | Setelah 1,5 detik, ronde menang. World tick dibekukan selama segel bersama valid |
| Rematch | Kedua pemain siap, host mulai | Dua peserta hadir dan kalibrasi valid | Semua nilai ronde, ID, dan event kembali ke awal |

Tombol aksi utama mengambil konteks dari reticle: paket berarti TENANGKAN, barang berarti AMBIL. Warna dan ikon membedakan fungsi, disertai teks. Menekan tombol ketika reticle kosong tidak menghasilkan aksi. Tombol segel terpisah hanya muncul setelah kuota tercapai.

Bila dua pemain menarik barang sama, host memberi lock kepada request pertama yang valid. Layar pemain kedua menampilkan "Lagi diambil teman". Tidak ada tarik-menarik fisika, skor ganda, atau transfer kepemilikan NetworkObject.

## Simulasi kanonis dua pemain

Host menghitung 20 tick per detik. Waktu game berhenti saat jeda tracking, recovery gigitan, atau penyegelan bersama. Jadwal barang dan event memakai waktu game tersebut, bukan jam perangkat.

```text
anger = clamp(anger + dt * (6 + 16 * jumlah_pengambil_aktif
                             - 26 * min(1, jumlah_penenang_valid)), 0, 100)
```

Anger mulai 20. Barang biasa menambah 8 saat berhasil diambil, emas menambah 20. Dengan satu penjaga dan satu pengambil, kemarahan turun 4 per detik sebelum tambahan saat barang terambil. Dengan satu pengambil tanpa penjaga, kenaikannya 22 per detik. Dua lampu tidak menggandakan penenangan.

Baterai tiap pemain mulai 100, turun 25 per detik saat menenangkan, pulih 25 per detik saat tidak menenangkan. Tidak bisa memulai penenangan jika baterai di bawah 10. Jika baterai mencapai nol, aksi berhenti dan harus melepas tombol sebelum memulai lagi. Semua baterai ditentukan host.

## Barang dan tekanan ronde

- Barang pertama muncul pada waktu game 1 detik, berikutnya setiap 5 detik. Maksimal tiga barang hidup sekaligus. Jika penuh, jadwal itu dilewati tanpa antrean.
- Ada enam socket tetap pada lingkaran radius 23 cm di sekitar paket. Host memilih socket kosong menggunakan PRNG dengan seed ronde. Tidak ada navmesh atau benda terlempar secara fisik.
- Setiap barang keempat yang berhasil di-spawn adalah emas. Barang biasa bernilai 1, ditarik 1,2 detik, hilang setelah 12 detik. Emas bernilai 3, ditarik 1,8 detik, hilang setelah 10 detik.
- Barang expired hilang tanpa penalti. Lock otomatis dibebaskan bila pemain berhenti, disconnect, atau barang expired. Spawn dan expiry diputuskan host.
- Pada detik 30 dan 60, paket bersin: cue 0,7 detik sebelumnya, lalu anger bertambah 25 dan semua progres tarik dibatalkan. Baterai tidak direset. Bersin dapat memicu gigitan jika anger mencapai 100.
- Pada detik 70 sampai 90, baseline anger naik dari 6 menjadi 10 per detik. HUD menampilkan **KURIR DATANG!**. Menyegel lebih awal adalah pilihan sah.

## Gigitan, atribusi, dan hasil

Anger mencapai 100 memicu tepat satu Bite event. Skor bersama berkurang paling banyak 3, minimum nol. Jumlah gigitan bertambah satu. Semua lock dibebaskan dan progres tarik dibatalkan. Anger direset menjadi 30.

Target animasi adalah pemain yang menyelesaikan pengambilan pemicu. Jika pemicu berasal dari kenaikan per tick, pilih pengambil aktif dengan waktu mulai paling akhir. Jika tidak ada pengambil, gunakan pemain dengan kontribusi penenangan paling kecil, tie-break ID pemain terkecil. Ini aturan atribusi komedi yang diketahui pemain, bukan penilaian moral.

Recovery berlangsung 1,5 detik wall clock: input ditolak, waktu game dibekukan, baterai pulih seperti keadaan idle. Setelah recovery, keduanya langsung bisa bermain. Gigitan ketiga berakhir gagal sesudah animasi singkat.

Urutan tick: apply intent valid, drain/recharge baterai, tentukan aksi efektif, expiry barang, spawn, bersin, progres pengambilan dan tambahan anger, integrasi anger, gigitan, lalu pemeriksaan timeout. Segel bersama yang valid ditangani sebelum world tick. Bila segel selesai pada tick yang sama dengan timeout, segel menang; bila anger mencapai 100 pada tick timeout tanpa segel aktif, gigitan diselesaikan sebelum hasil.

Penghargaan hasil dihitung dari event nyata: **Tangan paling cepat** (barang terbanyak), **Penjaga paket** (durasi lampu valid terbanyak), **Tersangka utama** (gigitan teratribusi terbanyak). Untuk tie tampilkan keduanya atau label seri. Jangan membuat angka atau menyalahkan pemain secara acak.

## Penampilan dan suara

Paket adalah kubus kardus sekitar 22 cm, dengan mata kecil, flap sebagai alis, mulut hitam dan gigi tumpul putih. Model dapat dibuat dari primitive Unity dengan satu lid pivot. Kulit orisinal: kertas cokelat, lakban lavender, stiker kuning "JANGAN DIGUNCANG". Tidak memakai karakter meme yang sudah dikenal atau suara berhak cipta.

Palet: latar HUD gelap #151721, lavender #B49CFF untuk lampu, kuning #FFD367 untuk barang emas, coral #FF6F77 untuk bahaya, dan mint #76E0BA untuk siap. HUD ringkas dan tembus pandang agar meja nyata tetap dominan.

Animasi minimum: idle bernapas, flap bergetar mengikuti anger, mata mengikuti kontribusi terbaru, bersin, gigit, dan tutup menang. Tween posisi/rotasi lokal berbasis waktu event. Efek benda memantul hanya visual dan tidak dihitung sebagai fisika jaringan.

Audio orisinal sintetis atau rekaman yang lisensinya jelas: hum lampu, gesek kardus, pop barang, cue bersin, bunyi gigitan seperti stapler, dan fanfare tiga nada. Tanpa mikrofon atau voice chat. Pemain berbicara langsung karena satu ruangan. Haptic singkat opsional; seluruh cue penting juga tampak secara visual.

## Cakupan terkunci

MVP wajib: dua perangkat nyata, marker yang sama, monster sama dengan parallax, host/client LAN, aim dan hold, baterai, item biasa/emas, bersin, gigitan, menang/kalah, rematch, tracking pause, HUD dan audio dasar, APK, serta bukti QA.

Setelah MVP lulus: empat pemain, pilihan warna paket, room join lebih mudah, tangkapan hasil native. Untuk empat pemain perlu uji balancing dan ubah laju pengambil menjadi `16 * activeLooters / (N - 1)` dengan target `6 * N`; ini proposal terpisah, belum konfigurasi MVP.

Di luar sesi beberapa jam: multiplayer internet, Relay/Lobby, Cloud Anchors, akun, matchmaking, voice recognition, generative AI runtime, hand tracking kamera, pemetaan seluruh ruangan, semantic furniture recognition, occlusion fisik wajib, toko, iklan, cloud leaderboard, perekam video internal, dan rilis Play Store.

MR di sini berupa handheld AR yang mendaftarkan objek virtual pada posisi meja nyata. Aplikasi tidak mengenali semua benda di ruangan. Monster tidak dijanjikan bisa bersembunyi di balik gelas atau tangan tanpa dukungan depth dan pengujian tambahan.
