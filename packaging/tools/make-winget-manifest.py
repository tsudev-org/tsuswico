#!/usr/bin/env python3
"""
Sinh manifest winget tu template, dien phien ban va SHA256 THAT cua file setup.

    python3 packaging/tools/make-winget-manifest.py \
        --version 26.8.1901 \
        --installer packaging/output/tsuswico_26.8.1901_windows-x64-setup.exe \
        --out packaging/output/winget

Vi sao KHONG cam ket san manifest vao repo: manifest bat buoc phai chua SHA256
cua chinh file setup, ma gia tri do chi biet SAU khi dong goi. Mot manifest cam
ket san se luon mang hash cu hoac hash gia - va do la mot cai bay: no trong nhu
da san sang nop, nhung nop len se bi tu choi ngay o khau kiem tra.
"""
import argparse, hashlib, datetime, pathlib, shutil, sys

TEMPLATE_DIR = pathlib.Path("packaging/winget/template")


def manifest_subdir(template_dir: pathlib.Path) -> pathlib.Path:
    """`tsudev.SWICO` => `t/tsudev/SWICO` (quy tac cua microsoft/winget-pkgs)."""
    ids = set()
    for tpl in template_dir.glob("*.yaml"):
        for line in tpl.read_text(encoding="utf-8").splitlines():
            if line.startswith("PackageIdentifier:"):
                ids.add(line.split(":", 1)[1].strip())
    if len(ids) != 1:
        raise SystemExit(f"LOI: template phai co dung MOT PackageIdentifier, thay {sorted(ids)}")
    parts = ids.pop().split(".")
    return pathlib.Path(parts[0][0].lower(), *parts)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--version", required=True)
    ap.add_argument("--installer", required=True)
    ap.add_argument("--out", default="packaging/output/winget")
    ap.add_argument("--date", default=None, help="mac dinh: hom nay (UTC)")
    a = ap.parse_args()

    installer = pathlib.Path(a.installer)
    if not installer.is_file():
        print(f"LOI: khong tim thay file setup '{installer}'", file=sys.stderr)
        return 1

    sha = hashlib.sha256(installer.read_bytes()).hexdigest().upper()
    date = a.date or datetime.datetime.now(datetime.UTC).strftime("%Y-%m-%d")

    # Duong dan trong kho winget-pkgs duoc suy ra TU PackageIdentifier, khong
    # duoc dat tuy y: manifests/<chu-cai-dau>/<NhaPhatHanh>/<Goi>/<PhienBan>/
    # Doc PackageIdentifier tu template - KHONG viet cung ten san pham: ban
    # 26.11.2001 tung sinh "tsudev/Tsu Swico/" vi lan doi ten (ADR-0001) thay
    # nham chuoi o day, trong khi dung phai la "tsudev/SWICO/".
    out = pathlib.Path(a.out) / "manifests" / manifest_subdir(TEMPLATE_DIR) / a.version
    if out.exists():
        shutil.rmtree(out)
    out.mkdir(parents=True)

    for tpl in sorted(TEMPLATE_DIR.glob("*.yaml")):
        text = (tpl.read_text(encoding="utf-8")
                   .replace("{{VERSION}}", a.version)
                   .replace("{{SHA256}}", sha)
                   .replace("{{DATE}}", date))
        if "{{" in text:
            leftover = [l for l in text.splitlines() if "{{" in l]
            print(f"LOI: con cho trong chua duoc dien trong {tpl.name}:", file=sys.stderr)
            for l in leftover:
                print(f"  {l.strip()}", file=sys.stderr)
            return 1
        (out / tpl.name).write_text(text, encoding="utf-8")
        print(f"  {out / tpl.name}")

    print(f"\nPhien ban : {a.version}")
    print(f"SHA256    : {sha}")
    print(f"Ngay      : {date}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
