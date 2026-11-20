# PHIẾU BÀN GIAO - Khép phiên tsuswico-8b

- **Mã phiếu**: 20261120-03
- **Thời điểm**: 20/11/2026
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG
- **Phiếu trước**: `20261120-02_khep-phien-tsuswico-22.md` - ĐÓNG (việc 1, 2, 4 của mục 3 đã xử lý ở phiên này)

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| (1) WINGET-2 | microsoft/winget-pkgs#484563 vẫn MỞ: bước 01-07 + CLA xanh, 08-10 (cài thử) chưa có kết quả, nhãn chỉ `New-Manifest`, không nhãn lỗi ⇒ không có gì để xử lý. KHÔNG chạy lại `release.yml` |
| (2) QU-STD-BRAND ✅ | **#47 `4bb68e6`**, CI xanh 10/10. Đối chiếu `BRAND_ASSETS.md` mục 14 (+12.1, 15): exe (`ApplicationIcon`) và bộ cài (`SetupIconFile`) nay dùng CHUNG `assets/brand/app/app.ico` của `brand-assets.mjs` (trước: `assets/swico.ico` từ bộ đổi cỡ tự viết trong `make-assets.py`, khác byte, không cổng nào canh); ảnh trình thuật sĩ Inno sinh trong `brand-assets.mjs` (`--check` canh), 5 cỡ lớn + 4 cỡ nhỏ phủ bảng cỡ Inno trước và từ 6.6.0, BMP 24-bit; `.iss` thiếu icon ⇒ `#error` (trước `#if FileExists` âm thầm bỏ); CI job Windows biên dịch thử bộ cài ở mọi PR (đo: "Successful compile", 26,3 MB) + cổng `ApplicationIcon`; bỏ `assets/swico.ico`, `assets/wizard-*.bmp`, `assets/favicon/` (không ai dùng); `make-assets.py` chỉ còn logo nhà làm; `assets/brand/README.md` ghi SHA-256 ảnh gốc + bảng mục 12.1 cho desktop |
| (3) PRO1 P3 | Hỏi phiên `tsudev-f6` (SendMessage) + ghi phiếu **`~/projects/tsudev/logs/handover/20261120-SW1_yeu-cau-client-oidc-native-tsuswico.md`** (tệp mới, CHƯA commit - phiên tsudev đang ở nhánh `feat/pro-b3-thanh-toan-vietqr`, đã nhờ họ commit). Đo: `/oidc/auth?client_id=tsuswico` ⇒ 400. P3 chưa làm được |

## 2. Cạm bẫy / phát hiện

- **Chèn bảng `OAuthClient` CHƯA đủ cho CLI**: `clientMetadata()` của auth-service không đặt `application_type` ⇒
  `oidc-provider` coi là `web`, redirect phải khớp tuyệt đối; loopback cổng ngẫu nhiên (RFC 8252, ADR-0002 2.3) cần
  client `native`. Phiếu SW1 nêu cả việc mã lẫn SQL.
- Inno Setup 6.6.0 đổi bảng cỡ ảnh trình thuật sĩ (100%: 202x386 / 58x58; trước: 164x314 / 55x58) và đọc được PNG,
  nhưng chưa rõ bản trên runner ⇒ giữ BMP + danh sách nhiều cỡ (Setup tự chọn khớp nhất).
- Bước CI in `ISCC: 0.0.0.0` - `VersionInfo` của ISCC.exe không mang số phiên bản; muốn biết bản Inno thì đọc dòng
  đầu ra của chính ISCC ("Inno Setup 6 Command-Line Compiler" + phiên bản). Vô hại, chưa sửa.
- Ảnh gốc 543px (< 1024 `SHOULD` của mục 15) - đủ cho mọi cỡ đang dùng; ghi nợ ở `assets/brand/README.md`.

## 3. Việc còn mở (thứ tự đề xuất)

1. **WINGET-2**: kiểm #484563; gộp ⇒ ✅ trong `logs/STATE.md`; nhãn lỗi ⇒ xử lý theo `docs/WINGET.md`; KHÔNG chạy lại
   `release.yml` cho 26.11.2001.
2. **PRO1 P3** ⛔ chờ phiếu SW1 (phiên tsudev). Khi có `client_id` + client native lên production: `login/logout/account`
   loopback PKCE `http://127.0.0.1:<cổng>/callback`, DPAPI, `/api/me/entitlements` kèm `X-Tsu-Installation`, đệm JWS +
   JWKS (`aud` = client_id chốt), nối `ProNoticePrompt` vào đầu `MainCore` (chỉ khi `!opts.Silent`), P5
   PRIVACY/THREAT_MODEL TRƯỚC khi phát hành. Nếu phiên tsudev trả lời qua SendMessage khi phiên này đã đóng: xem phiếu
   SW1 trạng thái.
3. QU-2 ca ngày một chữ số: tự kiểm ở bản phát hành đầu tháng 12 (`26.12.0101.0`).
4. Kiểm tsudev.com/apps hiện 26.11.2001 (V1 mục 5) - catalog ở repo tsudev.
5. Bản phát hành kế tiếp mang `--log-file`, nền Pro (chưa bật), icon/ảnh bộ cài mới: kiểm bằng mắt trình thuật sĩ ở
   125-200% (việc người dùng, cùng nhóm 3.3) và zip winget có thư mục `tsudev/SWICO/`.

## 3b. Cập nhật sau khi khép phiên (20/11/2026, tsudev-f6 trả lời phiếu SW1)

- `client_id` chốt: **`tsuswico`** (JWS quyền lợi `aud` = `tsuswico`).
- tsudev#339 (`70b7596`) đã gộp: KHÔNG có cột `applicationType`; client tự thành native khi MỌI redirect là
  `http://127.0.0.1/...` hoặc `http://[::1]/...` không ghi cổng ⇒ nhận cổng bất kỳ; `localhost`/path khác ⇒ 400.
- Đăng ký: `http://127.0.0.1/callback` VÀ `http://[::1]/callback`. P3 dùng `127.0.0.1` (ADR-0002 2.3); `[::1]` là
  đường lui nếu máy chỉ có IPv6 loopback.
- **Còn 👤 OWNER**: chạy INSERT ở phiếu SW1 mục 5 (máy agent không có chuỗi Neon). Đo lúc ghi: IdP vẫn 400.
  Mã P3 viết và test được với IdP giả trước; kiểm thật (303) chỉ sau INSERT.

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-03_khep-phien-tsuswico-8b.md (mục 2-3). Khởi động: ListAgents, git pull. Việc: (1) kiểm PR
microsoft/winget-pkgs#484563 (tsudev.SWICO 26.11.2001): gộp rồi thì đánh dấu WINGET-2 xong, có nhãn lỗi thì xử lý
theo docs/WINGET.md (KHÔNG chạy lại release.yml); (2) đọc phiếu ~/projects/tsudev/logs/handover/20261120-SW1_*.md
(hoặc hỏi phiên tsudev qua ListAgents → SendMessage): OWNER đã chạy INSERT client `tsuswico` chưa (đo
/oidc/auth với redirect 127.0.0.1 cổng bất kỳ ⇒ 303; client_id đã chốt, mục 3b) - làm PRO1 P3 theo docs/adr/0002-luu-cuc-bo-pro-dong-bo.md
mục 2.3 (nối ProNoticePrompt + P5 PRIVACY/THREAT_MODEL trước khi phát hành), một PR, CI xanh thì gộp (chưa INSERT thì vẫn
viết + test với IdP giả, KHÔNG phát hành). Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
