# STATE.md - Trạng thái project (agent đọc đầu phiên, cập nhật cuối phiên)

> **Phiên kế tiếp:** đọc `logs/handover/20261122-02_khep-phien-tsuswico-07.md` mục 2-3 (WINGET-2 chờ moderator; NET10 ✅ - bản phát hành kế tiếp chỉ khi chủ dự án yêu cầu).

> ## ⚠️ QUAN HỆ VỚI `docs/STATE.md` - đọc trước khi ghi vào file này
>
> Repo này có **hai** file tên `STATE.md`. Chúng **không trùng vai**, và việc để
> chúng lẫn vai là đúng loại lỗi "hai cái tên cho một thứ" mà
> `docs/VERSIONING.md` mục 2 đã cảnh báo:
>
> | File | Vai trò | Nguồn sự thật của |
> |---|---|---|
> | **`docs/STATE.md`** | Trạng thái **sản phẩm** | Việc gì đã xong/đang chờ, cạm bẫy đã biết, quyết định đã chốt |
> | **`logs/STATE.md`** (file này) | **Điều phối agent** theo `AGENTS.md` mục 2 | Ai đang làm task nào, hàng đợi ngắn hạn |
>
> **Quy tắc:** khi hai file nói khác nhau về trạng thái sản phẩm →
> **`docs/STATE.md` thắng**. File này chỉ trỏ tới đó, không chép lại nội dung
> (`AGENTS.md` mục 1: mỗi tri thức ghi một lần duy nhất).

- **Phiên gần nhất:** tsuswico-07 - 22/11/2026
- **Bàn giao gần nhất:** `logs/handover/20261122-02_khep-phien-tsuswico-07.md`

## Hàng đợi task (làm từ trên xuống)

Nguồn: `docs/STATE.md` mục 3. Ở đây chỉ ghi mã việc + trạng thái.

> **Việc làm được ngay, không cần ai** (thứ tự: phiếu `20261110-01` mục 1c): ~~`QU-7`~~ ✅, ~~`QU-STD-TABLE`~~ ✅, ~~`3.5`~~ ✅, ~~`QU-STD-BRAND`~~ ✅. Mọi việc
> còn lại đều chặn ở người dùng hoặc bên ngoài - xem cột lý do.

- [ ] **QU-2** 🔶 20/11/2026: release.yml (runner Windows) biên dịch Inno với `VersionInfoVersion=26.11.2001.0` THÀNH CÔNG. Còn ca ngày một chữ số (`26.12.0101.0`) - tự kiểm ở bản đầu tháng 12. Dòng cũ: Nhờ người dùng kiểm **Inno Setup** biên dịch được với
      `VersionInfoVersion=26.9.0901` (ngày một chữ số) - mục **F7/F8** trong
      `docs/WINDOWS-VERIFICATION.md`. **Rủi ro kỹ thuật còn lại duy nhất** của
      việc đổi quy ước; phía .NET đã đo xong, phía Inno Setup thì chưa.
- [x] **QU-3** ✅ 20/11/2026: chủ dự án chọn KHÔNG cầu nối (ADR-0003). Dòng cũ: Quyết có phát hành **bản cầu nối** dạng cũ không, để hai bản đã
      cài (`26.8.18`, `26.8.18.2`) không mất đường cập nhật - `docs/STATE.md` mục 4.7.
