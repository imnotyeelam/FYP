# Claude Code project instructions

Read `FYP_HANDOVER.md` before modifying anything. It documents the audited baseline, exact progression rules, known camera wiring issue, historical checks, and pending manual tests. Verify current source/scene state before relying on historical reports.

## Architecture and locations

- Unity **6000.0.44f1**, URP **17.0.4**, Input System **1.13.1**, UGUI/TMP. Do not upgrade versions or regenerate project configuration incidentally.
- Working branch: `fyp2-claude-development`. Confirm branch and `git status` before edits; preserve user changes.
- Main scene: `Assets/Scenes/MagicWorld_Development.unity`, the only enabled build scene. Hub, ruins, farm, player, UI and progression share one scene. `Level1.unity` and `SampleScene.unity` are legacy/reference scenes.
- Runtime lesson/world code: `Assets/Development/Recursion/`, mainly namespace `Fyp.Development.Recursion`.
- Pure models: `RecursionTrace.cs`, `BridgePracticeLesson.cs`, `Ruin02ReturnLesson.cs`, `Ruin02PracticeLesson.cs`, `RecursiveHarvestLesson.cs`. Keep them independent of Unity world state and AI.
- Interaction contracts/selection: `Assets/Development/Interactions/`. Dispatcher: `Assets/Script_Level1/InteractionHintManager.cs`.
- Inventory/toast/finale: `Assets/Development/Progress/`. Restoration sequence: `Assets/Script_Level1/PortalRestorationController.cs`.
- Player, cursor, books, teleport, legacy quiz and shared AI endpoint: `Assets/Script_Level1/`. Earlier countdown prototype: `Assets/Scripts/`.
- Editor installers and bespoke Play checks: `Assets/Development/Recursion/Editor/`; scene dressing tools: `Assets/Development/Environment/Editor/`.
- Main hierarchy: `MainHub`, `Ruin01_RelocationRoot`, `Ruin02_Area`, `Ruin02_MagicalFarm`, `Ruin03_Area`, `Canvas`, `InteractionHintManager`, `FragmentProgressTracker`.
- Packages/settings: `Packages/manifest.json`, `packages-lock.json`, `ProjectSettings/`, `Assets/Settings/`.
- Historical evidence: `Reports/`, `LocalBackups/`. Many reports/installers describe superseded stages; do not replay them blindly.
- AI backend is external: `E:\AIBackend\server.py`; historical matching snapshot: `LocalBackups/Ruin2AI_20261101/server.proposed.py`. Unity posts to `http://127.0.0.1:8000/feedback`. A Unity clone does not provision that service.

## Development conventions

- Make small, task-scoped changes consistent with nearby C# code. Preserve existing file/class names, namespaces, serialized public fields and `.meta` GUIDs. Some legacy names intentionally deserialize current systems (for example `Ruin02AirRestoration` handles four staffs).
- Prefer explicit Inspector references; verify the referenced component's actual type, not just its GameObject name. Several generic `cameraFollow` fields currently reference URP camera data; reproduce and address only within an authorized fix.
- Keep teaching playback, answer validation, world actions, and rewards separate. Player input is restricted teaching text, never executable C++.
- Route new world interactions through the shared E target/priority interfaces and register modal panels. Preserve one action per key edge and cursor/control/camera state restoration.
- Use per-instance overrides/property blocks for runtime appearance. Preserve vendor materials, prefabs and URP migration copies.
- Keep Editor scripts under `Editor/`. Developer shortcuts remain Editor-only, session-only, and cannot grant legitimate progression.

## Required invariants

