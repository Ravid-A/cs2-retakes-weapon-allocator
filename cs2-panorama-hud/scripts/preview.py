#!/usr/bin/env python3
"""
Render a Panorama layout to a browser-viewable HTML approximation.

    python3 tools/preview.py Workshop/content/panorama/layout/custom_game/admin_hud.xml

Writes <name>.preview.html next to the source.

WHY THIS EXISTS. The only way to see a layout today is: compile it, build a VPK, copy it to the
server, restart, join, run a command. That is minutes per iteration for what is usually a padding
value. This gets it to seconds, at the cost of fidelity.

WHAT IT IS NOT. Panorama is not a browser and this is not a renderer - it is a translation good
enough to judge spacing, hierarchy and colour. Anything it shows is a hypothesis, and the game is
the only authority. Notably it cannot resolve s2r:// images or the game's own fonts, and its flow
model is flexbox rather than Panorama's.
"""
from __future__ import annotations

import re, sys, html, pathlib, xml.etree.ElementTree as ET

TAGS = {"Panel": "div", "Label": "div", "Button": "button", "Image": "div"}

def translate_css(text: str) -> str:
    """Panorama CSS -> web CSS, well enough to look at."""
    def gradient(m):
        body = m.group(1)
        stops = re.findall(r'(?:from|to)\(\s*(#[0-9a-fA-F]+)\s*\)', body)
        mid   = re.findall(r'color-stop\(\s*[\d.]+\s*,\s*(#[0-9a-fA-F]+)\s*\)', body)
        cols  = ([stops[0]] + mid + stops[1:]) if len(stops) >= 2 else stops
        horiz = "100% 0%" in body or "100%  0%" in body
        return f"linear-gradient(to {'right' if horiz else 'bottom'}, {', '.join(cols) or '#444'})"

    t = text
    t = re.sub(r'gradient\(\s*(?:linear|radial)\s*,([^;]*?)\)\s*;', lambda m: gradient(m) + ";", t)
    # box-shadow / text-shadow put the colour first in Panorama
    t = re.sub(r'box-shadow:\s*(#[0-9a-fA-F]+)((?:\s+-?[\d.]+px){2,4})\s*;',
               lambda m: f"box-shadow:{m.group(2)} {m.group(1)};", t)
    t = t.replace("flow-children: down", "display:flex; flex-direction:column")
    t = t.replace("flow-children: right", "display:flex; flex-direction:row")
    t = re.sub(r'width:\s*fill-parent-flow\([^)]*\)', "flex:1 1 auto", t)
    t = re.sub(r'height:\s*fill-parent-flow\([^)]*\)', "flex:1 1 auto", t)
    t = t.replace("visibility: collapse", "display:none")
    t = re.sub(r'horizontal-align:\s*center', "margin-left:auto; margin-right:auto", t)
    t = re.sub(r'horizontal-align:\s*right',  "margin-left:auto", t)
    t = re.sub(r'horizontal-align:\s*left',   "margin-right:auto", t)
    t = re.sub(r'vertical-align:\s*center',   "align-self:center", t)
    t = re.sub(r'vertical-align:\s*(top|bottom)', r"align-self:flex-\1", t)
    t = re.sub(r'brightness:\s*([\d.]+)', r"filter:brightness(\1)", t)
    t = re.sub(r'wash-color:\s*[^;]+;', "", t)
    t = re.sub(r'background-image:\s*url\("s2r://[^"]*"\)\s*;',
               "background-image:linear-gradient(135deg,#ffffff22,#ffffff08);", t)
    t = re.sub(r'font-family:\s*Stratum2 Mono[^;]*;', "font-family:ui-monospace,monospace;", t)
    t = re.sub(r'font-family:\s*Stratum2[^;]*;', "font-family:'Barlow Condensed',system-ui,sans-serif;", t)
    t = re.sub(r'transition-property:', "transition-property:", t)
    t = re.sub(r'\btext-overflow:\s*ellipsis', "text-overflow:ellipsis; overflow:hidden; white-space:nowrap", t)
    return t

