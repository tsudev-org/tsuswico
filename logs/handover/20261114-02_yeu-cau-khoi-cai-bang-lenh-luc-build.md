# PHIẾU YÊU CẦU - Khối "Cài bằng lệnh" sinh lúc build từ catalog chính thức

- **Mã phiếu**: 20261114-02
- **Từ**: phiên 52 repo `tsudev`, 14/11/2026 - kế hoạch tổng 20261114-01, việc C5
- **Gửi**: tsudev-cwico, tsudev-swico, tsudev-contact, tsudev-trafficmonitor, tsufiles, lotus-share
- **Trạng thái**: ĐÓNG (16/11/2026) - PR tsuswico #20, đã deploy

## Yêu cầu

Trang sản phẩm `*.tsudev.com` của repo hiện một khối **"Cài bằng lệnh"** liệt kê MỌI kênh đang có của app (winget,
Scoop, Homebrew, APT, DNF), mỗi lệnh có nút Chép, nhóm theo hệ điều hành - giống thẻ app trên `tsudev.com/apps`.

## Cách làm (đã chốt, không chọn lại)

1. **Sinh lúc build**, không gọi mạng lúc chạy: bước build tải `https://pkg.tsudev.com/catalog.json` (APT/RPM), catalog
   của `tsudev-org/scoop-bucket` và `tsudev-org/homebrew-tap`, lọc theo `homepage` = URL website của repo, ghi ra tệp dữ
   liệu tĩnh. Mã gói winget giữ trong cấu hình của repo (manifest nằm ở microsoft/winget-pkgs). Tham chiếu cách ghép:
   `tsudev/packages/ui/src/lib/ecosystem.ts` (`withPkg`) + `scripts/pkg-catalog` của tsudev.
2. Tải catalog lỗi lúc build ⇒ build THẤT BẠI có thông báo rõ (không lặng lẽ ra khối rỗng).
3. Không chép tay tên gói - đổi tên gói ở kho thì trang tự đúng ở lần build sau.
4. Tên gói TsuFiles vẫn là `tsudev-files` (trình cập nhật bản 2.4.0-vn.1 cần) - đừng đổi theo tên hiển thị.
5. Khối nằm TRÊN nút tải tệp cài; kèm một dòng "Gói được ký - trình quản lý gói tự kiểm SHA-256 và chữ ký".

## Nghiệm thu

- Trang production hiện đúng các kênh như `tsudev.com/apps` cho cùng app (đối chiếu từng lệnh).
- `node ~/projects/logs/tools/quet-giao-dien.cjs https://<site>.tsudev.com /` ra OK.
- ⛔ Chạm chân trang / logo / script cài ⇒ PR + chờ chủ dự án duyệt (cổng Bảo vệ bản quyền).

## Kết quả (phiên tsudev-swico-34, 16/11/2026)

Đã đọc. `website/scripts/cai-bang-lenh.mjs` chạy trong `npm run build`: tải catalog APT/RPM (ghim vân tay khoá), Scoop,
Homebrew; lọc `homepage` = `https://swico.tsudev.com`, bỏ gói chuyển tiếp; mã winget đọc từ `packaging/package.json`.
Tải/kiểm lỗi ⇒ build dừng có thông báo. Nhóm theo hệ điều hành, nút Chép (`public/chep.js`, CSP `'self'`), nằm trên nút tải.
Production: Windows - `winget install --id tsudev.SWICO -e`, `scoop install tsudev/tsuswico` (gói mới, scoop-bucket#9).

Lệch có chủ đích: KHÔNG ghi "Gói được ký" - tệp `.exe` chưa ký Authenticode (SignPath chờ duyệt); trang chỉ ghi điều
đúng: trình quản lý gói tự kiểm SHA-256. Đối chiếu với `tsudev.com/apps`: thẻ ở đó còn `tsudev-swico` tới khi phiên
tsudev chạy `pkg:catalog` (phiếu `tsudev/logs/handover/20261116-S1`).
