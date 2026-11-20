# Tài sản thương hiệu - Tsu Swico

Chuẩn: `.standards/docs/BRAND_ASSETS.md` mục 12, 14, 15. **Không sửa tay, không cắt tay** tệp nào dưới đây.

## Ảnh gốc (một và chỉ một)

| Tệp | Cỡ | SHA-256 |
| --- | --- | --- |
| `source/logo-swico.png` | 554x543, logo vẽ sẵn trên ô nền `#2C3942` | `cb2f58a2f7344a6c065d5b959a9a1d9c300622403a36fb5f2dfe355fb5ee7bb8` |

Chủ dự án duyệt logo riêng ngày 13/11/2026 (phiếu `logs/handover/20261113-01_yeu-cau-logo-rieng-swico.md`).
Nhận ảnh logo rời từ bất kỳ đâu: đối chiếu SHA-256 với bảng trên trước khi dùng (mục 13).

**Nợ (mục 15, `SHOULD`):** cạnh ngắn 543px < 1024px. Đủ cho cỡ lớn nhất đang dùng (icon 512, ICO 256, ảnh trình
thuật sĩ lớn nhất dùng logo 314px) nên checklist mục 14.4 đạt; muốn thêm cỡ 1024 thì cần ảnh gốc lớn hơn.

## Sinh lại

```bash
node website/scripts/brand-assets.mjs           # ghi tệp
node website/scripts/brand-assets.mjs --check   # CI (job website): lệch ảnh gốc ⇒ đỏ
```

Cấu hình: `brand.json` (`mode: tile`, `installer` = dãy cỡ ảnh trình thuật sĩ).

## Đầu ra và nơi dùng

| Tệp | Dùng ở |
| --- | --- |
| `app/app.ico` (7 lớp 16-256) | Icon `swico.exe` (`ApplicationIcon` trong `Tsudev.Audit.Cli.csproj`) **và** icon bộ cài (`SetupIconFile`) - cùng một tệp nên không thể lệch |
| `app/icon-{16..512}.png` | Nguồn cho tài liệu, danh bạ tsudev.com/apps |
| `app/wizard-large-WxH.bmp` (5 cỡ) | `WizardImageFile` - trang chào/kết thúc của bộ cài |
| `app/wizard-small-NxN.bmp` (4 cỡ) | `WizardSmallImageFile` - góc trên các trang bộ cài |
| `website/public/` (favicon, PWA, apple-touch, og-image, logo `<picture>`) | swico.tsudev.com |
| `report/favicon-32.png` (trùng byte `website/public/favicon-32x32.png`), `report/logo-swico-72.png`, `report/logo-tsudev-56.png` | Nhúng vào assembly (`Tsudev.Audit.Core.csproj`) ⇒ data URI trong báo cáo HTML: favicon tab, logo đầu trang (ô 36px), logo nhà làm ở chân trang (ô 28px) - giống header/footer website |

Ảnh trình thuật sĩ là BMP 24-bit trộn sẵn nền trắng (mọi bản Inno 6.x đọc được); dãy cỡ phủ bảng cỡ của Inno trước
6.6.0 lẫn từ 6.6.0, Setup tự chọn ảnh khớp vùng ảnh nhất. CI (job Smoke test Windows) biên dịch thử bộ cài ở mỗi PR.

## Không thuộc dây chuyền này

`website/public/brand/logo-tsudev-*.{avif,webp}` là logo **nhà làm** tsudev (mục 15.5 giữ logo tsudev ở khối "nhà
làm") - bản đã sinh của dây chuyền repo `tsudev` (`packages/brand/`), chép nguyên. Dây chuyền ở đây chỉ THU NHỎ bản
144 thành `report/logo-tsudev-56.png` cho báo cáo; tsudev đổi logo ⇒ chép lại bản mới rồi chạy lệnh trên.

## Mục 12.1 cho sản phẩm desktop này

| Tài sản | Trạng thái |
| --- | --- |
| Icon ứng dụng theo nền tảng | `.ico` - chỉ phát hành Windows, không cần `.icns`/PNG Linux |
| Dấu hiệu thu gọn trong giao diện | Ứng dụng dòng lệnh; báo cáo HTML mang logo sản phẩm (đầu trang) + logo nhà làm (chân trang), favicon sản phẩm |
| Bản cho nền tối | Logo là ô nền đặc, đọc được trên cả nền Sáng và Tối |
| Icon khay hệ thống | Không có khay - không áp dụng |
| Ảnh mở đầu bản cài | `app/wizard-*.bmp` |
| Ảnh đại diện repo GitHub | Ảnh đại diện tổ chức `tsudev-org` |
