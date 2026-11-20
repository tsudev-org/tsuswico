#!/usr/bin/env python3
"""
Sinh logo NHA LAM (tsudev) nhung vao bao cao HTML tu assets/tsudev-logo.png.

Icon ung dung / bo cai / favicon: website/scripts/brand-assets.mjs (khong phai script nay).

Chay tay khi logo goc thay doi:
    python3 packaging/tools/make-assets.py

Vi sao tu viet giai ma PNG thay vi dung Pillow: moi truong dung de dung du an
nay khong co thu vien anh, va them mot phu thuoc chi de doi kich thuoc mot file
la khong dang. Chi ho tro dung dinh dang cua file goc: PNG 8-bit RGBA, khong
xen ke (non-interlaced) - kiem tra va bao loi ro rang neu gap dinh dang khac.
"""
import struct, zlib, sys, os

def decode_png(path):
    d = open(path, "rb").read()
    if d[:8] != b"\x89PNG\r\n\x1a\n":
        raise SystemExit(f"{path}: khong phai file PNG")

    w = h = None
    idat = bytearray()
    i = 8
    while i < len(d):
        ln = struct.unpack(">I", d[i:i+4])[0]
        tag = d[i+4:i+8]
        body = d[i+8:i+8+ln]
        if tag == b"IHDR":
            w, h, bd, ct, comp, filt, il = struct.unpack(">IIBBBBB", body)
            if (bd, ct, il) != (8, 6, 0):
                raise SystemExit(f"{path}: chi ho tro PNG 8-bit RGBA khong xen ke "
                                 f"(gap bitdepth={bd} colortype={ct} interlace={il})")
        elif tag == b"IDAT":
            idat += body
        elif tag == b"IEND":
            break
        i += 12 + ln

    raw = zlib.decompress(bytes(idat))
    bpp, stride = 4, w * 4
    out = bytearray(w * h * 4)
    prev = bytearray(stride)
    pos = 0
    for y in range(h):
        ft = raw[pos]; pos += 1
        line = bytearray(raw[pos:pos+stride]); pos += stride
        if ft == 1:
            for x in range(bpp, stride): line[x] = (line[x] + line[x-bpp]) & 0xFF
        elif ft == 2:
            for x in range(stride): line[x] = (line[x] + prev[x]) & 0xFF
        elif ft == 3:
            for x in range(stride):
                a = line[x-bpp] if x >= bpp else 0
                line[x] = (line[x] + ((a + prev[x]) >> 1)) & 0xFF
        elif ft == 4:
            for x in range(stride):
                a = line[x-bpp] if x >= bpp else 0
                b = prev[x]
                c = prev[x-bpp] if x >= bpp else 0
                p = a + b - c
                pa, pb, pc = abs(p-a), abs(p-b), abs(p-c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[x] = (line[x] + pr) & 0xFF
        elif ft != 0:
            raise SystemExit(f"{path}: bo loc hang khong ho tro: {ft}")
        out[y*stride:(y+1)*stride] = line
        prev = line
    return w, h, out


def resize(w, h, px, nw, nh):
    """Thu nho bang trung binh o (box filter), nhan trong so theo alpha.

    Nhan trong so alpha la can thiet: neu trung binh RGB ma bo qua alpha, cac
    diem trong suot (thuong la mau den) se keo mau vien anh toi di, tao vien
    xam quanh logo.
    """
    out = bytearray(nw * nh * 4)
    for y in range(nh):
        y0, y1 = y*h//nh, max(y*h//nh + 1, (y+1)*h//nh)
        for x in range(nw):
            x0, x1 = x*w//nw, max(x*w//nw + 1, (x+1)*w//nw)
            r = g = b = a = n = 0
            for sy in range(y0, y1):
                base = sy*w*4
                for sx in range(x0, x1):
                    o = base + sx*4
                    al = px[o+3]
                    r += px[o] * al; g += px[o+1] * al; b += px[o+2] * al
                    a += al; n += 1
            o = (y*nw + x) * 4
            if a:
                out[o], out[o+1], out[o+2], out[o+3] = r//a, g//a, b//a, a//n
            else:
                out[o+3] = 0
    return out


def encode_png(w, h, px):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        raw += px[y*w*4:(y+1)*w*4]

    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xffffffff))

    return (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
            + chunk(b"IEND", b""))


if __name__ == "__main__":
    src = "assets/tsudev-logo.png"
    if not os.path.exists(src):
        raise SystemExit(f"Khong tim thay {src}")

    w, h, px = decode_png(src)
    print(f"nguon: {w}x{h}")

    # Ban nhe de NHUNG vao bao cao HTML. Bao cao phai xem duoc khi khong co
    # mang va khi copy sang may khac ma khong keo theo file phu nao, nen logo
    # di kem duoi dang data URI - do do kich thuoc rat dang quan tam.
    for px_h in (72, 144):
        nw = max(1, round(w * px_h / h))
        data = encode_png(nw, px_h, resize(w, h, px, nw, px_h))
        out = f"assets/tsudev-logo-{px_h}.png"
        open(out, "wb").write(data)
        print(f"  {out}: {nw}x{px_h}, {len(data)} byte")

    # Icon ung dung, icon bo cai, anh trinh thuat si Inno va favicon KHONG con sinh o day: tat ca
    # di qua MOT day chuyen website/scripts/brand-assets.mjs tu assets/brand/source/ (CI --check).
    # Script nay chi con sinh logo NHA LAM tsudev-logo-{72,144} nhung vao bao cao HTML.
