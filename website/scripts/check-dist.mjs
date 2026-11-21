// Kiểm tra bản build (ECOSYSTEM_IDENTITY mục 2.2 và 4): mọi trang có title đúng khuôn, og:site_name = TsuDEV, canonical
// không www, description 120-160 ký tự, không CSS nội tuyến (CSP), script duy nhất là /theme.js nạp chặn trong <head>
// trước stylesheet và được CSP cho phép, liên kết mở tab mới có rel đủ,
// chỉ dùng gạch ngang ngắn trong chữ hiển thị (AGENTS.md mục 7), liên kết nội bộ không gãy.
import { createHash } from 'node:crypto';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const dist = join(dirname(fileURLToPath(import.meta.url)), '../dist');
const loi = [];
const lienKet = [];
function duyet(thuMuc) {
  for (const e of readdirSync(thuMuc, { withFileTypes: true })) {
    const p = join(thuMuc, e.name);
    if (e.isDirectory()) duyet(p);
    // Báo cáo mẫu là tệp chương trình sinh ra (không phải trang của website): có CSS/JS nội tuyến theo thiết kế,
    // được nới CSP riêng trong public/_headers. Chỉ kiểm các trang của website.
    else if (e.isFile() && e.name.endsWith('.html') && !p.includes('/bao-cao-mau/xem/')) kiemTra(p, readFileSync(p, 'utf8'));
  }
}
function kiemTra(tep, html) {
  const ten = tep.slice(dist.length);
  const title = html.match(/<title>([^<]*)<\/title>/)?.[1] ?? '';
  if (!/^(.+ - )?Tsu Swico \| TsuDEV$/.test(title)) loi.push(`${ten}: title sai khuôn: ${title}`);
  if (!html.includes('<meta property="og:site_name" content="TsuDEV">')) loi.push(`${ten}: thiếu og:site_name TsuDEV`);
  if (!/<link rel="canonical" href="https:\/\/swico\.tsudev\.com\//.test(html)) loi.push(`${ten}: thiếu canonical`);
  const mota = html.match(/<meta name="description" content="([^"]*)"/)?.[1] ?? '';
  const dai = [...mota.replace(/&[a-z#0-9]+;/g, 'x')].length;
  if (dai < 120 || dai > 160) loi.push(`${ten}: description ${dai} ký tự (cần 120-160)`);
  // Script: đúng một thẻ <script src="/theme.js"></script> (không defer/async/module: phải chặn để không nháy màu),
  // nằm trong <head> và trước stylesheet đầu tiên. Script nội tuyến bị CSP chặn im lặng ở production.
  const theScript = [...html.matchAll(/<script(?![^>]*type="application\/ld\+json")[^>]*>/g)].map((m) => m[0]);
  // Ngoại lệ: /tai-ve/ và trang chủ (vùng thao tác `cong-cu`) thêm <script src="/chep.js" defer> cho nút Chép (tệp tĩnh cùng nguồn, không chặn).
  const phu = ['/tai-ve/index.html', '/index.html'].includes(ten) ? ['<script src="/chep.js" defer>'] : [];
  const chinh = theScript.filter((t) => !phu.includes(t));
  if (chinh.length !== 1 || chinh[0] !== '<script src="/theme.js">' || theScript.length > 1 + phu.length) loi.push(`${ten}: script phải đúng một <script src="/theme.js">: ${theScript.join(' ')}`);
  const viTriScript = html.indexOf('<script src="/theme.js">');
  const viTriCss = html.search(/<link[^>]*rel="stylesheet"/);
  if (viTriScript < 0 || viTriScript > html.indexOf('</head>') || (viTriCss >= 0 && viTriScript > viTriCss)) loi.push(`${ten}: theme.js phải nằm trong <head>, trước stylesheet`);
  // Bộ chọn giao diện (phiếu 20261110-05): đúng ba lựa chọn, radiogroup, nằm ở chân trang; mặc định Hệ thống (PRODUCT_POLICY 1.2).
  const luaChon = [...html.matchAll(/data-chon-giao-dien="([a-z]+)"/g)].map((m) => m[1]).join(',');
  if (luaChon !== 'light,dark,system') loi.push(`${ten}: bộ chọn giao diện cần đúng light,dark,system: ${luaChon}`);
  // Mỗi id chỉ một lần (neo #... và aria-labelledby phải trỏ đúng một phần tử) - phiếu 20261113-07.
  const ids = [...html.matchAll(/\sid="([^"]+)"/g)].map((m) => m[1]);
  const trung = [...new Set(ids.filter((x, i) => ids.indexOf(x) !== i))];
  if (trung.length) loi.push(`${ten}: trùng id ${trung.join(', ')}`);
  const viTriChon = html.indexOf('id="giao-dien"');
  if (viTriChon < html.indexOf('<footer') || !/id="giao-dien" role="radiogroup" aria-label="Giao diện"/.test(html)) loi.push(`${ten}: bộ chọn giao diện phải là radiogroup trong <footer>`);
  if (!/<html lang="vi" data-product="swico" data-theme="dark">/.test(html)) loi.push(`${ten}: phải có data-product="swico" (bản sắc v5.0.0) và mặc định data-theme="dark"`);
  if (/<style[\s>]/.test(html) || / style="/.test(html)) loi.push(`${ten}: có CSS nội tuyến (CSP chặn)`);
  for (const a of html.matchAll(/<a [^>]*target="_blank"[^>]*>/g)) {
    if (!a[0].includes('rel="noopener noreferrer"')) loi.push(`${ten}: thiếu rel noopener noreferrer: ${a[0]}`);
  }
  // Em-dash/en-dash trong phần chữ do trang này viết. Nhật ký thay đổi và giấy phép bên thứ ba sao nguyên văn
  // từ upstream (ngoại lệ đã ghi trong logs/STATE.md) nên được bỏ qua.
  if (!/^\/(nhat-ky|ban-quyen)\//.test(ten)) {
    const chu = html.replace(/<pre[\s\S]*?<\/pre>/g, '').replace(/<[^>]+>/g, ' ');
    if (/[\u2013\u2014]/.test(chu)) loi.push(`${ten}: có em-dash/en-dash trong chữ hiển thị`);
  }
  for (const m of html.matchAll(/(?:href|src)="(\/[^"#?]*)/g)) lienKet.push([ten, m[1]]);
}
duyet(dist);
// CSP của production (public/_headers -> dist/_headers): cho phép script cùng nguồn, không 'unsafe-inline'/'unsafe-eval'
const csp = readFileSync(join(dist, '_headers'), 'utf8').match(/Content-Security-Policy:\s*(.+)/)?.[1] ?? '';
const scriptSrc = csp.split(';').map((x) => x.trim()).find((x) => x.startsWith('script-src ')) ?? '';
if (!scriptSrc.split(/\s+/).includes("'self'")) loi.push(`_headers: CSP thiếu script-src 'self' (theme.js sẽ bị chặn): ${csp}`);
if (/'unsafe-(inline|eval)'/.test(csp)) loi.push(`_headers: CSP không được có 'unsafe-inline'/'unsafe-eval'`);
if (!existsSync(join(dist, 'theme.js'))) loi.push('thiếu dist/theme.js');
// Báo cáo mẫu: mọi script nội tuyến và trình xử lý on* phải có băm trong CSP riêng của /bao-cao-mau/xem/*, và CSP đó
// không được dùng 'unsafe-inline' cho script (phiếu 20261110-05). Sinh lại báo cáo mà đổi script thì phải cập nhật băm.
{
  const headers = readFileSync(join(dist, '_headers'), 'utf8');
  const khoi = headers.split(/\n(?=\/)/).find((k) => k.startsWith('/bao-cao-mau/xem/*')) ?? '';
  const cspBaoCao = khoi.match(/^\s*Content-Security-Policy:\s*(.+)$/m)?.[1] ?? '';
  const ssBaoCao = cspBaoCao.split(';').map((x) => x.trim()).find((x) => x.startsWith('script-src ')) ?? '';
  if (!ssBaoCao || ssBaoCao.includes("'unsafe-inline'")) loi.push(`_headers /bao-cao-mau/xem/*: script-src phải theo băm, không 'unsafe-inline': ${ssBaoCao}`);
  const bam = (s) => `'sha256-${createHash('sha256').update(s, 'utf8').digest('base64')}'`;
  const duyetBaoCao = (thuMuc) => {
    for (const e of readdirSync(thuMuc, { withFileTypes: true })) {
      const p = join(thuMuc, e.name);
      if (e.isDirectory()) { duyetBaoCao(p); continue; }
      if (!e.name.endsWith('.html')) continue;
      const html = readFileSync(p, 'utf8');
      const can = [...html.matchAll(/<script(?![^>]*\ssrc=)[^>]*>([\s\S]*?)<\/script>/g)].map((m) => m[1])
        .concat([...html.matchAll(/\son[a-z]+="([^"]*)"/g)].map((m) => m[1].replace(/&quot;/g, '"').replace(/&amp;/g, '&')));
      for (const s of can) if (!ssBaoCao.includes(bam(s))) loi.push(`${p.slice(dist.length)}: script nội tuyến chưa có băm ${bam(s)} trong CSP /bao-cao-mau/xem/*`);
    }
  };
  if (existsSync(join(dist, 'bao-cao-mau/xem'))) duyetBaoCao(join(dist, 'bao-cao-mau/xem'));
}
for (const [ten, duong] of lienKet) {
  const dich = join(dist, decodeURI(duong));
  if (!existsSync(dich) && !existsSync(join(dich, 'index.html'))) loi.push(`${ten}: liên kết nội bộ gãy: ${duong}`);
}
for (const l of loi) console.error('[check-dist] LỖI', l);
console.log(`[check-dist] ${loi.length} lỗi`);
process.exit(loi.length ? 1 : 0);
