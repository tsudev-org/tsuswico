# ADR-0002: Lưu cục bộ mặc định, TsuDEV Pro = đồng bộ/sao lưu báo cáo qua TsuDEV Sync

- **Trạng thái**: Đã chấp nhận (20/11/2026, phiên tsuswico-68) - trong phạm vi chủ dự án đã chốt ở phiếu
  `20261119-PRO1` mục 5 và `PRODUCT_POLICY.md` mục 7 (bộ quy ước v3.9.1). ADR này KHÔNG thêm tính năng Pro nào ngoài
  đồng bộ/sao lưu đám mây, nên không cần duyệt riêng theo mục 7.2.
- **Phiếu**: `logs/handover/20261119-PRO1_luu-cuc-bo-pro-dong-bo.md`; phiếu gốc
  `~/projects/logs/handover/20261119-PRO1_ke-hoach-luu-cuc-bo-pro-dong-bo-toan-he.md`
- **Phụ thuộc**: tsudev ADR-0002 (máy chủ quyền lợi, đã chạy production 20/11/2026), PRO-D (`tsudev-org/tsusync` +
  thư viện .NET `TsuSync` - CHƯA có), đăng ký `client_id` OIDC cho Tsu Swico ở tsudev.com (CHƯA có)

## 1. Kiểm kê (đo trên mã `main` @ `2bf3b49`, 20/11/2026)

| Câu hỏi (phiếu gốc mục 5.1) | Thực tế |
| --- | --- |
| Dữ liệu người dùng nằm ở đâu | Chỉ tệp cục bộ do người dùng chọn: `tsudev-bao-cao-ra-quet-<Máy>-<Ngày>/` (HTML, JSON, XLSX, CSV) cạnh exe hoặc thư mục `-o`. `--json-only` không ghi tệp nào |
| Cài đặt/hồ sơ lưu ở đâu | Không có. CLI không ghi registry, không ghi `%APPDATA%`/`%LOCALAPPDATA%`; mọi tùy chọn là tham số dòng lệnh hoặc biến môi trường (`TSUSWICO_LANG`) |
| Kết nối mạng | Đúng một đường: `GET https://api.github.com/repos/tsudev-org/tsuswico/releases/latest` lúc khởi động (`src/Tsudev.Audit.Windows/UpdateAdapters.cs:27`), gửi IP + `User-Agent: TsuSwico/<v>`. Người dùng bấm Cập nhật ⇒ tải tệp cài + `SHA256SUMS.txt` từ Release đó. Tắt bằng `--no-update-check` |
| Đồng bộ / sao lưu / đo lường / báo lỗi | Không có. Không SDK theo dõi, không gửi báo lỗi, không máy chủ nào giữ dữ liệu người dùng |
| Tài khoản | Không có màn đăng nhập (hạng C - ngoại tuyến, `AUTH_AND_ACCOUNT.md`; kiểm chứng QU-STD-AUTH ở S007) |
| Gom nhiều máy hiện nay | Thủ công, miễn phí: chép thư mục cấp 3 của máy khác vào cấp 2, `tsudev-tong-hop.html` tự đọc đệ quy |

Kết luận: bản hiện tại đã đạt N2 ("miễn phí = không gửi dữ liệu lên mạng") và checklist "0 byte dữ liệu người dùng rời
máy". Không có đường đồng bộ miễn phí nào phải tắt ⇒ **không cần ân hạn chuyển tiếp** (cũng không có người dùng thật -
`PRODUCT_POLICY.md` mục 7).

## 2. Quyết định

### 2.1. Miễn phí (giữ nguyên, `MUST NOT` thu hẹp)

Toàn bộ chức năng quét, bốn định dạng báo cáo, `--json-only`, trang tổng hợp nhiều máy bằng cách chép thư mục, bộ luật
cập nhật độc lập. Không đăng nhập thì **không thêm bất kỳ kết nối mạng nào** ngoài kiểm tra bản mới hiện có.

### 2.2. TsuDEV Pro (gói chung 49.000 ₫/tháng, 490.000 ₫/năm - không gói riêng cho Tsu Swico)

Chỉ một nhóm tính năng, đúng N3:

1. **Đồng bộ báo cáo giữa các máy cùng tài khoản**: sau khi quét, báo cáo JSON (nguồn của HTML/XLSX/CSV) được mã hoá
   ở máy và đẩy lên TsuDEV Sync, không gian tên `tsuswico`; `tsudev-tong-hop.html` trên bất kỳ máy nào cùng tài khoản
   gom được báo cáo của mọi máy mà không phải chép thư mục. Đây là bản "đám mây" của tính năng gom nhiều máy đang miễn phí
   bằng tay - bản tay vẫn miễn phí.
