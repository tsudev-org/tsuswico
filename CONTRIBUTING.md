# Đóng góp cho Tsu Swico

_English summary below._

## Báo lỗi, đề xuất

- **Lỗi**: mở [issue](https://github.com/tsudev-org/tsuswico/issues/new/choose) theo mẫu "Báo lỗi" - phiên bản,
  Windows, lệnh đã chạy, kết quả. Che thông tin nhận dạng máy (tên máy, serial, khoá sản phẩm) trước khi đính kèm báo cáo.
- **Lỗ hổng bảo mật**: KHÔNG mở issue công khai - xem [SECURITY.md](SECURITY.md).

## Gửi thay đổi (pull request)

1. Fork, tạo nhánh từ `main`. Cần .NET SDK theo `global.json`.
2. Viết mã kèm **test** xUnit trong `tests/unittests`: mọi chức năng mới hoặc lỗi được sửa phải có test. Logic thuần đặt
   trong `Tsudev.Audit.Core` để test chạy được trên Linux ([docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) mục 2).
3. Chạy như CI trước khi gửi (cảnh báo là lỗi - `TreatWarningsAsErrors`, bộ phân tích .NET `latest-recommended`):
   ```bash
   dotnet build Tsudev.SystemAudit.sln -c Release
   dotnet test tests/unittests -c Release
   ```
4. Commit theo [Conventional Commits](https://www.conventionalcommits.org/). Mở PR vào `main`, mô tả cách đã kiểm.

---

## English summary

Report bugs with the issue form (redact machine identifiers); report vulnerabilities privately (SECURITY.md). Pull
requests branch from `main`, must include xUnit tests for every new feature or bug fix, must build with zero warnings
(`TreatWarningsAsErrors`, .NET analyzers `latest-recommended`) and pass `dotnet test tests/unittests`. Use Conventional
Commits.
