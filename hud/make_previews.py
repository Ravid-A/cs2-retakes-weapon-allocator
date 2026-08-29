#!/usr/bin/env python3
"""
Render alloc_menu.vxml into a single browser page, once per scenario.

The layout file stays the only source of truth: this fills the dialog variables and toggles the
classes exactly the way the plugin will (text into {s:ptN}, `sel` on the chosen row, `hidden` on
slots the server's configured list does not reach), then reuses the skill's own translate_css.

    python hud/make_previews.py

Writes hud/previews/alloc_menu.preview.html.
"""
from __future__ import annotations
import pathlib, sys, xml.etree.ElementTree as ET, html

ROOT = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT.parent / "cs2-panorama-hud" / "scripts"))
import preview as pv  # translate_css + render

LAYOUT = ROOT / "panorama" / "layout" / "custom_game" / "alloc_menu.vxml"
STYLES = ROOT / "panorama" / "styles" / "custom_game"

# ---- scenarios -------------------------------------------------------------
# Weapons are (item, display) exactly as Config.Weapons holds them: the item name drives the
# wi-<item> icon class, the display name is the label. picked is 1-based per row.
SCENARIOS = [
    ("Default config",
     "Config.Weapons straight out of the box - 2 / 3 / 2 / 3.",
     [("ak47", "AK-47"), ("sg556", "SG 553")],
     [("m4a1", "M4A4"), ("m4a1_silencer", "M4A1-S"), ("aug", "AUG")],
     [("glock", "Glock-18"), ("p250", "P250")],
     [("usp_silencer", "USP-S"), ("p250", "P250"), ("hkp2000", "P2000")],
     {"pt": 1, "pc": 2, "st": 1, "sc": 1}, 2),

    ("Server dev opened it up",
     "A wider config - 6 / 7 / 7 / 8. The 8th CT pistol is the last tile the layout has.",
     [("ak47", "AK-47"), ("sg556", "SG 553"), ("galilar", "Galil AR"), ("m249", "M249"),
      ("negev", "Negev"), ("g3sg1", "G3SG1")],
     [("m4a1", "M4A4"), ("m4a1_silencer", "M4A1-S"), ("aug", "AUG"), ("famas", "FAMAS"),
      ("scar20", "SCAR-20"), ("m249", "M249"), ("negev", "Negev")],
     [("glock", "Glock-18"), ("p250", "P250"), ("tec9", "Tec-9"), ("deagle", "Desert Eagle"),
      ("elite", "Dual Berettas"), ("cz75a", "CZ75-Auto"), ("revolver", "R8 Revolver")],
     [("usp_silencer", "USP-S"), ("hkp2000", "P2000"), ("p250", "P250"), ("fiveseven", "Five-SeveN"),
      ("cz75a", "CZ75-Auto"), ("deagle", "Desert Eagle"), ("elite", "Dual Berettas"),
      ("revolver", "R8 Revolver")],
     {"pt": 3, "pc": 4, "st": 5, "sc": 8}, 3),
]

AWP_HINT = "How often you take the AWP when you win the roll."


WASH_RE = __import__("re").compile(r"wash-color:\s*(#[0-9a-fA-F]{6,8})")

ICON_SRC = pathlib.Path(
    r"D:/SteamLibrary/steamapps/common/Counter-Strike Global Offensive/game/csgo")


