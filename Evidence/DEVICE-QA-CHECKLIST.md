# PAKET PANIK - Checklist Uji Dua Perangkat

Status: BELUM DIJALANKAN. Dokumen ini panduan saat dua handphone tersedia.
APK: `Builds/Android/PaketPanik.apk` (SHA256 3D8F092D...C23187A, 61.5 MB, arm64-v8a, minSdk 26).

## Persiapan

1. Cetak marker: buka `Assets/PaketPanik/Art/Marker_PaketPanik_Print.html` di browser, cetak skala 100% (tanpa fit-to-page). Ukur sisi gambar: harus 20,0 cm. Letakkan datar di meja terang.
2. Pasang APK di dua HP: `adb install -r Builds/Android/PaketPanik.apk` atau salin file lalu izinkan "install unknown apps".
3. Pastikan kedua HP: Android 8.0+ (minSdk 26), mendukung ARCore (cek https://developers.google.com/ar/devices), punya Google Play Services for AR, dan tersambung ke Wi-Fi yang sama.
4. Catat: model HP, versi Android, versi Google Play Services for AR, jenis jaringan Wi-Fi.

## Urutan uji (catat LOLOS/GAGAL per item)

| ID | Uji | Cara | Lulus jika |
| --- | --- | --- | --- |
| D01 | Instal & buka | Pasang APK, buka aplikasi | Menu PAKET PANIK tampil, tanpa crash |
| D02 | Izin kamera | Tekan BUAT MEJA di HP A | Dialog izin muncul; setelah diberi, status kamera/tracking tampil |
| D03 | Buat meja | HP A: BUAT MEJA | Muncul IP + PIN; minta izin yang wajar saja (kamera, jaringan) |
| D04 | Gabung | HP B: isi IP + PIN, GABUNG MEJA | HP B masuk lobby, terlihat 2/2 pemain |
| D05 | Marker bersama | Keduanya pindai kartu 20 cm | Empat sudut + panah dan monster terlihat di posisi konsisten pada kedua layar |
| D06 | Offset (T05) | Ukur perbedaan posisi objek di kedua layar, 3 kali | Offset <= 3 cm, yaw <= 5 derajat |
| D07 | Siap & mulai | Keduanya tekan POSISI COCOK / SIAP, host Mulai | Hitung mundur 3 detik lalu ronde berjalan |
| D08 | Jaga/ambil | Satu menahan TENANGKAN, satu menahan AMBIL | Lampu menekan anger; barang terambil sekali, skor sama di dua layar |
| D09 | Baterai & pergantian | Jaga sampai lampu rendah | Pemain diminta gantian oleh kondisi baterai, bukan instruksi penguji |
| D10 | Sinkron 3 ronde (T06) | Main 3 ronde penuh, termasuk rematch | Skor/anger/gigitan/hasil identik di dua layar; rematch bersih |
| D11 | Tracking hilang (T07) | Tutup kamera HP B sesaat | Game pause bersama, tanpa penalti; resume setelah keduanya siap |
| D12 | Disconnect (T08) | Matikan app host / background | Ronde berakhir jelas, tidak ada hold hantu |
| D13 | Performa (T09) | Main 5 menit; catat dari logcat: `adb logcat -s Unity | findstr PAKET_PERF` | Median >= 30 FPS, tanpa crash; catat angka nyata apa adanya |
| D14 | Disimpan | Video satu take memperlihatkan dua layar sekaligus | Klip 15-25 detik, reaksi pemain asli |

## Keseruan kecil (BUILD_PLAN, 3 pasangan x 2 ronde)

- >= 5 dari 6 orang menjelaskan "satu jaga, satu ambil, gantian" setelah tutorial.
- >= 2 dari 3 pasangan memilih rematch tanpa dibujuk.
- Setiap ronde ada pergantian penjaga karena baterai.

## Yang tidak boleh dilakukan

- Jangan menyebut berhasil jika uji belum dilakukan; tulis NOT RUN atau GAGAL apa adanya.
- Jangan rekam/menyalin serial perangkat, IMEI, atau detail jaringan pribadi ke dokumen publik.
- Jangan mengubah marker (cetak ulang miring/bukan 20 cm) lalu menyalahkan tracking.
- Hasil video pribadi hanya dipublikasikan dengan izin orang yang tampak/terdengar.
