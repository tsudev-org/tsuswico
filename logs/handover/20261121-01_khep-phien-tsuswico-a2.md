# PHIẾU BÀN GIAO - Khép phiên tsuswico-a2

- **Mã phiếu**: 20261121-01
- **Thời điểm**: 21/11/2026
- **Trạng thái**: ĐÓNG (22/11/2026, phiên tsuswico-d1)
- **Phiếu trước**: `20261120-09_khep-phien-tsuswico-0e.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| Khởi động | `git pull` (`0384480`, #65 sync v4.2.0 đã gộp từ phiên khác); ListAgents: qulythuchi-cb, tsucwico-ea, ubnd-b9 - không phiên nào làm repo này; `akb start` |
| Phiếu STD43 (v4.3.0) | ĐÓNG: sync thẳng v5.0.0; câu 7.5 đã đúng từ PRO2; PACKAGING 2.2 không phải sửa |
| Phiếu STD50 PR 1 | **#66 `7085d29`** - bản sắc swico: màu hổ phách, bo góc `sac`, Space Grotesk; web + báo cáo HTML/.xlsx; 357 PASS |
| Phiếu STD50 PR 2 | **#67 `68d6e95`** - trang chủ `cong-cu` + hoạ tiết `mach-in` (hero + og-image) |
| Deploy | swico.tsudev.com version `ca57caa8` (21/11/2026), kiểm production có `data-product="swico"`, vùng `cong-cu`, font 200 |
| Hồ sơ Windows tạm | ✅ máy đã khởi động lại (21/11 21:17); xoá hồ sơ `C:\Users\tsuswicop10` (SID `…-1006`, Loaded=False) + `Temp\tsuswico-p12` qua PowerShell quản trị (chủ dự án bấm UAC; lần đầu UAC lỗi, lần hai đạt). Kiểm lại: không còn thư mục, 0 mục hồ sơ |
| WINGET-2 #484563 | Vẫn MỞ; nhãn nay chỉ còn `New-Manifest` (bỏ `Validation-Executable-Error` + `Azure-Pipeline-Passed`) ⇒ đang chờ moderator duyệt. Không bình luận, không chạy lại release.yml |
| AKB | `LES-20261121-2329-5277` (bản sắc v5.0.0: gộp products JSON ở C#, bẫy test token v3, bẫy lưới CSS `cong-cu`) |

## 2. Cạm bẫy / phát hiện

- Token v3.0.0: nền tối `#0A0A0A` trùng `text-primary` sáng ⇒ so khối CSS theo cặp `--c-bg-base:<giá trị>`, không theo mã màu trần.
- Bảng bo góc `shape` không có dạng máy đọc trong bộ quy ước: `DesignTokens.ShapeRadii` chép tay, `ProductIdentityTests` đối chiếu `products.css` - nâng quy ước mà đổi bảng thì test đỏ, sửa bảng theo.
- Lưới hero `cong-cu` + `<pre>` dài: phải `minmax(0, 1fr)` (đã sửa ở #67), nếu không tràn ở 375px mà `scrollWidth` không báo (hero `overflow:hidden`).
- Ảnh trước/sau nằm trong `docs/anh-chup/ban-sac-v5/` (mô tả PR trỏ tới đó).
- Còn hiệu lực: `dotnet publish -r win-x64` cục bộ làm bẩn `packages.lock.json` (phiếu 07 mục 2).

## 3. Việc còn mở (thứ tự đề xuất)

1. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
2. Phát hành (gồm P3/PRO2, logo báo cáo #59, bản sắc báo cáo #66) chỉ khi chủ dự án yêu cầu - QU-2: bản đầu tháng 12 tự kiểm ca ngày một chữ số.
3. 👤 Quyết `installation-id` qua `logout` - tuỳ chọn.
4. Dependabot #64, #31, #30 đang mở (không thuộc phiếu nào) - xem/gộp nếu CI xanh khi chủ dự án muốn.
5. P4 / PRO2 mục 2.2 ⛔ PRO-D3 (`TsuSync` .NET).

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261121-01_khep-phien-tsuswico-a2.md (mục 2-3). Khởi động: ListAgents, git pull, akb start.
Việc: (1) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn mở thì chờ (KHÔNG chạy lại release.yml, KHÔNG bình luận);
(2) xem 3 PR Dependabot (#64, #31, #30): CI xanh thì gộp lần lượt, đỏ thì ghi lý do;
(3) nếu tôi yêu cầu phát hành: theo docs/GIT_WORKFLOW + release.yml; nếu tôi quyết giữ installation-id qua logout thì
sửa + test + THREAT_MODEL T9 + PRIVACY vi/en; P4/PRO2 2.2 chỉ khi đã có TsuSync .NET (PRO-D3).
Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
