# PHIẾU BÀN GIAO - Khép phiên tsuswico-22

- **Mã phiếu**: 20261120-02
- **Thời điểm**: 20/11/2026
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG
- **Phiếu trước**: `20261120-01_khep-phien-tsuswico-68.md` - ĐÓNG (mục 3 việc 1, 2, 4a đã làm ở phiên này)

## 1. Đã làm (đều đã gộp, CI xanh đủ 10 check)

| Việc | Bằng chứng |
| --- | --- |
| (1) WINGET-2 - chủ dự án đồng ý nộp | PR **microsoft/winget-pkgs#484563** "Update: tsudev.SWICO to 26.11.2001" từ fork `tsudev-tsudev/winget-pkgs`, nhánh `tsudev.SWICO-26.11.2001`, một commit 3 tệp ở `manifests/t/tsudev/SWICO/26.11.2001/`. Hash tính lại từ chính `InstallerUrl` ngay trước khi nộp: `7A1EC864…B196` khớp. AppId Inno không đổi ⇒ nâng cấp từ 26.8.18.2. Lúc khép phiên: bước 01-07 + CLA **SUCCESS**, 08-10 (cài thử) đang chạy |
| (1b) Lỗi trình sinh manifest | #43 `90a5f0c`: `make-winget-manifest.py` đặt thư mục `tsudev/Tsu Swico/` (lần đổi tên ADR-0001 thay nhầm) - nay suy từ `PackageIdentifier` của template. Zip trong Release 26.11.2001 vẫn mang thư mục sai (nội dung đúng); `docs/WINGET.md` ghi cạm bẫy |
| (2) PRO1 P2 | #44 `38ec751`, `src/Tsudev.Audit.Core/Pro/`: `EntitlementVerifier` + `JwksKeySet` (BCL, chỉ ES256, `typ`, `iss`, `aud`, `exp`, sống ≤ 7 ngày, `kid`, từ chối `crit`/DER), `CachedEntitlementSource`, `ProNotice` + `ProNoticePrompt` (5 test bắt buộc 7.3 `Bat_buoc_1..5`, không hiện ở `--silent`/`--json-only`, mốc `tsu.pro_notice.last_shown` trong `state.json`), `InstallationIdStore` (128 bit, chỉ sinh khi đăng nhập), nội dung vi/en **đối chiếu thẳng với bảng trong PRODUCT_POLICY.md**. 58 test. CHƯA nối vào `Program.cs` |
| (3) 3.5 logging có cấu trúc | #45 `c0414a2`: `--log-file` JSON Lines (`Core/Logging/RunLog.cs`, `LoggingProgressSink`): `run.start`, `update.check`, `step.ok/fail` + ms, `report.done/warning`, `run.fatal` (ngăn xếp), `run.end` (mã thoát). Không thêm gói NuGet. CI Windows quét thật: 29 dòng, 12 `step.ok`, `run.end exit_code=0`. README vi/en, PRIVACY, CHANGELOG [Chưa phát hành]. **3.5 hết việc** (trừ NativeAOT - chỉ cân nhắc) |

Tổng test: 216 → **282 PASS**.

## 2. Cạm bẫy gặp trong phiên

- Manifest trong `winget-manifest-<v>.zip` sinh trên runner Windows ⇒ **CRLF**; so với bản sinh trên Linux phải
  `diff --strip-trailing-cr`. winget-pkgs nhận CRLF bình thường.
- Nộp winget không cần clone kho winget-pkgs: Git Data API (blob → tree với `base_tree` của `master` upstream → commit
  → ref trên fork) rồi `gh pr create -R microsoft/winget-pkgs --head tsudev-tsudev:<nhánh>`. Máy KHÔNG có `jq` - dùng
  Python. Script mẫu đã dùng nằm ở scratchpad (không lưu lại); làm lại theo mô tả này.
- AUTH_AND_ACCOUNT 14.3 nói mã cài đặt "sinh ở lần chạy đầu"; ADR-0002 chốt "sinh lần đầu đăng nhập" (công cụ chạy
  RMM hàng loạt không để lại tệp khi không ai đăng nhập). Mã đã theo ADR và ghi rõ lệch có chủ đích - nếu phiên rà
  chuẩn hỏi thì trỏ về `InstallationId.cs` + ADR-0002 mục 2.3.
- CA1859 (warnings-as-errors) bắt đổi tham số `IProgressSink` riêng tư thành kiểu cụ thể `LoggingProgressSink`.

## 3. Việc còn mở (thứ tự đề xuất)

1. **WINGET-2 theo dõi**: microsoft/winget-pkgs#484563 - nếu bot gắn nhãn lỗi (`Error-Hash-Mismatch`,
   `Validation-*`, `Needs-Author-Feedback`) thì xử lý; ĐỪNG chạy lại `release.yml` cho 26.11.2001 (đổi hash). Gộp
   xong ⇒ đánh dấu WINGET-2 ✅ trong `logs/STATE.md`.
2. **PRO1 P3** ⛔ cần `client_id` Tsu Swico ở tsudev.com (phiên tsudev). Khi có: `login/logout/account` loopback PKCE,
   DPAPI, gọi `/api/me/entitlements` kèm `X-Tsu-Installation`, đệm JWS + JWKS, nối `ProNoticePrompt` vào đầu
   `MainCore` (cạnh kiểm tra cập nhật, chỉ khi `!opts.Silent`), P5 PRIVACY/THREAT_MODEL TRƯỚC khi phát hành.
   Có thể nhắn phiên `tsudev-*` (ListAgents) để hỏi tiến độ đăng ký client.
3. QU-2 ca ngày một chữ số: tự kiểm ở bản phát hành đầu tháng 12 (`26.12.0101.0`).
4. QU-STD-BRAND: đối chiếu checklist `BRAND_ASSETS.md` mục 14 cho icon app/bộ cài.
5. Kiểm tsudev.com/apps hiện phiên bản 26.11.2001 (V1 mục 5 nghiệm thu) - catalog ở repo tsudev.
6. Bản phát hành kế tiếp sẽ mang `--log-file` + nền Pro (chưa bật): kiểm zip winget có thư mục `tsudev/SWICO/`.

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-02_khep-phien-tsuswico-22.md (mục 2-3). Khởi động: ListAgents, git pull. Việc: (1) kiểm PR
microsoft/winget-pkgs#484563 (tsudev.SWICO 26.11.2001): gộp rồi thì đánh dấu WINGET-2 xong, có nhãn lỗi thì xử lý
theo docs/WINGET.md (KHÔNG chạy lại release.yml); (2) QU-STD-BRAND: đối chiếu .standards/docs/BRAND_ASSETS.md mục 14
cho icon app/bộ cài, sửa phần còn thiếu - một PR, CI xanh thì gộp; (3) hỏi phiên tsudev (ListAgents → SendMessage)
đã có client_id OIDC cho Tsu Swico chưa - có thì làm PRO1 P3 theo docs/adr/0002-luu-cuc-bo-pro-dong-bo.md mục 2.3
(nối ProNoticePrompt + P5 PRIVACY/THREAT_MODEL trước khi phát hành), chưa thì ghi phiếu yêu cầu vào logs/handover/
của repo tsudev. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
