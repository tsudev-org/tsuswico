# ADR-0001: Tên sản phẩm Tsu Swico, định danh `tsuswico`

- **Trạng thái**: Đã chấp nhận (16/11/2026, chủ dự án)
- **Tiền lệ**: `tsudev-org/tsufiles` ADR-0003 (TsuFiles), `tsudev-org/tsucontact` ADR-0001 (Tsu Contact)

## 1. Bối cảnh

Chủ dự án (16/11/2026) chốt tên sản phẩm mới **Tsu Swico** cho thống nhất với hệ sinh thái tsudev, đồng thời đổi tên
repo và thư mục `tsudev-swico` thành `tsuswico`. Các tên hiển thị cũ (`tsudev SWICO`, `SWICO`) phải biến mất khỏi sản
phẩm, trừ chỗ lịch sử hoặc tương thích bắt buộc giữ.

## 2. Quyết định

| Hạng mục | Trước | Sau |
| --- | --- | --- |
| Tên hiển thị (CLI, báo cáo, bộ cài, website, tài liệu) | `tsudev SWICO` | **Tsu Swico** |
| Repo GitHub | `tsudev-org/tsudev-swico` | `tsudev-org/tsuswico` (đổi 16/11/2026, cùng id `1338164150`) |
| Thư mục cục bộ | `~/projects/tsudev-swico` | `~/projects/tsuswico` (đã đổi 16/11/2026) |
| Tệp phát hành dạng mới | `tsudev-swico_<v>_x64-setup.exe` | `tsuswico_<v>_x64-setup.exe` (`ReleaseName.AppName`) |
| Chuỗi nhận dạng HTTP | `tsudev-SWICO/<v>` | `TsuSwico/<v>` |
| Worker Cloudflare | `tsudev-swico` | `tsuswico` (giữ tên miền `swico.tsudev.com`) |
| Gói Scoop | `tsudev-swico` | `tsuswico` mới; `tsudev-swico` thành gói chuyển tiếp (mẫu `tsudev-lotus`) |
| Ủng hộ | chưa có | liên kết "Ủng hộ tsudev" ở chân trang website + `.github/FUNDING.yml`, mở `https://tsudev.com/ung-ho/` (quyết định `20261112-02`) |

**Giữ nguyên có chủ đích:**

- Tệp chạy và lệnh `swico.exe` / `swico`: script RMM/GPO của người dùng gọi theo tên này; không chứa tên cũ cần bỏ.
- Mã gói winget `tsudev.SWICO`: đã phát hành (winget-pkgs PR #419878, 15/11/2026). Đổi mã là tạo gói mới, người đã cài
  không nhận bản cập nhật. Tên hiển thị trong manifest (`PackageName`) là Tsu Swico từ bản kế tiếp.
- Tiền tố tệp dạng CŨ `swico-setup-` / `swico-portable-` và tên phát hành cũ `tsudev-swico-v...`: trình cập nhật đọc
  được bản cũ nhờ chúng (`ReleaseName.Prefix` cố ý không ghép từ `AppName`).
- Namespace `Tsudev.Audit.*`, AppId Inno Setup (bộ cài mới tiếp quản bản đã cài), tên miền `swico.tsudev.com`,
  logo `logo-swico`, giấy phép và dòng bản quyền, lịch sử (`CHANGELOG.md` bản cũ, `docs/journal/`, `logs/`).

## 3. Hệ quả

- Bản 26.8.18 / 26.8.18.2 đã cài gọi `api.github.com/repos/tsudev-tsudev/swico/releases/latest`; GitHub chuyển hướng
  theo id repo nên vẫn tới `tsudev-org/tsuswico` (đo 16/11/2026: 301 -> `repositories/1338164150` -> 200; link tải
  Release cũ -> 200). Chưa bản nào phát hành theo dạng `tsudev-swico_`, nên đổi tiền tố không làm gãy đường cập nhật.
- **Cấm vĩnh viễn** tạo lại repo/fork/đổi tên thành `tsudev-swico` trong `tsudev-org` (và `swico` dưới
  `tsudev-tsudev`). Đã ghi vào `~/.claude/CLAUDE.md` của máy phát triển.
- Secret scanning, push protection, Dependabot giữ nguyên sau khi đổi tên (đối chiếu ảnh chụp trước/sau 16/11/2026).
