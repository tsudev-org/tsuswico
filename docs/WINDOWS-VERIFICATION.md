# Kịch bản kiểm chứng trên Windows thật

> **Dành cho bạn chạy, không phải cho tôi.** Môi trường phát triển là Linux nên
> mọi thứ dưới đây chưa từng được thực thi lần nào.
>
> Đây là hạng mục **rủi ro cao nhất** của cả dự án: lớp `WindowsAdapters.cs`
> chạm trực tiếp vào WMI/Registry và **chưa bao giờ chạy trên Windows**.
> Toàn bộ logic nghiệp vụ đã có 64 test bảo vệ, nhưng lớp chạm hệ điều hành thì chưa.

Chép kết quả từng mục vào cột **Kết quả thật** rồi gửi lại. Chỗ nào lệch, gần
như chắc chắn lỗi nằm gọn trong `src/Tsudev.Audit.Windows/WindowsAdapters.cs`.

---

## Chuẩn bị - đọc kỹ mục này trước

> ⚠️ **Repo `github.com/tsudev-org/tsuswico` CHƯA TỒN TẠI.** Đó là URL điền tạm,
> dùng trước cho tài liệu và manifest. Mã nguồn hiện **chỉ nằm trên máy dev
> Linux**, chưa từng được đẩy lên GitHub. `git clone` sẽ báo không tìm thấy.

### Phần lớn kịch bản này KHÔNG cần mã nguồn

Chỉ mục **A1** và **A2** cần build trên Windows. Mọi mục còn lại - gồm cả ba
mục then chốt **B2, C3, D1** - chỉ cần **một file `swico.exe`**.

### Cách 1 (nhanh nhất) - chỉ copy file exe

Trên máy dev, file đã publish sẵn tại `dist/swico-portable.zip` (~29 MB).
Copy sang Windows bằng USB hoặc chia sẻ mạng, giải nén, rồi:

```powershell
cd <thư mục đã giải nén>
.\swico.exe --help
```

Không cần cài .NET Runtime - bản publish là self-contained.

**Đối chiếu sau khi copy** (phòng file hỏng trên đường truyền):

```powershell
Get-FileHash .\swico.exe -Algorithm SHA256
```

So với giá trị trong `dist/SHA256SUMS.txt` trên máy dev.

Bỏ qua A1, A2. Làm được toàn bộ phần còn lại.

### Cách 2 - chuyển cả kho mã kèm lịch sử git

Nếu muốn làm cả A1/A2, hoặc muốn sửa mã trên Windows. Cần **.NET 8 SDK**.

Trên máy dev đã tạo sẵn `dist/swico-repo.bundle` (116 KB) - một file duy nhất
chứa **nguyên vẹn cả 7 commit và toàn bộ lịch sử**. Copy sang Windows rồi:

```powershell
git clone swico-repo.bundle swico
cd swico
.\build.ps1
```

Kỳ vọng: test báo `Failed: 0`, rồi sinh ra `publish\swico.exe`.

> Dùng `git bundle` thay vì zip thư mục vì nó giữ nguyên lịch sử git. Nếu sau
> này bạn tạo repo GitHub thật, chỉ cần `git remote add` rồi `git push` là toàn
> bộ 7 commit lên đúng như cũ - không mất mốc lùi nào.

### Cách 3 - tạo repo GitHub thật

Sớm muộn cũng phải làm, vì **SignPath Foundation bắt buộc repo công khai**
(xem `docs/SIGNING.md`). Tôi không tạo repo thay bạn được - cần tài khoản của
bạn. Sau khi bạn tạo repo rỗng trên GitHub:

```bash
# chạy trên máy dev Linux
git remote add origin https://github.com/<tài-khoản>/<tên-repo>.git
git branch -M main
git push -u origin main
```

Rồi trên Windows `git clone` như bình thường.

> Nếu đổi tên repo, phải sửa lại URL ở: `README.md`,
> `Directory.Build.props` (`RepositoryUrl`), `packaging/winget/...` (3 file),
> `packaging/innosetup/swico.iss` (`AppUrl`), `EULA.txt` mục 7, `PRIVACY.md`.