- [x] **3.1** ✅ PR winget #419878 đã MERGE (`tsudev.SWICO` 26.8.18.2 trên winget) - đo 16/11/2026.
- [x] **XM-2.3** ✅ 17/11/2026 (phiên tsudev-27): hồ sơ [16837](https://www.bestpractices.dev/projects/16837) passing 100% (`docs/best-practices.json`; `dynamic_analysis` Unmet trung thực). Kèm #27 (ghim SHA 16 action, quyền token, Dependabot, SECURITY.md, CONTRIBUTING.md, mẫu issue), bật CodeQL default setup, #28 sửa 2 cảnh báo CodeQL high ở website.
- [x] **TEN-1** ✅ Thư mục cục bộ đã đổi `~/projects/tsudev-swico` → `~/projects/tsuswico` (16/11/2026, phiên S008, chủ dự án cho phép làm trong phiên).
- [x] **TEN-2 / V1** ✅ 20/11/2026: `v26.11.2001` phát hành theo khuôn mới (#40, ADR-0003), Scoop `tsuswico` + `tsudev-swico` (scoop-bucket#25), attest đạt. Còn: PR winget-pkgs 26.11.2001 (mục WINGET-2 dưới). Dòng cũ: Bản phát hành đầu tiên tên `tsuswico_*`: cập nhật `autoupdate` của gói Scoop `tsuswico` + `tsudev-swico` (tên tệp mới, tag 3 thành phần) cùng lúc; kiểm bước cosign/attest chạy thật.
- [ ] **WINGET-2** 🔶 22/11/2026 (phiên 07: không đổi, nhãn như d1, không bình luận mới). (phiên d1: moderator stephengillie cho `@wingetbot run` 21/11 15:16, nhãn `Validation-Executable-Error` + `Azure-Pipeline-Passed` quay lại - cùng báo động nhầm mã thoát, đã giải thích từ 20/11; vẫn chờ). 21/11/2026 (phiên a2: nhãn chỉ còn `New-Manifest`, chờ moderator duyệt). 20/11/2026 (phiên 4c + 96 + 43 + 57 + 0e kiểm lại: vẫn MỞ, nhãn như cũ, không có bình luận mới của moderator. Phiên 93: nhãn `Validation-Executable-Error` + `Azure-Pipeline-Passed` = báo động nhầm mã thoát 20 như #419878, chờ moderator - `docs/WINGET.md`; đã bình luận giải thích 20/11, issuecomment-7030535588): chủ dự án đồng ý, đã nộp [microsoft/winget-pkgs#484563](https://github.com/microsoft/winget-pkgs/pull/484563)
      (bước 01-07 đạt lúc nộp). Chờ bot cài thử + gộp. Kèm #43 sửa thư mục manifest `tsudev/SWICO`.
- [x] **NET10** ✅ 22/11/2026 (phiên 07, #74 `9143f74`): SDK 10.0.404, net10.0, 0 cảnh báo, 357 PASS, Smoke Windows + Inno xanh, ignore dependabot đã gỡ. Dòng cũ: .NET 8 hết hỗ trợ 10/11/2026, exe self-contained nhúng runtime 8 ⇒ chuyển net10 LTS. Kế hoạch 6 bước: `docs/STATE.md` mục NET10. Làm được không cần người dùng (một PR); phát hành thì cần chủ dự án.
- [x] **DEP-1** ✅ 22/11/2026 (phiên d1): gộp Dependabot #64 (actions) + #30 (website); #31 đóng, thay bằng #69 (gói test; KHÔNG nhận System.Management 11 - không hỗ trợ net8). Đợt 2: #70, #71, #72 (System.Management 10.0.15 + sửa lock file Cli).
- [ ] **3.2** ⛔ Kiểm chứng chức năng tự cập nhật trên Windows thật - cần người dùng
- [ ] **3.3** ⛔ Kiểm bằng mắt phần hiển thị tiến trình (mục H) - cần người dùng
- [ ] **3.4** ⛔ Cấu hình SignPath sau khi được duyệt - chờ bên ngoài
- [x] **3.5** ✅ Việc kỹ thuật - **làm được ngay, không cần người dùng**.
      ~~xUnit~~ ✅ xong ở S006 (`docs/STATE.md` mục 3.5-1). ~~tách file gộp~~ ✅ 16/11/2026 (phiên tsudev-27):
      `InventoryCollectors.cs` + `LicenseCollectors.cs` → 12 tệp, mỗi tệp MỘT kiểu (kể cả record). ~~`--json-only`~~ ✅ + ~~căn
      cột dòng tiến trình~~ ✅ (#37, 20/11/2026). ~~logging có cấu trúc~~ ✅ #45 (`--log-file` JSON Lines).
      HẾT việc (NativeAOT chỉ là cân nhắc, `docs/STATE.md` mục 3.5 mục 5).
- [ ] **PRO2** 🔶 20/11/2026 (phiên 96): sync bộ quy ước **v4.0.0**; 7.5 trong `swico account` + 7.6 nhắc gia hạn/báo
      hết kỳ (`ProRenewal`), 347 PASS. Phiên 4c: P12 vi/en chạy thật ✅; P13 bỏ qua có chủ đích (`ProRenewalTests` phủ). Còn: mục 2.2 (đồng bộ) ⛔ PRO-D3. Phiếu
      `logs/handover/20261120-PRO2_*`.
- [ ] **PRO1** 🔶 P1 ✅ P2 ✅ (#44). **P3 + P5** ✅ #51 `a7cbf1d` 20/11/2026: `swico login|logout|account`, DPAPI,
      quyền lợi + `X-Tsu-Installation`, ProNoticePrompt nối `MainCore`, PRIVACY vi/en + `docs/THREAT_MODEL.md`; 334 PASS.
      Mục P chạy thật Win 11 + tsudev.com production ✅ (P7 + P10 xong phiên 4c) - tìm + sửa lỗi mở trình duyệt khi nâng quyền; câu chữ 7.5 (v3.9.2) cho liên
      kết mua - 20/11 chuẩn vẫn v3.9.1, đã gửi phiếu đề xuất `tsudev-standards/logs/handover/20261120-02_*` (vẫn MỞ); phiên 96: tsudev-standards đang soạn **v4.0.0** (Pro 1 năm không tự gia hạn, câu 7.5 mới thay đề xuất, thêm 7.6 nhắc gia hạn) - chưa tag, chờ. P4 ⛔ PRO-D (`TsuSync` .NET). Phiếu `logs/handover/20261119-PRO1_*.md` mục 4.
- [x] **3.6** ✅ thay bằng `26.11.2001` (ADR-0003). Dòng cũ: Quyết định có phát hành `26.8.1901` không - **đọc mục 4.7 trước**
- [x] **QU-6** ✅ S007 - tự giải theo bộ quy ước v3.2.0 (`BRAND_ASSETS.md`): wordmark lấy token, xem `docs/STATE.md` mục S007. Dòng cũ: ⛔ Quyết có đưa **4 sắc chữ ký thương hiệu** `tsudev` vào
      `tokens/design-tokens.json` không - hiện là bốn giá trị màu **duy nhất**
      còn viết cứng (`DesignTokens.cs`). Sửa file thuộc bộ quy ước nên **cần
      chủ project cho phép trực tiếp**. `docs/STATE.md` mục QU-4.
- [x] **QU-7** ✅ 16/11/2026 (phiên tsudev-27): `DataTable.Numeric(...)` khai cột số theo TÊN (sai tên ⇒ ném lỗi), HTML
      gắn `class="num"` (căn phải + `tabular-nums`); 10 bảng đã khai; Serial/mã sản phẩm CỐ Ý căn trái. JSON báo cáo
      có thêm trường `NumericColumns` (cộng thêm, không phá). Báo cáo mẫu website sinh lại. Dòng cũ: (a) số
      **căn phải** trong bảng - `DataTable` chưa mang kiểu cột nên phải thêm;
      (b) ~~**Warm Mode** cần nút chuyển~~ - BỎ: chủ dự án bỏ chế độ Ấm toàn hệ sinh thái (phiếu `20261110-05`,
      10/11/2026); chỉ còn Sáng / Tối / Hệ thống. `docs/STATE.md` mục QU-4.

- [x] **STD50** ✅ 21/11/2026 (phiên a2): bản sắc swico v5.0.0 - #66 màu hổ phách/bo góc `sac`/Space Grotesk (web +
      báo cáo HTML/.xlsx), #67 trang chủ `cong-cu` + hoạ tiết `mach-in` (hero + og-image); deploy `ca57caa8`. Kèm
      **STD43** ✅ (sync thẳng v5.0.0). Phiếu `logs/handover/20261121-STD50_*`, `20261121-STD43_*`.
- [x] **QU-STD-AUTH** ✅ S007 kiểm chứng: không có màn hình đăng nhập; chạy thật trên Windows 11 ở chế độ khách đủ chức năng. Kết nối duy nhất là kiểm tra bản mới (công bố trong PRIVACY.md, tắt bằng `--no-update-check`). Dòng cũ: Repo này là **hạng C - ngoại tuyến** (tài liệu nêu đích danh). Việc ở đây là **kiểm chứng chứ không phải cài đặt**: chạy đủ chức năng lõi ở chế độ khách, `MUST NOT` chặn chức năng nào sau màn hình đăng nhập, `MUST NOT` gọi mạng ngầm khi chưa đăng nhập. Checklist: `.standards/docs/AUTH_AND_ACCOUNT.md` mục 17, hai dòng cuối. Một phần mềm rà quét máy tính mà tự gọi ra ngoài là thứ đầu tiên bị công cụ bảo mật gắn cờ.
- [x] **QU-STD-TABLE** XONG 11/11/2026: báo cáo HTML - bảng liệt kê (có ô lọc) có chân bảng bộ chọn `10/20/50/100/200` (mặc định 10, nhớ theo bảng) - tóm tắt `aria-live` - phân trang; đổi mốc giữ bản ghi đầu; in ra giấy đủ dòng; script gắn sự kiện trong khối (không thêm `on*=`), băm CSP báo cáo mẫu cập nhật. Không có máy chủ nên mục 12 "tải lại từ máy chủ / chặn 200 ở máy chủ" không áp dụng. Dòng cũ: Thêm bộ chọn số bản ghi `10/20/50/100/200` (mặc định `10`, góc dưới bên trái) cho các bảng liệt kê chương trình, gói UWP, dịch vụ, tác vụ theo lịch và mục khởi động. Chuẩn: `.standards/docs/DATA_TABLE.md` mục 12.
- [x] **QU-STD-BRAND** ✅ 20/11/2026 #47: exe + bộ cài dùng chung `assets/brand/app/app.ico`, ảnh trình thuật sĩ 5+4 cỡ HiDPI sinh trong `brand-assets.mjs` (`--check`), CI biên dịch thử bộ cài, `assets/brand/README.md`. Dòng cũ: S007 làm phần web (favicon, og-image, manifest, `<head>` đủ ở swico.tsudev.com; README + metadata repo). Còn: đối chiếu checklist `BRAND_ASSETS.md` mục 14 cho icon app/bộ cài. Bổ sung tài sản nhận diện còn thiếu và siêu dữ liệu nối về `tsudev.com`. Chuẩn: `.standards/docs/BRAND_ASSETS.md` mục 14 và `.standards/docs/ECOSYSTEM_IDENTITY.md` mục 9.
- [x] **QU-STD-1** ✅ S007. Di trú `tokens/` sang `.standards/tokens/` (nguồn chân lý duy nhất). Hiện có **5 file mã nguồn** đọc token cục bộ. Đây là thay đổi PHÁ VỠ: `text-muted` đổi giá trị ở cả ba chế độ và có thêm `border-control`. Làm theo CHANGELOG mục 2.0.0 "Hướng dẫn nâng cấp", chạy lại ảnh chụp giao diện.
- [x] **QU-STD-2** ✅ S007. Xóa bản sao quy ước cũ nay đã trùng `.standards/`: docs/DESIGN_SYSTEM.md docs/PROJECT_STRUCTURE.md docs/templates/HANDOVER.md - giữ lại chỉ tạo hai nguồn chân lý.
- [x] **QU-STD-3** ✅ S007 (nút phụ của website dùng `border-control`; báo cáo không có viền nút phụ/ô nhập dùng `border-strong`). Rà chỗ dùng `border-strong` cho viền nút phụ hoặc ô nhập, đổi sang `border-control` (`.standards/docs/DESIGN_SYSTEM.md` mục 1).

## Đang thực hiện

| Task | Agent | Bắt đầu |
|---|---|---|
| (không có) | | |

## Đã hoàn thành (mới nhất trên cùng)

- 22/11/2026 - **tsuswico-07:** NET10 xong: #74 chuyển .NET 8 → .NET 10 LTS (SDK 10.0.404, lock file sinh lại, gỡ ignore
  dependabot; sửa NU1004 do `publish -r` ghi RID vào lock file Windows); CI đủ xanh; website deploy `b9e7bdfc`.
  WINGET-2 vẫn chờ. Bàn giao: `logs/handover/20261122-02_khep-phien-tsuswico-07.md`.

- 22/11/2026 - **tsuswico-d1:** tiếp phiên bị cắt (SES-0028, chưa ghi gì): gộp #64 + #30; #31 đỏ (lock file Cli bỏ sót +
  System.Management 11 không hỗ trợ net8) ⇒ #69 nhận 3 gói test, `dependabot.yml` bỏ qua System.Management/CodeDom >= 11;
  357 PASS, main xanh. Phát hiện .NET 8 EOL ⇒ việc NET10. WINGET-2 vẫn chờ. Bàn giao: `logs/handover/20261122-01_khep-phien-tsuswico-d1.md`.

- 21/11/2026 - **tsuswico-a2:** STD43 + STD50 xong: #66 bản sắc swico (sync v5.0.0; token sản phẩm cho web + báo cáo;
  357 PASS), #67 bố cục `cong-cu` + hoạ tiết `mach-in`; deploy swico.tsudev.com `ca57caa8`; dọn hồ sơ Windows tạm
  `tsuswicop10` sau khởi động lại; WINGET-2 chỉ còn nhãn `New-Manifest`. Bàn giao: `logs/handover/20261121-01_khep-phien-tsuswico-a2.md`.

- 20/11/2026 - **tsuswico-57:** #59 báo cáo HTML mang favicon + logo Tsu Swico giống website (đầu trang sản phẩm, chân
  trang nhà làm tsudev hiện hành; ảnh từ `brand-assets.mjs` → `assets/brand/report/`, nhúng assembly), gỡ logo cú cũ;
  349 PASS, website deploy. Phiếu LG1 sang tsucwico, qulythuchi, sound-db, tsudev. Máy chưa khởi động lại ⇒ chưa dọn
  hồ sơ tạm; WINGET-2 vẫn chờ. Bàn giao: `logs/handover/20261120-08_khep-phien-tsuswico-57.md`.

- 20/11/2026 - **tsuswico-4c:** chạy thật mục P (chủ dự án ủy quyền): P7 ✅ (proxy chết), P10 ✅ (tài khoản Windows tạm,
  DPAPI không giải được), P12 vi/en ✅; P13 bỏ qua có chủ đích; sửa dòng P12 (lệnh con phải đứng đầu) + đổi mã trùng P12 → P14;
  THREAT_MODEL mục 4 tích đủ. WINGET-2 vẫn chờ. Bàn giao: `logs/handover/20261120-07_khep-phien-tsuswico-4c.md`.

- 20/11/2026 - **tsuswico-96:** chỉ kiểm: P7/P10 chưa có kết quả; chuẩn vẫn v3.9.1, v4.0.0 đang soạn ở tsudev-standards
  (thay đề xuất v3.9.2); v4.0.0 phát hành giữa phiên ⇒ PRO2: sync + 7.5 + 7.6, 347 PASS; WINGET-2 vẫn chờ. Bàn giao: `logs/handover/20261120-06_khep-phien-tsuswico-96.md`.

- 20/11/2026 - **tsuswico-43:** kiểm WINGET-2 (chờ moderator), v3.9.2 chưa có ⇒ phiếu đề xuất câu chữ kênh web sang
  tsudev-standards; thêm cách làm P7 (proxy chết, không ngắt mạng) + P10 vào `docs/WINDOWS-VERIFICATION.md`.
  Bàn giao: `logs/handover/20261120-05_khep-phien-tsuswico-43.md`.

- 20/11/2026 - **tsuswico-93:** #51 PRO1 P3 + P5 (đăng nhập OIDC PKCE loopback, DPAPI, quyền lợi Pro, thông báo 7.3
  nối CLI, PRIVACY vi/en, THREAT_MODEL, test hạng C + IdP giả; 334 PASS). WINGET-2: nhãn báo động nhầm, ghi `docs/WINGET.md`.
  Bàn giao: `logs/handover/20261120-04_khep-phien-tsuswico-93.md`.

