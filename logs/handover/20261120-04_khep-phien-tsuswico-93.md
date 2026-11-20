# PHIẾU BÀN GIAO - Khép phiên tsuswico-93

- **Mã phiếu**: 20261120-04
- **Thời điểm**: 20/11/2026
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG
- **Phiếu trước**: `20261120-03_khep-phien-tsuswico-8b.md` - ĐÓNG (mục 3 việc 1, 2 xử lý ở phiên này)

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| PRO1 P3 + P5 | **#51 `a7cbf1d`**, CI xanh đủ check (gồm bước Windows mới "Lenh tai khoan khi chua dang nhap"). `swico login\|logout\|account` (chỉ tương tác, `--silent`/`--json-only` ⇒ mã 3); Authorization Code + PKCE S256 + `state` + `nonce` + `prompt=consent`, loopback `TcpListener` `127.0.0.1:<cổng>/callback` lùi `[::1]`, id_token ES256 theo JWKS, `iss` RFC 9207; refresh token DPAPI (P/Invoke `crypt32`, không NuGet) trong `%LOCALAPPDATA%\tsudev\tsuswico\`, xoay vòng trong khoá `session.lock`; `/api/me/entitlements` kèm `X-Tsu-Installation`, đệm ≤ 7 ngày, lần quét tương tác chỉ làm mới khi đệm > 24 giờ; `ProNoticePrompt` ở đầu `MainCore` khi `!Silent && !JsonOnly`. P5: `PRIVACY.md` + `PRIVACY.en.md`, `docs/THREAT_MODEL.md` (T1-T13 + phiếu rà soát mục 4), README vi/en, CHANGELOG, ADR-0002, `docs/WINDOWS-VERIFICATION.md` mục P. Test 282 → **334 PASS** trên IdP giả (`tests/unittests/FakeIdp.cs`), không gọi tsudev.com |
| WINGET-2 | microsoft/winget-pkgs#484563 MỞ, nhãn mới **`Validation-Executable-Error`** + `Azure-Pipeline-Passed` (02:43 20/11). Bước 08 NEUTRAL, 01-07/09/10 SUCCESS. Nguyên nhân = báo động nhầm đã gặp ở #419878: bot chạy `swico.exe` không tham số, nhận mã kết luận 20; moderator tự gỡ nhãn (~3 tuần) rồi gộp. Không sửa manifest, KHÔNG chạy lại `release.yml`. Ghi vào `docs/WINGET.md` (trong #51) kèm mẫu bình luận tiếng Anh - **chưa đăng** (bình luận công khai, chờ chủ dự án quyết) |

## 2. Cạm bẫy / phát hiện

- **oidc-provider 9.x bỏ `offline_access` nếu thiếu `prompt=consent`** (`actions/authorization/scopes.js`) ⇒ không có
  refresh token. Áp dụng cho MỌI client native của hệ (tsucwico...). Đã ghi ADR-0002 2.3.
- `HttpListener` trên Windows đi qua http.sys - tiền tố `127.0.0.1` cần URL ACL; `localhost` thì tsudev.com từ chối ⇒
  dùng `TcpListener`.
- swico.exe chạy quyền quản trị ⇒ ShellExecute URL mở trình duyệt nâng quyền; đã giao cho `explorer.exe`. **Chưa đo
  trên Windows thật** (WINDOWS-VERIFICATION P3).
- Refresh xoay vòng + phát hiện dùng lại: hai tiến trình swico refresh cùng lúc sẽ khiến tsudev.com thu hồi cả cây
  phiên ⇒ khoá tệp. Ai viết client .NET/Rust khác của hệ nên làm tương tự.
- URL ủy quyền chứa `state` - không được qua `Info()` (ghi vào `--log-file`); có test nguồn canh.
- `swico account` CHƯA in liên kết mua/điều khoản 7.5 (chờ câu chữ kênh web, ADR-0002 2.5).

## 3. Việc còn mở (thứ tự đề xuất)

1. 👤 **Chạy thử trên Windows thật** `docs/WINDOWS-VERIFICATION.md` mục P (P1-P12), exe từ artifact
   `swico-unsigned-<sha>` của run CI trên `main` sau `a7cbf1d`. Ghi kết quả vào bảng; lệch ⇒ sửa + test hồi quy.
   Quan trọng nhất: P3 (trình duyệt không Elevated), P10 (token chép sang tài khoản khác vô dụng).
2. WINGET-2: chờ moderator gỡ `Validation-Executable-Error`. Chủ dự án đồng ý thì đăng bình luận mẫu ở
   `docs/WINGET.md`. Gộp ⇒ ✅.
3. Trước khi phát hành bản có P3: xong mục 1; THREAT_MODEL mục 4 tích đủ; quyết câu chữ 7.5 (v3.9.2) hay phát hành
   không có liên kết mua.
4. P4 ⛔ PRO-D (`TsuSync` .NET). QU-2 ca ngày một chữ số ở bản đầu tháng 12.

## 3b. Cập nhật sau khép phiên (20/11/2026, cùng phiên - chủ dự án ủy quyền chạy tự động)

- **Chạy thật mục P trên Windows 11 26200** (máy chủ dự án, agent điều khiển qua WSL `powershell.exe`, người bấm
  UAC + đăng nhập tsudev.com): P1-P6, P8, P9, P11, P12 ✅; P7, P10 chưa làm. Kết quả ghi trong
  `docs/WINDOWS-VERIFICATION.md` mục P.
- **Lỗi thật tìm được (P2):** `explorer.exe <url>` từ tiến trình nâng quyền mở thư mục Documents, không mở trình duyệt.
  Sửa: `SystemBrowser` gọi `ShellExecute` qua COM `ShellWindows` của desktop (quyền thường), thất bại ⇒ in liên kết,
  không lùi về trình duyệt nâng quyền. Báo cáo HTML sau khi quét cũng mở theo đường này (trước: ShellExecute nâng
  quyền). Đo lại: Edge mở trang đăng nhập + báo cáo, mọi `msedge` Elevated = 0.
- Chữ `?` trong cửa sổ kiểm thử là do đường ống PowerShell 5.1 (bảng mã 437) của kịch bản, không phải của swico.
- Mục 3 việc 1 coi như xong trừ P7/P10; việc 3 (trước phát hành) còn câu chữ 7.5.

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-04_khep-phien-tsuswico-93.md (mục 2-3). Khởi động: ListAgents, git pull.
Việc: (1) mục P đã chạy thật (mục 3b) - còn P7 (ngoại tuyến) và P10 (tài khoản Windows thứ hai) nếu tôi làm được; (2) kiểm
microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅; vẫn Validation-Executable-Error thì xử lý theo docs/WINGET.md
(báo động nhầm mã 20, KHÔNG chạy lại release.yml, bình luận công khai chỉ khi tôi đồng ý); (3) kiểm tsudev-standards
đã có v3.9.2 (câu chữ thuê bao kênh web 7.5) chưa - có thì sync và in điều khoản trước liên kết mua trong swico account.
KHÔNG phát hành bản mới khi mục P chưa đạt. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
