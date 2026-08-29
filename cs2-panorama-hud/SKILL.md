---
name: cs2-panorama-hud
description: >
  Author CS2 Panorama HUD layouts and drive them from a CounterStrikeSharp plugin via PanoramaManager.
  Use for anything touching custom_hud_layout, .vxml/.vcss files, panorama/layout/custom_game,
  server-driven CS2 menus, weapon pickers, admin panels, HUD bars or toasts - and whenever writing
  Panorama CSS, because web CSS habits fail silently here.
---

# CS2 Panorama HUD menus

## Read this first: what the server can and cannot do

A CS2 client renders a Panorama layout **it already has on disk**. The server cannot create panels,
cannot send a colour, a width, a coordinate or an image path. It can do exactly two things:

1. **Write a string into a dialog variable** - fills a `text="{s:name}"` slot.
2. **Toggle a CSS class on a panel by id.**

Everything else is a consequence of those two. A progress bar is ten preset width classes. An accent
colour is a palette of classes. A hidden row is a class. If you catch yourself planning to "send the
colour from the plugin", stop - bake the options into the stylesheet and have the plugin name one.

**Layout changes are expensive, C# changes are free.** A layout ships to every client inside a VPK.
Be generous with pool sizes and palettes up front; running out of tiles later is a re-release.

## Panorama is not the web

These fail **silently**. Nothing is logged, the rule is simply dropped, and you are left staring at a
layout that looks wrong for no visible reason. This list is most of what goes wrong:

| Web reflex | Reality |
|---|---|
| `display: flex` / `grid` | No `display` property at all. Use `flow-children: down\|right` |
| `width: 100%` on a child of an unsized parent | Renders as **nothing** - parent sizes to child, child to parent, circle resolves to zero |
| `box-shadow: 0 4px 8px #000` | Colour comes **first**: `box-shadow: #000 0px 4px 8px 0px` |
| `background-size: contain` | It is `contains`. Wrong spelling falls back to `auto` = original size = overflow |
| `transition: color 0.2s ease` | Split form: `transition-property` / `-duration` / `-timing-function` |
| `display: none` | `visibility: collapse` - removes it from layout, leaves no gap |
| `rgb()` / `rgba()` / `hsl()` | `#rrggbb` / `#rrggbbaa` only |
| `calc()` / `var()` / custom properties | None exist |
| `@media` | Does not exist |
| `::before` / `::after` / `:not()` | Do not exist |
| `float`, `top/right/bottom/left`, `object-fit` | Do not exist. Use `x`/`y`/`z`, `align`, `ignore-parent-flow` |
| Flex wrapping | **No wrapping.** Five-per-line means five panels per line, structurally |
| `<Image src>` for a dynamic picture | `src` is static and the server cannot rewrite it. Use a `Panel` with `background-image` set by class |

`references/panorama-css-reference.txt` is the complete registered vocabulary, read out of
`libpanorama.so`. **Check it before using any property you have not used here before.**

`references/runtime-behaviour.md` covers what the vocabulary cannot tell you: which properties
survive a text update, why a same-tick class off-then-on sends nothing, and which registered
properties do not actually work. Read it before animating anything or building a bar.

`references/kit/` is 19 working layouts from the community - the best available evidence of what
passes the validator.

## The XML subset

`custom_hud_layout` runs a validator far narrower than Panorama's own. Only:

- **Tags:** `Panel`, `Label`, `Image`, `Button`
- **Attributes:** `id`, `class`, `hittest`, `text`, `src`, `texturewidth`, `textureheight`
- **No `<scripts>`.** No client-side JavaScript, ever. `hittestchildren` and `style` are rejected
  even though both are valid Panorama.
- **The root panel may not have an `id`** - the loader assigns it. Wrap: an anonymous full-screen
  Panel, with the real root one level in.
- **Author the root hidden.** The entity shows its layout to *every* player; per-player state only
  overlays. A root that is visible as authored renders an empty card to the whole server the moment
  the entity spawns.

The validator reports **one violation per load**, so expect to iterate.

## Workflow

```bash
python3 scripts/new_layout.py mymenu --rows 8 --out path/to/addon
python3 scripts/validate.py path/to/addon      # gate: XML, whitelist, CSS names AND values
python3 scripts/preview.py path/to/addon/panorama/layout/custom_game/mymenu.xml 5
```

`new_layout.py` writes both halves from one row count, which removes the class of bug where the
layout and the C# disagree about an id - a row that renders and does nothing, with no error
anywhere. It prints the matching `Spawn` call.

All three take paths and find the `panorama/` tree by searching upward, so they work in any project,
not only this one.

On Windows, `scripts/build-hud.ps1` compiles and packs without opening Workshop Tools -
`resourcecompiler.exe` and `vpk.exe` ship with the game and the GUI is a wrapper over them. `-Watch`
rebuilds on save.

