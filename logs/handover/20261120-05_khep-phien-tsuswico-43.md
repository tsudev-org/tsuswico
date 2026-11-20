# PHIẾU BÀN GIAO - Khép phiên tsuswico-43

- **Mã phiếu**: 20261120-05
- **Thời điểm**: 20/11/2026
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG
- **Phiếu trước**: `20261120-04_khep-phien-tsuswico-93.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| (1) Mục P còn P7/P10 | Không chạy được trong phiên (cần người bấm UAC + đăng nhập lại; P10 cần tài khoản Windows thứ hai). Thêm **cách làm** vào `docs/WINDOWS-VERIFICATION.md`: P7 giả ngoại tuyến bằng `HTTPS_PROXY=http://127.0.0.1:9` chỉ trong một cửa sổ PowerShell quản trị (`Program.Pro.cs` dùng `HttpClientHandler` mặc định ⇒ đọc proxy từ biến môi trường), KHÔNG cần ngắt mạng máy; P10 tạo tài khoản tạm, chép thư mục `%LOCALAPPDATA%\tsudev\tsuswico\` |
| (2) WINGET-2 | microsoft/winget-pkgs#484563 vẫn MỞ, nhãn `Azure-Pipeline-Passed` + `Validation-Executable-Error` + `New-Manifest`, bình luận cuối là của mình (04:36Z 20/11). Không làm gì thêm, KHÔNG chạy lại `release.yml`, KHÔNG đăng lại |
| (3) v3.9.2 | tsudev-standards vẫn **v3.9.1** (tag, release, `main` `6a8436a`), không có PR/phiếu nào về câu chữ kênh web. tsudev.com đã có câu riêng chạy production (`apps/frontend-main/lib/pay.ts` `webTerms`, commit `b892031`) kèm ghi chú "đề xuất đưa vào v3.9.2" nhưng chưa ai gửi đề xuất ⇒ viết phiếu `~/projects/tsudev-standards/tsudev-standards/logs/handover/20261120-02_de-xuat-v3.9.2-cau-chu-thue-bao-kenh-web.md` (câu vi chép nguyên văn `webTerms`, câu en đề xuất). Không sync, không sửa `swico account` |

## 2. Cạm bẫy / phát hiện

- Câu 7.5 hiện hành ("tự gia hạn cho tới khi bạn huỷ") SAI với kênh web (VietQR trả trước một kỳ). Đừng chép nó vào
  `swico account` để "cho xong" - chờ câu kênh web chuẩn.
- Không có phiên Claude nào đang mở cho tsudev-standards (ListAgents 20/11) ⇒ phiếu nằm chờ trong `logs/handover/` của
  repo đó (chưa commit, như các phiếu đến khác ở đó).

## 3. Việc còn mở (thứ tự đề xuất)

1. 👤 P7 + P10 theo cách làm ở `docs/WINDOWS-VERIFICATION.md` mục P (cần đăng nhập lại trước - P11 đã đăng xuất).
   Ghi kết quả vào bảng; lệch ⇒ sửa + test hồi quy.
2. tsudev-standards phát hành v3.9.2 ⇒ `scripts/sync-standards.sh`, in câu kênh web (vi/en theo `--lang`) TRƯỚC liên
   kết `https://tsudev.com/pro?app=tsuswico` trong `swico account` (chỉ khi chưa Pro), test + PRIVACY/README nếu đổi
   hiển thị. Bỏ ghi chú "chờ" ở ADR-0002 mục 2.5.
3. WINGET-2: chờ moderator; gộp ⇒ ✅.
4. Trước khi phát hành bản có P3: xong 1 (hoặc chủ dự án chấp nhận P7/P10 chỉ có test phủ), THREAT_MODEL mục 4 tích đủ,
   xong 2 hoặc quyết phát hành không có liên kết mua. KHÔNG phát hành khi mục P chưa đạt.
5. P4 ⛔ PRO-D (`TsuSync` .NET). QU-2 ca ngày một chữ số ở bản đầu tháng 12.

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-05_khep-phien-tsuswico-43.md (mục 2-3). Khởi động: ListAgents, git pull.
Việc: (1) nếu tôi đã chạy P7/P10 (docs/WINDOWS-VERIFICATION.md mục P) thì ghi kết quả, lệch thì sửa + test; (2) kiểm
tsudev-standards có v3.9.2 chưa - có thì sync và in câu kênh web trước liên kết mua trong swico account (vi/en);
chưa có thì xem phiếu tsudev-standards/logs/handover/20261120-02 đã được xử lý chưa, KHÔNG tự sửa repo đó; (3) kiểm
microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn nhãn thì chờ (KHÔNG chạy lại release.yml, KHÔNG bình luận thêm).
KHÔNG phát hành bản mới khi mục P chưa đạt. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
