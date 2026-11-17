# Chính sách bảo mật / Security policy

## Báo lỗ hổng / Reporting a vulnerability

**KHÔNG mở issue công khai.** Dùng [Báo cáo lỗ hổng riêng tư của GitHub](https://github.com/tsudev-org/tsuswico/security/advisories/new).

Cam kết phản hồi trong **7 ngày**, bản sửa cho lỗi nghiêm trọng trong **30 ngày**; người báo được ghi công trong ghi chú
phát hành nếu muốn.

_Please do not open a public issue. Report privately via GitHub private vulnerability reporting (link above). We reply
within 7 days and fix critical issues within 30 days._

## Phạm vi / Scope

- Mã trong repo này (CLI, thư viện lõi, bộ cài, website `swico.tsudev.com`).
- Kết nối mạng DUY NHẤT của phần mềm là kiểm tra bản mới trên GitHub (`src/Tsudev.Audit.Windows/UpdateAdapters.cs`,
  xem [PRIVACY.md](PRIVACY.md)); tệp cập nhật được đối chiếu SHA-256 với `SHA256SUMS.txt` của cùng bản phát hành trước khi
  chạy ([docs/UPDATES.md](docs/UPDATES.md)).

## Phiên bản được hỗ trợ / Supported versions

Chỉ bản phát hành mới nhất.
