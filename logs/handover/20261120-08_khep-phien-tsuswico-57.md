# PHIẾU BÀN GIAO - Khép phiên tsuswico-57

- **Mã phiếu**: 20261120-08
- **Thời điểm**: 20/11/2026
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG
- **Phiếu trước**: `20261120-07_khep-phien-tsuswico-4c.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| (1) Dọn hồ sơ Windows tạm `tsuswicop10` | ⏭ CHƯA: máy chưa khởi động lại (LastBootUpTime 20/11 01:37, hồ sơ `C:\Users\tsuswicop10` vẫn `Loaded=True`). Cách dọn: phiếu 07 mục 2 |
| (2) WINGET-2 #484563 | Vẫn MỞ, nhãn `Azure-Pipeline-Passed` + `Validation-Executable-Error` + `New-Manifest`, bình luận cuối vẫn của mình 04:36Z. Không làm gì |
| (3) Phát hành | Không làm (chủ dự án chưa yêu cầu); `installation-id` qua `logout` chưa có quyết định |
| Logo/favicon báo cáo | **#59 `d15fcb5`**: báo cáo HTML dùng CÙNG ảnh với swico.tsudev.com: favicon sản phẩm (trùng byte `favicon-32x32.png`), đầu trang logo Tsu Swico + "Tsu Swico" dẫn swico.tsudev.com, chân trang logo tsudev hiện hành + "TsuDEV" dẫn tsudev.com (thay logo cú cũ). Ảnh sinh bởi `brand-assets.mjs` vào `assets/brand/report/`, nhúng assembly. Gỡ `assets/tsudev-logo*.png` + `make-assets.py`; README vi/en dùng icon sản phẩm. 349 PASS, axe 0 lỗi, CI xanh; website deploy (báo cáo mẫu production đã có logo mới) |
| Phiếu sang repo khác | `tsucwico/…/20261120-LG1_logo-cwico-trong-app-va-bao-cao.md` (UI Tauri còn favicon + Brand logo tsudev), `qulythuchi/…/20261120-LG1_logo-rieng-quly-thu-chi.md`, `sound-db/…/20261120-LG1_logo-rieng-sound-db.md`, `tsudev/…/20261120-LG1_logo-eco-qulythuchi-sounddb.md` - tệp CHƯA commit trong repo đó. Đã nhắn `qulythuchi-df` + `tsudev-9d`; tsucwico và sound-db không có phiên chạy ⇒ chỉ có phiếu |

## 2. Cạm bẫy / phát hiện

- Báo cáo **không bao giờ có thẻ `<img>`**: test XSS `Mo_ta_chi_mo_lai_dung_the_trong_danh_sach` khẳng định điều đó ⇒
  logo là ảnh nền CSS (`.logo-sp`, `.logo-tsu`, chèn lúc chạy từ tài nguyên nhúng). Đừng đổi sang `<img>`.
- Logo nhà làm trong báo cáo thu nhỏ từ `website/public/brand/logo-tsudev-144.webp` (bản đã sinh của dây chuyền
  tsudev, chép nguyên). tsudev đổi logo ⇒ chép bản mới rồi chạy `brand-assets.mjs` (`--check` bắt lệch).
- `--brand-dev-hero` / `--brand-dev-foot` trong `DesignTokens.cs` nay không còn CSS nào dùng (chữ ký "tsu"/"dev" hai màu
  đã bỏ); để nguyên vì test token canh - dọn được nếu muốn.
- Logo Sound Db chủ dự án giao có dấu ✦ của công cụ tạo ảnh ở góc phải dưới - đã ghi trong phiếu sound-db + tsudev.

## 3. Việc còn mở (thứ tự đề xuất)

1. 👤 Sau khi khởi động lại máy: dọn hồ sơ tạm `tsuswicop10` (phiếu 07 mục 2).
2. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
3. Phát hành (gồm P3/PRO2 + logo báo cáo mới) chỉ khi chủ dự án yêu cầu (QU-2: bản đầu tháng 12 tự kiểm ca ngày một chữ số).
4. 👤 Quyết `installation-id` qua `logout` - tuỳ chọn.
5. Theo dõi các phiếu LG1 (không phải việc của repo này).
6. P4 / PRO2 mục 2.2 ⛔ PRO-D3 (`TsuSync` .NET).

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-08_khep-phien-tsuswico-57.md (mục 2-3). Khởi động: ListAgents, git pull.
Việc: (1) nếu tôi đã khởi động lại máy, dọn hồ sơ Windows tạm tsuswicop10 theo phiếu 20261120-07 mục 2 (tôi bấm UAC);
(2) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn nhãn thì chờ (KHÔNG chạy lại release.yml, KHÔNG bình luận thêm);
(3) nếu tôi yêu cầu phát hành: theo docs/GIT_WORKFLOW + release.yml (bản này có logo/favicon báo cáo mới #59); nếu tôi quyết
giữ installation-id qua logout thì sửa + test + THREAT_MODEL T9 + PRIVACY vi/en; P4/PRO2 2.2 chỉ khi đã có TsuSync .NET (PRO-D3).
Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
