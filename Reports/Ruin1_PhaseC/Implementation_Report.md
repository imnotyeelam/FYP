# Ruin 1 Phase C — Interactive Bridge Repair

## Scope and preserved state

Implemented on `fyp2-development-branch` in `Assets/Scenes/MagicWorld_Development.unity`. `Level1.unity`, `RecursionTrace.cs`, the six-decision `BridgePracticeLesson.cs`, Spellbook 2D code, package manifest, and project settings retain their pre-work SHA-256 hashes. The original Cube `BridgeDemonstration`, four manually placed Bridge groups, repaired poses, MotionRoots, connector, source prefabs, meshes, materials, teleport destinations, and Magic Book transforms were not altered. The Cube visualizer component remains in the scene but is disabled and removed from the shared target list, so it cannot compete for E input.

## New scene hierarchy and Inspector wiring

| Scene path | Purpose and assigned references |
|---|---|
| `Ruin01_Area/Gameplay/BridgePractice` | Empty root with `BridgePracticeController`. `rig` → `FloatingBridgeMotionRig`; `player` → `Player`; `interactionPoint` → `BridgePracticeInteractionPoint`; `panel` → `Canvas/RecursionPracticePanel`; `presentation` → child `BridgePracticeCamera`; `otherModalPanels` → the three existing modal panels. Interaction range 3 m; step interval 0.65 s. |
| `Ruin01_Area/Interactables/BridgePracticeInteractionPoint` | Empty marker at world `(-435.33, 43.57382, 53.638)`, checked against the fixed Bridge surface and CharacterController footprint. |
| `Ruin01_Area/Gameplay/BridgePractice/BridgePracticeCamera` | Disabled Camera until practice opens. `BridgePracticeCamera` links this camera, `Main Camera`, the practice panel RectTransform, and the motion rig. Scoped viewport stays left of the right panel and focuses the current module. |
| `Canvas/RecursionPracticePanel` | Editable clone of the original right panel. `BridgePracticePanel` links code/state/stack/explanation/progress labels, three answer buttons, Cast Spell, and Close. The cloned guided Next, Previous, Replay and checkpoint slider objects are inactive here. The original panel remains for fallback. |
| `InteractionHintManager` | Old `RuinRecursionVisualizer` target replaced with the single new `BridgePracticeController` target. Practice panel appended to `modalPanels`; existing targets and teleport references remain. |

`FloatingBridgeMotionRig` keeps explicit mapping: `placeStone(3)` → `Bridge (1)/MotionRoot`; `placeStone(2)` → `Bridge (2)/MotionRoot`; `placeStone(1)` → `Bridge (3)/MotionRoot`. `Bridge (3)/MotionRoot/PhaseA_EndConnector` follows module 3 and its collision rules. `Bridge` stays fixed.

## Code changes

- `Assets/Development/Recursion/BridgePracticeController.cs` and `.meta`: the runtime practice session, shared interaction target, sequential snapshot presentation, answer validation, scoped modal state, close/resume, three Action-only repairs, return highlight, and completion latch.
- `Assets/Development/Recursion/BridgePracticePanel.cs` and `.meta`: editable right-side question/answer UI bound to source snapshots; no practice playback controls.
- `Assets/Development/Recursion/BridgePracticeCamera.cs` and `.meta`: scoped active-module camera that restores the gameplay camera on close.
- `Assets/Development/Recursion/FloatingIslandMotion.cs`: opt-in practice ownership blocks legacy Repair/Reset calls from moving a traversable module beneath the player, including after completion.
- `Assets/Development/Recursion/Editor/BridgePracticeTools.cs` and `.meta`: exact-path read-only audit and idempotent Unity scene installer; no automatic scene mutation on domain reload.
- `Assets/Development/Recursion/Editor/BridgePracticePlayChecks.cs` and `.meta`: Unity Play Mode integration checks.
- `Assets/Development/Recursion/Editor/FloatingIslandPrototypeTools.cs`: Inspector prototype Repair/Reset controls disabled while practice owns a module.
- `Assets/Development/Recursion/Editor/FloatingIslandMotionPlayChecks.cs`: previous B2 reset test refuses to take over while Phase C owns the bridge.

