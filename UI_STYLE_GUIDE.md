# The Broken Portal — UI Style Guide

Status: **Approved direction.**
- **UI-0** (font, theme, sprite copies) and **UI-B** (Magical Farm Harvest Console prototype) are implemented as of 26 Nov 2026; see section 10.
- All other panels are unchanged and wait for UI-C approval.

Direction: **Cozy Fantasy / Magical Spellbook.** The UI is warm parchment and wood surfaces with gold accents, dark ink-slate wells for code, and one accent colour per area. It is built only from assets already in the project.

---

## 1. Audit summary (current state)

All measurements come from `MagicWorld_Development` through Unity MCP, at a 1920×1080 Game view.

### 1.1 Fonts in use

| Font asset | TMP texts | Where |
| --- | --- | --- |
| Chomage SDF | 119 | Most panels, hints, toast, developer panel |
| Type Machine SDF | 43 | Ruin 1/2 guided simulations, book code lines |
| ELEGANT TYPEWRITER Regular SDF | 10 | Legacy quiz, harvest console input, jump guide, Ruin 2 locked message |
| LiberationSans SDF (TMP default) | 7 | Book code example, legacy quiz |
| Vanilla Caramel SDF | 1 | `InteractionHint` (unsaved Editor change, not in the saved scene) |
| Legacy `TextMesh` (not TMP) | 36 | Farm world labels: row and plot numbers, `HarvestDebugDisplay`, `RETURN TO RITUAL` |

Some single panels mix three fonts. For example, `ChallengePanel` uses Chomage, Type Machine, Elegant Typewriter and Liberation.

### 1.2 Panel styles in use

| Family | Panels | Look |
| --- | --- | --- |
| **Parchment book** (Bamao) | `AncientSpellbookPanel`, `Ruin02AncientSpellbookPanel`, legacy `ChallengePanel` | `Popup_paper_bg` 9-slice, wood arrow and cross buttons. This is already the target style. |
| **Dark navy simulator** | `Ruin01GuidedSimulationPanel`, `Ruin02GuidedSimulationPanel`, `RecursionPracticePanel` (bridge), `Ruin02PracticePanel`, `JumpGuidedSimulationPanel`, `Ruin01JumpPanel`, `RecursionVisualizationPanel` | Flat `#060B12` panels, flat `#2B475C` buttons, no frame |
| **Sky-tinted console** | `MagicHarvestConsolePanel` | Navy `#0E2233` over a translucent sky layer |
| **Developer** (Editor only) | `DeveloperPanel` | Purple `#090613` / `#4C3085` |
| **Floating text** | `InteractionHint`, `FragmentRewardToast`, `Ruin02ReturnLockedMessage` | Bare text, or a thin dark strip (`#0A1A29` / `#14081F`) |
| **Legacy hints** (no owner script) | `ReturnHint`, `Ruin02_EnterHint`, `Ruin02_ReturnHint`, `Ruin03_EnterHint`, `Ruin03_ReturnHint` | Superseded by the shared `InteractionHint`. Leave them inactive and don't restyle them. |

All buttons use the ColorTint transition with default tints. Most simulator text uses TMP auto-size in the 16–30 pt range.

### 1.3 Colours set in code (these override Inspector styling at runtime)

| Script | What it colours |
| --- | --- |
| `RecursionPanelView.cs` | Active code line `<color=#FFD166>`, stack frame rows (active `#265766` / idle `#1F2936`), slider fill/handle, runtime-created labels and buttons |
| `BridgePracticePanel.cs` | Code highlight, stack frames, correct `#A8F0C9` / incorrect `#FFA68C` feedback, selected / idle answer buttons |
| `Ruin02PracticePanel.cs` | Code highlight, stack frames, correct/incorrect feedback |
| `Ruin02StaffDiagramView.cs` | Staff indicators Air/Earth/Fire/Ice and their idle grey |
| `Ruin01JumpGuidedSimulation.cs` | Executing line `<color=#E5B8FF>`, landing previews (violet) |
| `BridgeDiagramView.cs` | Bridge stone states (serialized colours) |
| `BaseCaseChallenge.cs` | Legacy quiz feedback. Out of scope. |

### 1.4 UI created at runtime

