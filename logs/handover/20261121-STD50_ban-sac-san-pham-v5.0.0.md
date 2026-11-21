# PHIẾU YÊU CẦU - Bản sắc riêng của Tsu Swico (tsudev-standards v5.0.0)

- **Mã phiếu**: 20261121-STD50
- **Từ**: phiên tsudev-standards, 21/11/2026 - **Đến**: phiên kế tiếp của `tsuswico`
- **Trạng thái**: ĐÓNG (21/11/2026, phiên tsuswico-a2)
- **Nguồn**: https://github.com/tsudev-org/tsudev-standards/releases/tag/v5.0.0 - `docs/DESIGN_SYSTEM.md` mục 1.2,
  CHANGELOG 5.0.0 "Hướng dẫn nâng cấp"
- **Bối cảnh**: chủ dự án muốn mỗi sản phẩm có đặc trưng nổi bật riêng (màu, giao diện, bố cục). Trước v5.0.0, 9/11
  sản phẩm dùng cùng một màu xanh. Từ v5.0.0: nền/chữ/trạng thái/tiếp cận là chung; màu nhấn, bố cục, bo góc, hoạ
  tiết, font tiêu đề là riêng, khai ở `.standards/tokens/products/swico.json` - KHÔNG tự khai lại tại repo này.

## Bản sắc đã chốt cho `tsuswico`

| Trường | Giá trị |
| --- | --- |
| `data-product` | `swico` |
| Màu chính Tối / Sáng | `#E49324` / `#975D01` (hue OKLCH 68; lý do: logo đơn sắc (trắng trên xám than) - chọn vàng hổ phách của con dấu bản quyền) |
| Bố cục `layout` | `cong-cu` - vùng thao tác chính (kéo thả/chọn tệp/nút chạy) ngay màn đầu, căn giữa; giới thiệu nằm dưới |
| Bo góc `shape` | `sac` - 2/3/4px, nút 3px |
| Hoạ tiết `motif` | `mach-in` (tĩnh, ở hero + `og-image`, phủ ≤ 15%) |
| Font tiêu đề | Space Grotesk (tự host woff2 bộ `vietnamese`, tối đa 2 độ đậm) |

Muốn đổi bất kỳ giá trị nào: PR vào tsudev-standards (sửa `hue`/`layout`... + chạy `gen-product-palette.mjs`), cổng
kiểm chống trùng phải xanh. KHÔNG ghi đè tại repo này.

## Việc cần làm - chia hai PR, mỗi PR một phiên

**PR 1 - màu + hình** (`feat(ui): bản sắc swico theo tsudev-standards v5.0.0`):
1. Sync `.standards/` lên v5.0.0 (bước `curl` sync-standards.sh trước, theo `docs/SYNC.md`).
2. Web: nạp `.standards/tokens/tokens.css` rồi `.standards/tokens/products.css`; `<html data-product="swico">` (cạnh
   `data-theme`). Desktop/di động: gộp đè `products/swico.json` lên `color.light`/`color.dark` lúc build.
3. Xoá bảng màu tự khai (`palette.css`, `extensions.*.json`, dải `--color-*` riêng) và mã màu cứng còn sót.
4. Giá trị đã lưu `warm` đọc thành `light` (khối `warm` đã gỡ khỏi token).
5. Nút dùng `var(--radius-control)`; h1-h3 và số liệu lớn dùng `var(--font-display)`.
6. Ảnh chụp trước/sau, Sáng + Tối, đính vào PR. Cổng kiểm + test xanh.

**PR 2 - bố cục** (`feat(ui): bố cục cong-cu + hoạ tiết mach-in`):
1. Dựng phần THÂN trang chủ website / màn hình đầu app theo `cong-cu`; header, khu tài khoản, chân trang, bộ chọn
   giao diện giữ theo `ECOSYSTEM_IDENTITY.md` mục 4.
2. Hoạ tiết `mach-in` tĩnh ở hero, vẽ bằng `accent`/`glow`; sinh lại `og-image.png` cùng hoạ tiết.
3. Ảnh chụp trước/sau, Sáng + Tối.

## Việc riêng của repo này

- Logo đơn sắc nên màu nhấn là vàng hổ phách (hue 68) - con dấu bản quyền. Báo cáo HTML xuất ra (nếu có tô màu) dùng cùng `colors.light`.

## Kết quả xử lý

- **PR 1 #66 `7085d29`**: sync v5.0.0; website nạp `.standards/tokens/tokens.css` + `products.css`,
  `<html data-product="swico">`, xoá `palette.css` + bản sao `tokens.css`; bo góc `var(--radius-*)`, nút
  `var(--radius-control)`; h1-h3 `var(--font-display)` = Space Grotesk tự host (500 + 600; latin, latin-ext,
  vietnamese - cần latin cho chữ cái thường). `warm` cũ vẫn đọc thành Sáng (`theme.js`, `check-theme`). Báo cáo
  HTML/.xlsx (mục "việc riêng"): `DesignTokens.Color` gộp đè `products/swico.json`, bộ bo góc `sac`, `--font-display`;
  chữ "Swico" ở đầu báo cáo lấy `colors.dark.accent` (gỡ 2 mã màu viết cứng); chữ ký TsuDEV chân trang giữ lớp nền.
  Test `ProductIdentityTests`; 357 PASS.
- **PR 2 #67 `68d6e95`**: trang chủ `cong-cu` (vùng thao tác: tải bộ cài + lệnh winget/Scoop có nút Chép, căn giữa
  màn đầu; giới thiệu xuống dưới); hoạ tiết `mach-in` tĩnh ở hero (tô `var(--accent)`, hai dải mép) + `og-image.png`
  sinh lại bằng chung `website/src/lib/mach-in.mjs`.
- Ảnh trước/sau Sáng + Tối: `docs/anh-chup/ban-sac-v5/` (đính trong mô tả PR). axe 16 trang x 2 chế độ 0 lỗi.
- Deploy swico.tsudev.com 21/11/2026 (version `ca57caa8`), production trả `data-product="swico"`.
- "Màn hình đầu app": swico là CLI, không có màn hình GUI ⇒ bố cục chỉ áp cho website; báo cáo HTML nhận màu/bo góc/font.
