# Báo nhầm antivirus - Tsu Swico

Quy trình: `.standards/docs/DISTRIBUTION_TRUST.md` mục 3. Job `virustotal` của `release.yml` quét mọi
`tsuswico_*.exe` / `tsuswico_*.zip` của Release nháp; có engine gắn cờ ⇒ Release giữ ở nháp.

## Sổ ghi

| Ngày | Tệp | SHA-256 | Engine | Xử lý | Kết quả |
| --- | --- | --- | --- | --- | --- |
| 22/11/2026 | `tsuswico_26.11.2201_windows-x64-setup.exe` (bản dựng đầu, Inno `SolidCompression=yes`, KHÔNG công bố) | `f198cc29ab1c4dc6be3a0cfc3e75899e11adb1c2aaafc505710f39a48d55ad9d` | APEX `Malicious`, DeepInstinct `MALICIOUS` (2/74, cả hai học máy) | `swico.exe` bên trong 0/70, zip 0/74 ⇒ cờ thuộc lớp bọc Inno nén solid. Đổi `SolidCompression=no` (cách tsucontact đã đo 0/74), xoá Release nháp, dựng lại cùng số hiệu. Không nộp báo nhầm | Vẫn 2/74 ⇒ không phải do solid |
| 22/11/2026 | `tsuswico_26.11.2201_windows-x64-setup.exe` (bản dựng lại, `SolidCompression=no`, **ĐÃ CÔNG BỐ**) | `5439b7cbb3cea3bfe73abcad2f597102ebf48f16d0f6ceb131875f927be346db` | APEX `Malicious`, DeepInstinct `MALICIOUS` (2/74) | Đo đối chứng: bộ cài **26.11.2001** đang công bố = 1/74 (APEX) ⇒ cờ APEX có từ trước, bám lớp bọc Inno chưa ký; `swico.exe` 0/70, zip 0/74. Công bố vì bản trước nhúng runtime .NET 8 hết vá (agent chọn theo uỷ quyền của chủ dự án 22/11/2026); ghi chú minh bạch trong Release notes | 👤 nộp báo nhầm APEX + DeepInstinct (hạn nội bộ 24/11/2026) - hồ sơ dưới |
| 22/11/2026 | `tsuswico_26.11.2001_windows-x64-setup.exe` (đo đối chứng, tải lên VT lần đầu) | `7a1ec864f8c3fcf28fccb3c183dfd2ca19f7ee49999ad93983028fe14a3cb196` | APEX `Malicious` (1/74) | Bản cũ, đã thay bằng 26.11.2201 | Gộp vào hồ sơ APEX |

## Hồ sơ báo nhầm (DISTRIBUTION_TRUST mục 3.1) - 👤 nộp ở cổng false positive của APEX và DeepInstinct

- **Sản phẩm:** Tsu Swico 26.11.2201 (`tsuswico_26.11.2201_windows-x64-setup.exe`), kèm 26.11.2001 cho APEX.
- **SHA-256:** `5439b7cbb3cea3bfe73abcad2f597102ebf48f16d0f6ceb131875f927be346db` (26.11.2201); `7a1ec864f8c3fcf28fccb3c183dfd2ca19f7ee49999ad93983028fe14a3cb196` (26.11.2001).
- **Nguồn:** https://github.com/tsudev-org/tsuswico/releases/tag/v26.11.2201 (commit `c4307d9`); chứng thực: `gh attestation verify <tệp> -R tsudev-org/tsuswico`; SBOM `swico-sbom.json` trong Release.
- **VirusTotal:** https://www.virustotal.com/gui/file/5439b7cbb3cea3bfe73abcad2f597102ebf48f16d0f6ceb131875f927be346db
- **Mô tả (gửi hãng, tiếng Anh):** *Tsu Swico is an open-source (Apache-2.0) Windows CLI that audits Windows/Office licensing and collects hardware inventory into a local HTML/XLSX report. It runs locally; its only network calls are an update check against the GitHub Releases API (can be disabled with `--no-update-check`) and, only after the user runs `swico login`, the TsuDEV account service. The installer is a standard Inno Setup 6 package built reproducibly on GitHub Actions with SLSA provenance; the contained `swico.exe` scans clean (0/70). Not yet Authenticode-signed (SignPath Foundation application pending).*
- **Chính sách:** `PRIVACY.en.md`, `SECURITY.md` trong repo.

## Phòng từ khi dựng

- Bộ cài Inno: `lzma2/max`, **không** solid.
- Gốc rễ lâu dài: ký Authenticode (SignPath, 3.4) - engine học máy chấm nặng bộ cài chưa ký.
- `swico.exe`: .NET self-contained single-file, không UPX/packer/obfuscator; chưa ký Authenticode (chờ SignPath, 3.4).
- Cờ học máy với bộ cài không ổn định giữa các lần dựng ⇒ bị cờ thì xem engine + quét riêng exe/zip trước; chỉ khi
  dựng lại vẫn bị cờ mới soạn hồ sơ báo nhầm (mục 3.1) gửi đúng hãng đang gắn cờ.
