# PHIẾU YÊU CẦU AV1 - Antivirus / báo nhầm (tsuswico)

- **Mã phiếu**: 20261122-AV1
- **Thời điểm**: 22/11/2026
- **Người gửi**: agent-tsuswico (phiên tsuswico-6e) theo uỷ quyền của chủ dự án
- **Phiếu điều phối**: `~/projects/logs/handover/20261122-AV1_bao-nham-antivirus-toan-he.md` (bảng kết quả cả hệ + thư soạn sẵn từng hãng)
- **Chuẩn**: `.standards/docs/DISTRIBUTION_TRUST.md` mục 3, 3.1
- **Trạng thái**: MỞ

## 1. Kết quả VirusTotal 22/11/2026 - bản Latest `v26.11.2201`

| Tệp | Kết quả | Engine |
| --- | --- | --- |
| `swico.exe` | 0+0/74 | - |
| `tsuswico_26.11.2201_windows-x64-portable.zip` | 0+0/74 | - |
| `tsuswico_26.11.2201_windows-x64-setup.exe` | 2+0/74 | APEX `Malicious`, DeepInstinct `MALICIOUS` |

## 2. Việc cần làm

1. Tạo/cập nhật `docs/store/antivirus.md` (mẫu `.standards/docs/templates/TRUST_TICKET.md` mục D): tệp, SHA-256, engine, ngày.
2. Kiểm phần của repo này trong thư soạn sẵn (phiếu điều phối mục 3): mô tả sản phẩm đúng hành vi thật, link Release đúng.
3. 👤 Chủ dự án gửi thư/biểu mẫu (agent không có hộp thư/tài khoản). Ghi ngày gửi + mã hồ sơ vào sổ.
4. Đo lại VT 3-7 ngày sau (`~/.config/tsudev/virustotal.env`, API `/api/v3/files/<sha256>`); hết cờ ⇒ ghi ngày gỡ.
5. Bản phát hành sau: Release nháp → job VirusTotal → sạch mới công bố; cờ ⇒ quét riêng exe/zip + bản trước làm đối chứng trước khi đổi đóng gói (bài học tsuswico ISS-20261122-2153-055f).
- Đã có sổ `docs/store/antivirus.md` + hồ sơ (phiên tsuswico-6e). Việc còn: theo dõi AV-1 trong `logs/STATE.md`.

## 3. Khép phiếu

Đổi Trạng thái → ĐÓNG khi mọi cờ đã gỡ (hoặc 👤 chấp nhận) và sổ đã ghi.
