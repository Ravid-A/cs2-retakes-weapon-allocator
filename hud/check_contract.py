#!/usr/bin/env python3
"""
Assert the plugin and the layout still agree.

    python hud/check_contract.py

A Panorama id the plugin drives but the layout does not declare fails silently and completely: the
tile renders, the click goes nowhere, and nothing is logged on either side. Same for a dialog
variable, and same for an icon class no stylesheet defines. There is no compiler across that
boundary, so this is it - run it after editing either half.

Exits non-zero on a mismatch, so it can gate a build.
"""
from __future__ import annotations

import pathlib
import re
import sys
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parent
REPO = ROOT.parent

# Must match LoadoutPanel.Slots and LoadoutPanel.Prefixes.
SLOTS = 8
PREFIXES = ["pt", "pc", "st", "sc"]


def main() -> int:
    layout = ET.fromstring((ROOT / "panorama/layout/custom_game/alloc_menu.xml").read_text(encoding="utf-8"))
    ids = {n.get("id") for n in layout.iter() if n.get("id")}
    # `text` on a Label, `src` on an Image - both are strings the server writes.
    variables = {v for n in layout.iter() for attr in ("text", "src")
                 for v in re.findall(r"\{s:([^}]+)\}", n.get(attr) or "")}

    css = "".join(
        (ROOT / "panorama/styles/custom_game" / name).read_text(encoding="utf-8")
        for name in ("hudkit.css", "alloc_menu.css", "weapon_icons.css"))
    classes = set(re.findall(r"\.([A-Za-z][\w-]*)", css))

    tiles = {f"{p}{i}" for p in PREFIXES for i in range(1, SLOTS + 1)}

    # Every weapon a server developer could put in a slot, as LoadoutPanel.IconClass derives it.
    items: set[str] = set()
    for source in ("Modules/Weapons/Allocator.cs", "Modules/Config/RetakesAllocatorConfig.cs"):
        items |= set(re.findall(r'"(weapon_[a-z0-9_]+)"', (REPO / source).read_text(encoding="utf-8")))

    checks = [
        ("panel ids", tiles | {f"awp{i}" for i in (1, 2, 3)} | {"save", "exit", "AllocMenu"}, ids),
        ("dialog variables", tiles | {"title", "tag", "status", "awphint"}, variables),
        ("state classes", {"sel", "hidden", "show"}, classes),
        ("icon classes", {"wi-" + i[len("weapon_"):] for i in items}, classes),
    ]

    failed = False
    for label, required, present in checks:
        missing = sorted(required - present)
        if missing:
            failed = True
            print(f"MISSING {label}: {', '.join(missing)}")
        else:
            print(f"ok  {label} ({len(required)})")

    orphans = sorted(ids - checks[0][1])
    if orphans:
        print(f"note: layout ids the plugin never drives: {', '.join(orphans)}")

    if failed:
        print("\nlayout and plugin disagree - this would fail silently in game")
        return 1

    print("\nlayout and plugin agree")
    return 0


if __name__ == "__main__":
    sys.exit(main())