- `RecursionPanelView.Create()` builds a panel at runtime. Its only runtime caller is the **disabled** legacy `RuinRecursionVisualizer`.
- Every live panel was generated **once by Editor installers** (`InstallWorldFirstBridge`, `Ruin02PhaseCTools`, `SpellbookStageTools`, ...) and saved into the scene.
- **Never re-run those installers after restyling.** They would recreate the old look, or duplicate panels.
- `Ruin01JumpTrail` and `Ruin02AirRestoration` create VFX objects only, no UI.

---

## 2. Font: Vanilla Caramel SDF

- Asset: `Assets/Font/Vanilla Caramel SDF.asset` (GUID `082dff01d7492d641b18b5c152ed52f1`) with material `Vanilla Caramel SDF Material`. Source: `Assets/Font/Vanilla Caramel.otf`.
- These files are **untracked in git**, so commit them together with the migration.
- Atlas: **Static**, 1024², 96 glyphs, sampling size 173, padding 6. It has no fallback list and no source font linked.

### 2.1 Character coverage

| Range | Atlas | `.otf` | Used by the game |
| --- | --- | --- | --- |
| Printable ASCII U+0020–U+007E, all C++ operators, brackets, `_`, digits | ✅ all | ✅ | Code, input, all text |
| `·` U+00B7 | ❌ | ✅ | Harvest console status, jump guide, Final Portal text |
| `—` U+2014 | ❌ | ✅ | Bridge practice, farm, Final Portal, console feedback |
| `–` U+2013 | ❌ | ✅ | Jump controller message |
| `…` U+2026 | ❌ | ❌ | Bridge panel, Ruin 2 "Generating hint…", "Tracing each call…" |
| `•` U+2022 | ❌ | ❌ | Ruin 1/Ruin 2 reward toasts |
| `→ ← ✓ ✗` | ❌ | ❌ | Not used. **Avoid them in new text.** |

**Silent substitution today.** Missing glyphs render from **LiberationSans**, the TMP default font, used as the last-resort lookup. There is no warning, and the substituted glyph doesn't match the rounded style. This was verified in Play Mode.

**Applied in UI-0:**

1. `–` U+2013 and `—` U+2014 were added to the static atlas from the bundled `.otf`, using TMP's own API: temporarily Dynamic, `TryAddCharacters`, back to Static, and the texture made non-readable again.
   - The GUID (`082dff01…`) is unchanged.
   - All 95 original glyph rects were verified unchanged.
   - The asset now has **98 characters**.
2. **`·` U+00B7 turned out to be unusable.** The `.otf` maps it to an empty outline (advance 69, size 0×0), so it would render as an invisible gap. It was removed again and is treated as unsupported.
3. Unsupported `…`, `•` and `·` in player-facing strings were replaced: `…` with `...`, and `•`/`·` with `|`. This changed string literals only, in `BridgePracticePanel`, `Ruin01FragmentReward`, `Ruin02FragmentReveal`, `Ruin01JumpGuidedSimulation`, `Ruin02FeedbackClient` and `Ruin02PracticeController`.
   - **`FinalPortalInteraction.cs` still contains `·`.** It was left alone because the Final Portal is out of scope; fix it in UI-C.
4. Dynamic AI hint text passes through `FypUiTheme.ForFont()` after trimming, so a reply can never trigger the silent LiberationSans substitution. Trimming happens first so that sentence counting is unchanged.
5. No other fallback font was added.

**Rule for all new text:** use only ASCII plus `–` and `—`. Use `|` as the separator. Don't use `· • … → ← ✓ ✗`.

### 2.2 Code readability (verified in Play Mode)

- Vanilla Caramel is **proportional** (`i` = 30 vs `m` = 122 advance units). Code still reads well because snippets only use leading indentation, never column alignment.
- `==`, `!=`, `&&`, `||`, `->`, `<=`, `_` and brackets all render clearly.
- Watch out for `l` / `1` / `|`. They are distinguishable at 20 pt or more; don't render code below 20 pt.
- The line height (ascent + descent ≈ 1.27 em) is taller than Chomage. Panels with auto-size **shrink** after the switch: the Ruin 2 code fell to its 18 pt minimum. Every migrated panel needs its sizes re-tuned (section 4), not just a font swap.

### 2.3 World-space text

