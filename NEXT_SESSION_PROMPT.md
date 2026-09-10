# Prompt penerus lintas harness

Lanjutkan project `C:\DevPath\260911_demo-mr`. Baca instruksi global yang berlaku, TASK_STATE.md, HANDOFF.md, progress.md, task_plan.md, findings.md, lalu Design/GAME_DESIGN.md, Design/TECH_SPEC.md dan Design/BUILD_PLAN.md. Jangan bergantung pada chat atau memori agent sebelumnya.

Bangun PAKET PANIK sesuai checkpoint terakhir yang benar-benar terverifikasi. Pengguna meminta game sederhana Android handheld AR multiplayer, riset tema pasar, target demo beberapa jam, dan sudah mengizinkan implementasi setelah rancangan, keputusan mandiri saat AFK, commit serta push GitHub berkala. Tersedia minimal dua handphone, tetapi model, koneksi USB, dan ARCore belum diketahui sampai diperiksa.

Pertahankan template MR Unity 6000.6.0f1 di project ini. Buat scene khusus handphone dengan AR Foundation/ARCore dan multiplayer LAN dua perangkat. Jangan membuat project baru atau mengganti seluruh template. Gunakan Unity CLI resmi untuk inspect, compile, scene generation, tests dan build. Jangan mengedit serialized scene/prefab/meta sebagai YAML mentah.

Verifikasi dahulu branch, HEAD, staging, pre-existing changes, remote dan Unity instance. Preserve perubahan pengguna. Gunakan scope MVP dua pemain pada desain, satu marker fisik sebagai koordinat bersama, state authoritative pada host, dan local BoardRoot tanpa NetworkTransform. Jangan memakai simulasi atau bot sebagai bukti multiplayer fisik.

Kerjakan tahap kecil sampai hasil lulus atau ada blocker nyata. Catat tujuan tahap sebelum memulai, update progress dan handoff setelah tahap dan sebelum build panjang, kemudian commit/push checkpoint yang sesuai. Jangan force push, menghapus aset sumber, membaca credential, memublikasikan release/Play Store atau memposting media sosial. Izin akses tool tetap harus dihormati.

Jika hardware belum dapat dioperasikan, tetap selesaikan pekerjaan yang dapat diuji di laptop, build APK bila memungkinkan, dan berikan prosedur uji dua perangkat dengan status NOT RUN. Jangan mengarang keberhasilan tracking, performa atau video. Selesaikan dengan file artifact, status tes, commit/push, blocker, dan langkah pertama sesi berikutnya.

Heartbeat, bila aktif, hanya melanjutkan task yang sama. Jangan membuat scheduler duplikat. Rujuk TASK_STATE.md untuk status aktivasi aktual dan cara berhenti. Selesainya desain saja tidak menyelesaikan tujuan yang sudah diperluas menjadi implementasi.
