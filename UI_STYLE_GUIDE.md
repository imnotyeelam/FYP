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
| **UI-C1 Shared HUD** ✅ done (see 11) | `InteractionHint`, `FragmentRewardToast`, `Ruin02ReturnLockedMessage` | Toast timer and shared label references unchanged |
| **UI-C2 Books** ✅ done (see 11) | `AncientSpellbookPanel`, `Ruin02AncientSpellbookPanel` (already parchment; mostly font and type scale) | Three pages, arrows and Close unchanged |
| **UI-C3 Guided simulations** ✅ done (see 12) | `Ruin01GuidedSimulationPanel`, `Ruin02GuidedSimulationPanel`, `JumpGuidedSimulationPanel` | Uses `FypUiTheme`; Finish gating unchanged; check the slider and diagrams |
| **UI-C4 Challenges** ✅ done (see 13) | `RecursionPracticePanel` (bridge), `Ruin02PracticePanel`, `Ruin01JumpPanel` | Answer selection colours and AI hint area; regression: wrong / double / stale input, ESC, cinematic queued close |
| **UI-C5 World text** ✅ done (see 11; outline replaced by colour/backdrop contrast) | Farm `TextMesh` → `TextMeshPro`, Vanilla font | Small typed-field script change for `HarvestDebugDisplay` |
| **UI-C6 Developer panel** ✅ done (see 11) | Wood panel variant, so it reads as clearly "not gameplay" | Editor only |
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

## 11. UI-C Part 1 (implemented 26 Nov 2026): farm refinements, C1, C2, C5, C6

### 11.1 Decisions

| Topic | Decision |
| --- | --- |
| **Text outlines** | **Don't use SDF outlines with Vanilla Caramel.** Its atlas has 6 px padding at 173 pt (gradient scale 7), so a 30 pt outline is under 1 px even with `OUTLINE_ON`. Get contrast from colour (Ink on light, Parchment on dark) or from a backdrop. The trial outline material was removed. |
| Text backdrops | `Fyp.Development.UI.FypTextBackdrop`, a sibling Image placed before the text. It sizes itself to the text's rendered bounds plus padding, and hides when the text is empty or inactive. It's used for the interaction-hint pill. (The spellbook code wells stopped using it on 27 Nov 2026; see section 16.) Use it only for single text pills, not multi-page code. |
| Interaction hint | Ink, bold, 30 pt on a `UI_ButtonPaper` pill (`Canvas/InteractionHintBackdrop`). `InteractionHintManager` still owns visibility; the pill follows it. |
| Toast / locked message | A `UI_RibbonWood` banner (PPU ×2.2) with Parchment bold text, 28 pt (auto-size from 20). `FragmentRewardToast` timing and the shared label are unchanged. |
| Spellbooks | Background art, buttons and page flow are unchanged. The dimmer is warm Ink at 45%. Title Ink 48 bold, body Ink 32 (auto-size from 22), page number InkSoft 24. Code is Parchment in a fixed-size InkSlate `CodeWell` (section 16 supersedes the auto-fitted well). Buttons use theme ColorTint. Duplicate button labels are transparent (the sprites already show the arrows and X). The Ruin 2 `CodeText` box was enlarged to 640×330 so the whole function fits. |
| World text (farm) | 35 `TextMesh` components were replaced in place by `TextMeshPro` on the same GameObjects; transform, orientation, children and active state are preserved. Sizes match the original rendered height. Plot numbers are Parchment bold on soil. Row labels and "RETURN TO RITUAL" are Ink bold on the pale platform. The status board is Ink text on a parchment card. `Ruin02FarmSimulation.statusDisplay` (TMP) is the runtime reference; the legacy `debugDisplay` (TextMesh) field is kept, now null, so old Editor tools still compile. Don't re-run them. |
| Status board | Hidden while the console is open: `Ruin02HarvestConsole.statusBoard` stores its active state on Open and restores it on every close path. |
| Farm framing | `FarmCameraView` was moved 8 m back along its own view direction, computed to fit every plot, carrot and row label at 4:3. FOV is chosen by aspect: `narrowAspectFov` 66 at 4:3 to `wideAspectFov` 52 at 16:9, so 16:9 fills the view. |
| Call stack | Text 21 pt. Only the top frame is gold and bold, with no tags on the frames. The heading carries "depth N &#124; top: CALL/BASE/HARVEST/SKIP". Progress labels are "Rows / Plots / Carrots" with the value at `<pos=62%>`. |
| Completion card | Wood ribbon title, `icon_medal`, stats at 28 pt, and a "Next" instruction box (info fill, gold accent, info icon). The wording is unchanged and read-only. |
| Developer panel | Parchment panel, `UI_RibbonWood` header, paper buttons and a Moss Close button. The subtitle's `•` was replaced with `|`. All buttons and listeners are unchanged. |

