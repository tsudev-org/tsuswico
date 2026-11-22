# PHIẾU BÀN GIAO - Khép phiên tsuswico-07

- **Mã phiếu**: 20261122-02
- **Thời điểm**: 22/11/2026
- **Trạng thái**: ĐÓNG (phiên tsuswico-ad, 22/11/2026)
- **Phiếu trước**: `20261122-01_khep-phien-tsuswico-d1.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| Khởi động | ListAgents: ubnd-3f, tsucontact-58, tsucwico-e3 - không phiên nào làm repo này. AKB `SES-20261122-2014-5856` |
| NET10 #74 `9143f74` | `global.json` SDK 10.0.404 (bản mới nhất 10/11/2026, giữ `rollForward: disable`); TFM `net10.0` / `net10.0-windows` (Core, Windows, Cli, unittests, `tools/demo-report`); lock file sinh lại; gỡ ignore System.Management/CodeDom `>= 11` trong `dependabot.yml`; README vi/en, ARCHITECTURE, WINDOWS-VERIFICATION, build.ps1, CHANGELOG (mục Bảo mật), `docs/STATE.md` mục NET10 ✅ |
| Kiểm chứng | Build Release 0 cảnh báo (không phải sửa mã), 357 PASS, không gói lỗ hổng; CI: Linux, Smoke test Windows, biên dịch Inno, CodeQL, website, quy ước - đủ xanh. `swico.exe` 86,7 MB (26.11.2001: 79,7 MB) |
| Website | Deploy lại swico.tsudev.com (README/CHANGELOG đổi) - phiên bản `b9e7bdfc`, 200 |
| WINGET-2 #484563 | Không đổi: MỞ, nhãn `Azure-Pipeline-Passed` + `Validation-Executable-Error` + `New-Manifest`, bình luận cuối vẫn là `@wingetbot run` 21/11. Không bình luận |

## 2. Cạm bẫy / phát hiện

- `dotnet publish -r win-x64` cục bộ SAU `restore --force-evaluate` ghi thêm khối RID `win-x64` vào
  `src/Tsudev.Audit.Windows/packages.lock.json` ⇒ CI `--locked-mode` đỏ NU1004. Publish thử xong thì chạy lại
  `dotnet restore Tsudev.SystemAudit.sln --force-evaluate` + `--locked-mode` trước khi commit (AKB `LES-20261122-2024-8a42`).
- Bỏ ignore ⇒ Dependabot có thể đề xuất System.Management/CodeDom 11.0.0 (gói .NET 11, hỗ trợ net10): nhận được nếu CI
  xanh; nhớ sửa lock file Cli (bẫy NU1004 của phiếu trước).
- Bản phát hành kế tiếp là lần đầu ra exe runtime 10 + lần đầu chạy action mới (#64) trong `release.yml`.

## 3. Việc còn mở (thứ tự đề xuất)

1. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
2. Phát hành (mang runtime .NET 10) chỉ khi chủ dự án yêu cầu - nên làm đầu tháng 12 để kiêm QU-2 (ngày một chữ số).
   Khi phát hành: theo dõi bước "Tao GitHub Release" (action-gh-release v3), Scoop autoupdate, deploy lại website.
3. 👤 3.2 / 3.3 kiểm trên Windows thật (nay nên kiểm bằng bản .NET 10).
4. 👤 Quyết `installation-id` qua `logout` - tuỳ chọn.
5. P4 / PRO2 mục 2.2 ⛔ PRO-D3 (`TsuSync` .NET).

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261122-02_khep-phien-tsuswico-07.md (mục 2-3). Khởi động: ListAgents, git pull, akb start.
Việc: (1) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn mở thì chờ (KHÔNG chạy lại release.yml,
KHÔNG bình luận); (2) xử lý PR Dependabot mới nếu có (nhớ bẫy lock file Cli NU1004); (3) phát hành bản .NET 10
chỉ khi tôi yêu cầu. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