- 20/11/2026 - **tsuswico-8b:** #47 QU-STD-BRAND (icon exe/bộ cài một dây chuyền, wizard HiDPI, CI biên dịch Inno ở
  PR); phiếu SW1 ở repo tsudev (client OIDC native cho PRO1 P3); WINGET-2 vẫn chờ bot.
  Bàn giao: `logs/handover/20261120-03_khep-phien-tsuswico-8b.md`.

- 20/11/2026 - **tsuswico-22:** WINGET-2 nộp microsoft/winget-pkgs#484563; #43 sửa thư mục manifest winget; #44
  PRO1 P2 (`Core/Pro/`, 58 test); #45 3.5 `--log-file` JSON Lines (CI Windows kiểm nhật ký thật). 282 PASS.
  Bàn giao: `logs/handover/20261120-02_khep-phien-tsuswico-22.md`.
- 20/11/2026 - **tsuswico-68 (tiếp):** #39 PRO1 P1 (kiểm kê + ADR-0002 + v3.9.1); #40 khuôn V1 + **phát hành
  `v26.11.2001`** (ADR-0003, không cầu nối) + scoop-bucket#25 + website `3efeca7e`; #41 AN1 (lock NuGet, ghim SDK,
  cổng lỗ hổng NuGet). Bàn giao: `logs/handover/20261120-01_khep-phien-tsuswico-68.md`.