def inline_icons(css_text):
    """
    Swap s2r:// icon paths for the real CS2 artwork, read straight out of the retail pak.

    The preview renderer stubs every s2r:// url with a flat gradient, which for a menu whose whole
    point is the icons means judging the thing without seeing it. The .vsvg_c wrapper carries the
    original SVG verbatim, so lifting it out costs one regex and makes the preview show exactly
    what the client will draw. No install, no icons, no crash - the stub is still there.
    """
    if not ICON_SRC.exists():
        return css_text
    import base64, struct, re as _re
    sys.path.insert(0, str(ROOT.parent / "cs2-panorama-hud" / "scripts"))
    dirv = ICON_SRC / "pak01_dir.vpk"
    raw = dirv.read_bytes()
    sig, ver, tsize = struct.unpack_from("<III", raw, 0)
    base = (12 if ver == 1 else 28) + tsize
    tree, off, end = {}, (12 if ver == 1 else 28), (12 if ver == 1 else 28) + tsize

    def cstr():
        nonlocal off
        e = raw.index(b"\x00", off); s = raw[off:e].decode("utf-8", "replace"); off = e + 1
        return s

    while off < end:
        ext = cstr()
        if not ext: break
        while True:
            path = cstr()
            if not path: break
            while True:
                name = cstr()
                if not name: break
                _crc, pre, arch, eoff, elen = struct.unpack_from("<IHHII", raw, off); off += 16
                off += 2 + pre
                if "/icons/" in path:
                    tree[path + "/" + name] = (arch, eoff, elen)

    def svg_for(path):
        hit = tree.get(path)
        if not hit: return None
        arch, eoff, elen = hit
        if arch == 0x7fff:
            blob = raw[base + eoff: base + eoff + elen]
        else:
            with open(ICON_SRC / f"pak01_{arch:03d}.vpk", "rb") as f:
                f.seek(eoff); blob = f.read(elen)
        m = _re.search(rb"<svg.*?</svg>", blob, _re.S)
        return m.group(0) if m else None

    def sub(m):
        svg = svg_for("panorama/images/icons/" + m.group(1))
        if not svg: return m.group(0)
        # Panorama tints an icon with wash-color and the translator drops it, which would render
        # three differently-meant AWP glyphs identically. The artwork is white, so painting the
        # declared wash-color into the fill reproduces the tint exactly. Falls back to the colour
        # .am-ico sets for the weapon tiles, which declare no wash-color of their own.
        block_end = css_text.find(chr(125), m.end())
        wash = WASH_RE.search(css_text, m.end(), block_end if block_end > 0 else len(css_text))
        tint = (wash.group(1) if wash else "#7b838d")[:7]
        svg = svg.replace(b'fill="#FFFFFF"', b'fill="' + tint.encode() + b'"')
        svg = svg.replace(b"fill:#FFFFFF", b"fill:" + tint.encode())
        return 'url("data:image/svg+xml;base64,' + base64.b64encode(svg).decode() + '")'

    return _re.sub(r'url\(\s*"s2r://panorama/images/icons/([a-z0-9_/-]+)\.vsvg"\s*\)',
                   sub, css_text)


def fill(tree, lists, picked, awp):
    """Do to the tree exactly what the plugin does to the live panel."""
    vals = {"title": "Retakes \u00b7 Loadout", "tag": "!guns",
            "status": "Unsaved changes", "awphint": AWP_HINT}
    for prefix, weapons in lists.items():
        for i, (_item, name) in enumerate(weapons, 1):
            vals[f"{prefix}{i}"] = name

    for node in tree.iter():
        text = (node.get("text") or "").strip()
        if text.startswith("{s:"):
            key = text[3:-1]
            node.set("text", vals.get(key, ""))            # unset var renders empty, as in game
        ident = node.get("id") or ""
        if ident[:2] in lists and ident[2:].isdigit():
            prefix, n = ident[:2], int(ident[2:])
            cls = node.get("class", "")
            if n > len(lists[prefix]):
                node.set("class", cls + " hidden")          # server collapses the unused tail
            else:
                item = lists[prefix][n - 1][0]
                cls += f" wi-{item}"                        # same class the plugin sets
                if picked.get(prefix) == n:
                    cls += " sel"
                node.set("class", cls)
        if ident.startswith("awp") and ident[3:] == str(awp):
            node.set("class", node.get("class", "") + " sel")
    return tree


CONTRACT = [
    ("pt1 - pt8", "{s:pt1}..{s:pt8}", "Config.Weapons.PrimaryT", "T primary tiles. Server writes the label, adds <b>wi-&lt;item&gt;</b> for the icon and <b>sel</b> on the pick, and <b>hidden</b> past the end of the configured list."),
    ("pc1 - pc8", "{s:pc1}..{s:pc8}", "Config.Weapons.PrimaryCt", "CT primary. Same four moves."),
    ("st1 - st8", "{s:st1}..{s:st8}", "Config.Weapons.PistolsT", "T secondary."),
    ("sc1 - sc8", "{s:sc1}..{s:sc8}", "Config.Weapons.PistolsCt", "CT secondary."),
    ("awp1 - awp3", "static text", "GiveAwp enum", "Never / Sometimes / Always. Labels and glyphs are baked in - the enum is not configurable, so nothing here is driven per player except <b>sel</b>."),
    ("save", "-", "WeaponStore", "Writes all five values in one round trip, then sets {s:status}."),
    ("exit", "-", "-", "Closes without saving. Also fires PanelAction.Close on a round restart."),
    ("-", "{s:title} {s:tag}", "Prefix config", "Card header. {s:status} is the footer line, {s:awphint} the note under the AWP column."),
]


