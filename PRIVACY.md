# Tuyên bố quyền riêng tư

**Áp dụng cho:** `Tsu Swico` (bản CLI `swico.exe`). Mục "Tài khoản TsuDEV" áp dụng từ bản phát hành đầu tiên có
lệnh `swico login` (sau `26.11.2001`).
**Cập nhật:** 20/11/2026 · [English](PRIVACY.en.md)

## Tóm tắt

- **Không đăng nhập (mặc định, đủ mọi chức năng quét):** phần mềm **không gửi dữ liệu nào của máy bạn đi đâu cả**.
  Nó thực hiện đúng **một** kết nối mạng - hỏi GitHub xem đã có phiên bản mới chưa - và kết nối đó tắt được bằng
  `--no-update-check`. Không tạo tệp nào ngoài báo cáo bạn yêu cầu.
- **Đăng nhập tài khoản TsuDEV (không bắt buộc, `swico login`):** thêm các kết nối tới `tsudev.com` để lấy quyền lợi
  TsuDEV Pro. Các kết nối này **chỉ xảy ra sau khi bạn tự đăng nhập**, không mang kết quả quét, và chấm dứt khi bạn
  chạy `swico logout`. Danh sách đầy đủ ở mục "Tài khoản TsuDEV".

> **Thay đổi so với các bản trước:** bản 26.8.18 tuyên bố *"không kết nối Internet vì bất kỳ mục đích nào"*; từ khi có
> tự cập nhật, tuyên bố đó không còn đúng. Bản `26.11.2001` tuyên bố *"đúng một kết nối"* - vẫn đúng khi **chưa đăng
> nhập**; đăng nhập thì có thêm các kết nối liệt kê dưới đây.

## Vì sao cần tuyên bố này

Công cụ đọc số sê-ri phần cứng, danh sách phần mềm đã cài, trạng thái Defender
và thông tin bản quyền. Đó là dữ liệu nhạy cảm về một máy tính cụ thể. Khi một
chương trình đọc những thứ đó **và** đòi quyền Administrator, người dùng có
quyền yêu cầu biết chính xác dữ liệu đi đâu.

## Dữ liệu được đọc

| Nhóm | Cụ thể | Nguồn |
|---|---|---|
| Bản quyền | Trạng thái kích hoạt Windows/Office, một phần mã sản phẩm, kênh cấp phép | WMI, `slmgr.vbs`, `ospp.vbs`, registry |
| Phần cứng | Máy, bo mạch chủ, CPU, RAM, ổ đĩa, số sê-ri | WMI |
| Phần mềm | Danh sách chương trình đã cài, phiên bản, ngày cài | Registry |
| Bảo mật | Trạng thái Defender, lịch sử phát hiện, tác vụ theo lịch đáng ngờ | WMI |
| Toàn vẹn | Kết quả DISM/SFC (chỉ khi được yêu cầu) | Tiến trình hệ thống |
| Định danh | Tên máy, tên miền, tên người dùng đang đăng nhập | Biến môi trường |

Không dữ liệu nào trong bảng này được dùng làm định danh thiết bị hay gửi tới `tsudev.com`, kể cả khi đã đăng nhập.

## Kết nối mạng khi chưa đăng nhập - đúng một

Khi khởi động, công cụ gọi **một** yêu cầu GET tới:

```
https://api.github.com/repos/tsudev-org/tsuswico/releases/latest
```

**Được gửi đi** (không thể tránh với bất kỳ yêu cầu HTTP nào):

- Địa chỉ IP công cộng của máy
- Chuỗi nhận dạng `TsuSwico/<phiên-bản>` - cho GitHub biết phiên bản đang dùng

**KHÔNG được gửi đi:** tên máy, tên người dùng, số sê-ri phần cứng, danh sách
phần mềm, trạng thái bản quyền, kết quả quét - **không một dữ liệu nào** trong
số công cụ thu thập.

Nếu bạn bấm "Cập nhật", công cụ tải thêm file cài đặt và file `SHA256SUMS.txt`
từ cùng bản phát hành đó, rồi **đối chiếu mã băm trước khi chạy**.

### Tắt hoàn toàn

```
swico.exe --no-update-check
```

Khi tắt và chưa đăng nhập, công cụ **không thực hiện bất kỳ kết nối mạng nào**.

## Tài khoản TsuDEV (không bắt buộc)

Đăng nhập chỉ để nhận quyền lợi **TsuDEV Pro** (đồng bộ/sao lưu báo cáo giữa các máy - đang xây dựng, xem
`docs/adr/0002-luu-cuc-bo-pro-dong-bo.md`). Không đăng nhập thì không mất chức năng quét nào.

### Đăng nhập diễn ra thế nào

`swico login` mở **trình duyệt mặc định của máy** tới `https://tsudev.com/oidc/auth` (OpenID Connect, Authorization Code
+ PKCE). Bạn nhập mật khẩu **trên trang của tsudev.com trong trình duyệt** - `swico.exe` không bao giờ thấy mật khẩu,
không nhúng trình duyệt riêng. Trang đăng nhập chuyển kết quả về một cổng tạm trên chính máy bạn
(`http://127.0.0.1:<cổng ngẫu nhiên>/callback`, đóng ngay sau đó).

### Các kết nối thêm (chỉ khi đã đăng nhập)