- The 36 farm labels are legacy `TextMesh`, so they can't use a TMP font.
- Converting them to `TextMeshPro` (world) is a component swap.
- `HarvestDebugDisplay` is a typed `public TextMesh debugDisplay` field in `Ruin02FarmSimulation`, so converting it needs a small script change, `TextMesh` → `TMP_Text`, plus re-wiring.
- This is planned for UI-C batch 5, after approval.

---

## 3. Colour tokens

These are derived from your reference palette and adjusted to colours sampled from the Bamao sprites, so tinted assets and flat fills match. Name the tokens in code and docs; don't hand-type hex values.

### 3.1 Core

| Token | Hex | Use | Source |
| --- | --- | --- | --- |
| `Parchment` | `#F5E8CE` | Text-heavy panel fill, light text on dark | Your palette |
| `ParchmentDeep` | `#F2E1B0` | `Popup_paper_bg` body (sprite colour, untinted) | Sampled |
| `ParchmentEdge` | `#E8CA92` | Borders on parchment, dividers | Sampled |
| `Ink` | `#302820` | Primary text on parchment | Your palette |
| `InkSoft` | `#5C4B3B` | Secondary text, captions, placeholders (≥ 4.5:1 on Parchment) | Derived |
| `InkSlate` | `#2B2721` | Code well and call-stack background | Derived from Ink |
| `MagicGold` | `#C6A15B` | Titles on wood, focus rings, progress fill, highlights | Your palette |
| `GoldGlow` | `#FFD56A` | Active code line, selected state (`F_select frame` glow) | Sampled; replaces `#FFD166` |
| `Wood` | `#B06049` | Secondary panels, close buttons (`Popup_wood_bg`, `Button_*_B`) | Sampled |
| `WoodDark` | `#6E3A2C` | Pressed wood, frame inner | Sampled |
| `Moss` | `#8E8948` | Confirm/primary button (`button short green`) | Sampled |
| `ForestGreen` | `#456C58` | Success text/icons, Ruin 1 accent | Your palette |
| `Ember` | `#A4493A` | Errors, incorrect answers | Derived from `button short red #A25F46` |
| `SkyBlue` | `#8CCCE6` | AI hints, Farm accent, info | Your palette; matches the farm sky `#92DCFC–#B2ECFC` |
| `Disabled` | `#A89F90` @ 60% | Disabled buttons and text | Derived |

### 3.2 Area accents (same design language, different accent)

| Area | Accent | Used for |
| --- | --- | --- |
| Main Hub / Final Portal | `MagicGold #C6A15B` | Header ribbon tint, progress |
| Ruin 1 (bridge, jump) | `ForestGreen #456C58` | Header tint, active stack frame border, progress fill |
| Ruin 2 (staffs) | `Arcane #7A5AA6` (from the existing staff violet `#9E61F2`, toned down) | Header tint, active frame. The four staff colours stay as content colours. |
| Magical Farm | `SkyBlue #8CCCE6` | Header tint, active frame, harvested = `#8CCCE6`, empty = `#E0B066` |

Rule: accents tint **headers, borders, progress and active states only**. Body fills stay Parchment or Ink Slate everywhere.

### 3.3 Semantic states

| State | Text | Fill tint (on parchment) | Icon (Bamao) |
| --- | --- | --- | --- |
| Correct / success | `ForestGreen` | `#DCE8D2` | `icon_check` |
| Incorrect | `Ember` | `#F3D9CF` | `icon_cross` |
| AI hint | `Ink` | `#DCEFF6` (SkyBlue at 30%) with a SkyBlue left border | `icon_lightning` or `icon_question mark` |
| Pending ("Generating hint...") | `InkSoft` italic | none | none |
| Completed | `ForestGreen` bold | `#DCE8D2` and a gold border | `icon_medal` / `icon_star` |
| Locked / requirement | `Ink` | `#EFE3C8` | `icon_lock` |

Contrast targets: 4.5:1 for body text and 3:1 for 24 pt or larger. Ink on Parchment ≈ 12:1; Parchment on InkSlate ≈ 12:1; Ember on Parchment ≈ 5.6:1.

---

## 4. Typography (Vanilla Caramel SDF only)

Sizes are for the 1920×1080 reference (the Canvas Scaler is set to Scale With Screen Size at 1920×1080, match 0.5).