- Books have three intro pages; standalone guides require explicit **Finish**. Replay/slider/illustrative movement must not mutate the real world or award fragments.
- Bridge: six decisions, 22 source checkpoints, exactly three repairs on `placeStone(3/2/1)`, no zero/return placements, empty final stack. Preserve practice ownership and collider safety while broken/moving/settling.
- Jump: eight-step UI example uses 3 leaps; real route has five destinations. Inputs 1..6; only 5 succeeds. Preserve supported-surface/prerequisite checks, animation, safe retry, temporary platform and trail cleanup.
- Ruin 1 fragment requires real bridge completion and successful landing, then separate E collection. Developer override alone cannot earn it.
- Staff: six typed questions, 28 checkpoints, Continue-paced initial descent, return restorations Air/Earth/Fire/Ice in order 1..4, no action at base case, final empty stack.
- Farm: exact `Row01..03/Plot01..10/Soil` hierarchy, optional `Carrot`, total fifteen carrots. Three expression gates; return-phase plot checking. Final state must be 3 rows, 30 checked, 15 harvested, stack 0, not running. Preserve queued close and partial progress within the session.
- Ruin 2 fragment requires **both** completed staff restoration/empty stack **and** complete farm invariants; reveal in chamber, collect manually once.
- Original Ruin 2 stone owns lock -> farm -> collect requirement -> hub routing. Disabled `MagicalFarmGateway` retains data; do not re-enable/register it or the legacy return entrance as duplicate targets.
- One explicit shared tracker, idempotent known-ID collection, shared timed toast. Final Portal requires **Ruin01 AND Ruin02**, never an arbitrary count or all three. Preserve direct `RestoreMagic()` guard and once-only arch swap; keep legacy quiz disabled at this gate.
- `_Archived_Objects` intentionally contains five inactive old visuals. Hidden/disabled objects may retain live references or runtime roles; investigate before cleanup.

## Unity MCP and editing safety

- Audit found **no Unity MCP package, project client configuration, or available Unity MCP tools**. Do not claim a connection exists or invent a server port/tool schema. Node REPL and Unreal MCP entries are unrelated.
- If a Unity bridge becomes available, discover its actual tools/configuration, verify the connected project and Editor version, then inspect hierarchy, actual Inspector component references, Console, Edit/Play state and unsaved changes read-only first.
- Installing/configuring a bridge is a separate implementation change. Do not add packages or MCP configuration merely to perform an audit.
- For authorized scene edits, prefer Unity serialization/Inspector with Undo and a scoped backup; preserve prefab connections and world transforms. Avoid broad hand-edits to YAML, scene reloads that discard unsaved work, and saving Play Mode state.
- Inspect Editor command source before invoking it. “Audit” and test menus can write files; older installers can save scenes, assume obsolete paths, or replace current routing. Do not run Install/Apply/Archive/Reset commands as diagnostic guesses.

## Validation and restrictions

- For documentation-only work, do not launch Unity, run mutating Editor checks, call the AI backend, or change gameplay assets. Verify only requested documentation changed.
- For implementation tasks, require Unity compilation and Console review, relevant compatible targeted checks, and manual validation of affected UI/travel/reward paths. Compilation alone does not establish scene wiring or playability. No first-party NUnit test assemblies were found; many checks are custom `Tools/FYP` menu runners.
- Consult handover section 17 for exact checks. Prioritize current Rock Navigation, Farm H3/H5, Jump J2, Final Portal P1 and Developer Farm checks as relevant. Inspect older H4/bridge checks for stale assumptions before running. Do not overwrite historical LocalBackups reports without need.
- Regression-test wrong/double/stale input, early Close/ESC, cinematic/row queued close, camera/cursor/controller recovery, one E action, no duplicate rewards, correct 0/2..2/2 finale, and ordinary progression without Developer bypasses. Verify no unintended scene changes remain after Play Mode.
- For UI changes, inspect Game view at 1024x768 and widescreen; for build/deployment changes, test a player build. Report unrun tests explicitly; historical PASS files are not fresh results.
- Do not rewrite working systems, mass-reformat, reorganize vendor assets, remove legacy compatibility fields, regenerate GUIDs, delete archived content, upgrade packages, or reset/clean unrelated changes without an explicit task requiring it. Use focused fixes and review `git diff`/`git diff --check`.
- Never read or disclose `.env` credential values, embed API keys in Unity, or log secrets. Backend credentials stay server-side. Live feedback POSTs may invoke paid inference; do not run probes as a read-only check. AI supplies hints only and must never determine correctness/progression.
- Keep status honest: implemented, partial, planned/recommended, and untested are different. No persistence, completed Ruin III lesson, onward Final Portal travel, or research outcome logging was established by this audit; confirm scope before adding them.
