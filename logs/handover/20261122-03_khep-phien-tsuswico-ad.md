# PHIẾU BÀN GIAO - Khép phiên tsuswico-ad

- **Mã phiếu**: 20261122-03
- **Thời điểm**: 22/11/2026
- **Trạng thái**: ĐÓNG (phiên tsuswico-6e, 22/11/2026)
- **Phiếu trước**: `20261122-02_khep-phien-tsuswico-07.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| Khởi động | ListAgents: ubnd-3f, tsucontact-58, tsucwico-e3 - không phiên nào làm repo này. AKB `SES-20261122-2032-cb29` |
| WINGET-2 #484563 | Không đổi: MỞ, nhãn `Azure-Pipeline-Passed` + `Validation-Executable-Error` + `New-Manifest`, bình luận cuối vẫn là `@wingetbot run` (stephengillie, 21/11 15:16). Không bình luận, không chạy lại `release.yml` |
| Dependabot #76 | System.Management 10.0.15 → **11.0.0** (kéo System.CodeDom 11.0.0). Dependabot chỉ sửa lock file `Tsudev.Audit.Windows` ⇒ CI Linux đỏ NU1004 ở `Tsudev.Audit.Cli`. Sửa: `dotnet restore --force-evaluate` trên nhánh PR, chỉ `src/Tsudev.Audit.Cli/packages.lock.json` đổi (commit `1b5a211`), `--locked-mode` đạt, build Release 0 cảnh báo, 357 PASS, không gói lỗ hổng. CI đủ xanh (Linux, Smoke Windows, CodeQL, website, quy ước). Gộp squash `b008f50` |

## 2. Cạm bẫy / phát hiện

- Bẫy NU1004 lặp lại lần thứ hai (#72, #76): mọi PR Dependabot đụng gói của `Tsudev.Audit.Windows` sẽ đỏ vì lock file
  Cli (dự án tham chiếu) không được sinh lại. Cách sửa cố định: checkout nhánh PR → `dotnet restore Tsudev.SystemAudit.sln
  --force-evaluate` → commit lock file Cli → push. Nếu thấy lặp nhiều, cân nhắc thêm thư mục `src/Tsudev.Audit.Cli` vào
  cùng nhóm Dependabot (chưa làm - không chắc Dependabot sinh lại lock file của dự án chỉ tham chiếu gián tiếp).
- Bản phát hành kế tiếp nay mang runtime .NET 10 **và** System.Management 11.0.0 - phần WMI chỉ được Smoke test Windows
  trên CI kiểm; nên kiểm 3.2/3.3 trên Windows thật bằng bản đó.

## 3. Việc còn mở (thứ tự đề xuất)

1. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
2. Phát hành (runtime .NET 10 + System.Management 11) chỉ khi chủ dự án yêu cầu - nên làm đầu tháng 12 để kiêm QU-2
   (ngày một chữ số). Khi phát hành: theo dõi bước "Tao GitHub Release" (action-gh-release v3), Scoop autoupdate, deploy lại website.
3. 👤 3.2 / 3.3 kiểm trên Windows thật (bằng bản mới).
4. 👤 Quyết `installation-id` qua `logout` - tuỳ chọn.
5. P4 / PRO2 mục 2.2 ⛔ PRO-D3 (`TsuSync` .NET).

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261122-03_khep-phien-tsuswico-ad.md (mục 2-3). Khởi động: ListAgents, git pull, akb start.
Việc: (1) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn mở thì chờ (KHÔNG chạy lại release.yml,
KHÔNG bình luận); (2) xử lý PR Dependabot mới nếu có (bẫy lock file Cli NU1004: restore --force-evaluate trên nhánh PR);
(3) phát hành bản .NET 10 chỉ khi tôi yêu cầu. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
