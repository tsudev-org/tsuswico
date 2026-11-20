# PHIẾU YÊU CẦU - PRO1: lưu cục bộ mặc định, Pro mới có đồng bộ/sao lưu đám mây (tsuswico)

- **Mã phiếu**: 20261119-PRO1 (phiếu con)
- **Từ**: agent-qulythuchi (phiên `qulythuchi-b2`), 19/11/2026, theo yêu cầu trực tiếp của chủ dự án
- **Đến**: phiên kế tiếp của repo `tsuswico`
- **Trạng thái**: MỞ - P1 (kiểm kê + ADR-0002 + v3.9.1) XONG 20/11/2026; trước đây: chưa viết mã trước khi tsudev-standards v3.9.0 (PRO-A) phát hành; phần nào cần máy chủ quyền lợi
  thì chờ PRO-B
- **Phiếu gốc (ĐỌC TRƯỚC)**: `~/projects/logs/handover/20261119-PRO1_ke-hoach-luu-cuc-bo-pro-dong-bo-toan-he.md` - mục 1 (yêu cầu nguyên văn tóm tắt), 2 (nguyên tắc N1-N8), 3 (thông báo nhiều
  thiết bị, chu kỳ 30 ngày), 5 (khung việc mỗi repo), 7 (nghiệm thu), 8 (câu hỏi 👤). Phiếu này THAY hàng T7 của
  `~/projects/logs/handover/20261119-01_ke-hoach-toan-he-v3.8.0.md` cho repo này.

## 1. Việc riêng của repo này

Desktop .NET, D1. Như tsufiles: Pro = đồng bộ cài đặt/hồ sơ qua PRO-D (thư viện .NET), đăng nhập loopback PKCE, thanh toán qua tsudev.com, huy hiệu.

## 2. Khung việc (tóm tắt phiếu gốc mục 5)

1. Kiểm kê: dữ liệu nào đang rời thiết bị, nằm ở đâu ⇒ ghi mục 4 dưới đây.
2. ADR riêng: phần nào là Pro, cách ân hạn cho người dùng cũ, kênh thanh toán theo PRODUCT_POLICY 3.3/4.3.
3. Mã: đọc quyền lợi (JWS tsudev, đệm ≤ 7 ngày), gate Pro ở MÁY CHỦ, thông báo nhiều thiết bị (5 test bắt buộc), màn
   Pro/Ủng hộ, huy hiệu PRO-C, vi + en, test.
4. Đo phản hồi thao tác chính (< 100 ms) và chịu tải (nếu có máy chủ), ghi vào PR.
5. Phiếu rà soát bảo mật + THREAT_MODEL. Không tự tạo khoá/tài khoản thanh toán thật ⇒ việc 👤.

## 3. Việc cần chủ dự án (👤)

Xem phiếu gốc mục 8 (giá Pro, ân hạn, nơi đặt dịch vụ đồng bộ dùng chung, ngoại lệ nhắc 30 ngày).

## 4. Kết quả xử lý (phiên nhận điền)

- 20/11/2026 - tsuswico-68: **P1 xong** (PR `feat/pro1-kiem-ke-adr-0002`). Kiểm kê: KHÔNG có dữ liệu người dùng nào rời
  máy; không lưu cài đặt/hồ sơ; kết nối mạng duy nhất là kiểm tra bản mới trên GitHub (tắt bằng `--no-update-check`)
  ⇒ không có đường đồng bộ miễn phí nào phải tắt, không cần ân hạn. **ADR-0002**: Pro = đồng bộ + sao lưu **báo cáo**
  qua TsuDEV Sync (không gian tên `tsuswico`, E2EE, báo cáo coi như D2 khi rời máy) - bản đám mây của tính năng gom
  nhiều máy vốn miễn phí bằng tay; không thêm tính năng Pro nào khác ⇒ không cần 👤 duyệt riêng. Đăng nhập loopback
  PKCE, token DPAPI, mã cài đặt chỉ sinh khi đăng nhập; thông báo nhiều thiết bị chỉ ở chế độ tương tác (không ở
  `--silent`/`--json-only`). Sync tsudev-standards **v3.9.1**.
  CÒN: P2 (`Entitlements` + `ProNotice` 5 test + `InstallationId`, làm được ngay với nguồn giả), P3 cần `client_id` Tsu
  Swico ở tsudev.com, P4 cần PRO-D (`TsuSync` .NET), P5 PRIVACY/THREAT_MODEL trước khi phát hành P3.
- 20/11/2026 - tsuswico-22: **P2 xong** (PR `feat/pro1-p2-entitlements`), `src/Tsudev.Audit.Core/Pro/`:
  `EntitlementVerifier` + `JwksKeySet` (BCL, chỉ ES256), `CachedEntitlementSource`, `ProNotice` + `ProNoticePrompt`
  (5 test bắt buộc 7.3 + `--silent`/`--json-only`, nội dung đối chiếu thẳng với bảng trong PRODUCT_POLICY),
  `InstallationIdStore` (sinh khi đăng nhập theo ADR-0002, lệch có chủ đích với 14.3 "lần chạy đầu"), `LocalStateStore`
  (`state.json`). 58 test mới. CHƯA nối vào `Program.cs` - nối ở P3 cùng lệnh `login` (cần `client_id`).

## 5. Đã chốt (19/11/2026, bổ sung sau khi mở phiếu)

Chủ dự án giao agent chốt mọi câu hỏi mở và xác nhận **chưa có người dùng thật** ⇒ phiếu gốc mục 8 ĐÃ CHỐT, mục 9
là **HỢP ĐỒNG CHUNG** (mọi repo dùng đúng định danh, không tự đặt khác): gói chung **TsuDEV Pro 49.000 ₫/tháng,
490.000 ₫/năm**; Ủng hộ 20/50/100/200 nghìn; Play `tsudev_pro` (`thang`/`nam`) + `ung_ho_20k`…`ung_ho_200k`; JWS
quyền lợi (ES256, `exp` ≤ 7 ngày, `badge`, `devices_30d`); HTTP 402 `pro_required`; mã cài đặt `X-Tsu-Installation`;
dịch vụ đồng bộ dùng chung ở repo mới `tsudev-org/tsusync`; huy hiệu `TsuBadge`; nội dung thông báo vi/en nguyên văn.
Pro hết hạn: chặn đẩy, kéo/xuất 90 ngày, báo trước 30 và 7 ngày, dữ liệu trên thiết bị không bao giờ bị xoá.

Làm được NGAY (không phụ thuộc PRO-B/PRO-D): kiểm kê, ADR, màn Pro/Ủng hộ, thông báo nhiều thiết bị (nguồn quyền lợi giả
trong test), tắt mọi đường gửi dữ liệu người dùng lên mạng ở bản miễn phí. Thứ tự toàn hệ: phiếu gốc mục 9.5.


