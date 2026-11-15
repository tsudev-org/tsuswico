// Nút "Chép" của khối Cài bằng lệnh (/tai-ve/). Tệp tĩnh cùng nguồn (CSP script-src 'self'). Không có JavaScript thì
// nút vẫn ẩn và khối lệnh vẫn chọn/chép tay được.
(function () {
  'use strict';
  if (!navigator.clipboard || !navigator.clipboard.writeText) return;
  var trangThai = document.getElementById('chep-trang-thai');
  document.querySelectorAll('[data-chep]').forEach(function (nut) {
    var nhan = nut.firstChild.nodeValue;
    nut.hidden = false;
    nut.addEventListener('click', function () {
      navigator.clipboard.writeText(nut.getAttribute('data-chep')).then(function () {
        nut.firstChild.nodeValue = 'Đã chép';
        if (trangThai) trangThai.textContent = 'Đã chép lệnh vào bộ nhớ tạm';
        setTimeout(function () { nut.firstChild.nodeValue = nhan; }, 2000);
      }, function () {
        if (trangThai) trangThai.textContent = 'Không chép được, hãy chọn lệnh và chép tay';
      });
    });
  });
})();