---

## A. Những thứ phải đúng trước tiên

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| A1 | `dotnet build Tsudev.SystemAudit.sln -c Release` | Không lỗi. Gói `System.Management` restore được. | |
| A2 | `dotnet test tests/unittests -c Release` | `Passed: 179, Failed: 0` | |
| A3 | Nháy đúp `publish\swico.exe` | **Hiện hộp thoại UAC** (do `app.manifest`) | |
| A4 | `.\publish\swico.exe --help` | In danh sách tham số bằng tiếng Việt có dấu, không lỗi font | |
| A5 | Chạy **không** quyền Administrator | Hiện cảnh báo, vẫn chạy, không sập | |

## B. Tên thuộc tính WMI - chỗ dễ sai nhất

> ⚠️ **Các lệnh dưới đây là PowerShell, chạy TRÊN MÁY WINDOWS.** Không phải bash,
> không phải trên máy Linux. Dấu hiệu nhận biết bạn đang ở sai chỗ:
>
> | Dấu nhắc | Đây là gì | `Get-CimInstance` |
> |---|---|---|
> | `PS C:\Users\...>` | PowerShell trên Windows ✅ | chạy được |
> | `~/projects/tsuswico$` | bash trên Linux ❌ | *command not found* |
>
> Cách mở đúng: bấm **Start** → gõ `powershell` → chuột phải →
> **Run as administrator**.
>
> Khi chép lệnh, gõ dấu ống `|` **trơn**. Nếu thấy `\|` (có gạch chéo ngược) thì
> đó là cách escape của bash - PowerShell sẽ báo lỗi cú pháp.

| # | Lệnh kiểm tra | Cần xác nhận | Kết quả thật |
|---|---|---|---|
| B1 | `Get-CimInstance -Namespace root\Microsoft\Windows\Defender -ClassName MSFT_MpComputerStatus \| Format-List *` | Có các trường mà `DefenderCollector` đang đọc | |
| B2 | `Get-CimInstance -Namespace root\Microsoft\Windows\Defender -ClassName MSFT_MpThreatDetection \| Format-List *` | **Có trường `ThreatName` không?** Nếu không, phải tra chéo qua `MSFT_MpThreat` | |
| B3 | `Get-CimInstance -Namespace root\Microsoft\Windows\TaskScheduler -ClassName MSFT_ScheduledTask \| Select -First 3 \| Format-List *` | Truy vấn được, tên thuộc tính khớp | |
| B4 | `Get-CimInstance Win32_ComputerSystem, Win32_BIOS, Win32_Processor` | Khớp với `HardwareCollector` | |
| B5 | `Get-CimInstance SoftwareLicensingProduct \| Where PartialProductKey \| Format-List *` | Khớp với `WindowsLicenseCollector` | |

> Nếu B2 không có `ThreatName`: đây là sai lệch đã được dự đoán trước. Báo lại
> tên trường thật, tôi sửa adapter.

