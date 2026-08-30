#!/usr/bin/env python3
"""
Assemble every project's panorama files into one addon content directory.

    python3 collect-panorama.py --out "X:/.../content/csgo_addons/hud_test1"
    python3 collect-panorama.py --out . --dry-run

WHY THIS EXISTS. Each project owns its own layouts - Shared/workshop, Toasts/workshop,
RetakesAllocator/hud - so a repo that gets split out takes its panorama files with it and still
builds. The VPK needs them in one place, and assembling them is this script's only job.

hud_shared.css is deliberately duplicated across projects so each one stands alone. That only works
if the copies stay identical, so this compares them and REFUSES to assemble mismatched ones. Copying
blind would let whichever project happened to be last silently win.
"""
from __future__ import annotations

import argparse
import hashlib
import pathlib
import shutil
import sys

# Any workshop/panorama or hud/panorama tree at any depth. Walking rather than globbing fixed
# depths, because how deeply a project nests its examples is not this script's business.
OWNERS = ("workshop", "hud")

SKIP = ("skills", "reference", "/obj/", "/bin/", "/.build/", "previews")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def sources(root):
    found = []
    for path in sorted(root.rglob("panorama")):
        if not path.is_dir() or path.parent.name not in OWNERS:
            continue
        if any(s in str(path).lower() for s in SKIP):
            continue
        found.append(path)
    return found


SOURCES = {".xml", ".css", ".vtex", ".svg", ".png", ".jpg"}


def main():
    ap = argparse.ArgumentParser(description="Assemble panorama files from every project.")
    ap.add_argument("--out", required=True, help="addon content directory; panorama/ is written into it")
    ap.add_argument("--root", default=".", help="repo root to search from")
    ap.add_argument("--exclude", action="append", default=[],
                    help="skip any source path containing this substring; repeatable. For a project "
                         "that owns a same-named layout and needs its own addon.")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    root = pathlib.Path(args.root).resolve()
    out = pathlib.Path(args.out).resolve() / "panorama"

    found = [f for f in sources(root)
             if not any(x.lower() in str(f).lower() for x in args.exclude)]

    if not found:
        print(f"no panorama trees under {root}")
        return 2

    seen = {}
    plan = []
    conflicts = []

    for base in found:
        print(f"  from {base.relative_to(root)}")

        for src in sorted(base.rglob("*")):
            # .png/.jpg ride along as the input a .vtex names; they are not compiled on their own.
            if not src.is_file() or src.suffix not in SOURCES:
                continue

            key = str(src.relative_to(base))
            d = digest(src)

            if key in seen:
                previous, where = seen[key]

                # The same file in two projects is expected for hud_shared.css - as long as it is
                # actually the same file. Differing contents means someone edited one copy.
                if previous != d:
                    conflicts.append(f"{key}: {where.relative_to(root)} != {src.relative_to(root)}")

                continue

            seen[key] = (d, src)
            plan.append((src, out / key))

    if conflicts:
        print("\nCONFLICT - the same file differs between projects:")
        for c in conflicts:
            print(f"  {c}")
        print("\nReconcile them first; assembling would silently pick one.")
        return 1

    for src, dest in plan:
        print(f"    {src.relative_to(root)}")

        if not args.dry_run:
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dest)

    verb = "would be written" if args.dry_run else "written"
    print(f"\n{len(plan)} file(s) {verb} to {out}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
