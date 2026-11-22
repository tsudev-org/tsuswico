# Báo nhầm antivirus - Tsu Swico

Quy trình: `.standards/docs/DISTRIBUTION_TRUST.md` mục 3. Job `virustotal` của `release.yml` quét mọi
`tsuswico_*.exe` / `tsuswico_*.zip` của Release nháp; có engine gắn cờ ⇒ Release giữ ở nháp.

## Sổ ghi

| Ngày | Tệp | SHA-256 | Engine | Xử lý | Kết quả |
| --- | --- | --- | --- | --- | --- |
| 22/11/2026 | `tsuswico_26.11.2201_windows-x64-setup.exe` (bản dựng đầu, Inno `SolidCompression=yes`, KHÔNG công bố) | `f198cc29ab1c4dc6be3a0cfc3e75899e11adb1c2aaafc505710f39a48d55ad9d` | APEX `Malicious`, DeepInstinct `MALICIOUS` (2/74, cả hai học máy) | `swico.exe` bên trong 0/70, zip 0/74 ⇒ cờ thuộc lớp bọc Inno nén solid. Đổi `SolidCompression=no` (cách tsucontact đã đo 0/74), xoá Release nháp, dựng lại cùng số hiệu. Không nộp báo nhầm | Xem dòng kế |

## Phòng từ khi dựng

- Bộ cài Inno: `lzma2/max`, **không** solid.
- `swico.exe`: .NET self-contained single-file, không UPX/packer/obfuscator; chưa ký Authenticode (chờ SignPath, 3.4).
- Cờ học máy với bộ cài không ổn định giữa các lần dựng ⇒ bị cờ thì xem engine + quét riêng exe/zip trước; chỉ khi
  dựng lại vẫn bị cờ mới soạn hồ sơ báo nhầm (mục 3.1) gửi đúng hãng đang gắn cờ.