| Role | Size | Style | Colour | Auto-size |
| --- | --- | --- | --- | --- |
| H1 panel title | 44 | Bold | Ink (on ribbon) / Parchment (on wood) | off |
| H2 section heading | 30 | Bold | Ink | off |
| Body / instructions | 26 | Regular | Ink | 22–26 |
| Caption / labels / progress | 22 | Regular | InkSoft | 20–22 |
| Code | 24 | Regular | Parchment on InkSlate, active line GoldGlow bold | 20–24, **never below 20** |
| Call-stack frame | 22 | Regular | Parchment (active: Ink on GoldGlow) | 20–22 |
| Button label | 26 | Bold | Ink (parchment/moss buttons) / Parchment (wood buttons) | off |
| Interaction prompt | 30 | Bold | Parchment with a 0.2 Ink outline | off |
| Toast | 30 | Bold | Ink on parchment strip | 24–30 |

- Line spacing is −10 for body and code (it compensates for the taller line height). Paragraph spacing is 8.
- Use TMP Bold or Italic flags rather than other fonts.
- Use one material: `Vanilla Caramel SDF Material`. If outlines are needed, use a single shared material preset (`Vanilla Caramel SDF - Outline`). Don't create a material per object.

---

## 5. Spacing and layout

- 8-px grid. Panel padding is 32. The gap between sections is 24, between related items 12, and between buttons 16.
- Corner treatment comes from sprite borders. Don't use flat rectangles for main surfaces.
- Minimum touch/click target is 56 px high. Primary buttons are 64 px high.
- Side panels (practice, console) keep their existing anchors: right 35–47%. Centred modals (books, guides) keep 6–94%.
- Text never touches a frame edge: inner padding is at least 24 from the sprite border.
- Responsive checks are at 1920×1080, 1366×768 and 1024×768 (4:3). At 1024×768 the canvas scales to 0.83, so the smallest body text renders at about 18 px. That is the reason for the 20–22 pt minimums above.

---

## 6. Component specification

All sprites are under `Assets/Bamao/BamaoUIPack/Sprites/`. A border marked "needs border" means its sprite import settings need a 9-slice border before it can be used as a Sliced Image. That changes the import settings only; the texture is untouched.

| Component | Sprite / fill | Image type | Notes |
| --- | --- | --- | --- |
| **Main panel** | `Button/Popup_paper_bg` (border 238/172/144/210 ✔) | Sliced, PPU multiplier tuned so corners stay ~48 px | Untinted (ParchmentDeep) |
| **Secondary panel / card** | `Shop/lil board paper` or `Select Character/Paper_small` (needs border) | Sliced | Stack panels, info cards, guide side panels |
| **Wood panel (accent)** | `Button/Popup_wood_bg` (needs border) | Sliced | Toasts, completion banners, locked messages, Developer panel |
| **Header ribbon** | `Button/Popup_paper_title` / `Popup_wood_title` (needs border) | Sliced | Tinted with the area accent at 25% mix; title text centred |
| **Code display well** | Flat `InkSlate` `#2B2721` at 96% inside the parchment, plus an `Outline` effect in `ParchmentEdge` | Simple (`UISprite` rounded) | Line numbers in InkSoft-light `#A89F90`; active line GoldGlow |
| **Call-stack frames** | `Button/button short dark` (needs border) per row | Sliced | Idle = sprite colour; active = GoldGlow tint + Ink text; base case = Moss tint |
| **Primary button** | `Button/button short green` (Moss) | Sliced (needs border) | Label Parchment bold |
| **Secondary button** | `Shop/paper button` → `paper button PRESS` | **Sprite Swap** (pressed = PRESS) | Label Ink |
| **Close button** | `Character sheet/Button_cross_B` | Simple, 64×58 | Top-right, 16 px inset |
| **Nav arrows** | `Character sheet/Button_left_B` / `Button_right_B` | Simple | Books and Previous/Next |
| **Danger / reset** | `Button/button short red` | Sliced | Developer/reset only |
| **Input field** | `Button/typing button` (border ✔) | Sliced | Text Ink 24; placeholder InkSoft italic; caret Ink; selection GoldGlow 40% |
| **Progress / slider** | Track `Button/State_blank`, fill `Button/State_brown` tinted with the area accent, handle `GoldGlow` circle (`C_circle`) | Sliced | Checkpoint text "Checkpoint 12 / 28" Caption |
| **Playback controls** | Secondary buttons, order: Previous · Replay · Next, Finish = Primary | — | Finish is hidden until it's enabled (existing logic) |
| **Selection highlight** | `Equipment 2/F_select frame` | Sliced overlay | Selected answer or focused element |
| **Feedback box** | Flat semantic tint (3.3) with a 6-px left border in the state colour | Simple | Icon 32 px at the left |
| **Reward toast** | Wood panel + `MagicGold` title, `icon_star` | Sliced | Existing `FragmentRewardToast` timer logic stays unchanged |
| **Interaction prompt** | Small parchment pill (`Shop/paper button`), `[E]` key in a GoldGlow chip | Sliced | Bottom centre; existing `InteractionHint` object |

