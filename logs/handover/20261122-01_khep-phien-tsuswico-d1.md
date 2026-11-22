# PHIẾU BÀN GIAO - Khép phiên tsuswico-d1

- **Mã phiếu**: 20261122-01
- **Thời điểm**: 22/11/2026
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG
- **Phiếu trước**: `20261121-01_khep-phien-tsuswico-a2.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| Khởi động | Phiên trước bị tắt đột ngột (AKB `SES-20261122-0028-fb4f`, rỗng; chỉ để lại nhánh cục bộ `pr30/pr31/pr64` - đã xoá). ListAgents: ubnd-3f, tsucontact-58, tsucwico-e3 - không phiên nào làm repo này |
| WINGET-2 #484563 | Vẫn MỞ. Moderator stephengillie `@wingetbot run` 21/11 15:16 ⇒ nhãn `Validation-Executable-Error` + `Azure-Pipeline-Passed` quay lại (báo động nhầm mã thoát, đã giải thích 20/11). Không bình luận, không chạy lại release.yml |
| Dependabot #64 | Gộp `8fb0c66`: checkout/setup-dotnet/setup-node/upload-artifact lên v6-v7, action-gh-release v3, signpath v3 (bước SignPath đang tắt). CI + Scorecard xanh |
| Dependabot #30 | Gộp `6ed7461`: astro 7.4.12, marked 18.2.1, playwright 1.65.1, wrangler 4.164.0. Không deploy (không đổi nội dung) |
| Dependabot #31 | Đóng. Đỏ vì bỏ sót `src/Tsudev.Audit.Cli/packages.lock.json` (NU1004) và System.Management 11.0.0 báo "doesn't support net8.0-windows" |
| #69 `29416f8` | Thay #31: Test.Sdk 18.12.0, xunit 2.9.3, runner 4.0.1; giữ System.Management 8.0.0; `dependabot.yml` bỏ qua System.Management/CodeDom `>= 11`. Build 0 cảnh báo, 357 PASS, CI đủ xanh |
| Dependabot đợt 2 | Sau khi đổi `dependabot.yml`: gộp #70 (astro 7.4.13, wrangler 4.165.0), #71 (scorecard-action 2.6.0); #72 System.Management 8.0.0 → **10.0.15** (còn hỗ trợ net8) đỏ cùng lỗi NU1004 ⇒ đẩy thêm commit lock file Cli lên nhánh Dependabot, 0 cảnh báo, 357 PASS, gộp |
| Phát hiện | .NET 8 hết hỗ trợ 10/11/2026; exe self-contained ⇒ việc mới **NET10** (`docs/STATE.md` mục NET10, kế hoạch 6 bước) |

## 2. Cạm bẫy / phát hiện

- Dependabot nhóm NuGet chỉ sửa lock file của project khai gói, bỏ sót project tham chiếu gián tiếp (Cli → Windows) ⇒
  CI `--locked-mode` đỏ NU1004. Sửa: `dotnet restore Tsudev.SystemAudit.sln --force-evaluate`.
- Gói runtime Microsoft bản N+1 phát cảnh báo "doesn't support netX" khi TFM đã hết hỗ trợ - dấu hiệu cần nâng TFM.
- Release.yml chỉ chạy khi gắn tag: action nhảy bản lớn ở #64 (gh-release v3, upload-artifact v7) lần đầu chạy thật ở
  bản phát hành kế tiếp - theo dõi bước "Tao GitHub Release" khi đó.

## 3. Việc còn mở (thứ tự đề xuất)

1. **NET10** - một PR theo `docs/STATE.md` mục NET10 (không cần người dùng); gỡ luật ignore trong `dependabot.yml` khi xong.
2. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
3. Phát hành chỉ khi chủ dự án yêu cầu - nên sau NET10; bản đầu tháng 12 kiêm QU-2 (ngày một chữ số) và lần chạy thật
   đầu tiên của action mới (#64).
4. 👤 Quyết `installation-id` qua `logout` - tuỳ chọn.
5. P4 / PRO2 mục 2.2 ⛔ PRO-D3 (`TsuSync` .NET).

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261122-01_khep-phien-tsuswico-d1.md (mục 2-3). Khởi động: ListAgents, git pull, akb start.
Việc: (1) NET10: chuyển .NET 8 → .NET 10 LTS theo docs/STATE.md mục NET10 (global.json, TFM, lock file, CI, 0 cảnh báo,
đủ PASS, Smoke test Windows + Inno xanh), gỡ ignore System.Management trong dependabot.yml, một PR;
(2) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn mở thì chờ (KHÔNG chạy lại release.yml, KHÔNG bình luận);
(3) phát hành chỉ khi tôi yêu cầu. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
