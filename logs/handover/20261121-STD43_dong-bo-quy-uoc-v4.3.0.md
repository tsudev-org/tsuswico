# PHIẾU YÊU CẦU - Đồng bộ tsudev-standards v4.3.0

- **Mã phiếu**: 20261121-STD43
- **Từ**: phiên tsudev-standards, 21/11/2026 - **Đến**: phiên kế tiếp của `tsuswico`
- **Trạng thái**: ĐÓNG (21/11/2026, phiên tsuswico-a2)
- **Nguồn**: https://github.com/tsudev-org/tsudev-standards/releases/tag/v4.3.0 (PR #66)

## Việc cần làm

1. Sync `.standards/` lên v4.3.0 trong PR kế tiếp.
2. Phiếu đề xuất 20261120-02 (câu kênh web, v3.9.2) ĐÓNG, KHÔNG phát hành: từ v4.0.0 mọi kênh đều kỳ 1 năm không tự gia hạn, `PRODUCT_POLICY.md` mục 7.5 chỉ còn MỘT câu đúng cho cả kênh web. `swico account` in nguyên văn câu 7.5 hiện hành trước liên kết mua.
3. `PACKAGING.md` mục 2.2 (mới) ghi lại đúng cách đã làm khi đổi `tsudev-swico` thành `tsuswico`: giữ manifest Scoop cũ kèm `notes`, winget `tsudev.SWICO` giữ nguyên. Không cần sửa gì thêm.

## Kết quả xử lý

- 21/11/2026 (tsuswico-a2): sync thẳng lên **v5.0.0** (bao trùm v4.3.0) trong PR bản sắc swico (phiếu STD50).
- Mục 2: `swico account` đã in nguyên văn câu 7.5 v4.0.0 từ PRO2 (#57); test `ProNoticeTests` đối chiếu bảng
  câu chữ trong `.standards/docs/PRODUCT_POLICY.md` vẫn xanh trên v5.0.0 ⇒ không phải sửa. Phiếu đề xuất
  `tsudev-standards/logs/handover/20261120-02_*` coi như ĐÓNG theo phiếu này.
- Mục 3: không cần làm gì (đã đúng `PACKAGING.md` mục 2.2).
