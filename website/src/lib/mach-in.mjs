// Hoạ tiết chữ ký `mach-in` (đường mạch in) của Tsu Swico - tsudev-standards v5.0.0 DESIGN_SYSTEM mục 1.2.4.
// TĨNH (không chuyển động), dùng CHUNG cho nền hero trang chủ (SVG nội tuyến, tô bằng CSS `var(--accent)`) và
// og-image (scripts/brand-assets.mjs, tô bằng `colors.dark.accent` của .standards/tokens/products/swico.json).
// Đường mạch chỉ nằm ở hai dải mép trái/phải (giữa để trống cho chữ) và vẽ nét mảnh, mờ: độ phủ < 15%.
// Tất định (seed cố định): build lại ra đúng cùng hình, og-image qua được `brand-assets.mjs --check`.

/** Sinh số giả ngẫu nhiên tất định (mulberry32). */
function rng(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/**
 * Các đường mạch trong khung w x h: mỗi đường là polyline đi ngang từ mép vào trong, gập 45 độ, rồi đi ngang/dọc,
 * kết thúc bằng một chân hàn (pad). `dai` = tỉ lệ bề rộng của mỗi dải mép.
 * @returns {{ d: string, pads: [number, number][] }}
 */
export function machIn({ w, h, soDuong = 9, dai = 0.24, seed = 68 } = {}) {
  const r = rng(seed);
  const buoc = 12; // lưới 12px, mọi điểm rơi đúng lưới cho nét sắc
  const snap = (v) => Math.round(v / buoc) * buoc;
  const parts = [];
  const pads = [];
  for (const ben of [0, 1]) {
    const dau = ben === 0 ? 0 : w;
    const huong = ben === 0 ? 1 : -1;
    const rong = w * dai;
    for (let i = 0; i < soDuong; i++) {
      const y0 = snap(((i + 0.5) / soDuong) * h + (r() - 0.5) * (h / soDuong) * 0.6);
      const x1 = dau + huong * snap(rong * (0.15 + r() * 0.35));
      const gap = snap(24 + r() * 60) * (r() < 0.5 ? -1 : 1);
      const x2 = x1 + huong * Math.abs(gap);
      const y2 = Math.min(h - buoc, Math.max(buoc, y0 + gap));
      const x3 = dau + huong * snap(rong * (0.55 + r() * 0.45));
      const xEnd = huong > 0 ? Math.max(x3, x2 + buoc) : Math.min(x3, x2 - buoc);
      parts.push(`M${dau} ${y0}H${x1}L${x2} ${y2}H${xEnd}`);
      pads.push([xEnd, y2]);
      // Nhánh phụ đi dọc ở khoảng một nửa số đường
      if (r() < 0.5) {
        const yNhanh = Math.min(h - buoc, Math.max(buoc, y2 + snap((r() - 0.5) * 120)));
        if (yNhanh !== y2) {
          parts.push(`M${xEnd} ${y2}V${yNhanh}`);
          pads.push([xEnd, yNhanh]);
        }
      }
    }
  }
  return { d: parts.join(''), pads };
}

/**
 * Chuỗi các phần tử SVG (không có thẻ <svg> bọc ngoài). `mau` = null ⇒ không ghi màu (để CSS tô).
 */
export function machInPhanTu({ w, h, mau = null, doDay = 1.5, banKinh = 3.5, ...tuyChon } = {}) {
  const { d, pads } = machIn({ w, h, ...tuyChon });
  const net = mau ? ` stroke="${mau}"` : '';
  const to = mau ? ` fill="${mau}"` : '';
  return `<path d="${d}" fill="none"${net} stroke-width="${doDay}" stroke-linecap="square"/>`
    + pads.map(([x, y]) => `<circle cx="${x}" cy="${y}" r="${banKinh}"${to}/>`).join('');
}
