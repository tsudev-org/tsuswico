# Mô hình đe doạ - Tài khoản TsuDEV và quyền lợi Pro trong Tsu Swico

- **Phạm vi**: `swico login` / `account` / `logout`, quyền lợi Pro, thông báo "nhiều thiết bị chưa có Pro" (PRO1 P3,
  ADR-0002 mục 2.3-2.4). Chưa gồm đồng bộ báo cáo (P4 - viết bổ sung khi có `TsuSync`).
- **Cập nhật**: 20/11/2026 (phiên tsuswico-93). Checklist nguồn: `.standards/docs/PRODUCT_POLICY.md` mục 7 (giả mạo
  quyền lợi, dùng lại token, liệt kê thiết bị, rò mã cài đặt), `.standards/docs/AUTH_AND_ACCOUNT.md` mục 7, 14.3.
- **Mã**: `src/Tsudev.Audit.Core/Pro/` (logic, test trên Linux), `src/Tsudev.Audit.Windows/ProAdapters.cs` (DPAPI,
  mở trình duyệt), `src/Tsudev.Audit.Cli/Program.Pro.cs` (nối CLI).

## 1. Tài sản và ranh giới tin cậy

| Tài sản | Ở đâu | Giá trị với kẻ tấn công |
| --- | --- | --- |
| Refresh token (30 ngày, xoay vòng) | `%LOCALAPPDATA%\tsudev\tsuswico\refresh-token.dpapi` (DPAPI CurrentUser + entropy riêng) | Lấy JWS quyền lợi của nạn nhân, đếm thêm thiết bị vào tài khoản |
| Access token (15 phút) | Chỉ trong bộ nhớ | Như trên, ngắn hạn |
| Mã cài đặt (128 bit) | `installation-id` | Làm sai số `devices_30d` của tài khoản đó |
| JWS quyền lợi đệm (≤ 7 ngày) | `entitlement.jws` + `jwks.json` | Không có - JWS có chữ ký, chỉ ẩn/hiện chức năng trong app |
| Dữ liệu quét (D1 cục bộ, D2 nếu rời máy) | Thư mục báo cáo | **Không** đi qua đường nào trong phạm vi này |

Ranh giới: (A) tiến trình `swico.exe` (chạy quyền quản trị) ↔ người dùng/tiến trình khác cùng máy; (B) máy ↔ `tsudev.com`
qua TLS; (C) cổng loopback - **mọi tiến trình trên máy** đều gọi tới được.

## 2. Đe doạ và biện pháp

