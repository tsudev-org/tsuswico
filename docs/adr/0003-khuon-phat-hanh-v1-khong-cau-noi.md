# ADR-0003: Khuôn phát hành thống nhất (phiếu V1) từ `v26.11.2001`, không phát hành bản cầu nối

- **Trạng thái**: Đã chấp nhận (20/11/2026, chủ dự án chọn trong phiên tsuswico-68)
- **Phiếu**: `logs/handover/20261117-V1_quy-uoc-phien-ban-thong-nhat.md`; chuẩn `.standards/docs/VERSIONING.md` mục 2.2
  và 2.4 (bộ quy ước v3.9.1). Thay thế câu hỏi mở QU-3 / 3.6 (`docs/STATE.md` mục 4.7).

## 1. Bối cảnh

Hai bản đã phát hành ra ngoài - `v26.8.18` và `v26.8.18.2` (có trên winget `tsudev.SWICO`, Scoop `tsuswico` +
`tsudev-swico`) - mang bộ đọc phiên bản cũ: chia chuỗi theo dấu chấm, coi phần thứ ba là ngày (1-31). Thẻ dạng mới
`v26.11.2001` có phần thứ ba `2001` ⇒ bộ đọc cũ trả "không đọc được số hiệu" ⇒ nhánh `CheckFailed`: app vẫn quét bình
thường, in ghi chú không kiểm tra được bản cập nhật, **không tự cập nhật**. Ngoài ra bộ đọc cũ chỉ nhận tệp
`swico-setup-*.exe`. VERSIONING 2.4 mục 2: không chứng minh được bản mới "mới hơn" trong mắt updater đã cài ⇒ phát
hành bản cầu nối hoặc ghi lý do trong ADR.

## 2. Quyết định

1. Bản kế tiếp và mọi bản sau theo khuôn V1:

   | Đối tượng | Giá trị |
   | --- | --- |
   | Thẻ | `v26.11.2001` (`v` thường, không tiền tố/hậu tố) |
   | Tiêu đề Release | `Tsu Swico 26.11.2001` |
   | Bộ cài | `tsuswico_26.11.2001_windows-x64-setup.exe` |
   | Portable | `tsuswico_26.11.2001_windows-x64-portable.zip` |
   | Tệp kiểm | `SHA256SUMS.txt` + `SHA256SUMS.txt.sigstore.json` |
   | FileVersion / Inno `VersionInfoVersion` | `26.11.2001.0` |

2. **Không phát hành bản cầu nối** dạng cũ. Lý do: chủ dự án xác nhận 19/11/2026 mọi sản phẩm chưa có người dùng thật
   (`PRODUCT_POLICY.md` mục 7); bản cầu nối đòi nới cổng `ReleaseName.Validate` và tốn thêm một vòng phát hành chỉ để
   phục vụ hai bản thử; và bản cũ không hỏng - chỉ mất tự cập nhật.
3. Người đang dùng bản cũ cập nhật bằng: `winget upgrade tsudev.SWICO`, `scoop update tsuswico`, hoặc tải tay từ trang
   Release / swico.tsudev.com. Ghi chú Release và CHANGELOG nêu điều này.
4. Đồng bộ CÙNG lúc mọi nơi đọc tên (VERSIONING 2.4 mục 4): `ReleaseName` (sinh tên), `release.yml`, Inno
   `swico.iss`, `packaging/package.json` (`tagRegex`, `assets.*.file`), mẫu winget, website (`LOAI_GOI`, trang tải về),
   README vi/en, Scoop `tsuswico` + `tsudev-swico` (`autoupdate`, repo `tsudev-org/scoop-bucket`).
5. Bộ đọc cập nhật của bản MỚI vẫn nhận cả ba dạng tên tệp (`swico-setup-*`, `tsuswico_*_x64-setup`,
   `tsuswico_*_windows-x64-setup`) - có test.

## 3. Hệ quả

- Từ `26.11.2001` trở đi, chuỗi so sánh của updater là khuôn `YY.M.DDNN` nhất quán; các bản sau tự cập nhật bình thường.
- Hai thẻ cũ giữ nguyên (ruleset "Tag phát hành bất biến").
- Nếu sau này phát hiện có người dùng thật kẹt ở bản cũ: vẫn có thể phát hành cầu nối `v26.M.DD` dạng cũ một lần
  (cách làm ở `docs/STATE.md` mục 4.7) - quyết định này không đóng cửa phương án đó.