## C. Kết quả đầu ra

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| C1 | `.\publish\swico.exe --silent` | Tạo đủ 3 cấp thư mục theo quy ước | |
| C2 | Mở file `.html` sinh ra | Hiển thị đúng, tiếng Việt có dấu, không vỡ trang | |
| C3 | **Mở file `.xlsx` bằng Excel thật** | **Excel KHÔNG báo "file hỏng, cần sửa"** | ❌ **ĐÃ LỖI (18/08) - đã sửa, cần thử lại** |
| C4 | Kiểm tra `.xlsx`: dòng tiêu đề | Đóng băng khi cuộn, in đậm, nền xanh nhạt | |
| C5 | Kiểm tra `.xlsx`: cột mã số | `"007"` giữ nguyên số 0 ở đầu, **không** thành `7` | |
| C6 | Mở file `.json` | Đọc được, đúng `SchemaVersion` | |
| C7 | `.\publish\swico.exe --scope license` rồi `--scope hardware` | Mỗi lần chỉ sinh báo cáo tương ứng | |
| C8 | Mở `tsudev-tong-hop.html` | Bảng gom đủ các báo cáo, bấm "Mở →" nhảy đúng file | |
| C9 | Copy thư mục kết quả từ máy khác vào cấp 2, chạy lại | Trang tổng hợp gom cả máy mới | |
| C10 | `echo $LASTEXITCODE` sau khi chạy | Đúng quy ước 0/1/2/3 | |
| C12 | Đặt Windows sang **chế độ tối** rồi mở lại file `.html` | Trang đổi sang nền tối, chữ đọc được. **Đầu trang giữ nguyên** nền xanh đậm | |
| C13 | Bấm **In** (xem trước) khi máy đang ở chế độ tối | Bản in dùng bảng màu **sáng**, không phải nền đen | |
| C14 | Mở `.xlsx`, nhìn dòng tiêu đề | Nền xanh nhạt cùng tông với bản HTML, không lệch màu | |
| C11 | Nhìn mọi cột ngày trong `.html` và trang tổng hợp | Ngày giờ dạng `HH:mm DD/MM/YYYY` (`14:30 19/08/2026`), ngày dạng `DD/MM/YYYY` (`05/01/2024`). **Không** còn dạng `2026-08-19` hay `19/08/2026 14:30:05` | |

> **C12/C13 - vì sao phải thử thật.** Chế độ tối và khối ép màu khi in là thứ
> mới có từ phiên S005, và **chưa máy nào chạy thật**. Test chỉ đọc được chuỗi
> CSS sinh ra, không dựng trang. Riêng C13 đáng để mất một trang giấy: nếu khối
> `@media print` không ăn, người dùng in báo cáo lúc máy đang ở chế độ tối sẽ ra
> tờ giấy đen kín mực.

> **C11 - vì sao vẫn phải nhìn bằng mắt dù đã có 12 test.** Test chạy trên máy
> dev Linux, nơi `CultureInfo.CurrentCulture` là `en-US`. Định dạng đã bị khoá
> `InvariantCulture` nên về lý thuyết máy nào cũng ra như nhau, nhưng Windows
> tiếng Việt là môi trường thật duy nhất chưa ai nhìn. Xem cả **cột "Ngày cài"**
> trong bảng phần mềm - cột đó đọc thẳng từ registry của máy thật, nơi có những
> chuỗi ngày mà máy dev không tạo ra được.

> **C3 là mục quan trọng nhất trong bảng này.** Bộ ghi XLSX được viết tay theo
> chuẩn OOXML và ở phiên S001 đã có **một lỗi đặt sai vị trí phần tử `<pane>`**
> - file vẫn là XML hợp lệ nhưng Excel sẽ từ chối. Lỗi đó đã sửa, nhưng **chưa
> ai mở bằng Excel thật lần nào**. Môi trường phát triển không có Excel,
> LibreOffice, cũng không cài được `openpyxl`.

## D. Đối chiếu với bản PowerShell cũ

Đây là **bài kiểm tra hồi quy thật sự duy nhất** hiện có.

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| D1 | Chạy bộ PowerShell cũ và `swico.exe` trên **cùng một máy** | Kết luận bản quyền giống nhau | ❌ **ĐÃ LỖI (18/08): swico báo hợp lệ, PowerShell báo không - đã sửa, cần thử lại** |
| D2 | So danh sách phần mềm phát hiện được | Không thiếu mục nào so với bản cũ | |
| D3 | So điểm rủi ro và số phát hiện | Cùng thang, cùng kết luận | |

## E. Ma trận cấu hình

Chạy tối thiểu trên 3 máy khác nhau. Càng khác nhau càng tốt.

| Máy | Windows | Office | Kích hoạt | Quyền | Kết quả |
|---|---|---|---|---|---|
| 1 | Win 11 | Có | Đã kích hoạt | Admin | |
| 2 | Win 10 | Không | Đã kích hoạt | Admin | |
| 3 | Win 10/11 | Có | **Chưa** kích hoạt | Admin | |
| 4 | bất kỳ | - | - | **Không** admin | |