def write_artifact(dest, css, cards):
    css, cards = chr(10).join(css), "".join(cards)
    rows = "".join(
        f'<tr><td class="pg-id">{i}</td><td class="pg-var">{v}</td>'
        f'<td class="pg-src">{s}</td><td>{d}</td></tr>' for i, v, s, d in CONTRACT)
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(f"""<title>alloc_menu</title>
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Barlow+Condensed:wght@400;500;600&family=JetBrains+Mono:wght@400;500&display=swap">
<style>
/* Deliberately single-theme. The subject is a dark in-game HUD panel; rendering it on a light
   ground would misrepresent the thing being reviewed. Every colour is painted explicitly. */
:root {{
  --ground:#0b0f14; --surface:#11161d; --hair:#ffffff14; --hair-2:#ffffff0a;
  --ink:#e7ebf0; --muted:#6b747e; --dim:#4d555e;
  --gold:#f0a531; --t:#e0a24a; --ct:#56a0dd; --go:#9ccf4f;
  --ui:'Barlow Condensed',system-ui,sans-serif; --mono:'JetBrains Mono',ui-monospace,monospace;
}}
*{{box-sizing:border-box;margin:0;padding:0;border:0 solid transparent}}
body{{background:var(--ground);color:var(--ink);font-family:var(--mono);
     font-size:13px;line-height:1.7;padding:44px 24px 80px}}
.pg{{max-width:980px;margin:0 auto;display:flex;flex-direction:column;gap:14px}}

.pg-eyebrow{{font-family:var(--mono);font-size:11px;letter-spacing:2.4px;text-transform:uppercase;color:var(--dim)}}
h1{{font-family:var(--ui);font-size:40px;font-weight:600;letter-spacing:1px;
   text-transform:uppercase;color:var(--gold);line-height:1;text-wrap:balance}}
.pg-lede{{color:var(--muted);max-width:66ch}}
.pg-lede b{{color:var(--ink);font-weight:500}}

h2{{font-family:var(--ui);font-size:21px;font-weight:600;letter-spacing:1.6px;
   text-transform:uppercase;color:var(--ink);margin-top:34px}}
.pg-sub{{color:var(--muted);max-width:70ch;margin-top:-8px}}

.stage{{background:var(--surface) url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='40' height='40'%3E%3Cpath d='M0 0h40v40H0z' fill='%2311161d'/%3E%3Cpath d='M0 0h20v20H0zM20 20h20v20H20z' fill='%23141a22'/%3E%3C/svg%3E");
        border:1px solid var(--hair);border-radius:3px;padding:38px 20px;
        display:flex;justify-content:center;overflow-x:auto}}
button{{background:none;font:inherit;color:inherit;text-align:inherit;cursor:default}}

table{{width:100%;border-collapse:collapse;font-size:12px}}
th{{font-family:var(--ui);font-size:12px;font-weight:600;letter-spacing:1.6px;text-transform:uppercase;
   color:var(--dim);text-align:left;padding:0 12px 7px 0;border-bottom:1px solid var(--hair)}}
td{{padding:9px 12px 9px 0;border-bottom:1px solid var(--hair-2);vertical-align:top;color:var(--muted)}}
td.pg-id{{color:var(--gold);white-space:nowrap;font-variant-numeric:tabular-nums}}
td.pg-var{{color:var(--ct);white-space:nowrap}}
td.pg-src{{color:var(--ink);white-space:nowrap}}
td b{{color:var(--t);font-weight:500}}

ul{{list-style:none;display:flex;flex-direction:column;gap:9px;color:var(--muted);max-width:74ch}}
li{{padding-left:20px;position:relative}}
li::marker{{content:none}}
li span.pg-b{{position:absolute;left:0;color:var(--gold)}}
li b{{color:var(--ink);font-weight:500}}

.pg-note{{color:var(--dim);font-size:11.5px;max-width:74ch;
        border-left:2px solid var(--hair);padding-left:14px;margin-top:18px}}
@media (prefers-reduced-motion:reduce){{*{{animation:none!important;transition:none!important}}}}

{css}
[data-id="AllocMenu"]{{opacity:1!important;transform:none!important;filter:none!important}}
/* Panorama spells the fit keyword `contains`, and tints icons with wash-color.
      tint is baked into the inlined SVG; the selected-state tint is only approximated. */
.am-ico{{background-size:contain !important}}
.am-tile.sel .am-ico{{filter:brightness(1.7)}}
.hidden{{display:none!important}}
</style>
<div class="pg">
  <div class="pg-eyebrow">RetakesAllocator &middot; Panorama HUD</div>
  <h1>alloc_menu</h1>
  <p class="pg-lede">One card replacing the five chained <b>CenterHtmlMenu</b> screens. Three
  horizontal bands &mdash; primary, secondary, AWP &mdash; with save in the footer. Primary and
  secondary each carry a <b>T</b> row and a <b>CT</b> row, because a retake swaps you between both
  and the store keeps a preference for each. Icons are the real CS2 equipment artwork, read out of
  the retail <b>pak01</b>.</p>

  {cards}

  <h2>What the plugin drives</h2>
  <p class="pg-sub">The server can do exactly two things to a Panorama layout: write a string into a
  dialog variable, and toggle a CSS class on a panel id. Everything below is one of those two.</p>
  <table><thead><tr><th>Panel id</th><th>Dialog var</th><th>Source</th><th>Behaviour</th></tr></thead>
  <tbody>{rows}</tbody></table>

  <h2>Decisions worth your call</h2>
  <ul>
    <li><span class="pg-b">&rsaquo;</span><b>Eight tiles per row, and that is a build-time number.</b>
    Panorama has no wrapping and the server cannot add panels, so a config with a ninth weapon
    silently loses it. <b>build_layout.py --tiles N</b> sets the pool and the card width together, so
    a server running three-weapon lists builds a card that fits three, not one with five tiles of
    dead space.</li>
    <li><span class="pg-b">&rsaquo;</span><b>Both teams at once.</b> The alternative is a T/CT toggle
    and half the card. Shown together, one open of the menu sets every preference you have.</li>
    <li><span class="pg-b">&rsaquo;</span><b>Nothing saves until SAVE.</b> Exit and a round restart
    both discard. The old menus wrote each pick as you made it.</li>
    <li><span class="pg-b">&rsaquo;</span><b>The AWP options carry their own glyphs.</b> The band
    header already says AWP, so the tile art is free to say how often instead: <b>cancel</b>,
    <b>random</b>, <b>check</b>, tinted red / amber / green. <b>random</b> is not a stand-in &mdash;
    <b>GiveAwp.Sometimes</b> is a roll. All three are stock CS2 UI icons, so nothing ships with the
    addon.</li>
    <li><span class="pg-b">&rsaquo;</span><b>Icons cost nothing to maintain.</b> The server cannot send
    an image path, so all 34 are baked into <b>weapon_icons.vcss</b> as classes. The class is the item
    name minus its <b>weapon_</b> prefix, so editing Config.Weapons never touches the stylesheet
    &mdash; only Valve shipping a new gun does.</li>
    <li><span class="pg-b">&rsaquo;</span><b>One thing to confirm in game.</b> Icon paths use
    <b>.vsvg</b>, which is what Valve&rsquo;s own compiled stylesheets contain. No stock stylesheet
    references the equipment icons from CSS at all &mdash; the buy menu sets them from JavaScript,
    which a custom_hud_layout cannot use. If a tile comes up blank, <b>.svg</b> is the one other form
    worth trying.</li>
    <li><span class="pg-b">&rsaquo;</span><b>M4A4&rsquo;s item name is weapon_m4a1.</b> Mapping icons
    surfaced it: the display dictionary in <b>Menu.cs</b> has a dead <b>m4a4</b> key and labels
    <b>m4a1</b> as &ldquo;M4A1&rdquo;. Allocator.PrimaryCt already gets it right. Worth a one-line
    fix while we are in there.</li>
  </ul>

  <p class="pg-note">Approximation, not a render. Flexbox stands in for <b>flow-children</b>, Barlow
  Condensed for Stratum2; hover states and the reveal animation are not shown. Icon tints are real
  &mdash; each <b>wash-color</b> is painted into the artwork here &mdash; but the brighter tint a
  selected tile gets is only suggested. Layout and stylesheet pass the CustomHud validator. Judge
  spacing and hierarchy here &mdash; judge anything else in game.</p>
</div>
""", encoding="utf-8")
    print(dest)