No reward, fragment, AI, NPC, skybox, terrain, or portal progression was added. Practice completion is retained while the current Play session and scene remain loaded; save-game persistence is a later feature.

## Runtime behavior

One shared E interaction opens the panel; movement, camera follow, world interactions, and teleport interaction scripts pause. The cursor unlocks and the shared hint hides. Closing during island motion waits for settling and collision activation before releasing controls. Closing at any other snapshot pauses presentation and preserves the accepted range, checkpoint, answer gate, and repaired modules. Spellbook has its own `RecursionPlayback` and does not call the real bridge controller. Only source `RecursionPhase.Action` snapshots initiate movement; `buildBridge(0)` initiates no placement. The return snapshots pulse the already repaired modules. Completion requires source checkpoint 22, an empty stack, and exactly three placements. Completion occurs once and does not award a fragment.

## Automated verification

`Tools > FYP > Ruin 1 > Phase C > Run Interactive Practice Play Checks` (Ctrl+Shift+Alt+F6) completed in the actual Unity Editor: **PASS, 1469 assertions, 0 Console errors**. Details: `Temp/BridgePracticeC/PlayChecks.txt`.

The run covered queued E input and held E, single shared hint, six right/wrong/stale/unknown/double submissions, all 22 source checkpoints in order, 3/2/1 placement only on Action, base case and empty-stack returns, collision gating, close during motion, close and reopen during returns, completion only once, post-completion reset guard, Spellbook fourth-page Jump/Previous/Replay independence, cursor/modal/teleport suspension and restoration, and twelve actual CharacterController walking/running crossings in both directions on three lanes. The 1920×1080 first-question capture is at `Temp/BridgePracticeC/PracticeFirstQuestion.png`. Before/after Play scene exports are byte-identical.

The saved scene was compared to the pre-install live-scene export by Unity YAML object ID: all 1,139 previous records remain; only five previous records changed (Canvas child list, Gameplay/Interactables child lists, shared interaction manager registration, and the old visualizer enabled flag). No prior object record was removed. Unity-generated empty YAML fields cause `git diff --check` whitespace warnings; these have not been hand-edited to avoid rewriting Unity serialization.

## Manual Play Mode pass

1. Enter Ruin 1 and walk to the fixed first Bridge section near `(-435.33, 43.57, 53.64)`. Confirm a single repair hint appears and E opens the right panel once. Check the active floating section is easy to see; adjust only the new `BridgePracticeCamera` if its view needs artistic refinement.
2. For each question, deliberately choose one wrong answer, read the feedback, then choose the correct answer and Cast Spell. Confirm the appropriate floating section moves for `placeStone(3)`, `(2)`, `(1)`, with collisions unavailable until it settles.
3. Close during one four-second motion and check controls remain paused until settling. Reopen and finish; at the base case confirm no fourth island appears. Watch the call stack unwind and sequential module highlight.
4. Cross the completed bridge both ways, including the end connector. Open the Spellbook's fourth page separately and use Next/Previous/Replay; the real bridge must stay unchanged.
5. Confirm Escape and Close restore cursor, player movement, camera and return teleport. Recheck the existing Ruin 2/3 gateway interactions. Check Console for errors.

## Return marker note

`Ruin01_Area/SpawnPoints/L1_ReturnPoint` is a marker at `(-436.71, 45.865, 167)`; it is not the E interaction. `Ruin01_Area/Interactables/ReturnInteractionPoint` is the `RuinEntrance` at `(-432.5, 53.45658, 230)` with a 5 m range. It teleports to `MainHub/Ruin1/Destination` at approximately `(94.95, 0.00341, 198.406)`. Neither marker nor interaction was moved in Phase C.
