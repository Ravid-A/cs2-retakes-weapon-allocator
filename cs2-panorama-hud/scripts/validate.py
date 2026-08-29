#!/usr/bin/env python3
"""
Check every layout and stylesheet before packaging.

    python3 tools/validate.py

Exits non-zero on any failure, so it can gate a build. Catches the two classes of mistake that are
silent in game: XML that does not parse (the layout simply never loads, and the reason is a
client-side line number you have to go and read), and CSS properties Panorama does not register
(dropped without a word, so the rule just does nothing).

It does not catch semantic problems - an id the plugin drives that the layout does not declare, a
class the stylesheet never defines. Those still need the preview or the game.
"""
from __future__ import annotations

import re, sys, pathlib, xml.etree.ElementTree as ET

def find_panorama(start):
    """
    Walk up looking for a panorama/ tree, checking a couple of levels down at each step.

    Hardcoding "Workshop/content/..." tied this to one repo, which is fine until the script is
    bundled into a skill and run somewhere else - where it would silently validate nothing and
    report success. Bounded to eight levels so a bad starting point fails fast instead of walking to
    the filesystem root.
    """
    for base in [start, *list(start.parents)[:8]]:
        direct = base / "panorama" / "layout" / "custom_game"

        if direct.is_dir():
            return base / "panorama"

        try:
            for nested in sorted(base.glob("*/panorama/layout/custom_game")):
                return nested.parents[2]

            for nested in sorted(base.glob("*/*/panorama/layout/custom_game")):
                return nested.parents[2]
        except OSError:
            continue

    return None


def find_reference(start: pathlib.Path) -> pathlib.Path | None:
    """The CSS vocabulary, wherever it was bundled."""
    names = ["references/panorama-css-reference.txt",
             "reference/panorama-css-reference.txt",
             "panorama-css-reference.txt",
             # Bundled inside a skill, which is where it lives once this is not a monorepo.
             "skills/cs2-panorama-hud/references/panorama-css-reference.txt",
             "cs2-panorama-hud/references/panorama-css-reference.txt"]

    for base in [pathlib.Path(__file__).resolve().parent,
                 pathlib.Path(__file__).resolve().parent.parent,
                 *([start, *start.parents])]:
        for name in names:
            candidate = base / name
            if candidate.is_file():
                return candidate

    return None


_root      = pathlib.Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else pathlib.Path.cwd()
_panorama  = find_panorama(_root)

if _panorama is None:
    print(f"no panorama/layout/custom_game found at or above {_root}")
    sys.exit(2)

LAYOUTS = _panorama / "layout" / "custom_game"
STYLES  = _panorama / "styles" / "custom_game"
REF     = find_reference(_root)

if REF is None:
    print("panorama-css-reference.txt not found - cannot check CSS property names")
    sys.exit(2)

ALLOWED_TAGS  = {"Panel", "Label", "Image", "Button", "root", "styles", "include"}
ALLOWED_ATTRS = {"id", "class", "hittest", "text", "src", "texturewidth", "textureheight"}

# Properties whose values are a fixed vocabulary, taken from the reference's own descriptions. These
# are the ones where a web-CSS reflex produces a value Panorama drops without a word.
KEYWORDS = {
    "background-size":  {"contain", "auto", "cover", "clip_then_cover"},
    "background-repeat": {"repeat", "space", "round", "no-repeat", "repeat-x", "repeat-y"},
    "overflow":         {"squish", "clip", "scroll", "noclip"},
    "visibility":       {"visible", "collapse"},
    "white-space":      {"normal", "nowrap"},
    "text-transform":   {"none", "uppercase", "lowercase"},
    "text-overflow":    {"clip", "ellipsis", "shrink", "noclip", "min"},
    "font-style":       {"normal", "italic"},
    "font-weight":      {"light", "thin", "normal", "medium", "bold", "black"},
    "flow-children":    {"down", "right", "up", "left", "none"},
    "text-decoration":  {"none", "underline", "line-through"},
}

def main() -> int:
    failures = []

    known = set(re.findall(r'(?m)^([a-z0-9-]+|-s2-mix-blend-mode)\n-{2,}$', REF.read_text()))

    for path in sorted(LAYOUTS.glob("*.xml")):
        try:
            tree = ET.parse(path)
        except ET.ParseError as e:
            failures.append(f"{path.name}: XML does not parse - {e}")
            continue

        for node in tree.iter():
            if node.tag not in ALLOWED_TAGS:
                failures.append(f"{path.name}: <{node.tag}> is not on the CustomHud whitelist")
            if node.tag in {"Panel", "Label", "Image", "Button"}:
                for attr in node.attrib:
                    if attr not in ALLOWED_ATTRS:
                        failures.append(f"{path.name}: {node.tag} attribute '{attr}' is not allowed "
                                        f"(hittestchildren and style are both rejected)")

        # The compiler refuses an id on the root panel; the loader assigns it.
        body = [c for c in tree.getroot() if c.tag != "styles"]
        if len(body) == 1 and body[0].get("id"):
            failures.append(f"{path.name}: root panel carries an id, which the compiler rejects")

        print(f"  {path.name:<24} ok")

    for path in sorted(STYLES.glob("*.css")):
        # Strip comments first. Prose wraps, and a line that happens to begin "cannot:" or "note:"
        # looks exactly like a declaration to a regex.
        text = re.sub(r'/\*.*?\*/', '', path.read_text(), flags=re.S)
        # Parse declarations out of rule bodies rather than matching line starts. A line-anchored
        # regex silently skips every single-line rule -- ".anchor-tl { horizontal-align: left; }"
        # was never checked at all.
        used = set()
        for body in re.findall(r'\{([^{}]*)\}', text):
            for decl in body.split(";"):
                if ":" in decl:
                    used.add(decl.split(":", 1)[0].strip())

        used.discard("")
        for prop in sorted(used - known):
            failures.append(f"{path.name}: '{prop}' is not a registered Panorama property")

        # Values matter as much as names, and are just as silent when wrong. background-size takes
        # NOT "contains": the CS2 client rejects that outright ("Invalid value for property
        # 'background-size': contains"), and Valve's own csgostyles.vcss uses "contain" -
        # the string "contains" appears in no stock stylesheet. The libpanorama doc string
        # says "contains", but the parser's accepted enum is what ships.
        # which draws the image at its original size and overflows the panel.
        for prop, allowed in KEYWORDS.items():
            for m in re.finditer(rf'(?:[{{;]|^)\s*{re.escape(prop)}\s*:([^;}}]+)', text, re.M):
                for word in re.findall(r'[a-z][a-z0-9-]*', m.group(1)):
                    if word not in allowed and not re.search(r'\d', m.group(1)):
                        failures.append(f"{path.name}: {prop}: '{word}' is not a valid value "
                                        f"(expected one of {', '.join(sorted(allowed))})")

        print(f"  {path.name:<24} ok ({len(used)} properties)")

    if failures:
        print("\nFAILED:")
        for f in failures:
            print(f"  - {f}")
        return 1

    print("\nall layouts and stylesheets valid")
    return 0

if __name__ == "__main__":
    sys.exit(main())