| Khi nào | Tới đâu | Gửi gì |
|---|---|---|
| `swico login` | `POST https://tsudev.com/oidc/token` | Mã đăng nhập một lần, mã xác minh PKCE, `client_id` = `tsuswico` |
| `swico login`, `swico account`, đầu lần quét **tương tác** khi bản quyền lợi lưu trên máy đã cũ hơn 24 giờ | `POST https://tsudev.com/oidc/token` | Refresh token (đổi lấy token mới - token cũ hết hiệu lực) |
| như trên | `GET https://tsudev.com/oidc/jwks` | Không gì ngoài IP + `User-Agent` (lấy khoá công khai để kiểm chữ ký) |
| như trên | `GET https://tsudev.com/api/me/entitlements` | Access token + **mã cài đặt** (header `X-Tsu-Installation`) |
| `swico logout` | `POST https://tsudev.com/oidc/token/revocation` | Refresh token (để tsudev.com thu hồi nó) |

Mọi kết nối đều kèm IP công cộng và `User-Agent: TsuSwico/<phiên-bản>`.

**Không bao giờ** có kết nối tài khoản nào khi chạy `--silent` hoặc `--json-only` (triển khai qua RMM/GPO), kể cả khi
đã đăng nhập. Mạng lỗi hay tsudev.com không trả lời thì công cụ coi như bản miễn phí và **quét tiếp bình thường**.

**Mã cài đặt** là 128 bit ngẫu nhiên sinh ở lần đăng nhập đầu tiên trên máy này - **không** suy ra từ số sê-ri, địa chỉ
MAC, mã sản phẩm Windows hay bất kỳ dữ liệu quét nào. tsudev.com chỉ lưu bản băm HMAC của nó để đếm số thiết bị dùng
tài khoản trong 30 ngày (`devices_30d`); bản ghi không hoạt động 90 ngày bị xoá. Tsu Swico chỉ nhận lại **con số**,
không nhận danh sách thiết bị.

**Kết quả quét KHÔNG được gửi đi** ở bất kỳ kết nối nào trên. Khi tính năng đồng bộ báo cáo (Pro) ra đời, nó sẽ mã hoá
đầu cuối và tài liệu này **bắt buộc** được cập nhật trước khi phát hành.

### Lưu gì trên máy

Chỉ sau khi đăng nhập, trong `%LOCALAPPDATA%\tsudev\tsuswico\` của tài khoản Windows đang chạy:

| Tệp | Nội dung |
|---|---|
| `refresh-token.dpapi` | Refresh token, **mã hoá bằng DPAPI** của Windows (chỉ đúng tài khoản Windows này trên đúng máy này giải được) |
| `account.json` | Mã tài khoản TsuDEV (`sub`) và thời điểm đăng nhập - không email, không tên |
| `entitlement.jws`, `jwks.json` | Quyền lợi đã ký (hết hạn tối đa sau 7 ngày) và khoá công khai để kiểm nó khi ngoại tuyến |
| `installation-id` | Mã cài đặt (xem trên) |
| `state.json` | Lần cuối hiện thông báo "nhiều thiết bị chưa có Pro" |

Không lưu mật khẩu. Không ghi registry.

### Xoá

```
swico.exe logout
```

thu hồi refresh token ở tsudev.com (nếu có mạng) và **xoá toàn bộ thư mục trên**, kể cả mã cài đặt. Báo cáo đã quét
không bị xoá. Phiên trên máy chủ cũng tự hết hạn sau 30 ngày không dùng; bạn có thể đăng xuất từ xa ở trang "Thiết bị và
phiên đang đăng nhập" của tsudev.com.

## Dữ liệu quét đi đâu

**Chỉ ghi ra đĩa của chính máy đó**, vào thư mục bạn chỉ định (mặc định là thư
mục chứa file thực thi), dưới dạng HTML, JSON, XLSX và CSV.

Kết quả quét **không bao giờ** rời khỏi máy - có đăng nhập hay không.

**Tệp nhật ký** chỉ được tạo khi bạn tự thêm `--log-file <tệp>`, ở đúng chỗ bạn chỉ.
Nó chứa những dòng đã in ra màn hình (gồm tên máy và đường dẫn thư mục kết quả),
tên và thời lượng từng bước quét, mã thoát và chi tiết lỗi - **không** chứa kết
quả quét (số sê-ri, khoá sản phẩm, danh sách phần mềm) và **không** chứa token. Công cụ không gửi tệp này
đi đâu; nếu bạn gửi nó cho người hỗ trợ thì đó là lựa chọn của bạn.

## Sau khi ghi ra đĩa

Từ thời điểm đó, dữ liệu nằm dưới quyền kiểm soát của bạn. Nếu bạn copy thư mục
kết quả sang máy khác hoặc gửi cho người khác, **bạn** là người chịu trách nhiệm
với dữ liệu đó. Hãy nhớ rằng báo cáo chứa số sê-ri phần cứng và toàn bộ danh
sách phần mềm của máy.

## Nếu phạm vi thay đổi

Mọi kết nối mới hoặc dữ liệu mới rời khỏi máy (ví dụ đồng bộ báo cáo Pro) **bắt buộc** được ghi vào tài liệu này
**trước** khi phiên bản đó phát hành.

## Tự kiểm chứng

Mã nguồn công khai. Toàn bộ mã chạm tới mạng nằm trong **hai file**:

- `src/Tsudev.Audit.Windows/UpdateAdapters.cs` - kiểm tra và tải bản cập nhật
- `src/Tsudev.Audit.Core/Pro/ProHttp.cs` - các kết nối tài khoản ở bảng trên; chỉ được gọi khi đã đăng nhập
  (`ProAccount.cs`, có test `ProOfflineGuardTests` canh)

Mọi lệnh gọi hệ thống phục vụ quét tập trung tại `src/Tsudev.Audit.Windows/WindowsAdapters.cs`. Bạn cũng có thể chặn
công cụ bằng tường lửa và quan sát - nó vẫn quét bình thường, chỉ ghi một ghi chú rằng chưa đối chiếu được phiên bản.
