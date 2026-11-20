# PHIẾU BÀN GIAO - Khép phiên tsuswico-96

- **Mã phiếu**: 20261120-06
- **Thời điểm**: 20/11/2026
- **Trạng thái**: MỞ - phiên kế tiếp đọc, làm hàng đợi `logs/STATE.md`, rồi đổi thành ĐÓNG
- **Phiếu trước**: `20261120-05_khep-phien-tsuswico-43.md` - ĐÓNG

## 1. Đã làm (phiên chỉ kiểm, không đổi mã)

| Việc | Kết quả |
| --- | --- |
| (1) P7/P10 | Người dùng CHƯA gửi kết quả ⇒ bảng mục P giữ nguyên ⏭. Không sửa mã |
| (2) Bộ quy ước | Bản phát hành mới nhất vẫn **v3.9.1** (tag + Release `Latest`); repo này đang ghim v3.9.1 ⇒ không sync. Phiếu đề xuất v3.9.2 `tsudev-standards/logs/handover/20261120-02_*` vẫn **MỞ**, chưa ai xử lý. NHƯNG cây làm việc cục bộ `~/projects/tsudev-standards/tsudev-standards` đang ở nhánh `feat/v4.0.0-pro-ky-1-nam` có thay đổi đã stage (12:13, chưa commit, chưa PR): **v4.0.0** - Pro kỳ 1 năm KHÔNG tự gia hạn ở mọi kênh, viết lại câu 7.5 (vi/en), thêm mục 7.6 nhắc gia hạn 7 ngày cuối. Câu 7.5 mới đúng cho kênh web ⇒ **thay thế** đề xuất v3.9.2. Một phiên khác đang làm - KHÔNG đụng (CLAUDE.md) |
| (3) WINGET-2 | microsoft/winget-pkgs#484563 vẫn MỞ, nhãn `Azure-Pipeline-Passed` + `Validation-Executable-Error` + `New-Manifest`, bình luận cuối vẫn là của mình (04:36Z). Không làm gì thêm |


### 1b. Cập nhật cuối phiên (sau tin nhắn của phiên điều phối `projects-a6`)

- tsudev-standards **phát hành v4.0.0** lúc 05:19Z (sau lần kiểm đầu phiên). `projects-a6` để phiếu
  `logs/handover/20261120-PRO2_pro-ky-1-nam-nhac-gia-han-xoa-co-xac-minh.md` - đã commit cùng PR PRO2.
- PR PRO2: sync v4.0.0; `ProRenewal`/`ProRenewalPrompt` (7.6: nhắc ≤ 7 ngày mỗi lần chạy tương tác, báo hết kỳ một
  lần, khoá `tsu.pro_expired.notified_until` + `tsu.pro.seen_until`); câu 7.5 (vi/en) TRƯỚC liên kết mua trong
  `swico account` khi chưa Pro; 6 test bắt buộc + 7 test khác (một test so câu 7.5 với `.standards`); 347 PASS.
  ADR-0002 2.5/2.5a, PRIVACY vi/en, CHANGELOG, `docs/WINDOWS-VERIFICATION.md` P12/P13.
- ⇒ Mục 2 ý 1-3 bên dưới ĐÃ LÀM; mục 3 việc 2 XONG. Còn: P12/P13 trên Windows thật (P13 cần tài khoản Pro sắp hết
  hạn - hỏi chủ dự án), mục 2.2 phiếu PRO2 chờ PRO-D3.

## 2. Cạm bẫy / phát hiện