| # | Đe doạ (STRIDE) | Biện pháp | Kiểm chứng |
| --- | --- | --- | --- |
| T1 | **Giả mạo JWS quyền lợi** (S/T): sửa `entitlement.jws` để thành Pro, ký bằng khoá tự sinh, `alg: none`/`HS256` dùng khoá công khai làm bí mật, đổi `aud` lấy JWS của app khác | `EntitlementVerifier`: chỉ `ES256`, `typ` = `tsu-ent+jwt`, khoá theo `kid` từ JWKS, chữ ký P1363 64 byte, `iss`, `aud` = `tsuswico`, `exp`, `exp - iat ≤ 7 ngày`, từ chối `crit`. Kiểm lại MỖI lần đọc đệm. Gate thật ở máy chủ (TsuDEV Sync trả 402) - gate trong app chỉ ẩn/hiện | `EntitlementTests` (P2) |
| T2 | **Thay JWKS đệm** (T): ghi `jwks.json` bằng khoá của kẻ tấn công rồi ký JWS giả | Ai ghi được `jwks.json` là đã chạy mã dưới tài khoản người dùng ⇒ ngoài phạm vi (A). Hệ quả tối đa: app hiện mình là Pro trên máy đó; không lấy được gì từ máy chủ. JWKS chỉ ghi đè khi JWS mới kiểm được bằng nó | `ProLoginTests.JWS_quyen_loi_sai_chu_ky_*` |
| T3 | **Dùng lại / trộm refresh token** (I/E): đọc tệp token, chép sang máy khác | DPAPI CurrentUser: tệp chép sang tài khoản/máy khác không giải được ⇒ coi như chưa đăng nhập. Xoay vòng mỗi lần dùng + phát hiện dùng lại ở tsudev.com (thu hồi cả cây phiên). **Giới hạn đã biết**: tiến trình độc chạy CÙNG tài khoản Windows giải được DPAPI - không cơ chế phía khách nào chặn được việc đó; thiệt hại giới hạn ở quyền lợi (không có dữ liệu quét trên máy chủ) | `Token_tep_tu_tai_khoan_khac_*`, `Refresh_xoay_vong_*` |
| T4 | **Tự gây phát hiện dùng lại** (D): hai cửa sổ swico refresh cùng lúc ⇒ lần hai dùng token đã xoay ⇒ tsudev.com thu hồi mọi phiên của người dùng | Khoá tệp `session.lock` (FileShare.None) quanh refresh; không lấy được trong 10 s ⇒ dùng đệm, không refresh. Token mới ghi NGAY sau khi nhận | `Hai_tien_trinh_khong_refresh_song_song` |
| T5 | **Chặn mã ủy quyền trên loopback** (I): tiến trình khác nghe trộm/đọc URL callback | PKCE S256 (verifier 256 bit chỉ ở trong bộ nhớ) - mã vô dụng nếu thiếu verifier; mã dùng một lần, sống 60 s | `PKCE_S256_dung_vector_RFC_7636_*`, IdP giả kiểm verifier |
| T6 | **Tiêm phản hồi giả vào cổng loopback** (S): tiến trình khác gọi `/callback?code=<mã của kẻ tấn công>` để đăng nhập nạn nhân vào tài khoản lạ (login CSRF) | `state` 256 bit so thời gian hằng số TRƯỚC mọi thứ; lặp tham số ⇒ sai dạng; `nonce` trong id_token buộc vào đúng lượt; `iss` (RFC 9207) phải là `https://tsudev.com/oidc`. Đường khác `/callback` ⇒ 404 và chờ tiếp; kết nối treo bị cắt sau 10 s; đầu yêu cầu ≤ 8 KB | `State_sai_*`, `Thieu_state_*`, `Tham_so_iss_khac_*`, `Nonce_sai_*`, `Callback_lap_tham_so_*`, `Duong_khac_callback_*` |
| T7 | **Lẫn IdP / giả mạo discovery** (S) | Endpoint ghi cứng (không đọc discovery lúc chạy), chỉ `https://tsudev.com`, `HttpClient` KHÔNG theo chuyển hướng. id_token: chỉ ES256, `iss`, `aud` (mảng nhiều phần tử ⇒ `azp`), `exp`/`iat` lệch ≤ 5 phút | `IdTokenValidatorTests`, `Diem_cuoi_production_*` |
| T8 | **Trình duyệt chạy quyền quản trị** (E): swico.exe đã nâng quyền, ShellExecute URL sẽ mở trình duyệt nâng quyền | Nhờ explorer của desktop (không nâng quyền) gọi `ShellExecute` qua COM `ShellWindows` ⇒ `IShellBrowser` ⇒ `IShellView` ⇒ `IShellDispatch2`; thất bại ⇒ in liên kết, KHÔNG lùi về ShellExecute nâng quyền; chỉ nhận URL `https`. Không WebView. Báo cáo HTML sau khi quét cũng mở theo đường này. (`explorer.exe <url>` - cách đầu tiên - đo SAI trên Windows thật: mở thư mục Documents) | ✅ đo 20/11/2026 Win 11: Edge Elevated = 0 (`WINDOWS-VERIFICATION` P2-P3) |
| T9 | **Liệt kê / đánh dấu thiết bị** (I): dùng mã cài đặt để theo dấu máy | Mã ngẫu nhiên 128 bit, KHÔNG từ MAC/serial/mã sản phẩm dù app đọc được; chỉ sinh khi đăng nhập; chỉ gửi trong `X-Tsu-Installation` tới `/api/me/entitlements`; máy chủ lưu HMAC, JWS chỉ trả số đếm; `logout` xoá mã | `InstallationIdTests`, `Dang_nhap_thanh_cong_*` (header đúng mã) |
| T10 | **Rò mã cài đặt / token qua nhật ký** (I) | `--log-file` chỉ ghi trạng thái (`pro.login status=...`), không ghi token, mã, URL có `code`. URL ủy quyền in ra màn hình chỉ chứa `state`/`nonce`/challenge (không bí mật sau khi dùng) | Rà mã `Program.Pro.cs` |
| T11 | **Gọi mạng ngầm khi chưa đăng nhập** (I, vi phạm hạng C) | Mọi đường trừ `login` kiểm `HasSession` trước khi chạm `ProHttp`; đọc không tạo tệp; `--silent`/`--json-only` không bao giờ gọi tsudev.com | `ProOfflineGuardTests` (bộ xử lý HTTP ném lỗi), CI Windows "Lenh tai khoan khi chua dang nhap" |
| T12 | **Làm hỏng lần quét / triển khai hàng loạt** (D): tsudev.com chậm/sập, tệp hỏng, DPAPI lỗi | Thông báo Pro: timeout 5 s, mọi ngoại lệ nuốt + ghi nhật ký, quét tiếp; không đổi mã thoát; không bao giờ chờ phím ở `--silent`/`--json-only` | `Ngoai_tuyen_*`, `ProNoticeTests` |
| T13 | **Lùi đồng hồ máy để kéo dài Pro đệm** (T) | Chỉ ảnh hưởng ẩn/hiện trong app trên máy đó; JWS vẫn bị giới hạn bởi chữ ký + máy chủ gate 402 cho mọi thứ có giá trị | Chấp nhận |