- 20/11/2026 - **tsuswico-68:** #37 gộp (`2bf3b49`) - **T3** song ngữ vi/en theo hệ thống (`--lang`, `TSUSWICO_LANG`),
  `--json-only`, căn cột tiến trình; website deploy `54039846`; README vi/en + CHANGELOG. Phiếu T3 toàn hệ ghi dòng.

- 16/11/2026 - **S008 (tiếp):** tắt JavaScript Detections của zone `tsudev.com` (xem Quyết định); 7 site trả 200,
  không còn `challenge-platform`; quét trình duyệt swico + files sạch console (trừ trang 404 cố ý). Đổi thư mục
  cục bộ thành `~/projects/tsuswico` (TEN-1).
- 16/11/2026 - **S008:** sản phẩm đổi tên **Tsu Swico**, repo `tsudev-org/tsuswico` (ADR-0001), Worker `tsuswico`;
  Scoop `tsuswico` (+ gói chuyển tiếp); Ủng hộ → `tsudev.com/ung-ho/`; khối Cài bằng lệnh sinh lúc build; ký
  SHA256SUMS Sigstore + provenance + Scorecard; README tiếng Anh; sửa trùng id, nút Chép, footer di động.
  PR #20, #23; scoop-bucket#9. 187 PASS, axe 0 lỗi. Chi tiết: `logs/handover/20261116-01_khep-phien-S008.md`.

