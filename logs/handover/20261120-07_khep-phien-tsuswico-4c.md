# PHIẾU BÀN GIAO - Khép phiên tsuswico-4c

- **Mã phiếu**: 20261120-07
- **Thời điểm**: 20/11/2026
- **Trạng thái**: ĐÓNG (20/11/2026, phiên tsuswico-57 - việc mở chuyển sang phiếu `20261120-08`)
- **Phiếu trước**: `20261120-06_khep-phien-tsuswico-96.md` - ĐÓNG

## 1. Đã làm (chỉ tài liệu, không đổi mã)

Chủ dự án ủy quyền agent tự chạy mục P trên Windows 11 của họ (agent điều khiển qua WSL `powershell.exe`, người bấm
UAC + đăng nhập tsudev.com). Exe build tại chỗ từ `main` `d043e76` (`dotnet publish ... -r win-x64`), kịch bản ở
`C:\Users\devng\AppData\Local\Temp\tsuswico-p12\` (`p12.ps1`, `p12b.ps1`, `clean.ps1`, `ket-qua*.txt`).

| Việc | Kết quả |
| --- | --- |
| P7 | ✅ `HTTPS_PROXY=http://127.0.0.1:9` ⇒ cảnh báo "Không lấy được quyền lợi mới...", mã 0; bỏ proxy ⇒ bình thường |
| P10 | ✅ Tài khoản Windows tạm `tsuswicop10` (quản trị, Task Scheduler `RunLevel Highest`), chép 6 tệp ⇒ "Phiên đăng nhập đã hết hạn hoặc bị thu hồi", swico xoá `refresh-token.dpapi` ở B; A không bị ảnh hưởng |
| P12 | ✅ vi + en in nguyên văn câu 7.5 trước liên kết mua. Dòng tài liệu cũ ghi `--lang en account` ⇒ mã 3: **đúng thiết kế** (test `Lenh_chi_o_vi_tri_dau`, lệnh con phải đứng đầu) - sửa tài liệu, KHÔNG sửa mã |
| P13 | ⏭ Bỏ qua có chủ đích (ủy quyền): không cấp được Pro ≤ 7 ngày trên production mà không sửa dữ liệu thật ở repo khác; `ProRenewalTests` phủ 6 ca bắt buộc 7.6 |
| Mã trùng | Mục P có hai dòng P12 ⇒ dòng `login --silent` (cũ, ✅) đổi thành **P14**; THREAT_MODEL sửa theo |
| THREAT_MODEL mục 4 | Tích đủ (chạy thật + 7.5) |
| WINGET-2 | #484563 vẫn MỞ, nhãn như cũ, bình luận cuối vẫn của mình 04:36Z. Không làm gì |

## 2. Cạm bẫy / phát hiện

- **Hồ sơ Windows tạm còn sót**: tài khoản `tsuswicop10` ĐÃ xoá, nhưng `C:\Users\tsuswicop10` + mục hồ sơ mồ côi bị
  khoá (registry hive còn nạp) tới khi khởi động lại. Không chứa token (swico đã xoá `refresh-token.dpapi`); còn
  `account.json`, `entitlement.jws`, `installation-id` cũ (đã vô hiệu vì A đã `logout`). Dọn sau khi khởi động lại,
  PowerShell quản trị:
  `Get-CimInstance Win32_UserProfile | ? LocalPath -like '*\tsuswicop10*' | Remove-CimInstance` (hoặc System >
  Advanced > User Profiles), rồi xoá `C:\Users\devng\AppData\Local\Temp\tsuswico-p12\`.
- **Số thiết bị phình theo vòng đăng nhập/đăng xuất**: `logout` xoá `installation-id` (THREAT_MODEL T9, cố ý vì riêng
  tư) ⇒ mỗi lần đăng nhập lại = một thiết bị mới trong `devices_30d` (đo: 2 → 3 trong phiên này). Hệ quả: người
  dùng miễn phí đăng nhập/đăng xuất vài lần trên MỘT máy có thể thấy thông báo 7.3 (≥ 2 thiết bị). Không sửa - là
  đánh đổi thiết kế; chủ dự án quyết có cần giữ `installation-id` qua `logout` không (đổi T9 + PRIVACY).
- `dotnet publish -r win-x64` cục bộ làm bẩn `packages.lock.json` (thêm gói runtime) - đã `git checkout`, đừng commit.

## 3. Việc còn mở (thứ tự đề xuất)

1. 👤 Sau khi khởi động lại máy: dọn hồ sơ tạm (mục 2).
2. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
3. Mục P đã đạt (P13 bỏ qua có chủ đích) + THREAT_MODEL mục 4 đủ ⇒ điều kiện "KHÔNG phát hành khi mục P chưa đạt" đã
   gỡ. Phát hành bản có P3/PRO2 chỉ khi chủ dự án yêu cầu (QU-2: bản đầu tháng 12 tự kiểm ca ngày một chữ số).
4. 👤 Quyết điểm `installation-id` qua `logout` (mục 2) - tuỳ chọn.
5. P4 / PRO2 mục 2.2 ⛔ PRO-D3 (`TsuSync` .NET).

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-07_khep-phien-tsuswico-4c.md (mục 2-3). Khởi động: ListAgents, git pull.
Việc: (1) nếu tôi đã khởi động lại máy, dọn hồ sơ Windows tạm tsuswicop10 theo mục 2 (tôi bấm UAC);
(2) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn nhãn thì chờ (KHÔNG chạy lại release.yml, KHÔNG bình luận thêm);
(3) nếu tôi yêu cầu phát hành: mục P đã đạt - theo docs/GIT_WORKFLOW + release.yml; nếu tôi quyết giữ installation-id qua
logout thì sửa + test + THREAT_MODEL T9 + PRIVACY vi/en; P4/PRO2 2.2 chỉ khi đã có TsuSync .NET (PRO-D3).
Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