2. **Sao lưu báo cáo** lên cùng kho đó (khôi phục về máy mới).

Không đồng bộ cài đặt: app không có cài đặt để đồng bộ (mục 1). Nếu sau này có hồ sơ quét/bộ luật tự soạn thì thêm vào
cùng không gian tên, ghi bổ sung ADR này.

Dữ liệu báo cáo gồm tên máy, tên người dùng, số sê-ri phần cứng, danh sách phần mềm ⇒ coi như **D2** khi rời máy, dù
mức cao nhất của repo là D1 khi nằm cục bộ: `MUST` mã hoá đầu cuối bằng thư viện .NET `TsuSync`
(`crypto_secretstream_xchacha20poly1305`, khoá Argon2id từ mật khẩu đồng bộ NFKC - hợp đồng 7.4). Máy chủ không giữ khoá.
Hạn mức mặc định 100 MiB dữ liệu + 1 GiB tệp mỗi tài khoản (7.5) - một báo cáo JSON ~50-300 KB ⇒ đủ hàng trăm máy, không
cần ADR riêng về hạn mức.

### 2.3. Đăng nhập, quyền lợi, mã cài đặt

- Lệnh mới `swico.exe login` / `logout` / `account` (chỉ ở chế độ tương tác): OIDC Authorization Code + PKCE qua
  **loopback** `http://127.0.0.1:<cổng ngẫu nhiên>/callback` (RFC 8252, đường lui `http://[::1]:...`), trình duyệt hệ
  thống; scope `openid offline_access tsudev.entitlements` kèm `prompt=consent`, `state`, `nonce`; id_token kiểm ES256
  theo JWKS. Không nhúng WebView. *(Bổ sung P3, 20/11/2026: `offline_access` bắt buộc để có refresh token;
  oidc-provider 9.x bỏ `offline_access` khi yêu cầu thiếu `prompt=consent`. Bộ nhận dùng `TcpListener`, không
  `HttpListener`/http.sys. Mở trình duyệt qua `explorer.exe` để không chạy trình duyệt ở quyền quản trị.)*
- Token làm mới lưu bằng **DPAPI** (phạm vi CurrentUser, P/Invoke `crypt32!CryptProtectData` - không cần gói
  `ProtectedData`) ở `%LOCALAPPDATA%\tsudev\tsuswico\`. Không lưu mật khẩu. Refresh xoay vòng nằm trong khoá tệp
  `session.lock` (hai tiến trình refresh cùng lúc sẽ kích hoạt phát hiện dùng lại của tsudev.com). Lần quét tương tác
  chỉ làm mới khi JWS đệm cũ hơn 24 giờ; `--silent`/`--json-only` không bao giờ gọi mạng tài khoản.
- **Mã cài đặt** 128 bit (`RandomNumberGenerator`), sinh lần đầu đăng nhập (không sinh khi chưa đăng nhập - bản miễn phí
  không cần), lưu cùng thư mục, gửi trong `X-Tsu-Installation` (AUTH_AND_ACCOUNT 14.3). Không dùng MAC/serial dù app
  đọc được chúng - dữ liệu quét KHÔNG BAO GIỜ dùng làm định danh thiết bị.
- Quyền lợi: `GET https://tsudev.com/api/me/entitlements` ⇒ `{"jws": ...}`; kiểm ES256 bằng `ECDsa` của .NET BCL với
  JWKS `https://tsudev.com/oidc/jwks` (chọn theo `kid`), kiểm `typ` = `tsu-ent+jwt`, `iss` = `https://tsudev.com/oidc`,
  `aud` = `client_id` của Tsu Swico, `exp`, chỉ nhận `alg` = `ES256`. Đệm JWS ≤ 7 ngày; quá `exp` ⇒ miễn phí, không lỗi.
- **Gate ở máy chủ**: TsuDEV Sync trả `402 pro_required` khi không Pro; app gặp mã đó ⇒ in hướng dẫn mở
  `https://tsudev.com/pro?app=<client_id>`, không thử lại. Gate trong app chỉ để ẩn/hiện lệnh đồng bộ.
- Pro hết hạn: theo 7.2 (chặn đẩy, kéo/xuất 90 ngày, nhắc ở mốc 30 và 7 ngày). Báo cáo trên máy không bao giờ bị xoá.

