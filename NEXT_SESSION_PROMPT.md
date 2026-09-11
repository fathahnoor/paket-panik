# Prompt penerus lintas harness

Lanjutkan project C:\DevPath\260911_demo-mr. Baca instruksi global/project, TASK_STATE.md, HANDOFF.md, Evidence/UIUX/WORKLOG.md, progress.md, task_plan.md dan findings.md terlebih dahulu.

PAKET PANIK sudah memiliki implementasi Android AR multiplayer LAN dari OpenCode. Codex kemudian memperbarui UI/UX: beranda paket, alur gabung, kesiapan, HUD, panduan dan struk hasil. Jangan mengulang pembangunan awal. Mulai dari checkpoint yang benar-benar terverifikasi di HANDOFF.md.

Pengguna meminta commit lokal berkala dan tepat satu push setelah tugas UI/UX selesai. Periksa catatan push dan git status sebelum bertindak; jangan mengulang push yang sudah selesai. Perubahan marker cache, simulation settings dan preloaded assets sudah ada sebelumnya, jadi pertahankan dan jangan stage tanpa sengaja.

Gunakan Unity CLI resmi untuk inspect, Play Mode, compile, tests dan build. Jangan mengedit YAML scene/prefab/meta secara manual. Tools/UIUXReview.cs menyediakan fixture Editor, bukan bukti multiplayer di HP. Gunakan PaketPanik.Tests untuk seluruh 22 kasus EditMode.

Sebelum menyatakan siap untuk itch.io, jalankan pengujian dua HP nyata dan onboarding pemain mandiri melalui Evidence/DEVICE-QA-CHECKLIST.md. Jangan mengarang keberhasilan AR, networking, performa atau keyboard Android. Publikasi ke itch.io belum dilakukan dan bukan langkah otomatis dari handoff ini.

Catat setiap checkpoint penting di progress.md, TASK_STATE.md dan HANDOFF.md. APK berada di Builds/Android/PaketPanik.apk dan diabaikan Git. Handoff harus tetap dapat dipakai tanpa chat, memori atau alat khusus harness asal.
