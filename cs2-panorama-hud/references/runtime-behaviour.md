# Runtime behaviour of custom_hud_layout

Empirical notes from someone else's live testing (CS2 build 1.41.7.7, 2026-08-26/27), distilled.
Source: the TnmsGameHud implementation notes and Panorama CSS quick reference.

**These are field observations, not documentation.** The author was explicit about separating
measured from inferred and about having published one wrong conclusion from a single observation.
Everything below is marked accordingly. Verify before betting a design on it.

---

## The one that changes designs

**State lives on the panel instance, and the server can only toggle classes.** There is no "reset"
to send. Reuse a panel and the previous animation keeps playing from where it was.

### Same-tick off-then-on sends nothing (measured)

Class state travels as an **entity netvar diff**, not a message. Write the same field twice in one
tick and only the difference from the last value ships. `off` then `on` is a zero diff:

```
SetHasClass(p, "run", false)      diff = none        nothing reaches the client
SetHasClass(p, "run", true)                          the animation never restarts

SetHasClass(p, "run-a", false)    diff = run-b       always arrives
SetHasClass(p, "run-b", true)                        ping-pong between two names
```

**Ping-pong between two class names is the only reliable restart.** `off`, wait, `on` also works but
needed **0.3s or more** - 0.05s and 0.016s both failed, which a 15.6ms tick does not explain. The
author flagged the mechanism as unknown.

### Writing a dialog variable resets a `width` transition (measured)

Update any text in the same subtree as a panel with `transition-property: width` and the bar snaps
back to full and redraws. Ruled out: where the write goes, where the class is applied, and whether
the string length changes.

Inferred cause: text change -> label re-measure -> row re-layout -> `width: 100%` re-resolves ->
transition restarts. So it hits **properties whose resolved size depends on the parent**.

| Property | Survives a text update in the same subtree? |
|---|---|
| `clip` | yes (measured, and Valve's own docs say it does not affect layout) |
| `position` | yes (measured - surprising, so "layout properties are all unsafe" is wrong) |
| `width` | **no** (measured) |
| `opacity` / `wash-color` / `background-color` / `brightness` / `transform` | untested, presumed safe |
| `height` / `margin-*` / `padding` | untested, presumed unsafe |

**Build gauges and bars with `clip`, not `width`.** Valve's own description is the tell: *"This
clipping has no impact on layout, and is fast and supported for transitions/animations."* Treat that
sentence as the safety test for any property.

`@keyframes` cannot animate `width` at all (re-verified with ping-pong to rule out non-restart).

---

## Traps

**`background-blur` does not work.** Registered, with a doc string, and Valve's own stylesheets use
it zero times. Use `world-blur` to blur behind a panel. *Registered is not the same as working - check
whether stock vcss actually uses a property before trusting it.*

**`z-index` only orders siblings within one parent.** To sit above the built-in HUD it has to go on
the **outermost panel of the layout**, not on an inner one - and the value has to be large. The
pattern the CS2 modding community uses is `z-index: 99999` on the root wrapper, usually with
`overflow: noclip` so children can draw past it:

```css
.Root {
    width: 100%;
    height: 100%;
    flow-children: down;
    overflow: noclip;
    z-index: 99999;
}
```

Putting it on an inner panel does nothing regardless of the number.

**This is sufficient on its own.** With it in place a layout draws above the crosshair with no HUD
flags set - verified by deploying the stylesheet change alone against an unchanged plugin. If you
find yourself hiding the crosshair server-side to get a menu on top, the z-index is in the wrong
place or too small.

**String interning is capped at 1024 per entity**, separately for panel ids, class names and dialog
variable names. Values themselves do not count. No log was observed on overflow, so assume silent
failure and count your own if you generate names dynamically.

**`visibility: collapse` removes the panel from flow**, so toggling it re-lays-out the parent and
fires any `transition-property: position` nearby.

**Vertical `%` in `margin` resolves against the parent's *width*** (as on the web). Non-square
containers shift by a surprising amount.

**`position` is one property taking `x y z`**, so axes cannot be transitioned separately. Nest two
panels to split them. `position: 100%` puts the panel's *left edge* at the parent's right edge -
its own size is not subtracted.

---

## Worth using

**Stock stylesheets can be included.** `s2r://.../csgostyles.vcss_c` brings the Stratum font set,
`fontSize-*`, and `csgo-hud__color-0..12`. No need to invent a type or colour system.

**CSS can play sounds** - `sound:` on a selector, `sound-out:` when it is removed. No usermessage or
client script involved.

**State is three layers**: undefined -> global -> per-player. Only write per-player for the players
who differ; omit the value to clear the override and fall back.

**`background-image` takes `url()` and accepts `.webm`.** It is also the only way to change a picture
at runtime, since `Image`'s `src` is a static attribute a class cannot touch.

**Hot reload**: `.vxml` and `.vcss` reload on save; `z-index` changes do not, and scripts need a full
map reload. Land the plugin-side driver first, then iterate on CSS alone.

---

## Open questions the author left open

- Why `off` -> wait -> `on` needed 0.3s when a tick is 15.6ms.
- Whether `x` / `y` / `z` work as standalone properties with `%` and transitions.
- **Per-player state reaches spectators and GOTV.** If `m_bInputCaptureEnabled` propagates, a
  spectator's mouse gets taken. Design admin HUDs with that in mind.
- **Slot reuse on reconnect keeps the previous occupant's per-player state.** A leftover input
  capture drops a freshly connected player straight into cursor mode.
- Whether the entity survives a round restart or map change. If it does not, the intern tables go
  with it and any client-side diff cache has to be invalidated on a new entity handle.

Valve marks the whole API `@experimental`. Expect breaking changes.