Always validate and preview before compiling. A VPK round trip is minutes; these are seconds. The
preview is an approximation - flexbox stands in for `flow-children`, `s2r://` images are
placeholders, fonts are substituted. Judge spacing and hierarchy there, everything else in game.

## Driving it from C#

```csharp
Panorama.Init(this);                                   // once, in Load

var menu = Panorama.Spawn("panorama/layout/custom_game/mymenu.vxml_c",
                          new LayoutContract { RevealClass = "show" });

menu.Title = "Admin";
menu.SetItems(players.Select(p => new MenuItem(
    Id:       $"player:{p.Slot}",
    Title:    p.PlayerName,
    Subtitle: $"{p.Ping}ms",
    OnSelect: e => Kick(p))));                          // the action rides with the row

menu.SetVariant("accent", "red");                       // picks accent-red from the stylesheet
menu.Open(player);
```

- `SetItems` + `MenuItem.OnSelect` for a list. `SetVariableFor` / `SetClassFor` for anything
  per-viewer that is not a uniform list (a grid, an inventory).
- **`OnEvent` runs before any row's `OnSelect`, and can veto with `e.Cancel = true`.** That is what
  makes one authorisation check possible instead of one per row.
- **Authorise inside the handler.** Never assume the menu could only have been opened by someone
  allowed to use it.
- **To draw above the game's HUD, use `z-index: 99999` on the layout's outermost panel** - not HUD
  flags. That alone puts a menu above the crosshair.
- **`LayoutContract.HideHud`** hides parts of the HUD when a menu genuinely wants them gone - a
  cutscene without a radar, a full-screen overlay. Defaults to none. The flags live on the player's
  **pawn**, so a dead or spectating viewer has nothing to carry them and a respawn drops them, and
  they must be restored on close - which the library does.
- **Handle `PanelAction.Restored`.** A round restart destroys the layout entity; the library
  rebuilds it and restores rows, title and handle-level variables, but anything written with
  `SetVariableFor` / `SetClassFor` is yours to redraw - it never saw what those meant.
- **`OnEvent` fires `PanelAction.Close` for every close** - a click on the X, a round restart, a
  `Dispose`. Undo anything you set up on open there, not only where you handle the button.
- **Native calls are not thread-safe.** Anything touching the menu after an `await` must come back
  through `Server.NextFrame`.

### Text input

A layout cannot accept a keystroke - `TextEntry` is not on the tag whitelist and there is no
scripting - so chat is the only text channel a player has. `PromptText` borrows it:

```csharp
menu.PromptText(player, new TextPrompt
{
    Variable = "input_preview",                       // {s:input_preview} in the layout
    Hint     = "Type the reason in chat, or 'cancel'.",
    OnResult = r => { if (r.Submitted) Kick(target, r.Text); },
});
```

The message is swallowed rather than broadcast, the answer is echoed into the named variable so the
player sees what the server got, and the prompt ends on cancel, timeout, or the menu closing. Text
arrives from a client, so it is trimmed, stripped of control characters and truncated to
`MaxLength` before it reaches a Label - but validate it for your own purposes too.

## Diagnosing

Every failure this library has had looks identical from outside: a menu that renders but does
nothing. They are told apart by which native resolved.

```
css_panorama_diag
```

Prints the gamedata source, whether per-player text is available, whether the click channel
installed, and the live menus. The same information goes to the log at startup, but it scrolls away.

Reading the client console when a layout will not load - the two messages mean opposite things and
both end with the same useless summary line:

| Line above `did not pass CustomHud validation` | Meaning |
|---|---|
| `Failed to load layout '<path>'` | The resource was never obtained. Nothing was parsed, so this says **nothing** about your XML. Look at the mount, the search path, and whether the `.vxml_c` exists |
| `Layout contains disallowed attribute X for panel type 'Y'` | It loaded and parsed. This is a real content rejection. Fix the XML |

`Failed to create '<path>': client disallowing panorama layout file creation` above either of them is
the addon-layout gate, and means the delivery route itself was refused.

### After a CS2 update

Signatures are per-build. When they break, `css_panorama_diag` shows which, and the repair is a text
edit to `gamedata/panoramamanager.json` rather than a rebuild. To re-derive: test the known signatures
against the new binary, wildcard the immediate operands of whichever failed and re-match, then anchor
on a string the function references. Offsets that exist in the schema should be read by name instead
- those survive updates.

## Shipping

Compile in Workshop Tools; the plugin asks for the compiled path (`mymenu.vxml_c` - note the `_c`).
Signatures live in `gamedata/panoramamanager.json` so a CS2 update is a text edit rather than a rebuild.

Addon-supplied layouts are still refused by the retail client. Today a layout must reach clients
through a `gameinfo.gi` search path, which is a development harness and not a shipping method.
