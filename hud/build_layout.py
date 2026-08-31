#!/usr/bin/env python3
"""
Generate alloc_menu.xml and weapon_icons.css from one slot count.

Both halves come from the same loop so the layout and the C# cannot disagree about an id - the
failure mode that produces a tile which renders and does nothing, with no error anywhere.

    python hud/build_layout.py
"""
from __future__ import annotations
import argparse
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent

# Tiles per row, and the card width that exactly fits them. Structural: Panorama cannot wrap and
# the server cannot add panels, so this is the ceiling on a configured weapon list. It is also a
# per-server number - a server whose lists are 3 long should build with --tiles 3 and get a card
# that fits, rather than one sized for eight with five tiles of dead space.
TILE, GAP, CHIP, PAD = 96, 4, 35, 32

# Icon file == item name minus the weapon_ prefix, for every weapon the allocator can hand out.
# Verified against panorama/images/icons/equipment/ in the retail pak01. Note M4A4 is `m4a1`
# and M4A1-S is `m4a1_silencer`; there is no `m4a4` icon because there is no such item.
WEAPONS = """deagle elite fiveseven glock tec9 hkp2000 p250 usp_silencer cz75a revolver
xm1014 mag7 sawedoff nova mac10 mp5sd p90 ump45 bizon mp7 mp9 ak47 aug famas galilar sg556
m4a1 m4a1_silencer m249 negev awp scar20 g3sg1 ssg08""".split()

_ap = argparse.ArgumentParser(description=__doc__)
_ap.add_argument("--tiles", type=int, default=8, help="tiles per row (default 8)")
N = _ap.parse_args().tiles

ROWS = [("Primary", "pt", "pc"), ("Secondary", "st", "sc")]
AWP_ROW_W = 3 * (132 + GAP) + 210      # three AWP tiles plus room for the hint
AWP = [("awp1", "Never", "never"), ("awp2", "Sometimes", "sometimes"), ("awp3", "Always", "always")]


def tiles(prefix):
    return "\n".join(
        f'\t\t\t\t\t<Button id="{prefix}{i}" class="am-tile">'
        f'<Panel class="am-ico" />'
        f'<Label class="am-tile-l" text="{{s:{prefix}{i}}}" /></Button>'
        for i in range(1, N + 1))


def band(title, tprefix, ctprefix):
    return f'''\t\t\t<Panel class="am-band">
\t\t\t\t<Label class="am-band-h" text="{title}" />
\t\t\t\t<Panel class="am-row">
\t\t\t\t\t<Label class="am-chip t" text="T" />
{tiles(tprefix)}
\t\t\t\t</Panel>
\t\t\t\t<Panel class="am-row">
\t\t\t\t\t<Label class="am-chip ct" text="CT" />
{tiles(ctprefix)}
\t\t\t\t</Panel>
\t\t\t</Panel>'''


awp_tiles = "\n".join(
    f'\t\t\t\t\t<Button id="{i}" class="am-tile am-awp {c}">'
    f'<Panel class="am-ico am-ico-awp" />'
    f'<Label class="am-awp-l" text="{t}" /></Button>' for i, t, c in AWP)

xml = f'''<!--
  alloc_menu - RetakesAllocator loadout picker. One card, three horizontal bands.

  Primary and Secondary each carry a T row and a CT row of {N} tiles; the plugin fills
  {{s:pt1}}..{{s:sc{N}}} from Config.Weapons, adds `hidden` past the configured list length and
  `sel` on the pick. The weapon icon rides on the SAME class the plugin already sets: putting
  `wi-ak47` on the tile picks the icon up through `.wi-ak47 .am-ico` in weapon_icons.css, so a
  tile needs one id, not two.

  {N} tiles is the ceiling for a configured list - Panorama has no wrapping and the server cannot
  create panels, so raising it is a VPK re-release rather than a plugin update.
  Clicks arrive as the button ids. `save` writes to the DB, `exit` closes without saving.

  The header logo is a plain panel: the addon ships the picture, so alloc_menu.css paints it
  as a background and nothing about it is configurable at runtime.
-->
<root>
\t<styles>
\t\t<include src="s2r://panorama/styles/custom_game/hudkit.vcss_c" />
\t\t<include src="s2r://panorama/styles/custom_game/alloc_menu.vcss_c" />
\t\t<include src="s2r://panorama/styles/custom_game/weapon_icons.vcss_c" />
\t</styles>
\t<Panel class="HudScreen">
\t<Panel id="AllocMenu" class="HudRoot kit-panel am-card">
\t\t<Panel class="kit-head">
\t\t\t<Panel class="am-logo" />
\t\t\t<Label class="kit-title" text="{{s:title}}" />
\t\t\t<Label class="kit-tag" text="{{s:tag}}" />
\t\t\t<Button id="exit" class="kit-exit"><Label class="kit-nav-x" text="\u2715" /></Button>
\t\t</Panel>

\t\t<Panel class="am-body">
{band(*ROWS[0])}
{band(*ROWS[1])}
\t\t\t<Panel class="am-band am-band-awp">
\t\t\t\t<Label class="am-band-h" text="AWP" />
\t\t\t\t<Panel class="am-row">
\t\t\t\t\t<Label class="am-chip any" text="\u2691" />
{awp_tiles}
\t\t\t\t\t<Label class="am-hint" text="{{s:awphint}}" />
\t\t\t\t</Panel>
\t\t\t</Panel>
\t\t</Panel>

\t\t<Panel class="am-foot">
\t\t\t<Label class="am-status" text="{{s:status}}" />
\t\t\t<Button id="save" class="am-save"><Label class="am-save-l" text="SAVE" /></Button>
\t\t</Panel>
\t</Panel>
\t</Panel>
</root>
'''

icons = "\n".join(
    f'.wi-{w} .am-ico {{ background-image: url("s2r://panorama/images/icons/equipment/{w}.vsvg" ); }}'
    for w in WEAPONS)

css = f'''/* ============================================================================
   weapon_icons.css - one class per weapon the allocator can hand out. GENERATED by
   hud/build_layout.py; edit that, not this.

   The server cannot send an image path, so every icon it might ever need has to be
   baked in as a class. The plugin puts `wi-<item>` on the tile button, where <item>
   is the Weapon.Item name minus its `weapon_` prefix - so this file needs no changes
   when a server developer edits Config.Weapons, only when Valve adds a gun.

   Paths use `.vsvg`, which is what Valve's own compiled stylesheets contain for an
   SVG (`s2r://panorama/images/icons/ui/player.vsvg` in avatar.vcss). If an icon comes
   up blank in game, `.svg` is the one other form worth trying.
   ============================================================================ */

{icons}
'''

card_w = max(CHIP + N * (TILE + GAP) + PAD, CHIP + AWP_ROW_W + PAD)
css = f'''{css}
/* Sized to fit {N} tiles. Regenerate with --tiles to match your own Config.Weapons. */
.am-card {{ width: {card_w}px; }}
'''

(ROOT / "panorama/layout/custom_game/alloc_menu.xml").write_text(xml, encoding="utf-8")
(ROOT / "panorama/styles/custom_game/weapon_icons.css").write_text(css, encoding="utf-8")
print(f"alloc_menu.xml: {N} tiles x 4 rows + 3 AWP, card {card_w}px")
print(f"weapon_icons.css: {len(WEAPONS)} weapon classes")