- 10/11/2026 - **S007:** trang sản phẩm **https://swico.tsudev.com** (`website/`, Workers Static Assets);
  README nhận diện + hạng C (phiếu `20261109-01` đóng); updater trỏ `tsudev-org/tsudev-swico`;
  sửa báo cáo in nguyên văn `<b>`; 3 lỗi axe trong báo cáo; QU-STD-1/2/3, QU-6, QU-STD-AUTH.
  186 PASS. Chạy thật trên Windows 11 (UAC, `--silent`): mã thoát 10, 0 lỗi axe trên báo cáo thật.
  Chi tiết: `docs/STATE.md` mục S007 + phiếu `20261110-01`.

- 20/08/2026 - **3.5-1 XONG:** bộ test chuyển sang **xUnit**. 1313 dòng
  top-level statements tách thành 22 lớp test theo chủ đề. Chạy bằng
  `dotnet test tests/unittests` (`build.ps1` + hai workflow CI đã đổi theo);
  cổng `--validate-release-name` giữ nguyên qua `dotnet run`. **179 PASS,
  0 FAIL** - 179 ca xUnit bao trọn 298 phép kiểm cũ, đã đối chiếu từng nhãn.
  Một bảng kiểm biên điểm rủi ro trước nay nằm sau `break` nên chưa từng chạy,
  nay chạy thật. Chi tiết + ba điều không được dọn: `docs/STATE.md` mục 3.5-1.

