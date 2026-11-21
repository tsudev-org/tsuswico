# PHIẾU BÀN GIAO - Khép phiên tsuswico-0e

- **Mã phiếu**: 20261120-09
- **Thời điểm**: 20/11/2026
- **Trạng thái**: ĐÓNG (21/11/2026, phiên tsuswico-a2 - xem `20261121-01_khep-phien-tsuswico-a2.md`)
- **Phiếu trước**: `20261120-08_khep-phien-tsuswico-57.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| Khởi động | `git pull` (đã mới nhất `0d58e0b`); ListAgents: qulythuchi-df, ubnd-31, lotus-share-07, tsudev-9d - không phiên nào làm repo này |
| (1) Dọn hồ sơ Windows tạm `tsuswicop10` | ⏭ CHƯA: máy VẪN chưa khởi động lại (LastBootUpTime 20/11 01:37:28), `C:\Users\tsuswicop10` vẫn `Loaded=True` (SID `…-1006`). Cách dọn: phiếu 07 mục 2 |
| (2) WINGET-2 #484563 | Vẫn MỞ, chưa gộp; nhãn `Azure-Pipeline-Passed` + `Validation-Executable-Error` + `New-Manifest`; bình luận cuối vẫn của mình 04:36Z. Không làm gì (không chạy lại release.yml, không bình luận) |
| (3) Phát hành / `installation-id` qua `logout` | Không làm: chủ dự án chưa yêu cầu phát hành, chưa quyết `installation-id`. P4/PRO2 2.2 vẫn ⛔ PRO-D3 |

## 2. Cạm bẫy / phát hiện

- Không có phát hiện mới. Cạm bẫy còn hiệu lực: phiếu 08 mục 2 (logo báo cáo là ảnh nền CSS, KHÔNG `<img>`) và
  phiếu 07 mục 2 (`dotnet publish -r win-x64` làm bẩn `packages.lock.json`).

## 3. Việc còn mở (thứ tự đề xuất)

1. 👤 Khởi động lại máy, rồi dọn hồ sơ tạm `tsuswicop10` (phiếu 07 mục 2) - đã hoãn qua 2 phiên.
2. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
3. Phát hành (gồm P3/PRO2 + logo báo cáo mới #59) chỉ khi chủ dự án yêu cầu (QU-2: bản đầu tháng 12 tự kiểm ca ngày một chữ số).
4. 👤 Quyết `installation-id` qua `logout` - tuỳ chọn.
5. Theo dõi các phiếu LG1 (không phải việc của repo này).
6. P4 / PRO2 mục 2.2 ⛔ PRO-D3 (`TsuSync` .NET).

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-09_khep-phien-tsuswico-0e.md (mục 2-3). Khởi động: ListAgents, git pull.
Việc: (1) nếu tôi đã khởi động lại máy, dọn hồ sơ Windows tạm tsuswicop10 theo phiếu 20261120-07 mục 2 (tôi bấm UAC);
(2) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn nhãn thì chờ (KHÔNG chạy lại release.yml, KHÔNG bình luận thêm);
(3) nếu tôi yêu cầu phát hành: theo docs/GIT_WORKFLOW + release.yml (bản này có logo/favicon báo cáo mới #59); nếu tôi quyết
giữ installation-id qua logout thì sửa + test + THREAT_MODEL T9 + PRIVACY vi/en; P4/PRO2 2.2 chỉ khi đã có TsuSync .NET (PRO-D3).
Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