### 6.1 Button states (ColorTint unless Sprite Swap is noted)

| State | Tint multiplier | Extra |
| --- | --- | --- |
| Normal | `#FFFFFF` | none |
| Highlighted | `#FFF1CC` (warm) | Add the selection frame on keyboard focus |
| Pressed | `#D9C39A` | Paper buttons swap to `paper button PRESS` |
| Selected | `#FFE7A8` | none |
| Disabled | `#A89F90`, alpha 0.6 | Label alpha 0.6 |
| Fade duration | 0.08 s | none |

The existing `Button.onClick` listeners and Inspector references are untouched. Only the `Image`, `TMP_Text` and `Button.colors` / `transition` / `spriteState` properties change.

---

## 7. Code-driven colours: shared theme

Add one presentation-only static class, `Fyp.Development.UI.FypUiTheme`, holding the tokens from section 3. Replace the hard-coded `new Color(...)` and `<color=#...>` literals in the scripts listed in 1.3 with references to it.

- This is purely visual: no gameplay, validation or progression code changes.
- Without it, any Inspector restyle is overwritten as soon as a panel presents a step.
- `RecursionPanelView.Create()` and its helpers should also read fonts and colours from the theme, so any future runtime-created UI follows this guide. That includes the planned Magical Farm Simulation Guide.

---

## 8. Migration plan

Each batch: take a scoped scene backup, edit through Unity MCP / SerializedObject with one Undo group, check the Console, capture Play Mode screenshots at three resolutions, run the batch's regression checks, and review `git diff`. Nothing is committed automatically.

| Phase / batch | Scope | Risk notes |
| --- | --- | --- |
| **UI-0 Font prerequisites** ✅ done (see 2.1, 10.3) | Add `· – —` to the Vanilla atlas; `…` → `...`, `•` → `·` in strings; mark 9-slice borders on the chosen Bamao sprites; add `FypUiTheme` | Changes a font asset and sprite import settings, and touches script string literals. No logic changes. |
| **UI-B Prototype** ✅ done (see 10) | `MagicHarvestConsolePanel`: parchment main panel, ribbon header, InkSlate code well, Moss "Run row", wood close, typed input, feedback states, Vanilla font, sky accent. Also includes the approved Ruin 2 Phase B console items (opaque sky surround). | Console logic, Enter submit, queued close and camera split stay as they are. Before/after at 1920×1080, 1366×768, 1024×768. |
| **UI-C1 Shared HUD** | `InteractionHint`, `FragmentRewardToast`, `Ruin02ReturnLockedMessage` | Toast timer and shared label references unchanged |
| **UI-C2 Books** | `AncientSpellbookPanel`, `Ruin02AncientSpellbookPanel` (already parchment; mostly font and type scale) | Three pages, arrows and Close unchanged |
| **UI-C3 Guided simulations** | `Ruin01GuidedSimulationPanel`, `Ruin02GuidedSimulationPanel`, `JumpGuidedSimulationPanel` | Uses `FypUiTheme`; Finish gating unchanged; check the slider and diagrams |
| **UI-C4 Challenges** | `RecursionPracticePanel` (bridge), `Ruin02PracticePanel`, `Ruin01JumpPanel` | Answer selection colours and AI hint area; regression: wrong / double / stale input, ESC, cinematic queued close |
| **UI-C5 World text** | Farm `TextMesh` → `TextMeshPro`, Vanilla font, Parchment + outline | Small typed-field script change for `HarvestDebugDisplay` |
| **UI-C6 Developer panel** (optional) | Wood panel variant, so it reads as clearly "not gameplay" | Editor only |
| **Future UI** | Farm Simulation Guide, Final Portal messages | Must be built from this guide and `FypUiTheme` |