### 2.4. Thông báo "nhiều thiết bị chưa có Pro" (7.3) trong một CLI

- Hiện ở **đầu lần chạy tương tác** (trước khi quét, cùng chỗ với hộp thoại cập nhật), nội dung vi/en nguyên văn 7.3,
  hai lựa chọn: **Xem gói Pro** (mở `tsudev.com/pro?app=<client_id>`) / **Để sau** (tiếp tục quét); liên kết miễn phí
  "Sao lưu ra tệp" trỏ tới thư mục báo cáo hiện có.
- `--silent` và `--json-only` `MUST NOT` hiện (chạy qua RMM/GPO, không có người xem; chặn chờ phím là làm hỏng triển
  khai hàng loạt) và không đổi mã thoát.
- Mốc `tsu.pro_notice.last_shown` (UTC ISO 8601) lưu trong `%LOCALAPPDATA%\tsudev\tsuswico\state.json`.
- Đủ 5 test bắt buộc với nguồn quyền lợi giả.
- Lưu ý vận hành: kỹ thuật viên đăng nhập trên nhiều máy khách sẽ làm `devices_30d` lớn - đúng ý đồ (đúng đối tượng
  cần Pro để gom báo cáo), không phải lỗi.

### 2.5. Kênh thanh toán (PRODUCT_POLICY 3.3)

Tsu Swico chỉ phát hành ngoài cửa hàng (GitHub Release, winget, Scoop) ⇒ Pro và Ủng hộ qua **tsudev.com**
(`/pro?app=<client_id>`, `/ung-ho?app=<client_id>`). Không nhúng cổng thanh toán, không mã Play/App Store. Màn
`swico.exe account` in điều khoản thuê bao nguyên văn 7.5 TRƯỚC liên kết mua. Ghi chú: VietQR trên web là trả trước
một kỳ, không tự gia hạn - chờ câu chữ kênh web ở tsudev-standards v3.9.2 (bàn giao 20261120-01 mục 6) rồi mới in.

### 2.6. Huy hiệu (PRO-C)

`swico.exe account` in nhãn chữ `[Pro]` / `[Ủng hộ]` theo `badge` của JWS. Báo cáo HTML/trang tổng hợp vẽ lại SVG từ
`.standards/assets/badges/` khi bộ quy ước đồng bộ thư mục đó (hiện `sync-standards.sh` chưa chép `assets/`), tôn
trọng `prefers-reduced-motion`. Không tự thiết kế huy hiệu.

## 3. Thứ tự làm và điều kiện mở khoá

| Bước | Việc | Chặn bởi |
| --- | --- | --- |
| P1 | ADR này + kiểm kê + sync v3.9.1 | - (XONG cùng PR) |
| P2 | `Entitlements` (.NET, kiểm JWS bằng JWKS giả trong test), `ProNotice` + 5 test, `InstallationId`, chuỗi vi/en | - (XONG 20/11/2026, `src/Tsudev.Audit.Core/Pro/`; nối vào CLI ở P3) |
| P3 | `login`/`logout`/`account` loopback PKCE | - (XONG 20/11/2026, client `tsuswico` - tsudev#339; chạy thật trên Windows: `docs/WINDOWS-VERIFICATION.md` mục P) |
| P4 | Đồng bộ báo cáo qua `TsuSync` + tổng hợp đa máy | PRO-D1..D3 (`tsudev-org/tsusync`, thư viện .NET) |
| P5 | PRIVACY.md + THREAT_MODEL (giả mạo JWS, dùng lại token, liệt kê thiết bị, rò mã cài đặt) + phiếu rà soát bảo mật | XONG cùng P3 (`PRIVACY.md`, `PRIVACY.en.md`, `docs/THREAT_MODEL.md` mục 4); còn mục P Windows thật trước khi phát hành |

Khi P3 phát hành, `PRIVACY.md` `MUST` đổi tuyên bố "đúng một kết nối" thành danh sách đầy đủ, nêu rõ các kết nối mới
chỉ xảy ra **sau khi người dùng tự đăng nhập**.

## 4. Hệ quả

- Bản miễn phí không đổi hành vi mạng ⇒ không phải sửa tờ khai winget/Scoop, không đụng quy tắc chống virus.
- Thêm một phụ thuộc mã nguồn khi P4 (`TsuSync`); P2 chỉ dùng BCL (`ECDsa`, `ProtectedData` qua gói
  `System.Security.Cryptography.ProtectedData` của Microsoft).
- Cần `client_id` riêng - việc của phiên tsudev (đăng ký client loopback công khai, không có secret).