### 11.2 Validation (Play Mode through Unity MCP, direct listener/method calls, not a human playtest)

- **1920×1080:** Ruin 1 book (pages 1→3 via the forward button, Close), Ruin 2 book page 3, Developer panel with a toast, farm world labels with the hint pill, console running, completion card.
- **1024×768:** console ready, Ruin 2 book page 3.
- No TMP overflow after the fixes. The farm and all row labels are in view at both aspects.
- The status board hides on Open and is restored on Close.
- Wizard renderers, camera, FOV, cursor and controls were restored.
- The Developer Farm teleport button works.
- The status board's TMP text updates from the simulation.
- The Console showed 0 errors.

**Not re-tested this session:** ESC and queued close (that code didn't change; validated in UI-B), the locked message (no live owner), real mouse and keyboard play by a person, and a player build.

### 11.3 Remaining for UI-C Part 2

- Guided Simulation and Challenge panels (C3/C4), with code colours routed through `FypUiTheme`.
- The `·` in `FinalPortalInteraction.cs` (Final Portal is out of scope).
- Legacy unowned hint objects (`ReturnHint`, `Ruin02/03_*Hint`) are still old-style but are never shown.
- World labels are small at 4:3 by design (whole-farm framing).

## 12. UI-C3 Guided Simulations (implemented 26 Nov 2026)

Panels: `Ruin01GuidedSimulationPanel` (bridge), `JumpGuidedSimulationPanel`, `Ruin02GuidedSimulationPanel` (Elemental Staff). All existing anchors are kept. New background elements are siblings placed behind their text: `Ribbon_*`, `Well_*`, `InfoBox_*`.

| Element | Standard |
| --- | --- |
| Panel | `Popup_paper_bg` (PPU ×2.2). The `ExecutionDetails` sub-panel is a rounded ParchmentEdge card at 35%. |
| Header | Title in Ink bold, centred over a `UI_RibbonPaper` ribbon tinted 30% with the area accent (Ruin 1 ForestGreen, Ruin 2 Arcane). |
| Code panel | Static InkSlate rounded well behind the code, Parchment text. Active/executing line in `FypUiTheme.ActiveLineTag` (GoldGlow). The RecursionPanelView themed code uses `<mspace=0.56em>` (font-relative), because Vanilla is proportional. |
| Call stack | `UI_StackFrame` rows. The top/active frame is GoldGlow with Ink text; waiting frames use the sprite colour with Parchment text; labels are vertically centred. This is set at runtime by `RecursionPanelView` (themed) and by `Ruin01JumpGuidedSimulation.ShowStack`. |
| Explanation / instruction | InfoFill rounded box, Ink text. |
| Progress slider | Track and fill `UI_BarTrack` (fill tinted with the area accent) and a GoldGlow knob. Theme ColorTint. |
| Playback buttons | Previous / Next / Replay / Close use `UI_ButtonPaper` with Ink labels. Finish uses `UI_ButtonMoss` with a Parchment label. Theme ColorTint, including the disabled tint. |
| Diagrams | Bridge (`BridgeDiagramView` instance colours): grey `#8E8578`, teal `#4FA3A5`, gold `GoldGlow`, green `#6E9E6A`, matching the scene legend; banks `#7C9A6B`. Staff: the content colours (Air/Earth/Fire/Ice) are unchanged and idle is `InkSlateMuted`. The jump preview landings are GoldGlow when reached and muted when pending. |
| Type | Vanilla Caramel. Maximum = original rendered size × 0.92, minimum = 70% (≥ 12). The Ruin 2 guide has an 18 pt floor. `State` minimum is 11 so its two lines fit. |

**Opt-in theming for shared views.**
- `RecursionPanelView.useFypTheme` and `Ruin02StaffDiagramView.useFypTheme` are **true only on the guided instances**.
- The challenge panels (`RecursionPracticePanel`, `Ruin02PracticePanel`) and the legacy `RecursionVisualizationPanel` serialize `false`, so they render exactly as before until UI-C4.
- Lesson for UI-C4: when restyling, set the auto-size maximum from the rendered size. Don't derive the minimum from a large inherited maximum; this made text huge during UI-C3 before it was corrected.

**Validation (Play Mode through MCP; direct listener/method calls).**
- **Bridge:** Next ×5 and the slider drag moved the checkpoint (9/22). Finish was hidden until 22/22 with an empty stack. Finish completed the lesson and restored the cursor.
- **Staff:** Next ×17 reached 18/28, with Air restored and the idle colours muted. Early close (`CloseEarly`, the ESC path) left the lesson **not** completed. Reopen + Finish completed it, and the practice hint became "Seal the Magic".
- **Jump** (via the Developer Ruin 1 teleport + session unlock): opened, Next to checkpoint 3 with the correct TOP frame, Close works.
- 1920×1080 and 1024×768 (bridge) had no TMP overflow. The Console showed 0 errors.
- The scene diff touches only the three guided panels in this batch. The other `RecursionPanelView` instances only gained `useFypTheme: 0`.
- Screenshots were taken through a camera render with the Canvas temporarily set to Screen Space – Camera in Play Mode only, because the Editor couldn't be focused for Game-view capture.

**Not tested:** the jump guide's return-leap animation (checkpoints 6→7) and its Finish, because Play Mode could not advance frames while unfocused. Also real input and a player build.

**Follow-ups:** the jump guide's illustration labels ("Leap 1–3", "ILLUSTRATION ONLY") are still small (16–17 pt originals). The purple `SpellOrb` is a 16 px square (content, left unchanged).

## 13. UI-C4 Challenge Panels (implemented 26 Nov 2026)

Panels: `RecursionPracticePanel` (bridge challenge, `BridgePracticePanel`), `Ruin02PracticePanel` (Elemental Staff challenge), `Ruin01JumpPanel` (magic jump input). These follow the same standards as section 12: parchment panel, accent ribbon title, InkSlate code well, GoldGlow active line and active frame, `UI_StackFrame` rows, InfoFill explanation box, themed slider, paper/moss buttons.

| Element | Standard |
| --- | --- |
| Theme switch | `useFypTheme = true` on both challenge `RecursionPanelView`s. `BridgePracticePanel` and `Ruin02PracticePanel` build their own code and stack text, so they call the shared helpers `CodeOpen/CodeClose` (themed `<mspace=0.56em>`), `ActiveLineOpen`, `StyleFrame(row)` and `StyleFeedback(text, box, kind)`. The legacy colours remain the fallback when the switch is off. |
| Answer buttons (bridge) | `UI_ButtonPaper`, Ink label. **Selected = GoldGlow tint**, idle = white (set at runtime). |
| Primary action | Cast Spell / Submit / Cast use `UI_ButtonMoss`. Staff **Continue** (`nextButton`) moved into the Submit slot, because the two are never visible together, and is primary too. It previously overlapped the feedback area. |
| Input fields | Staff answer and jump `n`: `typing button` sprite, Ink text, InkSoft italic placeholder, Ink caret, GoldGlow selection. |
| Feedback | `FeedbackBox_PracticeFeedback` (rounded) behind the feedback text, assigned to the new optional `feedbackBox` fields. Correct/neutral = CorrectFill + Ink, incorrect = IncorrectFill + Ember. **Staff AI hint or pending = HintFill**: LiveAI and Pending show as a hint, local fallbacks show as incorrect. |
| Completion | The existing completion strings ("Bridge Restored...", "Ritual Complete", empty stack) render in the same panel styles. No separate card was added, to avoid layout changes in the challenge logic. |
| Type | Max = rendered size × 0.92 (≥ 14), min = 70% (≥ 12). Code 24 pt (min 16) and an 18 pt floor for other text in both list-style challenges. Frame labels 20 pt. `State` min 11. |

**Validation (Play Mode through MCP; no AI requests made).**
- **Bridge:** opened after a legitimate guide Finish. The wrong answer (`buildBridge(2)`) was rejected with Ember on IncorrectFill, and the selected answer turned gold. The correct `placeStone(3)` was accepted (CorrectFill) and the bridge advanced to decision 2 / checkpoint 3.
- **Staff:** the correct `sealMagic(3)` was accepted and entered the Continue-paced descent. Continue is visible as primary; no wrong answer was sent, because that could trigger a paid AI request.
- **Jump:** `n = 9` was rejected with "Enter one whole number from 1 to 6", no jump started, and Close restored cursor lock.
- 1920×1080 checked for all three, 1024×768 for the staff challenge: no TMP overflow. The Console showed 0 errors.
- The scene diff since the UI-C4 backup touches only these three panels.

**Limitations.**
- The Editor lost focus during part of the session, freezing Play Mode. Bridge practice needs frames to finish opening, which one focus attempt resolved. Some captures used the camera-render method (section 12).
- **Not tested:** the jump animation for a valid `n`, the staff AI-hint colour (only checked in code), full six-decision runs, real input, and a player build.
- In the narrow 4:3 side panel, body text renders at about 11 px: legible but small.

## 14. Magical Farm Simulation Guide (Phase D, implemented 27 Nov 2026)

`Canvas/FarmGuidedSimulationPanel` was built as a copy of the UI-C3 themed Ruin 2 guided panel, so it shares the exact standards in section 12. The farm accent (SkyBlue) tints the header ribbon and slider fill.

| Element | Spec |
| --- | --- |
| Right column | Shared `RecursionPanelView` (`useFypTheme`, lesson label "Magical Farm", title "Harvest Guide &#124; gather(4)"). The code area is enlarged for the 11-line example: Code (0.05, 0.60)–(0.95, 0.885), min 13 pt. Close is visible. |
| Four-plot diagram | `FarmGuideDiagram` (`Ruin02FarmGuideDiagramView`): heading, a "Learning only" note, 4 rounded plot cards, the Bamao `Pause/carrot` icon on plots 1, 3 and 4, a gold `UI_GoldFrame` outline (fillCenter off) on the plot being checked, status "Checked n / 4 &#124; Picked n / 3", and a legend. |
| Plot colours | Unchecked = soil `#8A6545`; picked = `FarmHarvested` (SkyBlue); empty and skipped = `FarmEmpty` (amber). These match the real farm's highlight meanings. |
| Finish | `UI_ButtonMoss`. It appears only after the final checkpoint has been reached and the guide isn't completed yet. |

The carrot icon is the pack's illustrated carrot character. It reads on the soil cards; swap it if a plainer plant icon is wanted later.

---

## 9. Known risks and pre-existing issues found during this audit

- **InteractionHint (resolved in UI-B):** your Vanilla Caramel font is kept and saved. The unsaved Edit-mode "active" flag was set back to inactive, matching the saved scene. `InteractionHintManager` already deactivates and shows it at runtime. Its full restyle (Parchment text with an outline, which needs a material preset) is part of UI-C1.
- **Pre-existing teardown error:** stopping Play Mode while Ruin 2 practice is open throws a `MissingReferenceException` at `Ruin02PracticeController.cs:104` (`Close()` from `OnDisable` touches the destroyed panel). It isn't caused by the UI work; it could be a small separate fix.
- **Auto-size shrink:** see 2.2. Re-tune sizes per panel; don't just swap the font.
- **Installer re-runs:** they would regenerate the old style (see 1.4).

## 15. Polish pass decisions (27 Nov 2026)

- **Headers:** use a plain header, i.e. a centred Ink bold title over a thin `MagicGold` rule (`HeaderRule`). **Don't put long titles in ribbon graphics.** Ribbons are reserved for short banners (toasts, "Harvest Complete!", Developer panel).
- **Spellbook code (superseded by section 16):** a content-sized code block (`ContentSizeFitter` preferred size, left-aligned, overflow mode Overflow) centred on the page, inside the `FypTextBackdrop` well. Don't centre-align code lines.
- **Wide challenge layout:** the Ruin 2 staff challenge is the reference for crowded challenges. Use a two-column panel; code and stack on the left, prompt, input, feedback and actions on the right.
- **World labels:** add `FypBillboard` and place them above and in front of their object. Role-dependent labels (e.g. `Ruin02ReturnStoneLabel`) only read state.
- **AI hint state:** a pending AI hint shows the local hint plus an italic "The Magic Tutor is thinking..." line. A live AI hint uses the `HintFill` / SkyBlue style (farm `HarvestConsoleMessageKind.Hint`).

## 16. Spellbook code panels: fixed layout (27 Nov 2026)
The content-sized approach (section 15) misaligned in the real Game View. `FypTextBackdrop` only re-measures when the text or position changes, while the `ContentSizeFitter` resized the text afterwards, so the well and the code drifted from page to page. Both Spellbooks now use a fixed layout:

| | Ruin 1 `AncientSpellbookPanel` | Ruin 2 `Ruin02AncientSpellbookPanel` |
|---|---|---|
| `CodeWell` (InkSlate Image) | 480×380, centre anchor, y −135 | 680×470, centre anchor, y −135 |
| `CodeText` rect | 370×324, same centre | 560×414, same centre |
| Font | Vanilla Caramel SDF 42, Parchment | Vanilla Caramel SDF 34, Parchment |
| Largest page block | 356×278 | 547×370 |

Rules:
- No `ContentSizeFitter` and no `FypTextBackdrop` on spellbook code. The well and text never move between pages. `FypTextBackdrop.cs` itself is unchanged and still drives the interaction-hint pill.
- Alignment is **Left (vertically middle)**, no wrapping, no auto-size. Indentation is kept, and the text rect is only as wide as the book's widest code, so the block sits centred in the well with about 55 px padding.
- Size each book from its largest page: the font is the largest size whose biggest page fits the inner rect. Re-measure if any page's code changes.
- Validate in the Game View on every page, forward and backward, at 1920×1080 and 1024×768, not only with TMP overflow flags.