- 20/08/2026 - **QU-4 XONG:** báo cáo HTML + `.xlsx` chạy hoàn toàn bằng
  `tokens/` (nhúng vào assembly, sinh biến CSS nội tuyến). Thêm chế độ tối theo
  hệ điều hành + ép bảng màu sáng khi in. +19 test (mục 19). 298 PASS, 0 FAIL.
  Ba thay đổi nhìn thấy được + phần còn nợ: `docs/STATE.md` mục QU-4.
- 20/08/2026 - **QU-5 XONG:** gom định dạng ngày giờ hiển thị về `DateDisplay`
  (`Core/Reports/DateDisplay.cs`) - trước đó báo cáo in **ba** dạng khác nhau và
  không test nào chặn. +12 test (mục 18). 276 PASS, 0 FAIL. Chi tiết + hai chỗ
  cố ý KHÔNG thống nhất: `docs/STATE.md` mục QU-5.
- 20/08/2026 - **3.0 XONG:** push 4 commit S004 lên `origin/main` (`ce24691`).
  CI run **32360493509 xanh cả hai job** - lần đầu luật đặt tên mới chạy trên
  runner GitHub. Bước `Kiem tra quy uoc dat ten` của `release.yml` vẫn chưa từng
  chạy (chỉ kích hoạt khi gắn tag).
