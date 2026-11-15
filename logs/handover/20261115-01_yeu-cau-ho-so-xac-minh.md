# PHIẾU YÊU CẦU - Hồ sơ trên các nền tảng xác minh (Best Practices, Scorecard, phát hành, kênh gói) cho `tsudev-swico`

- **Mã phiếu**: 20261115-01 (phiếu con)
- **Từ**: phiên agent-files (repo `tsufiles`), chủ dự án yêu cầu 15/11/2026 - **Đến**: phiên kế tiếp của repo `tsudev-swico`
- **Trạng thái**: ĐANG - 2.1, 2.2 xong (PR #20); 2.3 hồ sơ Best Practices còn lại
- **Phiếu mẹ (ĐỌC TRƯỚC, mục 0 nguyên tắc + mục 3 quy trình)**: `~/projects/logs/handover/20261115-01_yeu-cau-ho-so-xac-minh-toan-he.md`
- **Công cụ + mẫu đã đạt passing 100%**: `~/projects/logs/tools/bestpractices/` (`README.md`, `bp.cjs`, `mau-tsufiles.json`);
  mẫu hồ sơ dạng đọc: `~/projects/tsufiles/docs/best-practices.md`.

## 1. Hiện trạng đo 15/11/2026

Apache-2.0, 2 Release, SHA256SUMS có nhưng **không có chữ ký**.

## 2. Việc của repo này (theo thứ tự)

- [x] 2.1. Ký SHA256SUMS (ECDSA như tsufiles, hoặc cosign keyless) + `attest-build-provenance`; ghi cách kiểm trong README.
- [x] 2.2. Best Practices passing + `scorecard.yml`.
- [ ] 2.3. Hồ sơ Best Practices (nếu áp dụng): soạn `docs/best-practices.json` + `.md` (mỗi `Met` có URL bằng chứng trên
  `main`), gộp PR, rồi `bp.cjs new` -> `fill` chạy thử -> xem ảnh -> `fill --submit` -> `inspect` ra passing 100% -> huy hiệu README.
- [ ] 2.4. Ghi kết quả có số đo dưới đây + đánh dấu dòng `tsudev-swico` ở mục 4 phiếu mẹ + một dòng mục 7 kế hoạch `20261113-02`.

**Nhắc lại**: không ghi `Met` cho điều repo chưa làm. Chưa đạt thì sửa repo trước, hoặc ghi `Unmet`/`N/A` trung thực.
Tự chứng nhận sai bị phát hiện sẽ làm hỏng hồ sơ SignPath của cả hệ.

## Kết quả xử lý (phiên nhận điền)

- 16/11/2026 (tsudev-swico-34): 2.1 `release.yml` ký `SHA256SUMS.txt` bằng Sigstore keyless (`SHA256SUMS.txt.sigstore.json`) + `attest-build-provenance` cho exe/zip/SHA256SUMS; cách kiểm ở README "Xác minh tệp tải về". Có hiệu lực từ bản phát hành KẾ TIẾP (chưa gắn tag mới - QU-2 chặn). 2.2 `scorecard.yml` + huy hiệu. Còn 2.3 (Best Practices: cần `bp.cjs` + phiên đăng nhập bestpractices.dev) và 2.4.