## F. Kiểm tra installer (sau khi có file setup)

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| F1 | Chạy `tsuswico_<phiên-bản>_x64-setup.exe` | Hiện EULA, cài được, hiện tiếng Việt | |
| F2 | Mở Command Prompt mới, gõ `swico --help` | Chạy được (nếu đã chọn thêm vào PATH) | |
| F3 | Bảng điều khiển → Programs | Có mục "Tsu Swico", gỡ được | |
| F4 | Sau khi gỡ: kiểm tra biến PATH | Đường dẫn đã bị rút ra, không để lại rác | |
| F5 | Cài đè phiên bản cũ | Nâng cấp tại chỗ, **không** tạo mục thứ hai | |
| **F7** | Biên dịch `swico.iss` với `/DAppVersion=26.8.1901` | `ISCC` không lỗi; file ra tên `tsuswico_26.8.1901_x64-setup.exe` | ⚠️ **CHƯA kiểm** |
| **F8** | Biên dịch với `/DAppVersion=26.9.0901` (**ngày một chữ số**) | `ISCC` **không** báo lỗi số 0 đứng đầu ở `VersionInfoVersion` | ⚠️ **CHƯA kiểm - rủi ro còn lại của việc đổi quy ước** |
| F6 | `tsuswico_<phiên-bản>_x64-setup.exe /VERYSILENT` | Cài im lặng, không hiện cửa sổ nào | |

## G. Phần mềm diệt virus

Rủi ro đặc thù: công cụ đòi quyền Administrator, đọc registry bản quyền, quét
dấu hiệu crack, đọc trạng thái Defender - mô tả gần trùng khớp phần mềm độc hại.

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| G1 | Tải exe về máy có Defender bật đầy đủ | Không bị chặn/xoá | |
| G2 | Nộp exe lên VirusTotal | Ghi lại số hãng báo động | |
| G3 | Nếu có báo động: gửi mẫu qua cổng báo nhầm của Microsoft và các hãng liên quan | | |

---

## H. Hiển thị tiến trình quét trên terminal thật

**Vì sao mục này phải làm bằng mắt người:** thứ tự các bước, việc huỷ và mã thoát
đều đã có test tự động (264 test trên Linux + kiểm tra dòng-theo-bước trong CI
Windows). Nhưng *dáng vẻ* của nó thì không: một con quay đứng im, một dòng bị vỡ
vì cửa sổ hẹp, hay một con trỏ không được trả về sau Ctrl+C đều **không** làm test
nào đỏ - chỉ có người ngồi trước màn hình mới thấy.

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| H1 | Chạy `swico.exe --scope hardware --no-update-check` trong Windows Terminal | Mỗi bước hiện **một dòng riêng** kèm thời gian; con quay quay đều ở dòng đang chạy | |
| H2 | Nhìn kỹ lúc bước "Toàn vẹn file hệ thống" chạy | Có dòng ghi chú thời gian dự kiến của DISM; con quay **không đứng im** | |
| H3 | Chạy với `--sfc` rồi bấm **Ctrl+C** lúc `sfc` đang chạy | Dừng trong vòng ~1 giây; các bước đã xong **vẫn còn** trên màn hình; mã thoát `130` | |
| H4 | Ngay sau H3, gõ tiếp một lệnh bất kỳ | **Con trỏ hiện lại bình thường**, chữ không bị mất màu, terminal không cần `reset` | |
| H5 | Ngay sau H3, mở Task Manager | **Không còn** tiến trình `sfc.exe` hay `DISM.exe` nào sót lại | |
| H6 | Thu hẹp cửa sổ terminal còn ~40 cột rồi chạy lại | Dòng con quay bị **cắt ngắn**, không xuống dòng, không để lại dòng rác | |
| H7 | Chạy trong `cmd.exe` cũ (mã trang 437) | Nếu không đọc được UTF-8 thì tự lùi về `+` và `\|/`, **không** hiện ký tự rác | |
| H8 | `swico.exe --scope hardware > log.txt` rồi mở `log.txt` | Mỗi bước một dòng sạch, **không** có ký tự `\r`, không có mã màu ANSI | |
| H9 | Chạy qua Task Scheduler / RMM với `--silent` | Log không có ký tự điều khiển; mã thoát như cũ | |

