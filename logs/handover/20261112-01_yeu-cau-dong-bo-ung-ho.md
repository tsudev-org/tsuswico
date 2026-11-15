# PHIẾU YÊU CẦU - Đồng bộ mục Ủng hộ

- **Mã phiếu**: 20261112-01
- **Từ**: phiên tsudev-files-01 (12/11/2026), theo yêu cầu trực tiếp của chủ dự án
- **Đến**: phiên làm việc trên `tsudev-swico`
- **Trạng thái**: ĐÓNG (16/11/2026) - PR tsuswico #20, đã deploy

## 1. Đọc trước

Phiếu gốc `~/projects/logs/handover/20261112-01_yeu-cau-dong-bo-ung-ho-he-sinh-thai.md`: thông tin Ủng hộ chuẩn (VIB `056485506`, NGUYEN TRANG TINH SU, chuỗi VietQR, ảnh
`vietqr-vib.svg` + SHA-256) và kiến trúc bắt buộc (một nguồn dữ liệu, app chỉ trỏ về trang, không dịch vụ QR bên thứ
ba, ghim SHA-256 + kiểm VietQR lúc build, CODEOWNERS, chính sách kho di động, không lưu bí mật).
Bản tham chiếu: repo `tsudev-files` (`website/src/data/ung-ho.json`, `website/src/lib/ung-ho.ts`,
`website/public/ung-ho/vietqr-vib.svg`, `tools/ung-ho/tao-vietqr.py`).

## 2. Việc riêng của repo này (khảo sát nhanh 12/11/2026 - cần rà lại)

- Khảo sát nhanh: chưa có trang/nút/FUNDING ủng hộ nào (không có gì cũ để gỡ).
- Khi sản phẩm có chỗ phù hợp (hộp Giới thiệu, chân trang): thêm liên kết "Ủng hộ" mở `https://tsudev.com/ung-ho/`
  (chưa có trang trung tâm thì dùng `https://files.tsudev.com/ung-ho/`). Không nhúng số tài khoản vào bản build.
- Thêm `.github/FUNDING.yml` với `custom: ["https://tsudev.com/ung-ho/"]` khi trang trung tâm đã chạy.

## 3. Việc chung

Theo phiếu gốc mục 4: gỡ kênh cũ, `FUNDING.yml`, trang/nút Ủng hộ, cổng kiểm + test, ghi `logs/STATE.md`.

## 4. Kết quả xử lý (phiên nhận điền, và ghi một dòng vào phiếu gốc mục 6)

- 16/11/2026 (phiên tsudev-swico-34): rà lại - không có kênh/nút/FUNDING ủng hộ cũ nào. Thêm "Ủng hộ tsudev" ở chân trang swico.tsudev.com + `.github/FUNDING.yml`, đều mở `https://tsudev.com/ung-ho/` (một hằng `UNG_HO_URL`). Không có STK/QR trong repo hay bản build. App là CLI không có hộp Giới thiệu nên không thêm nút trong app. Production đo 16/11: liên kết hiện ở mọi trang.

## 5. Bổ sung 12/11/2026 - phương án theo nền tảng (đã chốt)

Áp dụng phiếu `~/projects/logs/handover/20261112-02_quyet-dinh-ung-ho-theo-nen-tang.md` (thay mục 3.5 của phiếu gốc):
web hiện đầy đủ; desktop/Android chỉ nút mở trang web kèm câu "tự nguyện, 100% tới nhà phát triển, không mở khóa gì";
iOS App Store ẩn hoàn toàn. Ghi một dòng kết quả vào mục 5 của phiếu đó.