- 20/08/2026 - Sửa `docs/STATE.md`: mục 3.0 và khối trạng thái đang nói S003
  **chưa push**, trong khi `git ls-remote` cho thấy đã push và CI xanh.
- 20/08/2026 - Xoá `tsudev-conventions.zip` sau khi đối chiếu đủ 11 file và
  dựng lại được nguyên vẹn từ repo. Cách dựng lại: `docs/CONVENTIONS-README.md`.
- 20/08/2026 - **QU-1 XONG:** đổi quy ước đặt tên phiên bản sang `YY.M.DDNN`
  (D-S004-1). Mã + 30 test mới + CI/CD + đóng gói + tài liệu. 264 PASS, 0 FAIL.
- 20/08/2026 - Áp dụng bộ quy ước `tsudev-conventions` v1.0.0 vào repo
  (`AGENTS.md`, `docs/DESIGN_SYSTEM.md`, `docs/PROJECT_STRUCTURE.md`,
  `docs/ARCHITECTURE.md`, `tokens/`, `logs/`, hợp nhất `.gitignore`).
- 19/08/2026 - Khởi tạo bộ quy ước v1.0.0

## Quyết định quan trọng

- 16/11/2026 - **Tắt JavaScript Detections (`enable_js=false`) cho zone `tsudev.com`** - chủ dự án giao agent tự chọn
  phương án an toàn cho cả hệ. Lý do: Bot Fight Mode đã tắt (`fight_mode=false`) và gói Free không dùng được trường
  `cf.bot_management.js_detection` trong luật WAF ⇒ script chèn ở tầng biên không cấp tín hiệu cho cơ chế chặn nào,
  chỉ thêm mã bên thứ ba vào mọi trang. Cách khác đều kém hơn: nới CSP (`'unsafe-inline'`) mở lỗ XSS; băm không phủ
  được (tham số đổi mỗi request); thêm Worker sinh nonce cho mọi site tĩnh tốn công và tốn lượt gọi. Turnstile của
  tsudev.com độc lập, không bị ảnh hưởng. Qua API `PUT /zones/{id}/bot_management`; ảnh chụp trước/sau chỉ khác đúng
  trường `enable_js`. Muốn bật lại Bot Fight Mode thì phải bật lại `enable_js` - khi đó lỗi console CSP quay lại.
- 20/11/2026 - **ADR-0003: khuôn phát hành V1 từ `v26.11.2001`, KHÔNG bản cầu nối** (chủ dự án chọn trong phiên).
  Bản 26.8.18/26.8.18.2 cập nhật một lần qua winget/Scoop/tải tay.
- 20/11/2026 - **ADR-0002: Pro = đồng bộ/sao lưu báo cáo qua TsuDEV Sync**; miễn phí giữ nguyên, không thêm kết nối mạng.
- 16/11/2026 - **ADR-0001: tên Tsu Swico / `tsuswico`** (chủ dự án). Giữ `swico.exe`, winget `tsudev.SWICO`, tên miền.
  Tên `tsudev-swico` (org) và `swico` (tsudev-tsudev) cấm tạo lại.

- 20/08/2026 - **D-S004-1: đổi quy ước đặt tên phiên bản** sang `DESIGN_SYSTEM.md`
  mục 6. Chủ project quyết sau khi được nêu rõ rủi ro: chạm vào đường cập nhật
  của hai bản đã phát hành. Bù trừ đã làm: parser nhận **cả hai** dạng tên.
- 20/08/2026 - **Không** ghi đè `README.md` sản phẩm bằng README của bộ quy ước;
  bản gốc giữ tại `docs/CONVENTIONS-README.md`.
- 20/08/2026 - `.gitignore` **hợp nhất** (chuẩn tối thiểu + phần riêng của repo),
  không thay thế: giữ `publish/`, `packaging/output/`, `*.zip` và đặc biệt
  `tsudev-bao-cao-ra-quet-*/` - báo cáo chứa dữ liệu máy thật (`PRIVACY.md`).
- 20/08/2026 - **Không** tái cấu trúc `src/` theo cây mẫu của
  `PROJECT_STRUCTURE.md`; lý do ở `docs/ARCHITECTURE.md` mục 3.
- 19/08/2026 - Dùng Inter làm font chuẩn; token là nguồn chân lý duy nhất;
  region ưu tiên Singapore → Nhật Bản.