def render(node, out, populate, seen, parent=""):
    tag = TAGS.get(node.tag)
    if tag is None:
        return
    cls = node.get("class", "")
    ident = node.get("id", "")

    # Un-hide a sample of each repeated pool so the preview is not an empty shell.
    if "hidden" in cls.split() and populate:
        # Key the budget by parent as well as by shape, so every pool gets its own allowance
        # instead of the first one on the page consuming all of it.
        stem = parent + "/" + re.sub(r'\d+', "#", ident or cls)
        seen[stem] = seen.get(stem, 0) + 1
        if seen[stem] <= populate:
            cls = " ".join(c for c in cls.split() if c != "hidden")

    attrs = f' class="{html.escape(cls)}"' if cls else ""
    attrs += f' data-id="{html.escape(ident)}"' if ident else ""
    text = node.get("text", "")
    body = ""
    if text:
        m = re.fullmatch(r'\{s:([^}]+)\}', text.strip())
        body = f'<span class="pv-var">{html.escape(m.group(1))}</span>' if m else html.escape(text)

    out.append(f"<{tag}{attrs}>{body}")
    for child in node:
        # Raw id, not the stemmed shape: every pool instance gets its own allowance, so a layout with
        # ten identical rows shows ten populated rows instead of one and nine empty ones.
        render(child, out, populate, seen, ident or cls)
    out.append(f"</{tag}>")

def main():
    src = pathlib.Path(sys.argv[1])
    populate = int(sys.argv[2]) if len(sys.argv) > 2 else 3
    root = ET.fromstring(src.read_text())

    css = []
    for inc in root.iter("include"):
        name = (inc.get("src") or "").rsplit("/", 1)[-1].replace(".vcss_c", ".css")
        cand = src.parents[2] / "styles" / "custom_game" / name
        if cand.exists():
            css.append(f"/* ---- {name} ---- */\n" + translate_css(cand.read_text()))

    out = []
    for child in root:
        if child.tag != "styles":
            render(child, out, populate, {})

    # Beside the panorama/ tree, never inside it: anything left in there gets packed into the VPK
    # and shipped to every client. Previews are a dev artifact, not addon content.
    #
    # Located relative to the source rather than to a fixed repo path, so this works wherever the
    # script is bundled - the layout is always at panorama/layout/custom_game/<name>.xml.
    panorama = src.parents[2]
    out_dir  = panorama.parent / "previews"
    out_dir.mkdir(parents=True, exist_ok=True)
    dest = out_dir / (src.stem + ".preview.html")
    dest.write_text(f"""<!doctype html><meta charset="utf-8"><title>{src.stem} preview</title>
<style>
  *{{box-sizing:border-box;margin:0;padding:0;border:0 solid transparent}}
  body{{background:#12161c url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='40' height='40'%3E%3Cpath d='M0 0h40v40H0z' fill='%23161b22'/%3E%3Cpath d='M0 0h20v20H0zM20 20h20v20H20z' fill='%23181e26'/%3E%3C/svg%3E");
       min-height:100vh;display:flex;align-items:center;justify-content:center;
       font-family:'Barlow Condensed',system-ui,sans-serif;color:#fff}}
  button{{background:none;font:inherit;color:inherit;cursor:pointer;text-align:inherit}}
  .pv-var{{opacity:.45;font-style:italic}}
  .pv-note{{position:fixed;left:12px;bottom:10px;font:11px/1.5 ui-monospace,monospace;color:#5d6672;max-width:46ch}}
{chr(10).join(css)}
  /*
     Reveal override, last so it wins. A layout that animates its entry sits at opacity 0 until the
     server toggles a class, which means a naive preview renders a blank page. Anything carrying an
     id is forced visible here.
  */
  [data-id]{{opacity:1 !important;transform:none !important;filter:none !important}}
  /*
     Panorama has no display property, so its visibility:collapse competes with nothing. The web
     does, and flow-children translates to display:flex - same specificity, later rule wins, and a
     collapsed panel renders anyway. Force it.
  */
  .hidden{{display:none !important}}
</style>
<div class="show" style="display:contents">{''.join(out)}</div>
<div class="pv-note">{html.escape(src.name)} - approximation. Flexbox stands in for flow-children,
s2r:// images are placeholders, game fonts are substituted. Judge spacing and hierarchy here; judge
anything else in game.</div>
""")
    print(dest)

if __name__ == "__main__":
    main()
