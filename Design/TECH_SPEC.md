# Spesifikasi implementasi

## Keputusan template

Lanjutkan project `C:\DevPath\260911_demo-mr`. Tidak perlu mengganti dengan AR Template. Template adalah titik awal konfigurasi, bukan pembatas jenis aplikasi. Buat scene `Assets/PaketPanik/Scenes/PaketPanik.unity` dan konfigurasi build Android handphone. Pertahankan scene MR dan aset bawaan.

Pemeriksaan 11 September 2026: Unity 6000.6.0f1, URP 17.6.0, Input System 1.20.0, AR Foundation 6.6.2, XR Management 4.7.0, dan Pipeline 0.6.0-exp.1. Android loader saat diperiksa adalah OpenXR; minimum dan target API keduanya 34. ARCore dan Netcode belum ada. SDK Android 34, 36, 37.0, NDK, JDK dan adb tersedia sebagai file. Versi runtime dan dukungan perangkat belum diuji.

Dokumentasi AR Foundation yang terpasang membedakan Android handphone dengan Android XR. Untuk handphone diperlukan ARCore. [Panduan Google](https://developers.google.com/ar/develop/unity-arf/getting-started-ar-foundation) mendukung struktur AR Session, XR Origin, dan provider sesuai platform.

## Stack yang dituju

| Bagian | Keputusan |
| --- | --- |
| Editor | Pertahankan 6000.6.0f1 |
| AR | AR Foundation 6.6.2 + ARCore XR Plugin 6.6.2, ketersediaan registry diperiksa sebelum instalasi |
| Jaringan | Netcode for GameObjects 2.13.0, Unity Transport 2.6.0 sebagai dependensinya |
| Mode | Satu Android host dan satu Android client, LAN UDP port 7777 |
| Render | URP, OpenGLES3 untuk jalur kamera paling sederhana, background kamera AR terpasang pada renderer |
| Build | IL2CPP, ARM64, portrait, minimum API 26 bila validation editor/provider mengizinkan, target API 36 terpasang |
| Input | Input System, reticle tengah dan tombol UI hold; tidak bergantung controller headset |
| Akun/backend | Tidak dibutuhkan untuk multiplayer LAN |

NGO 2.13.0 dipilih sebagai versi konkret yang rilisnya terverifikasi, bukan klaim versi terbaru. [Manifest resmi tag v2.13.0](https://raw.githubusercontent.com/Unity-Technologies/com.unity.netcode.gameobjects/v2.13.0/com.unity.netcode.gameobjects/package.json) menyebut Unity minimum 6000.0 dan Transport 2.6.0. [Pengumuman Unity](https://discussions.unity.com/t/netcode-for-gameobjects-v2-13-0-is-now-publicly-available/1723847) menekankan kompatibilitas Editor terbaru. Resolusi package dan kompilasi tetap wajib diuji di project ini. Jika versi ARCore yang direncanakan tidak tersedia, catat lalu pilih pasangan versi resmi yang cocok tanpa upgrade editor spekulatif.

## Penyesuaian MR ke handphone

1. Rekam state awal settings dan scene. Baca pre-existing diff sebelum menulis.
2. Tambah ARCore dan NGO melalui Package Manager/API Unity setelah izin yang berlaku terpenuhi. Lock versi hasil resolusi.
3. Android XR loader untuk build handphone hanya ARCore. Pertahankan package OpenXR/Hands agar sampel lama tetap dapat dikompilasi. Jangan mencabut seluruh XR stack.
4. Bangun scene baru melalui script Editor dan API Unity. Jangan menyunting YAML scene, prefab atau meta secara manual.
5. Scene memiliki AR Session, XR Origin skala 1, kamera dengan ARCameraManager, ARCameraBackground dan pose tracking. Tambahkan ARTrackedImageManager dengan library satu gambar serta ARAnchorManager.
6. Buat URP renderer khusus dengan AR background feature. Satu kamera aktif; hindari kamera kedua dari rig headset. Setelan rendering dan loader melalui API Editor yang tersedia di versi terpasang.
7. Gunakan Canvas overlay dan InputSystemUIInputModule. Rig handphone tidak membutuhkan locomotion, controller, interactor tangan atau XR Origin offset setinggi pengguna headset.
8. Pastikan manifest hasil build tidak memiliki persyaratan headset-only. CAMERA untuk AR, INTERNET untuk LAN. Tidak meminta microphone, location atau storage permission tanpa kebutuhan.
9. AR Required dengan pemeriksaan ARSession availability/install/permission. Jika tidak didukung, tampilkan pesan jelas. Mode simulator Editor tidak dihitung sebagai bukti AR perangkat.

## Koordinat bersama adalah syarat multiplayer AR

Sinkronisasi NetworkObject tidak otomatis menyamakan ruang AR. Kedua sesi AR mempunyai origin berbeda. Gunakan **marker fisik yang sama sebagai frame papan**.

```text
          keadaan game host dalam koordinat papan B
                          |
                 network state dan events
                    /                 \
             handphone A          handphone B
             transform BA         transform BB
             kamera AR A          kamera AR B
                    \                 /
               kartu fisik yang sama di meja

worldPosition_i = BoardRoot_i.TransformPoint(boardPosition)
boardRay_i      = inverse(BoardRoot_i) * localCameraRay
```

BoardRoot adalah transform lokal per perangkat dan **tidak** mempunyai NetworkTransform. Monster dan barang dirender sebagai child lokal dari BoardRoot memakai ID dan posisi papan dari state host. Game state NetworkObject terpisah dari seluruh transform AR. Jangan broadcast pose dunia Unity dari host sebagai koordinat klien.

Marker awal dirancang sebagai ilustrasi tekstur kardus dengan kolase stiker tidak berulang, kontras tinggi, minimal 512 x 512 px, lebar fisik 0,20 m. Marker bukan QR untuk join. Simpan PNG, lebar fisik, SHA256 dan versi image library bersama project. Gunakan [panduan Augmented Images Google](https://developers.google.com/ar/develop/augmented-images): gambar berfitur unik, flat, ukuran diketahui, dan evaluasi `arcoreimg` bila tersedia. Skor target 75 adalah saran kualitas gambar, bukan pengganti tes tracking.

Kalibrasi: tunggu TrackingState.Tracking stabil 0,5 detik, simpan pose marker sebagai anchor lokal, tampilkan empat sudut dan panah. Normal papan +Y ke atas, +X ke kanan gambar, +Z menuju bagian atas gambar. Verifikasi orientasi dengan gizmo pada satu perangkat sebelum mengandalkannya. Koreksi rotasi image-to-board secara eksplisit jika sumbu provider berbeda.

Tidak mengubah pose BoardRoot setiap frame setelah start. Anchor mengikuti koreksi tracking normal perangkat. Jika marker teramati bergeser lebih dari 3 cm atau yaw 5 derajat relatif anchor, jeda dan kalibrasi ulang bersama. Marker harus tetap di tempat. Hilangnya marker dari kamera sesaat tidak otomatis berarti session tracking hilang.

Untuk MVP, jangan mengganti marker dengan dua tap bebas: dua tap pada dua sesi tidak menjamin origin sama. Printed marker adalah prasyarat demo. Tampilan marker pada layar boleh dicoba sebagai bantuan awal, tetapi ukur lebar dan uji glare lebih dulu.

## State jaringan dan authority

Satu `NetworkObject` game state. Host memegang tick 20 Hz, timer, seed PRNG, item ID, score, anger, baterai, lock, bite count dan hasil. Tidak ada Rigidbody network, client-authoritative score, atau multiplayer berbasis AI dummy yang diklaim sebagai dua pemain.

Kontrak awal paling sederhana adalah snapshot state kecil melalui NGO RPC/message. Boleh memakai NetworkVariables jika lebih bersih pada implementasi. Hindari dua sumber kebenaran. Kirim full state pada join dan update reliable untuk spawn/collect/bite/result. Pose reticle dan held input dikirim 10 Hz dengan seq number dan latest-wins semantics. Heartbeat input tidak boleh menjaga hold selamanya ketika client disconnect.

| Pesan | Arah | Isi minimum | Validasi host |
| --- | --- | --- | --- |
| Ready | client ke host | protocolVersion, build ID, marker hash, calibration generation | Maksimal dua pemain, versi/marker sama, lobby saja |
| Intent | client ke host | roundId, seq, action, targetId, rayOriginB, rayDirectionB, trackingValid | Sender ID dari transport, bukan payload; seq naik; rate limit; finite values; target hidup; jarak dan ray masuk collider board-space |
| Snapshot | host ke semua | roundId, revision, gameTime, phase, seed, score, anger, bites, players, items | Client menerima revision baru dari server saja |
| Event | host ke semua | roundId, eventId, type, actorId, itemId, serverGameTime | Deduplicate audio/animasi memakai eventId |
| Leave/Abort | host ke semua | reason | Kembali lobby dengan alasan yang terbaca |

Host memakai pose/ray client yang dilaporkan sebagai input tervalidasi secara geometri. Ini cukup untuk demo bersama teman, bukan anticheat kompetitif. Tidak mengirim video kamera, gambar ruang atau audio melalui jaringan.

LAN connect: host bind `0.0.0.0:7777`, tampilkan IPv4 adaptor Wi-Fi yang dipilih, client memakai IPv4 itu, bukan localhost. Jika banyak adaptor, tampilkan pilih alamat dan bantuan. Gunakan `UnityTransport.SetConnectionData` sesuai signature versi terpasang. Tanpa Relay, server cloud, atau port forwarding. Jika AP mengisolasi client, gunakan jaringan lain atau hotspot yang mengizinkan koneksi. Jangan otomatis mengubah firewall global.

Client baru ditolak selama ronde. Host disconnect atau app suspend membatalkan ronde dan kembali lobby; tidak ada host migration. Client disconnect juga membatalkan ronde dua pemain. Rematch menaikkan roundId, mereset seq/event state dan tidak menerima pesan lama.

## Tracking dan gangguan koneksi

Intent lebih tua dari 0,25 detik dianggap Idle. Kehilangan session tracking atau input heartbeat 0,75 detik menjeda game bersama, membekukan timer dan anger. Client tidak mendapat penalti akibat gangguan tersebut. Recovery memerlukan dua perangkat tracking valid, marker alignment masih valid, dan kedua pemain siap. Gangguan 15 detik membatalkan ronde ke lobby. RT T di atas 150 ms ditampilkan sebagai koneksi lambat; nilai ini target internal untuk Wi-Fi uji.

## Struktur yang disarankan

```text
Assets/PaketPanik/
  Runtime/Core/              rules, deterministic state, configuration
  Runtime/Network/           LAN session, validated intents, snapshots
  Runtime/AR/                support check, marker frame, alignment, ray conversion
  Runtime/Presentation/      HUD, monster, loot, sound, haptics
  Editor/                    idempotent scene/prefab/marker/build generator
  Tests/Editor/              rules and coordinate tests
  Art/                       marker, materials and original generated assets
  Scenes/PaketPanik.unity
Builds/Android/PaketPanik.apk
Design/evidence/             design baseline only
Evidence/                   test reports, build logs, device QA and timings
```

Pisahkan core rules dari AR dan transport supaya tes aturan tidak membutuhkan kamera. Jangan membuat framework generik, asset pipeline kompleks, ECS, dependency injection container atau arsitektur plugin. Batas modul di atas adalah tanggung jawab, bukan kewajiban membuat banyak kelas kosong.

## Command dan akses

Unity CLI resmi berada di `C:\Users\fathahnoor\AppData\Local\Unity\bin\unity.exe`. Versi terverifikasi 1.0.0-beta.8. Pada akses akun host, `unity status --json` menemukan project ini pada port 7800 dalam state ready. Akses sandbox saja sebelumnya tidak menemukannya. Gunakan `unity list --json`, help command, lalu command yang benar-benar tersedia. Selalu pilih instance project ini ketika beberapa editor terbuka.

Gunakan Pipeline eval untuk inspeksi dan memanggil builder setelah script berhasil dikompilasi. Jika menggunakan batch build, jangan menjalankan editor kedua pada project yang sedang terbuka. Build lewat editor yang sedang terbuka. Simpan status sebelum build panjang.

Google menjelaskan perbedaan AR Required/Optional dan pengecekan runtime dalam [Enable AR](https://developers.google.com/ar/develop/unity-arf/enable-arcore). Model handphone perlu dicocokkan dengan [daftar perangkat ARCore](https://developers.google.com/ar/devices), lalu tetap diuji pada perangkat nyata.