def main():
    css = []
    for name in ("hudkit.vcss", "alloc_menu.vcss", "weapon_icons.vcss"):
        css.append(f"/* ---- {name} ---- */\n" + pv.translate_css(inline_icons((STYLES / name).read_text(encoding="utf-8"))))

    cards = []
    for title, blurb, pt, pc, st, sc, picked, awp in SCENARIOS:
        tree = ET.fromstring(LAYOUT.read_text(encoding="utf-8"))
        fill(tree, {"pt": pt, "pc": pc, "st": st, "sc": sc}, picked, awp)
        out = []
        for child in tree:
            if child.tag != "styles":
                pv.render(child, out, 0, {})
        cards.append(f'<h2>{html.escape(title)}</h2><p>{html.escape(blurb)}</p>'
                     f'<div class="stage">{"".join(out)}</div>')

    if len(sys.argv) > 2 and sys.argv[1] == "--artifact":
        write_artifact(pathlib.Path(sys.argv[2]), css, cards)
        return

    dest = ROOT / "previews" / "alloc_menu.preview.html"
    dest.parent.mkdir(exist_ok=True)
    dest.write_text(f"""<!doctype html><meta charset="utf-8"><title>alloc_menu preview</title>
<link rel="preconnect" href="https://fonts.gstatic.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Barlow+Condensed:wght@400;500;600&display=swap">
<style>
  *{{box-sizing:border-box;margin:0;padding:0;border:0 solid transparent}}
  body{{background:#0e1218;color:#e7ebf0;font-family:'Barlow Condensed',system-ui,sans-serif;padding:32px}}
  h1{{font-size:22px;letter-spacing:2px;text-transform:uppercase;color:#f0a531;margin-bottom:4px}}
  h2{{font-size:15px;letter-spacing:1.6px;text-transform:uppercase;color:#e7ebf0;margin:34px 0 2px}}
  p{{font:12px/1.6 ui-monospace,monospace;color:#6c757f;margin-bottom:14px}}
  .stage{{background:#161b22 url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='40' height='40'%3E%3Cpath d='M0 0h40v40H0z' fill='%23161b22'/%3E%3Cpath d='M0 0h20v20H0zM20 20h20v20H20z' fill='%23181e26'/%3E%3C/svg%3E");
          padding:34px;display:flex;justify-content:center;border-radius:4px;overflow-x:auto}}
  button{{background:none;font:inherit;color:inherit;cursor:pointer;text-align:inherit}}
  .note{{font:11px/1.6 ui-monospace,monospace;color:#5d6672;margin-top:28px;max-width:70ch}}
{chr(10).join(css)}
  [data-id="AllocMenu"]{{opacity:1 !important;transform:none !important;filter:none !important}}
  /* Panorama spells the fit keyword `contains`, and tints icons with wash-color.
        tint is baked into the inlined SVG; the selected-state tint is only approximated. */
  .am-ico{{background-size:contain !important}}
  .am-tile.sel .am-ico{{filter:brightness(1.7)}}
  .hidden{{display:none !important}}
</style>
<h1>alloc_menu</h1>
<p>RetakesAllocator loadout picker &mdash; one card, three sections, save in the footer.</p>
{''.join(cards)}
<p class="note">Approximation. Flexbox stands in for flow-children, Barlow Condensed stands in for
Stratum2, hover and the reveal animation are not shown. Judge spacing and hierarchy here; judge
anything else in game.</p>
""", encoding="utf-8")
    print(dest)


if __name__ == "__main__":
    main()