**Out of scope:** the legacy quiz `ChallengePanel` and `RecursionVisualizationPanel` (legacy visualizer, disabled) unless they become player-facing; unowned legacy hint objects; vendor demo scenes; the AI backend; Ruin III; the Final Portal logic.

---

## 10. UI-B prototype: Magic Harvest Console (implemented)

Reference implementation for UI-C. Code: `Ruin02HarvestConsoleView.cs` (presentation only) and `Fyp.Development.UI.FypUiTheme`. Scene: `Canvas/MagicHarvestConsolePanel`, `Ruin02_MagicalFarm/HarvestConsole`.

### 10.1 Decisions made during the prototype

| Topic | Decision |
| --- | --- |
| Button transitions | **ColorTint everywhere**, using `FypUiTheme.ButtonColors()`. Sprite Swap was dropped because one transition type can't give both a highlight and a pressed sprite. `UI_ButtonPaperPressed` is kept for future use. |
| Primary / secondary | Primary = `UI_ButtonMoss` (Run Row) with a Parchment bold label. Secondary = `UI_ButtonPaper` (Close) with an Ink label. |
| Active code line | **Colour only** (`GoldGlow`). Bold widened the leading spaces and broke the indentation. Line numbers are in `InkSlateMuted`. |
| Code well | Built-in rounded `UISprite` tinted `InkSlate`. Code is Parchment 24 pt (auto-size 20–24), no wrapping. |
| Progress bars | Track and fill both use the light `UI_BarTrack`. The fill is `Filled / Horizontal`, tinted MagicGold (rows), SkyBlue (plots) or amber `#E0B066` (carrots). Tinting the brown `UI_BarFill` came out too dark. |
| Call stack | Five visible frames, most recent first. The top frame is GoldGlow with Ink text and a phase tag (calling, base case, harvest plot n, skip plot n). The rest say "waiting". Below them: "+ k more waiting below" or "Stack empty". Frame depth d is `harvest(row, 11 − d)`. |
| Feedback | A rounded box with an 8-px accent strip and a Bamao icon: info (`icon_information`, gold), correct (`icon_check`, ForestGreen), incorrect (`icon_cross`, Ember), queued close (`icon_clock`, SkyBlue). |
| Completion | The work area is replaced by a card: `UI_GoldFrame` border, wood ribbon "Harvest Complete!", stats aligned with `<pos=68%>`, and a next-step line that depends on the staff state and whether the fragment is collected. These are read-only references. |
| Background | The sky surround is now opaque `FarmSky #9BDDF8` and extends past the undrawn screen strip, which removes the ghosting. |
| New TMP objects | Set the wrapping mode explicitly. Script-created TMP objects defaulted to no wrapping here. |
| Messages | The open message, each row's result, the queued close and the completion text are all player-facing. The state line keeps the phrase "Magical Farm Completed!" because the H3 Play check asserts it. |

### 10.2 Layout (anchors relative to the panel, which spans 0.67–0.99 × 0.06–0.95 of the screen)

| Element | Anchors (x0, y0) – (x1, y1) |
| --- | --- |
| Sky surround | (−0.07, −0.07) – (1.04, 1.07) |
| Parchment frame `Popup_paper_bg` (PPU ×2.6) | (0, 0) – (1, 1) |
| Header ribbon / title | (0.04, 0.905) – (0.96, 0.985) / (0.12, 0.912) – (0.88, 0.978) |
| Status line | (0.06, 0.855) – (0.94, 0.90) |
| Code well / code text | (0.06, 0.605) – (0.94, 0.85) / (0.09, 0.62) – (0.92, 0.835) |
| Call stack / progress cards | (0.06, 0.33) – (0.49, 0.59) / (0.51, 0.33) – (0.94, 0.59) |
| Feedback box / text | (0.06, 0.205) – (0.94, 0.315) / (0.17, 0.21) – (0.92, 0.31) |
| Input | (0.06, 0.12) – (0.94, 0.19) |
| Run Row / Close | (0.06, 0.035) – (0.60, 0.105) / (0.62, 0.035) – (0.94, 0.105) |
| Completion card | (0.06, 0.205) – (0.94, 0.85) |

