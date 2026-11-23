# PHIẾU BÀN GIAO - AV1 toàn hệ + chặn ở Resend

- **Mã phiếu**: 20261123-01
- **Thời điểm**: 23/11/2026
- **Phiên**: tsuswico-6e (tiếp)
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG
- **Phiếu trước**: `20261122-05_phat-hanh-26.11.2201.md` - ĐÓNG

## 1. Đã làm

| Việc | Kết quả |
| --- | --- |
| Quét VT cả hệ (22/11) | 35 tệp Latest của 9 repo; 14 tệp chưa từng quét đã tải lên. Bị cờ: tsuswico, lotus-share, tsucwico (Google/Ikarus `Trojan.Win64.Spy`/Bkav/CrowdStrike/APEX/Trapmine), trafficmonitor (Malwarebytes), tsufiles (Microsoft ML). Tên miền sạch |
| Kênh báo nhầm | Nguồn docs.virustotal.com/docs/false-positive-contacts: APEX = SecureAge (biểu mẫu), DeepInstinct `vt-fps-requests@deepinstinct.com`, ... |
| Phiếu | Điều phối `~/projects/logs/handover/20261122-AV1_*` (thư soạn sẵn từng hãng, bảng theo dõi) + phiếu AV1 ở 28 repo (chưa commit ở repo khác); nhắn qulythuchi, tsudisk, ubnd |
| Gửi DeepInstinct (chủ dự án chọn Resend API) | Bản thư cuối kiểm chứng (SLSA 3/3, mô tả đúng). Resend `403 tsudev.com domain is not verified`; verify ⇒ `claimed by another team`. **CHƯA gửi thư nào** |
| Phiếu EM1 | `~/projects/tsudev/logs/handover/20261123-EM1_ra-soat-email-toan-he.md` - rà mọi `*@tsudev.com` + mục 2b KHẨN: khoá Resend trên Render thuộc team có tsudev.com failed ⇒ thư giao dịch production nhiều khả năng đang hỏng |
| Token winget | Chủ dự án đã thay; `~/.config/tsudev/winget.env` vẫn token cũ (👤 ghi token mới) - đã nhắn tsudisk-64 |

## 2. Cạm bẫy / phát hiện

- DMARC tsudev.com `p=reject`, SPF gốc chỉ Cloudflare ⇒ gửi `From: *@tsudev.com` chỉ qua Resend (DKIM `resend`); Gmail
  "Send mail as" qua máy chủ Gmail sẽ bị từ chối.
- Token `cloudflare.env` không có quyền Email Routing (không đọc được luật chuyển tiếp).
- Thư Bkav/Ikarus/CrowdStrike/Trapmine/Google chỉ có tệp Tsu Cwico ⇒ chờ agent tsucwico xác minh hành vi trước.

## 3. Việc còn mở

1. 👤/phiên tsudev: sửa Resend (EM1 mục 2b). Xong ⇒ gửi thư DeepInstinct (AV1 mục 3.1, From/Reply-To `security@tsudev.com`,
   khoá lấy từ env Render, không in), ghi ngày vào AV1 mục 5 + `docs/store/antivirus.md`.
2. 👤 Nộp SecureAge (APEX, 4 tệp lotus + swico), Microsoft WDSI (tsufiles), Malwarebytes (trafficmonitor).
3. WINGET-2: #486462 + #484563 (không chạy lại release.yml).
4. Đo lại VT bộ cài 26.11.2201 sau khi gửi 3-7 ngày.

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261123-01_av1-resend-chan.md (mục 2-3). Khởi động: ListAgents, git pull, akb start.
Việc: (1) kiểm phiếu tsudev EM1 mục 2b (Resend) - đã sửa thì gửi thư DeepInstinct ở ~/projects/logs/handover/20261122-AV1_*
mục 3.1 qua Resend từ security@tsudev.com và ghi sổ; chưa sửa thì không gửi đường khác; (2) kiểm microsoft/winget-pkgs#486462
và #484563 (KHÔNG chạy lại release.yml); (3) đo lại VirusTotal bộ cài 26.11.2201; (4) PR Dependabot mới nếu có (bẫy NU1004).
Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
