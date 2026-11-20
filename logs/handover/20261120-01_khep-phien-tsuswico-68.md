# PHIẾU BÀN GIAO - Khép phiên tsuswico-68

- **Mã phiếu**: 20261120-01
- **Thời điểm**: 20/11/2026
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG

## 1. Đã làm (đều đã gộp; đo trên production)

| Việc | Bằng chứng |
| --- | --- |
| (1) T3 song ngữ vi/en + `--json-only` + căn cột tiến trình | #37 gộp `2bf3b49` (10/10 check); website deploy `54039846`; README vi/en + CHANGELOG + STATE ở #38; dòng kết quả ở `~/projects/logs/handover/20261119-T3_*.md` mục 4 |
| (2) PRO1 bước P1 | #39: kiểm kê (KHÔNG có dữ liệu người dùng rời máy; kết nối duy nhất là kiểm tra bản mới), `docs/adr/0002-luu-cuc-bo-pro-dong-bo.md` (Pro = đồng bộ/sao lưu **báo cáo** qua TsuDEV Sync, E2EE), sync tsudev-standards **v3.9.1**; phiếu PRO1 mục 4 |
| (3) V1 / TEN-2 + phát hành | #40 khuôn `tsuswico_{v}_windows-x64-{setup.exe,portable.zip}`, tiêu đề `Tsu Swico {v}`, Inno `VersionInfoVersion={v}.0`; `docs/adr/0003-khuon-phat-hanh-v1-khong-cau-noi.md` (chủ dự án chọn KHÔNG cầu nối). Thẻ **`v26.11.2001`**, Release công bố là latest, 7 tệp; `sha256sum -c` đạt; `gh attestation verify` đạt. scoop-bucket#25 gộp (cài thử thật xanh). Website deploy `3efeca7e` - trang tải về hiện 26.11.2001 |
| (4) AN1 Scorecard | #41: `packages.lock.json` + `dotnet restore --locked-mode` (Pinned #17), SDK ghim 8.0.427 `rollForward: disable`, `PackageVersion` cố định 1.0.0; cổng `dotnet list package --vulnerable` (B2). #29 Vulnerabilities đã tự đóng. Dòng ở phiếu AN1 |

## 2. Cạm bẫy gặp trong phiên

- **`--` trong chú thích XML** của `Directory.Build.props` làm MSBuild không nạp tệp, nhưng `dotnet restore` vẫn in
  "Restored" mà không lỗi ⇒ lock file không sinh. Kiểm bằng `dotnet msbuild <csproj> -getProperty:...`.
- Lock file NuGet ghi phiên bản gói của ProjectReference (= `Version`) ⇒ đã cố định `PackageVersion`. Đổi gói NuGet ⇒
  `dotnet restore Tsudev.SystemAudit.sln --force-evaluate` rồi commit lock. Nâng SDK ⇒ sửa `global.json` + sinh lại lock
  (`Microsoft.NET.ILLink.Tasks` đi theo bản vá SDK).
- Bản 26.8.18/26.8.18.2 đã cài: không tự cập nhật lên 26.11.2001 (ADR-0003) - cần winget/Scoop/tải tay.
- Máy không có `cosign` ⇒ chưa tự kiểm chữ ký bundle Sigstore (bundle đã sinh trong Release).

## 3. Việc còn mở (thứ tự đề xuất)

1. **WINGET-2**: PR `microsoft/winget-pkgs` cho `tsudev.SWICO` 26.11.2001 (manifest trong Release) - hỏi chủ dự án trước
   vì là PR công khai ở repo bên thứ ba.
2. **PRO1 P2** (không bị chặn): `Entitlements` .NET (kiểm JWS ES256 bằng `ECDsa` + JWKS giả trong test; `iss`, `aud`,
   `exp`, `typ`, chỉ `ES256`), `ProNotice` + 5 test PRODUCT_POLICY 7.3 (không hiện ở `--silent`/`--json-only`),
   `InstallationId` 128 bit, chuỗi vi/en nguyên văn. P3 chờ `client_id` Tsu Swico ở tsudev.com; P4 chờ PRO-D.
3. QU-2 ca ngày một chữ số: tự kiểm ở bản phát hành đầu tháng 12 (`26.12.0101.0`).
4. 3.5 logging có cấu trúc; QU-STD-BRAND icon app/bộ cài.
5. Kiểm tsudev.com/apps hiện phiên bản 26.11.2001 (V1 mục 5 nghiệm thu) - catalog ở repo tsudev.

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-01_khep-phien-tsuswico-68.md (mục 2-3), docs/adr/0002-luu-cuc-bo-pro-dong-bo.md mục 2.3-2.4,
.standards/docs/PRODUCT_POLICY.md mục 7.3-7.4. Khởi động: ListAgents, git pull. Việc: (1) hỏi chủ dự án có nộp PR
winget-pkgs cho tsudev.SWICO 26.11.2001 không, có thì làm theo docs/WINGET.md; (2) PRO1 P2: Entitlements (.NET BCL,
JWKS giả trong test), ProNotice + 5 test bắt buộc, InstallationId, vi/en - một PR, CI xanh thì gộp; (3) nếu còn ngữ
cảnh: 3.5 logging có cấu trúc. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