### 10.3 Scoped sprite copies (`Assets/Development/UI/Sprites/`)

These are copies of Bamao sprites with 9-slice borders. Vendor sprites and their import settings are unchanged.

| Copy | Source | Border (L, B, R, T) |
| --- | --- | --- |
| `UI_RibbonPaper` / `UI_RibbonWood` | `Popup_paper_title` / `Popup_wood_title` | 150, 40, 150, 40 |
| `UI_ButtonMoss` / `UI_StackFrame` | `button short green` / `button short dark` | 40, 40, 40, 40 |
| `UI_ButtonPaper` / `UI_ButtonPaperPressed` | `Shop/paper button` / `paper button PRESS` | 40, 24, 40, 24 |
| `UI_BarTrack` / `UI_BarFill` | `State_blank` / `State_brown` | 57, 20, 57, 20 |
| `UI_GoldFrame` | `Equipment 2/F_select frame` | 32, 32, 32, 32 |

`Popup_paper_bg` and `typing button` are used directly, because they already had borders.

### 10.4 World-side changes (Phase B)

- **The Wizard is hidden while the console is open.** `Ruin02HarvestConsole` records each player renderer's `enabled` flag on Open, then restores exactly those values in `ClosePanel`, which every close path goes through: the Close button, ESC, queued close and `OnDisable`. The CharacterController, colliders and movement state are not touched.
- **Console visual:** `HarvestConsole/ConsoleVisual` is a `stone_001` pedestal with the Low Poly Casual Magic Book (the same model as the Ruin 2 spellbook) and a soft sky-blue point light, without shadows. It has **no colliders** and doesn't move the interaction origin or range.
- **`FarmReturnPoint/GatewayLabel`:** Y rotation 180 → 0, so it's no longer mirrored.
- **Farm world status board:** the H2 developer text was replaced with "Use the Magic Harvest Console to begin the harvest." The board is still a legacy `TextMesh` until UI-C5.

### 10.5 Validation (26 Nov 2026, Play Mode through Unity MCP)

These were direct method or listener calls plus Input System events, not a human playtest.

- Captured at 1920×1080, 1366×768 and 1024×768 in the ready, running and completion states. No TMP overflow at any size, and the code stays at 24 pt.
- Wrong answer: rejected with no progress. Correct answer: plays exactly one row.
- The Enter path (`onSubmit`) and the Run Row and Close buttons (`onClick`) all work.
- ESC sent as an Input System keyboard event closes the idle console. ESC during playback queues the close until the row finishes.
- All three rows complete: 3 / 30 / 15, stack 0, not running.
- After every close, all 4 Wizard renderers, the viewport, FOV 60, CameraFollow, movement and cursor lock are restored.
- The Console had 0 errors.
- **Not tested:** the staff-complete and fragment-collected wordings on the completion card (this test run came through the Developer teleport); real mouse clicks or typing by a human; a player build.

### 10.6 Known limitations to address in UI-C

- At 1024×768, the 14–18 pt stack and progress captions render at about 11 px. They're legible but small. Consider dropping the frame tag text, or showing 4 frames instead of 5, at 4:3.
- The world status board and plot labels are still legacy `TextMesh` with the default font (UI-C5).
- `FinalPortalInteraction.cs` still uses `·` (Final Portal is out of scope).

---

## 9. Known risks and pre-existing issues found during this audit

- **InteractionHint (resolved in UI-B):** your Vanilla Caramel font is kept and saved. The unsaved Edit-mode "active" flag was set back to inactive, matching the saved scene. `InteractionHintManager` already deactivates and shows it at runtime. Its full restyle (Parchment text with an outline, which needs a material preset) is part of UI-C1.
- **Pre-existing teardown error:** stopping Play Mode while Ruin 2 practice is open throws a `MissingReferenceException` at `Ruin02PracticeController.cs:104` (`Close()` from `OnDisable` touches the destroyed panel). It isn't caused by the UI work; it could be a small separate fix.
- **Auto-size shrink:** see 2.2. Re-tune sizes per panel; don't just swap the font.
- **Installer re-runs:** they would regenerate the old style (see 1.4).