## 3. Rủi ro còn lại (chấp nhận, ghi rõ)

1. Mã độc chạy cùng tài khoản Windows đọc được refresh token (T3) - giới hạn của mọi app desktop không có phần cứng bảo
   mật; thiệt hại: quyền lợi + đếm thiết bị, không có dữ liệu người dùng phía máy chủ ở P3.
2. Kỹ thuật viên chạy swico bằng tài khoản quản trị khác (UAC "over-the-shoulder"): token nằm trong
   `%LOCALAPPDATA%` của tài khoản quản trị đó, không phải của người đang ngồi máy - đúng hành vi DPAPI CurrentUser.
3. Mở trình duyệt ở quyền thường cần explorer.exe đang chạy làm shell (máy không có desktop/explorer thay thế ⇒ người dùng tự dán liên kết). Chưa đo ca Edge chưa chạy sẵn (T8).

## 4. Phiếu rà soát bảo mật P3 (trước khi phát hành)

- [x] Không thêm gói NuGet (DPAPI qua `crypt32`, HTTP/JSON/ECDsa của BCL)
- [x] Client công khai: không secret nào trong exe (`token_endpoint_auth_method: none`)
- [x] Mọi kết nối mới liệt kê trong `PRIVACY.md` / `PRIVACY.en.md`, nêu rõ chỉ sau khi đăng nhập
- [x] Test hạng C + IdP giả, không test nào gọi `tsudev.com`
- [x] Chạy thật trên Windows 11 với tsudev.com production: P1-P12, P14 (`login --silent`, trước mang mã P12) đạt (20/11/2026; P7 proxy chết, P10 tài khoản Windows thứ hai - DPAPI không giải được); P13 bỏ qua có chủ đích, `ProRenewalTests` phủ
- [x] Điều khoản thuê bao 7.5 (bộ quy ước v4.0.0) trước liên kết mua trong `swico account` - PRO2 #57, test so nguyên
      văn với `.standards`; chạy thật vi/en 20/11/2026 (`docs/WINDOWS-VERIFICATION.md` P12)