- ĐỪNG sync khi v4.0.0 chưa có tag + Release: nội dung đang stage có thể còn đổi. `scripts/sync-standards.sh` lấy theo tag.
- v4.0.0 là MAJOR và kéo theo việc mới cho swico (khi phát hành):
  1. In câu 7.5 mới (vi/en, `{giá}` = 490.000 ₫) TRƯỚC liên kết `https://tsudev.com/pro?app=tsuswico` trong
     `swico account` (chỉ khi chưa Pro); test so nguyên văn. Câu nhắc "đồng bộ lên đám mây ... 90 ngày" - swico chưa
     có đồng bộ đám mây: chuẩn chỉ cho bỏ câu cuối ở mục 7.6 (báo hết kỳ), mục 7.5 thì KHÔNG nói được bỏ ⇒ chép
     nguyên văn, có thắc mắc thì gửi phiếu, không tự cắt.
  2. Mục 7.6: nhắc gia hạn (0 < `pro_until` - bây giờ ≤ 7 ngày, mỗi lần chạy) + báo hết kỳ một lần (khoá
     `tsu.pro_expired.notified_until`), nội dung vi/en nguyên văn, **6 test bắt buộc**. Với CLI: "mỗi lần mở app" =
     mỗi lần chạy `swico.exe`; không hiện khi `--silent`/`--json-only` (đối chiếu cách ProNoticePrompt 7.3 đang làm).
     "Sản phẩm không có đồng bộ đám mây bỏ câu cuối" ⇒ bỏ câu `{ngày_xoá}`.
  3. Mã swico KHÔNG có gói `thang` (đã grep) ⇒ không có gì để bỏ.
- Phiếu 20261120-02 ở tsudev-standards: khi v4.0.0 ra, phiên bên đó nên đóng phiếu là "thay thế bởi v4.0.0 mục 7.5".
  Không phải việc của repo này.

## 3. Việc còn mở (thứ tự đề xuất)

1. 👤 P7 + P10 theo `docs/WINDOWS-VERIFICATION.md` mục P (đăng nhập lại trước). Ghi kết quả; lệch ⇒ sửa + test.
2. tsudev-standards phát hành v4.0.0 (hoặc v3.9.2) ⇒ `scripts/sync-standards.sh`, làm mục 2 ý 1-2 ở trên, cập nhật
   ADR-0002 mục 2.5 (bỏ "chờ"), PRIVACY/README nếu đổi hiển thị, `docs/THREAT_MODEL.md` nếu cần.
3. WINGET-2: chờ moderator; gộp ⇒ ✅. KHÔNG chạy lại `release.yml`, KHÔNG bình luận thêm.
4. Trước khi phát hành bản có P3: xong 1 (hoặc chủ dự án chấp nhận P7/P10 chỉ có test phủ), THREAT_MODEL mục 4 tích đủ,
   xong 2 hoặc quyết phát hành không có liên kết mua. KHÔNG phát hành khi mục P chưa đạt.
5. P4 ⛔ PRO-D (`TsuSync` .NET). QU-2 ca ngày một chữ số ở bản đầu tháng 12.

## 4. Câu lệnh khởi động phiên kế tiếp

```
Bạn là agent-tsuswico. Đọc ~/.claude/CLAUDE.md, AGENTS.md, logs/STATE.md và
logs/handover/20261120-06_khep-phien-tsuswico-96.md (mục 2-3). Khởi động: ListAgents, git pull.
Việc: (1) nếu tôi đã chạy P7/P10 (docs/WINDOWS-VERIFICATION.md mục P) thì ghi kết quả, lệch thì sửa + test;
(2) nếu tôi đã chạy P12/P13 thì ghi kết quả; P13 cần tài khoản Pro còn ≤ 7 ngày - hỏi tôi bỏ qua hay cấp;
phiếu logs/handover/20261120-PRO2_* mục 2.2 chỉ làm khi đã có thư viện .NET TsuSync (PRO-D3);
(3) kiểm microsoft/winget-pkgs#484563: gộp ⇒ WINGET-2 ✅, còn nhãn thì chờ (KHÔNG chạy lại release.yml, KHÔNG bình luận thêm).
KHÔNG phát hành bản mới khi mục P chưa đạt. Cuối phiên ghi bàn giao vào logs/handover/ và gợi ý prompt phiên kế.
```