> H4 và H5 là hai mục quan trọng nhất. Một công cụ để lại terminal hỏng hoặc để
> `sfc.exe` chạy tiếp sau khi đã "thoát" thì tệ hơn hẳn một công cụ không có
> thanh tiến trình nào.

---

## P. Tài khoản TsuDEV - `swico login` / `account` / `logout` (PRO1 P3)

**Vì sao cần người:** luồng đăng nhập đã có test trên IdP giả (loopback thật, PKCE, refresh xoay vòng), nhưng ba thứ
chỉ đo được trên Windows thật với tsudev.com production: DPAPI, trình duyệt có bị mở ở quyền quản trị không, và client
`tsuswico` thật của tsudev.com. Dùng bản chưa phát hành: artifact `swico-unsigned-<sha>` của lần chạy CI trên `main`.

| # | Việc cần làm | Kỳ vọng | Kết quả thật |
|---|---|---|---|
| P1 | Trước khi đăng nhập: `swico.exe account` | "Chưa đăng nhập..."; thư mục `%LOCALAPPDATA%\tsudev\tsuswico\` **không** tồn tại | ✅ 20/11/2026 Win 11 26200 (agent qua WSL, UAC do người bấm): "Chưa đăng nhập", thư mục không tồn tại, mã 0 |
| P2 | `swico.exe login` | Trình duyệt mặc định mở `tsudev.com/oidc/auth?...`; in kèm liên kết dự phòng | ❌ bản `a7cbf1d`: `explorer.exe <url>` từ tiến trình nâng quyền mở **thư mục Documents**, không mở trình duyệt. ✅ sau sửa (COM ShellWindows): Edge mở trang tsudev.com |
| P3 | Trong lúc P2, mở Task Manager > Details, bật cột **Elevated** | Tiến trình trình duyệt **Elevated = No** (swico.exe = Yes) | ✅ sau sửa: 2 tiến trình Edge mới + toàn bộ 66 tiến trình `msedge` Elevated = 0 (Edge đang chạy sẵn - chưa đo ca Edge chưa mở) |
| P4 | Đăng nhập tsudev.com, bấm Cho phép | Tab hiện "Đã nhận phản hồi đăng nhập"; cửa sổ swico in "Đã đăng nhập", mã tài khoản, Gói, số thiết bị | ✅ tab "Đã nhận phản hồi đăng nhập - Tsu Swico"; in tài khoản, Gói: Miễn phí, 1 thiết bị, đệm đến +7 ngày; mã 0 |
| P5 | Mở `%LOCALAPPDATA%\tsudev\tsuswico\` | Có `refresh-token.dpapi` (nhị phân, **không** đọc được chữ `ey`/token), `account.json`, `entitlement.jws`, `jwks.json`, `installation-id` (22 ký tự) | ✅ 5 tệp; `refresh-token.dpapi` 262 B mở đầu khối DPAPI `01000000D08C9DDF`, không chứa token rõ; `installation-id` 22 ký tự |
| P6 | `swico.exe account` | Thông tin như P4, không lỗi; chạy lần 2 vẫn được (refresh xoay vòng không tự đăng xuất) | ✅ hai lần liên tiếp đều được (refresh xoay vòng, không tự đăng xuất) |
| P7 | Ngắt mạng, `swico.exe account` | Cảnh báo "Không lấy được quyền lợi mới - đang dùng bản lưu..."; mã thoát 0 | ⏭ chưa làm (không ngắt mạng máy người dùng); test `Ngoai_tuyen_dung_JWS_dem_con_han` phủ logic. Cách làm KHÔNG cần ngắt mạng: đăng nhập lại, mở PowerShell **quyền quản trị**, `$env:HTTPS_PROXY="http://127.0.0.1:9"` rồi `.\swico.exe account` trong CHÍNH cửa sổ đó (`HttpClient` dùng proxy mặc định ⇒ đọc biến môi trường; cổng 9 không nghe ⇒ lỗi mạng) |
| P8 | Có mạng, `swico.exe --scope hardware` (tương tác) | Quét bình thường; nếu tài khoản có ≥ 2 thiết bị và chưa Pro thì hiện thông báo 7.3 trước khi quét, bấm Enter = Để sau | ✅ quét tương tác 9 s, mã 0; 1 thiết bị ⇒ không hiện thông báo (đúng), không tạo `state.json` |
| P9 | `swico.exe --scope hardware --silent` | Không thông báo, không chờ phím | ✅ không thông báo, không chờ phím, mã 0 |
| P10 | Chép `refresh-token.dpapi` sang tài khoản Windows khác (hoặc máy khác) cùng thư mục, chạy `swico.exe account` ở đó | Báo phiên hết hạn / chưa đăng nhập - DPAPI không giải được | ⏭ cần tài khoản Windows thứ hai - chưa làm. Cách làm: đăng nhập ở tài khoản A, chép cả thư mục `%LOCALAPPDATA%\tsudev\tsuswico\` sang cùng đường dẫn của tài khoản B (`net user test2 * /add` tạo tạm, xoá sau), đăng nhập Windows bằng B, chạy `swico.exe account` |
| P11 | `swico.exe logout` | "Đã đăng xuất và xoá..."; thư mục ở P5 **biến mất**; báo cáo cũ còn nguyên | ✅ thư mục biến mất, 3 báo cáo còn nguyên, `account` lại báo chưa đăng nhập |
| P12 | (đăng nhập lại, tài khoản chưa có Pro) `swico.exe account` rồi `swico.exe --lang en account` | Sau "Gói: Miễn phí" có đoạn "TsuDEV Pro 490.000 ₫ cho 1 năm, KHÔNG tự gia hạn..." rồi dòng "Mua TsuDEV Pro: https://tsudev.com/pro?app=tsuswico" (en: "490,000 ₫ for 1 year, does NOT renew automatically..." / "Get TsuDEV Pro:") | ⏭ chưa làm (PRO2, 20/11/2026) |
| P13 | Tài khoản có Pro còn ≤ 7 ngày (cần tsudev.com cấp, hoặc bỏ qua): chạy `swico.exe` tương tác hai lần; `swico.exe --silent` một lần | Mỗi lần tương tác hiện "=== Gia hạn TsuDEV Pro ===" + [1] Gia hạn [2] Để sau, [1] mở trang Pro ở quyền thường; `--silent` không hiện; mã thoát như không có hộp thoại | ⏭ chưa làm - test `ProRenewalTests` (6 ca bắt buộc 7.6) phủ logic; cần chủ dự án quyết bỏ qua hay cấp tài khoản thử |
| P12 | `swico.exe login --silent` | Mã thoát 3, báo chỉ chạy tương tác | ✅ mã 3, thông báo "chỉ chạy ở chế độ tương tác" |

> P3 và P10 là hai mục quan trọng nhất: trình duyệt chạy quyền quản trị, hoặc token chép sang máy khác dùng được, đều
> là lỗi bảo mật (`docs/THREAT_MODEL.md` T8, T3).

---

## Sau khi chạy xong

Gửi lại bảng này kèm mọi thông báo lỗi nguyên văn. Với mỗi mục lệch, ghi rõ:

1. Lệnh đã chạy
2. Kết quả mong đợi và kết quả thật
3. Thông báo lỗi đầy đủ (nếu có)

Kết quả sẽ được ghi vào `docs/journal/` của phiên tương ứng và chuyển thành
test hồi quy để lần sau không phải kiểm thủ công.
