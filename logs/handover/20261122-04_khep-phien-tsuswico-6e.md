# PHIẾU BÀN GIAO - Khép phiên tsuswico-6e

- **Mã phiếu**: 20261122-04
- **Thời điểm**: 22/11/2026
- **Trạng thái**: ĐÓNG (tiếp ở 20261122-05, cùng phiên)
- **Phiếu trước**: `20261122-03_khep-phien-tsuswico-ad.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| Khởi động | ListAgents: ubnd-3f, tsucontact-58, tsucwico-e3 - không phiên nào làm repo này. `git pull`: đã mới nhất. AKB `SES-20261122-2058-5210` |
| WINGET-2 #484563 | Không đổi: MỞ, nhãn `Azure-Pipeline-Passed` + `Validation-Executable-Error` + `New-Manifest`, bình luận cuối vẫn là `@wingetbot run` (stephengillie, 21/11 15:16). Không bình luận, không chạy lại `release.yml` |
| Dependabot | Không có PR mở nào. Main `868fb5a` xanh (CI, Cổng kiểm quy ước, Scorecard) |
| Phát hành | Không làm - chủ dự án chưa yêu cầu |

## 2. Cạm bẫy / phát hiện

- Không có gì mới. Bẫy NU1004 lock file Cli vẫn áp dụng cho PR Dependabot kế tiếp đụng gói `Tsudev.Audit.Windows`
  (phiếu `20261122-03` mục 2).

## 3. Việc còn mở (thứ tự đề xuất)

1. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
2. PR Dependabot mới (nếu có): checkout nhánh PR → `dotnet restore Tsudev.SystemAudit.sln --force-evaluate` → commit
   lock file Cli → push.
3. Phát hành (runtime .NET 10 + System.Management 11) chỉ khi chủ dự án yêu cầu - nên làm đầu tháng 12 để kiêm QU-2.
4. 👤 3.2 / 3.3 kiểm trên Windows thật (bằng bản mới).
5. 👤 Quyết `installation-id` qua `logout` - tuỳ chọn.
6. P4 / PRO2 mục 2.2 ⛔ PRO-D3 (`TsuSync` .NET).

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261122-04_khep-phien-tsuswico-6e.md (mục 2-3). Khởi động: ListAgents, git pull, akb start.
Việc: (1) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn mở thì chờ (KHÔNG chạy lại release.yml,
KHÔNG bình luận); (2) xử lý PR Dependabot mới nếu có (bẫy lock file Cli NU1004: restore --force-evaluate trên nhánh PR);
(3) phát hành bản .NET 10 chỉ khi tôi yêu cầu. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
