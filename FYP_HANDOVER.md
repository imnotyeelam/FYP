# FYP Unity development handover

Audit date: 26 November 2026 (Asia/Kuala_Lumpur). Workspace: `E:\FYP`.
Branch: `fyp2-claude-development`; HEAD: `21a097e10ff938ee695477beb35319ba08c0717a` (`farm basic done`, 24 November 2026). At audit time, `fyp2-development-branch` and this branch point to the same commit. The starting working tree was clean.

> **Update 26 November 2026 (Claude Code onboarding, see section 18):** Unity MCP is now installed and connected; the camera-reference issue affected **five** components, not four, and has been fixed statically in the saved scene (not yet Play-tested). Working tree is no longer clean: uncommitted changes are `Packages/manifest.json`, `Packages/packages-lock.json`, `Assets/Scenes/MagicWorld_Development.unity`, plus untracked `CLAUDE.md`, `FYP_HANDOVER.md` and `LocalBackups/CameraFollowRefFix_20261126/`. Nothing has been committed. Sections 12, 13 and 14 below are amended accordingly; other sections are unchanged from the original audit.

## 1. Scope and evidence

This is a read-only source and serialized-scene audit. Only this file and `CLAUDE.md` were created. No Unity launch, import, compilation, Play Mode session, build, backend request, package installation, scene installer, or gameplay change was performed.

Status terms throughout:

- **Implemented:** runtime code exists and, where specified, references are present in the saved development scene. This does not certify runtime correctness.
- **Partially implemented:** a usable portion exists but the wider feature is incomplete.
- **Planned / recommended:** absent work or a suggested next step; recommendations are not an approved research specification.
- **Untested in this audit:** runtime, visual, network, and build behavior was not exercised here. Historical test reports are identified separately.

Primary evidence: `Assets/Scenes/MagicWorld_Development.unity`, first-party runtime and Editor scripts, `ProjectSettings`, `Packages`, `Reports`, and `LocalBackups`. The separately located `E:\AIBackend\server.py` was inspected without reading its `.env`. Saved scene data takes precedence over old installation reports. Prefab instances and package components require Unity inspection for a complete runtime reference audit; this document does not claim that every asset, shader, or collider has been validated.

## 2. Project overview and educational purpose

The project is a third-person fantasy educational game teaching C++ recursion through books, guided call-stack simulations, prediction questions, and visible magical world actions. Backend tutor text calls the game **The Broken Portal**; Unity's configured product name is `FYP1.0`.

The research aim inferred from implemented lessons is to help novice programmers understand recursive execution by connecting code, stack frames, parameters, and world effects, with optional AI hints for misconceptions. No formal research proposal, participant protocol, assessment rubric, or measured learning results was found in the inspected project documentation. Confirm the formal aim with the project owner before presenting this inference as the approved research statement.

Learning objectives evidenced by code:

1. Identify the base case and explain why it stops further calls.
2. Follow decreasing arguments while retaining each caller's own parameter.
3. Distinguish actions before recursion from actions after recursion.
4. Trace stack growth, last-in-first-out returns, and the final empty stack.
5. Predict exact action counts and order without assuming a void function returns a number.
6. Apply the same recursion pattern to bridge construction, magical jumps, staff restoration, and harvesting occupied and empty plots.

## 3. Unity environment and dependencies

| Item | Audited configuration |
| --- | --- |
| Editor | Unity `6000.0.44f1`, revision `101c91f3a8fb`, from `ProjectSettings/ProjectVersion.txt` |
| Render pipeline | Universal Render Pipeline `17.0.4` |
| Graphics / PC quality | `Assets/Settings/PC_RPAsset.asset`; current quality index 1 = PC |
| Mobile quality | `Assets/Settings/Mobile_RPAsset.asset` |
| Input | Input System `1.13.1`; `activeInputHandler: 2` (both input backends); first-party gameplay polls Input System keyboard/mouse |
| UI | UGUI `2.0.0`, TextMesh Pro UI, plus a legacy `TextMesh` farm world display |
| Tests | Unity Test Framework `1.5.1`; main bespoke checks live under an `Editor` folder |
| Other direct packages | AI Navigation `2.0.6`, Terrain Tools `5.2.1`, Post Processing `3.4.0`, Timeline `1.8.7`, Visual Scripting `1.9.6`, Multiplayer Center `1.0.0` |
| Editor tooling packages | Collaborate Proxy `2.13.6`, Rider `3.0.31`, Visual Studio `2.0.22` |
| Build scene | Only `Assets/Scenes/MagicWorld_Development.unity` is enabled in `EditorBuildSettings.asset` |

`Packages/manifest.json` and `packages-lock.json` are authoritative for package resolution. Installed packages do not by themselves imply implemented navigation, networking, or multiplayer gameplay. There are no first-party `.asmdef` or `*Tests.cs` files found under Assets; runtime code uses the default Unity assembly and Editor-folder compilation.

Important imported art includes FantasyForest, Fantastic Ancient Forest, FANTASTIC - Ancient Forest, WizardPolyArt, Bamao UI Pack, BTM gems, stylized magic staffs, Low Poly Casual Magic Book, Hovl glowing orbs, and GabrielAguiar projectile assets. Development URP migration work is isolated under `Assets/Development/AncientForestURP17`, `AncientForestPOC`, and `FW01_URP`, with migration maps and reports. Preserve these copies and vendor source assets; do not mass-convert materials or upgrade the render pipeline as routine cleanup.

## 4. Scenes, hierarchy, and progression

### Authoritative scene

`Assets/Scenes/MagicWorld_Development.unity` contains the hub and gameplay areas in **one scene**. Teleports reposition the existing player; they do not load separate ruin scenes.

Important saved hierarchy:

```text
Player / Main Camera / EventSystem / Canvas
GameManager                         CursorManager and TeleportManager
InteractionHintManager              coordinated E input and modal registry
FragmentProgressTracker             shared scene-session inventory
PortalRestorationManager            final restoration sequence
EditorDeveloperMode                 Editor-only testing panel
MainHub
  Ruin1 / Ruin02_Gateway / Ruin03_Gateway
  BrokenPortal                      FinalPortalInteraction
    BrokenPortal / CompletePortal   visual swap targets
Ruin01_RelocationRoot
  Ruin01_Area
    SpawnPoints / Interactables / Gameplay
      Gameplay/BridgePractice
      Gameplay/FloatingBridgeMotionRig
      Gameplay/Ruin01GuidedSimulationManager
  Ruin01JumpRoute
    JumpChallenge / JumpGuidedSimulation
    JumpStartPoint / MagicStep01..04 / JumpLandingPoint
Ruin02_Area
  SpawnPoints/EntrySpawn
  Interactables/ReturnInteractionPoint
  Interactables/Ruin02ChallengeLandmark/Ruin02ChallengeInteractionPoint
  Gameplay/Ruin02PracticeManager
  Gameplay/Ruin02GuidedSimulationManager
  Gameplay/Ruin02SpellbookManager
  Gameplay/AirStaffRestoration
  Gameplay/Ruin02FragmentRevealController
  Gameplay/RitualChamber
    FragmentCollectionPoint
    MagicalFarmGateway/FarmRitualReturnSpawn
Ruin02_MagicalFarm
  Row01..03/Plot01..10
  HarvestConsole / HarvestDebugDisplay
  FarmEntrySpawn / FarmReturnPoint / FarmCameraView
Ruin03_Area                         gateway/return scaffold
_Archived_Objects                   five deliberately inactive former visuals
```

Additional roots include legacy `Portal`, `Ruin1`, `Ruin2`, `Ruin3`, environment groups, terrain, and book managers. Names alone do not establish which object owns live gameplay. Inspect component enabled states, references, prefab overrides, and the shared target registry before editing.

`Assets/Scenes/Level1.unity` and `SampleScene.unity` remain but are disabled in Build Settings. Imported demo scenes and development forest validation scenes are references, not the current gameplay entry point. Existing `Build/` outputs are ignored generated artifacts whose freshness was not verified.

### Current player progression

1. Explore MainHub and travel to a ruin with shared E interaction.
2. **Ruin 1:** read the optional introductory book; at the bridge landmark complete the standalone guided simulation using **Finish**, then solve six bridge decisions. Three floating modules restore on `placeStone(3)`, `(2)`, `(1)`.
3. Reach the Jump Rock on its supported start surface. Normal access requires bridge practice and its guided lesson. Complete the separate eight-checkpoint magic-jump guide, explicitly Finish, and choose a jump argument. Exactly `n = 5` succeeds on the five-destination route.
4. Successful landing after real bridge completion reveals the Fragment of Calling. Collect with E; collection enables the reward-location return to the hub.
5. **Ruin 2:** study its book and standalone guided return simulation, explicitly Finish, then answer six staff-restoration questions. The initial call-down is paced by Continue; four return actions drive Air, Earth, Fire, Ice restoration cinematics.
6. Use the **original Ruin 2 return stone**. Before staff completion it reports requirements. After the full staff sequence and empty stack, it transports the player to Magical Farm.
7. Solve and watch three harvest rows. Return from farm to the ritual chamber. Collect the Fragment of Returning Magic there with E; staff completion alone no longer reveals it.
8. The same original stone now returns to MainHub. At the Final Portal, E reports missing fragments or restores the arch when Ruin01 and Ruin02 have both been collected.

The two ruin fragments can be collected in either order; there is no requirement in the inspected portal gate that Ruin 1 precede Ruin 2. Ruin III travel exists, but a third lesson/reward path is not implemented in the inspected first-party gameplay code.

## 5. All identified recursion lessons and challenges

| Lesson / challenge | Learning objective and implementation | Status |
| --- | --- | --- |
| Ruin 1 three-page Magic Book | Recursion, recursive calls, changing arguments; `buildBridge(3)` introduction | Implemented; separate from guided UI |
| Bridge guided simulation | 22 immutable checkpoints, code highlighting, parameter frames, base/return/empty stack; 2D illustration | Implemented, UI-only; Finish gates practice |
| Bridge practice | Six decisions: action-before-call at 3; call with 2 while caller keeps 3; place 2 then call 1; place 1 then call 0; base return; unwind without replacing stones or returning a number | Implemented; three world repairs |
| Magic Jump guided simulation | Eight checkpoints using `magicJump(3)`; no leaps on descent; three illustrative return leaps | Implemented; separate example, not the real route |
| Magic Jump challenge | Input integer 1..6; `magicJump(n-1)` before each leap; relate argument to leap count; retry too-small/too-large attempts | Implemented; only 5 succeeds |
| Ruin 2 three-page Magic Book | Waiting callers and work after the recursive call; staff order 1..4 | Implemented; no world mutation |
| Staff guided simulation | `sealMagic(4)`, 28 checkpoints, calls 4..0, then `absorbMagic(1..4)` | Implemented; Finish gates normal interaction |
| Staff practice | Six typed operations: `sealMagic(3)`, `return`, then `absorbMagic(1)`, `(2)`, `(3)`, `(4)` | Implemented; deterministic answers, AI/local hints on mistakes |
| Farm row 1 | Fill base condition: `n == 0` (also accepts `0 == n`) | Implemented |
| Farm row 2 | Fill recursive argument: `n - 1` | Implemented |
| Farm row 3 | Fill occupancy check: `hasCrop(row, n)`; skip empty plots while still visiting them | Implemented |
| Legacy Portal base-case quiz | `restoreMagic(3)`, `CodeValidator` accepts `n==0` or `0==n`, whitespace and optional colon | Legacy implementation; replaced at the active Final Portal |
| Legacy countdown stack prototype | `SimulationController`, `CommandInput`, `CallStackBlock`; commands containing call/base/return | Prototype code remains; not established as part of the current progression |
| Ruin III | Gateway and return route; `Ruin03` inventory ID exists | Partially implemented scaffold; lesson and reward absent |

C++ snippets are teaching content. The game does **not** compile or execute player-supplied C++. Bridge/staff answers authorize deterministic snapshot ranges; farm input uses a narrow string validator. Staff matching ignores whitespace, case, and one trailing semicolon. Farm matching is case-sensitive, allows whitespace and supported balanced grouping, and rejects unrelated expressions; it is not a general C++ parser.

## 6. Simulation Guide and simulation separation

No standalone source class or saved object named exactly `Simulation Guide` was found. The closest implemented systems are the three **Guided Simulation** interfaces above. Clarify the intended product label before creating a duplicate guide system.

- Books intentionally contain three introduction pages. Old reports describing a fourth simulation page predate the separation.
- Bridge and staff guided panels each own their own `RecursionPlayback`; Next, Previous, Replay, and the checkpoint slider affect teaching snapshots and diagrams, not the real world.
- Reaching the last checkpoint enables Finish. Only `CompleteLesson()` at the final checkpoint marks the lesson completed. Escape/Close does not substitute for Finish. A slider jump to the final checkpoint is permitted by the present teaching player; visiting every checkpoint is not enforced.
- `Ruin01JumpGuidedSimulation` is a separate eight-step example with a UI orb, four stack labels including the zero frame, and three preview landings. Finish requires the return preview to have completed. It does not move the Wizard, restore bridges, or award fragments.
- The farm has its own world simulation and console; no separate farm tutorial/Simulation Guide was found. Its stale ready/debug message still references the H2 Editor start menu.

## 7. Latest Magical Farm implementation

Primary files: `RecursiveHarvestLesson.cs`, `Ruin02FarmSimulation.cs`, `Ruin02HarvestConsole.cs`, `Ruin02FarmAccess.cs`, `Ruin02ReturnGate.cs`, and `Ruin02FragmentReveal.cs` under `Assets/Development/Recursion`.

**Implemented and wired:** `Ruin02_MagicalFarm` has its simulation, console, camera marker, farm entry and return references. The simulation requires an exact `Row01..03/Plot01..10` hierarchy with a `Soil` MeshRenderer at each plot and optional child `Carrot`. Initialization rejects a total other than fifteen carrots. These names and counts are runtime contracts.

For each of three rows, `RecursiveHarvestLesson.Create()` clones crop occupancy, descends from `harvest(row,10)` to zero, then emits return steps for plots 1..10. Only return steps visit soil and collect crops. Empty plots still increment `PlotsChecked`; they do not increment `CropsHarvested`. Occupied carrots rise, shrink, then deactivate. A property block highlights soil without editing shared materials. Each row ends with stack depth zero. The final invariant is **3 rows, 30 plots, 15 crops, empty stack, stopped playback**.

The console presents one missing expression per row. A correct answer runs only the next row; a wrong answer gives local guidance and changes no progress. Enter submits; Escape/Close during playback queues closing until the row finishes. Partial row completion survives closing, returning to the chamber, and re-entry within the same session. Disabling an unfinished simulation resets its plots/history/counters coherently; there is no save/load system. `StartSimulation()` remains an Editor/test-oriented API that can run all rows; ordinary console play uses `StartNextRow()`.

The console temporarily disables real movement and `CameraFollow`, places the existing camera at `FarmCameraView`, uses the left 65% viewport at FOV 66, and displays the input panel on the right. Close restores position, rotation, viewport, FOV, cursor, and prior control states. Saved polish includes sky/panel backgrounds and a world status backing; visual readability is untested in this audit.

H1 through H5 Editor tools represent stages of installation, simulation, console, access, and reward gating. **Do not rerun older installers as a repair procedure.** Later single-rock navigation supersedes H4's independent chamber entrance:

- `Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway` retains a **disabled** `Ruin02FarmAccess` for prerequisite/destination references.
- That disabled component is intentionally not registered as an E target. `Ruin02ReturnGate` references it as data.
- `Ruin02_MagicalFarm/FarmReturnPoint` has an enabled return-only `Ruin02FarmAccess`, registered once.
- The old gateway label and landmark are archived, not missing live gameplay.

Developer Mode's Farm teleport bypasses travel prerequisites for inspection; it does not complete staffs or grant a fragment. Even legitimately finishing the farm after such a teleport cannot meet Ruin 2's combined reward condition without staff practice/restoration.

## 8. Interaction, UI, movement, and teleportation

`InteractionHintManager` on the scene root has `coordinateInteractions = 1`, **17 serialized target entries**, and **12 modal-panel entries**. Entries may be deliberately disabled. Its shared `Canvas/InteractionHint` is the active hint; the older RuinInteractionHint is archived.

Selection normally uses nearest eligible in-range target with stable list-order ties. `ISharedInteractionPriority` overrides selection for eligible targets: Jump Rock priority 100, farm/return gate priority 20. `SharedInteractionPressGate` consumes at most one E edge per frame before invoking the chosen target. A visible modal, unlocked cursor, or `allowCursorLock = false` blocks shared input. Update dispatches; LateUpdate refreshes/hides stale hints after teleports or UI changes. Preserve this coordination instead of adding independent E polling.

`PlayerMovement` uses CharacterController, camera-relative WASD, Shift running, gravity, and `ResetVerticalVelocity()`. `CameraFollow` handles mouse orbit in LateUpdate. `PlayerAnimation` drives locomotion; the jump controller owns Running Jump animation during its sequence, suppresses root motion, and cleans up its temporary platforms/trail.

`TeleportManager.TeleportTo()` disables the CharacterController, sets position/rotation, then enables it. Generic `RuinEntrance` adds distance/shared interaction. Farm wrappers additionally restore the controller's prior state, reset vertical velocity, shift the camera by the teleport offset, and sync transforms. Generic teleport itself does not provide those safeguards; verify legacy routes for camera lag or falling after a transition before changing it globally.

Current Ruin 2 stone state machine:

| State | E behavior at original return stone |
| --- | --- |
| Staff practice/restoration unfinished | Toast: restore all four staffs and finish restoration |
| Staff solved, empty stack, four effects finished; farm incomplete | Teleport to `FarmEntrySpawn` |
| Farm complete, Ruin02 fragment uncollected | Remain in chamber; prompt to collect fragment |
| Ruin02 fragment collected | Teleport to original hub destination |

The underlying Ruin 2 `RuinEntrance` is intentionally disabled/unregistered; the wrapper owns E. Ruin 1's old return-rock `RuinEntrance` is also saved disabled. Its reward collection point contains a separate return entrance, enabled only after fragment collection. Do not enable old entrances to make a missing prompt appear.

The Developer panel opens with Tab in Editor Play Mode, contains hub/Ruin1/Ruin2/farm teleports, a session-only Jump prerequisite override, and Close. The old Reset button was replaced by Farm teleport; reset APIs remain. The component hides/disables itself in player builds. Teleporting alone does not unlock a challenge, complete a lesson, or award rewards.

## 9. Fragment rewards and Final Portal

`Assets/Development/Progress/FragmentProgressTracker.cs` is one explicit scene-session tracker. It stores known IDs in a HashSet, rejects unknown IDs and duplicate collections, and raises collection events. There is no PlayerPrefs/file persistence. `Ruin03`, `AllThreeCollected`, and `AllFragmentsCollected` remain compatibility/general inventory concepts.

**Current Final Portal policy is specifically Ruin01 AND Ruin02, displayed as 0/2, 1/2, 2/2.** A Ruin03 fragment cannot substitute. Do not change the gate to `CollectedCount >= 2`, `AllThreeCollected`, or the all-fragments event.

Ruin 1 reward requires real bridge completion plus the jump's successful-landing event. It reveals once and enables collection only in range with the gem visible and practice closed. Collection hides the gem, shows the altar, enables the collection-point hub return, and shows the shared toast.

Ruin 2 reveal requires all of the following simultaneously:

- Staff practice `IsSolved`, a non-null session, and zero remaining frames.
- `RestoredCount == 4`, every staff ID 1..4 restored, and no active restoration cinematic.
- Farm `IsComplete`, not running, `CompletedRows == 3`, `PlotsChecked == 30`, `CropsHarvested == 15`, and `StackDepth == 0`.

It listens to practice completion and farm completion; a coroutine waits for staff effects to finish. The fragment remains in the **ritual chamber**, not on the farm. Collection is a separate E action; completing the farm does not increment the tracker automatically.

`FragmentRewardToast.Show()` shares `Canvas/FragmentRewardToast/Message` for rewards and requirement messages, attaches one timer to the existing parent panel, and hides it using unscaled time. Overlapping callers replace text/reset the same timer. A missing toast reference silently prevents display, so inspect visibility and reference wiring separately from actual collection state.

The active `MainHub/BrokenPortal` has enabled `FinalPortalInteraction` and disabled legacy `PortalInteraction`. `PortalRestorationManager` has `requireFinalPortalFragments = 1`, the shared tracker, two ruin targets, broken/complete arch references, no challenge panel, empty optional ruinEffects, and no optional completePortalEffect. E with missing fragments shows requirements; with both fragments it calls `RestoreMagic()`. The controller also checks fragments internally, latches at sequence start, activates available ruin glows, and swaps the arch once. Missing optional FX references are not by themselves a bug.

**Partially implemented endgame:** arch restoration exists. No inspected runtime code implements traversal through the restored portal, a next scene, an ending screen, or research completion recording. Those must be scoped before implementation.

## 10. Important scripts and dependencies

All paths in this table are relative to the project root.

| Location / scripts | Responsibility and dependencies |
| --- | --- |
| `Assets/Development/Recursion/RecursionTrace.cs` | Pure immutable snapshots, phases, playback, bridge trace; no Unity/world/reward dependency |
| `Ruin02ReturnLesson.cs`, `RecursiveHarvestLesson.cs` in the same folder | Pure staff and harvest execution models |
| `BridgePracticeLesson.cs`, `Ruin02PracticeLesson.cs` | Deterministic questions/sessions, stale/busy protection, approved advancement, completion invariants |
| `RecursionPanelView.cs`, `BridgeDiagramView.cs`, `Ruin02StaffDiagramView.cs` | Code/stack/diagram presentation; guided navigation and frame display |
| `SpellbookRecursionSimulation.cs`, `Ruin02SpellbookSimulation.cs`, `Ruin01GuidedSimulationUI.cs`, `Ruin02GuidedSimulationUI.cs` | Independent teaching playback and explicit Finish prerequisite |
| `BridgePracticeController.cs`, `BridgePracticePanel.cs`, `BridgePracticeCamera.cs` | Connect six decisions to world repairs, pause controls, focus camera, preserve close/reopen progress |
| `FloatingBridgeMotionRig.cs`, `FloatingIslandMotion.cs` | Explicit 3/2/1 module mapping, broken/repaired poses, practice ownership and collision gating |
| `BridgeSnapshotProjection.cs`, `BridgeWorldView.cs`, `BridgeStoneView.cs`, `RecursionWorldView.cs`, `RuinRecursionVisualizer.cs` | Earlier snapshot/world demonstration support; do not confuse legacy Cube visualizer with current bridge practice |
| `Ruin01MagicJumpController.cs`, `Ruin01MagicJumpTrigger.cs`, `Ruin01JumpTrail.cs` | Recursive five-segment player motion, transient platform effects, animation/trail lifecycle and failure recovery |
| `Ruin01JumpGuidedSimulation.cs`, `Ruin01FragmentReward.cs` | Jump example UI and legitimate Ruin 1 reward authorization |
| `Ruin02PracticeController.cs`, `Ruin02PracticePanel.cs` | Typed staff operations, Continue-paced initial descent, cinematic/modal state, asynchronous hints |
| `Ruin02AirRestoration.cs`, `Ruin02CinematicCamera.cs` | Despite the legacy name, data-driven restoration of all four staffs; projectile/camera sequences and per-instance materials |
| `Ruin02BookInteraction.cs`, `Ruin02SpellbookUI.cs` | Ruin 2 book interaction and three-page introduction |
| `MagicBookIdleMotion.cs`, `FloatingIslandMotion.cs` | Authored book motion and controlled bridge motion; preserve independent responsibilities |
| Farm and reward files in sections 7 and 9 | Farm simulation, local expressions, travel prerequisites, chamber fragment reveal |
| `RuinDeveloperPanel.cs` | Editor-only test shortcuts; requires explicit player, camera, destinations, jump controller, UI and hint manager |
| `Assets/Development/Interactions/*.cs` | Shared target/priority interfaces, candidate selection and E-press consumption |
| `Assets/Development/Progress/*.cs` | Shared tracker, reusable toast, Final Portal interaction |
| `Assets/Script_Level1/PlayerMovement.cs`, `PlayerAnimation.cs`, `CameraFollow.cs`, `CursorManager.cs` | Core player and UI cursor/control ownership |
| `TeleportManager.cs`, `RuinEntrance.cs`, `InteractionHintManager.cs` in the same folder | Player relocation, opt-in shared travel interactions, centralized hints/dispatch |
| `AncientSpellBookUI.cs`, `KnowledgeInteractable.cs` | Ruin 1 introduction book and its interaction; file/class capitalization differs intentionally |
| `BaseCaseChallenge.cs`, `CodeValidator.cs`, `PortalInteraction.cs` | Legacy base-case portal quiz; do not replace current fragment gate with it |
| `AIFeedbackManager.cs`, `PortalRestorationController.cs`, `RuinGlow.cs` | Shared endpoint, restoration policy/sequence, ruin lighting |
| `Assets/Scripts/{SimulationController,CommandInput,CallStackBlock}.cs` | Earlier countdown stack prototype |
| `Assets/Development/Recursion/Editor/*.cs` | Scene installers, audits and bespoke Play checks; many assume exact historical paths |
| `Assets/Development/Environment/Editor/RuinOneDressing.cs` | Scoped Ruin 1 environment tooling |

Bridge movement uses independent pose markers and kinematic geometry. Walkable colliders are disabled while broken/moving and enabled only at a settled repaired pose. Practice claims its modules so old Repair/Reset tools cannot move a bridge under the player. Staff VFX disable vendor movement, rigidbody simulation, and colliders on instantiated runtime projectiles only, preserving source prefabs.

## 11. AI feedback backend and integration

**Implemented Unity integration:** scene `AIFeedbackManager.backendURL` is `http://127.0.0.1:8000/feedback`. The legacy client posts JSON `{ playerAnswer }`, waits up to 15 seconds, and supplies a local unavailable message on transport failure. It expects a `feedback` string.

Ruin 2's `Ruin02FeedbackClient` references that same manager, with a saved timeout of **8 seconds** (source default 10). It posts `playerAnswer`, `ruinId`, `currentStep`, `currentN`, `phase`, `expectedOperation`, `attemptCount`, and `functionCode`. A live response must include nonempty `feedback` and `source: "openai"`. It classifies offline, HTTP, timeout, malformed/unacceptable JSON, pending, local fallback, and live AI states. Output is trimmed to three sentence ends / roughly 350 characters.

`Ruin02PracticeController` validates correctness locally first. For mistakes it can give syntax guidance immediately, otherwise requests AI advice with an attempt-aware fallback. Request versioning and current-question checks reject stale replies after closing or advancing. It rejects bridge-topic advice and some first-attempt answer leakage. AI never authorizes advancement, inventory, restoration, or rewards. Farm and bridge practice use local feedback; there is no farm AI request path in the inspected code.

**Backend source found outside this repository:** `E:\AIBackend\server.py`. It is byte-identical at audit time to `LocalBackups/Ruin2AI_20261101/server.proposed.py`. The directory contains a local virtual environment and `.env`; no requirements file was found there. These external files are not delivered by cloning the Unity repository.

Architecture: Unity UnityWebRequest -> local FastAPI `POST /feedback` -> Pydantic validation -> OpenAI Python SDK `client.responses.create()` using configured model string `gpt-4.1-mini` -> JSON feedback -> Unity hint/fallback. `GET /` is a basic service message. The Python source calls `load_dotenv()` and `OpenAI()`; credentials belong only in the external backend environment. No credential values are included here.

The backend keeps the legacy request compatible, adds optional Ruin 2 context with length/range constraints, supplies separate phase-aware tutoring instructions, treats player input as untrusted data, and returns HTTP 502 with a generic detail on provider failures. A typical local startup, after confirming its environment/dependencies, is `python -m uvicorn server:app --host 127.0.0.1 --port 8000` from `E:\AIBackend`. This command was not run during the audit.

**Untested:** current service availability, installed Python package versions, credential validity, provider/model availability, live hint quality, latency, and packaged-build connectivity. Do not run `probe_backend.py` casually during a read-only audit: it makes feedback POSTs that can invoke paid inference. Loopback is local to the running player's machine; distributing a build does not distribute or start this service. Deployment, authentication, rate limiting, dependency pinning, and request-length alignment need separate work if distribution is intended. Unity's `insecureHttpOption` is saved as 0; verify the local HTTP path in a player build rather than assuming Editor behavior carries over.

## 12. Unity MCP integration audit

> **Superseded 26 November 2026.** The original finding below ("no integration") was correct at the original audit time but is now outdated. Current verified state:
>
> - Package `com.coplaydev.unity-mcp` (MCP for Unity, CoplayDev) added to `Packages/manifest.json` as git URL `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`; lock hash `81542a7e86292b5c4c21b1782c5af23b567f45ce`; cached as `Library/PackageCache/com.coplaydev.unity-mcp@4384f8b518e8`. **Uncommitted** and tracking the moving `#main` branch (not pinned). Pinning/committing is a pending owner decision; do not change the version without instruction.
> - The package registered itself with Claude Code using HTTP transport at user/client level; no project `.mcp.json` or `.claude/` folder exists. Its local server listens on `http://127.0.0.1:8080` (distinct from the AI backend on port 8000).
> - Verified via MCP: one connected instance `FYP@4ec12a780d9662b5`, Unity `6000.0.44f1`, project root `E:/FYP`, active scene `Assets/Scenes/MagicWorld_Development.unity`, Edit mode, not compiling. Console showed 0 errors and only two vendor CS0618 warnings (`GabrielAguiarProductions/.../SaveParticleSystemScript.cs`).
> - Tools include scene/hierarchy/component reads and edits, console, menu execution, Play Mode control and **arbitrary in-Editor C# execution** (`execute_code`). Treat execute_code and menu execution as mutating; use read-only resources/queries first and Undo for edits, per `CLAUDE.md`.
>
> Original audit text follows for history.

**No existing Unity MCP integration was found in the inspected project/session (original audit).**

- No Unity MCP package appears in the manifest/lock or local PackageCache inventory; no corresponding bridge/server source was found under Assets.
- Project `.mcp.json`, `.claude.json`, `.claude/settings.json`, and `.vscode/mcp.json` are absent. No root `AGENTS.md` was found.
- The current agent tool inventory exposes no Unity MCP tools.
- Selected local Codex MCP configuration contains Node REPL and an unrelated Unreal entry; these are not Unity connections. No Unity server is configured there. Unrelated configuration values are intentionally omitted.
- The local Unity Editor log had no MCP/Coplay matches at inspection time. This is supporting evidence, not proof about every possible user profile or external tool.

Historical custom Unity Editor checks and installation reports prove that tooling existed for earlier work, but they do not identify an installed Unity MCP transport. Do not invent a Unity server name, port, tool schema, or claim that an active Unity connection is ready for Claude Code. A separate client/global configuration not inspected here could still exist.

For future MCP use: first establish the supported bridge and actual client configuration, obtain read-only scene/hierarchy/Inspector/Console state, verify the connected project and Unity version, and inspect Edit/Play and dirty-scene state before actions. Add/install/configure an integration only within an authorized implementation task; this audit did not add one. Preserve unsaved work, use Undo for edits, and inspect serialized changes afterward. Never treat arbitrary Editor execution or an installer command as a harmless read operation.

## 13. Current development progress and verification history

| Area | Current assessment |
| --- | --- |
| Hub exploration and ruin travel | Implemented; runtime routes untested here |
| Ruin 1 books, guided lesson, bridge practice, animated jump, fragment | Implemented; significant historical targeted checks |
| Ruin 2 book, guide, six staff questions, four VFX restorations | Implemented; current saved camera-reference issue below |
| Farm H1-H5, single-rock routing, combined reward requirements | Implemented and wired; historical verification, no fresh playtest |
| Farm UI polish / Developer Farm teleport | Implemented; historical visual/interaction checks |
| Shared E/modal/toast/progress systems | Implemented |
| Final Portal two-fragment lock and arch swap | Implemented; endgame after arch swap incomplete |
| Ruin III lesson / reward | Not found; gateway/return scaffold only |
| Persistence / research outcome logging | Not found in inspected runtime code; future scope to confirm |
| Unity MCP | Installed (uncommitted, unpinned `#main`) and connected as of 26 Nov 2026; see section 12 |
| CameraFollow references | Five wrong references reassigned and saved 26 Nov 2026; static verification only, Play Mode untested (section 18) |
| Current compilation, player build, full ordinary-player run | Untested in this audit |

Historical evidence, not fresh test results:

- `Reports/Ruin1_PhaseC/Implementation_Report.md` reports 1,469 assertions / zero Console errors; `Landmark_Update.md` reports a later 1,385-assertion run. They describe an earlier hierarchy and stage; some statements about absent rewards or book pages are superseded.
- `LocalBackups/SceneMaintenanceC2_20261119/PlayVerification.txt` records three passing suites: jump guide/real jump/reward; Ruin 2 single-rock farm route and chamber collection; six portal fragment combinations, direct restoration guard, single arch swap and toast timeouts. It records zero play errors.
- `LocalBackups/SceneMaintenanceC2_20261119/ArchiveVerification.txt` records exactly five inactive archived objects, 1,071 prior objects checked, 16,085 reference properties unchanged, and zero world-matrix error.
- `LocalBackups/DeveloperFarmTeleport_20261119/PlayVerification.txt` records Tab/UI teleport/console/modal checks, all farm rows, and no illicit staff/fragment/portal progression.
- `LocalBackups/FarmVisualPolish_20261119/PreviewResult.txt` records farm preview and camera/control/cursor restoration with zero errors.

Use reports for intent and regression coverage, not as proof that this checkout compiles or a human can complete the entire game without shortcuts.

## 14. Known issues, unfinished features, and risk register

### Findings directly supported by current source/serialization

1. **Incorrect camera-component references — fixed statically 26 Nov 2026, Play Mode pending.** The original audit listed four components; re-audit found a **fifth, `AncientSpellbookManager` (`AncientSpellbookUI`, Ruin 1 book)**, with the same wrong reference. All five were reassigned to the real `CameraFollow` (fileID `961739754`) and the scene saved; see section 18. Original text: In `MagicWorld_Development.unity`, `cameraFollow` points to fileID `961739750` on `Ruin01GuidedSimulationManager`, `Ruin02GuidedSimulationManager`, `Ruin02SpellbookManager`, and `Ruin02PracticeManager`. That ID is URP `UniversalAdditionalCameraData` (script GUID `a79441f348de89743a2939f4d699eac1`), not the project's `CameraFollow`. The real `CameraFollow` is fileID `961739754`, script GUID `985f982b48544c444bb153235ddc9700`. Those fields accept generic MonoBehaviour, so the wrong assignment is type-compatible and may compile. Their open/close paths consequently toggle URP camera data while leaving actual follow behavior active. Suspected effects include camera motion during supposedly frozen lessons and rendering changes. Runtime impact must be reproduced before a narrowly scoped Inspector fix; no fix was made here. Jump, Developer panel, and farm console use the actual CameraFollow reference.
2. **No persistent progress.** Fragment tracker, lesson completion, bridge/jump/staff/farm state reset with a fresh scene/session. This is an existing design limit, not proof of failed collection.
3. **Final Portal ends at visual restoration.** No onward travel or ending is implemented in the inspected runtime code.
4. **Third-fragment APIs coexist with a two-fragment finale.** Preserve the explicit current gate; clarify whether Ruin III is future work or out of scope.
5. **Legacy quiz is incomplete/superseded.** `BaseCaseChallenge` still comments that portal activation will be added later. The active finale bypasses that quiz; do not interpret its TODO as instructions to reconnect it.
6. **External backend is not reproducibly packaged with the Unity repo.** Requirements/version locks and a deployment handover are absent from the inspected backend directory.
7. **Farm startup/debug copy still references an Editor H2 command.** Simulation is playable through the console; the message needs a scoped wording review if visible to players.
8. **Historical Editor tools may assume obsolete paths or prerequisites.** Ruin 1 moved under `Ruin01_RelocationRoot`; H4 expects an entrance superseded by single-rock routing. Some checks create report files/backups or manipulate test state. Read the implementation before running them.

### Fragment reward issues: diagnosis before changes

No currently reproduced missing/duplicate-reward bug is claimed by this audit. Historical verification records passing collection and toast behavior. When investigating a report:

- Ruin 1: distinguish a missing Jump hint caused by unfinished bridge/guide from a broken interaction. The C1 hierarchy audit previously diagnosed exactly this prerequisite case. Developer teleport alone does not satisfy prerequisites; the Developer override does not complete the real bridge, so it cannot legitimately grant its fragment.
- Ruin 1: inspect both bridge completion and successful landing before checking gem visibility. The original return rock is not the collection-point return.
- Ruin 2: inspect every condition listed in section 9. Staff completion alone is intentionally insufficient after H5. Wait for the last cinematic and empty stack, then finish all farm rows, return to the chamber, and collect manually.
- Check that every reward and the portal reference the same tracker, that shared E selects the expected in-range target, and that no modal/cursor state blocks input. Confirm the tracker changed even if the toast is hidden or replaced.
- Do not respond by automatically granting fragments, removing farm prerequisites, or enabling duplicate legacy teleport targets.

### Untested risks / pending decisions

- Reproduce the camera-reference issue, input/ESC timing, UI focus, cinematic queued-close behavior, and shutdown/interruption cleanup in current Unity.
- Verify full normal progression without Developer shortcuts, both fragment orders, walkable surfaces and jump failures, farm return/re-entry, and saved scene cleanliness after testing.
- Verify UI layout at 1024x768 (configured default), common widescreen sizes, scaling settings, and farm world-status readability. Historical captures at 1920x1080 are not sufficient.
- Validate shader compatibility, VFX/audio, rendering and performance in a current player build; package/migration reports are historical.
- Define whether save/load, farm-specific guide, Ruin III, endpoint deployment, finale UX, and learning analytics are actually required. No approved pending-task backlog specifying them was found.

## 15. Design decisions to preserve

| Decision | Rationale supported by implementation/comments/history |
| --- | --- |
| Pure lesson/session models separate from world/UI | Deterministic teaching trace; allows presentation and validation without world mutation |
| Guided examples separate from real challenges and books | Replay/slider cannot repair the world or award rewards; explicit Finish establishes readiness |
| Work before vs after recursive call differs across lessons | Bridge demonstrates 3/2/1 action order; staffs/jumps/farm demonstrate return-phase work |
| Local validators own correctness; AI supplies hints only | Prevent nondeterministic progression and keep gameplay working offline |
| One E dispatcher, modal registry, priorities and consumed key edge | Prevent competing prompts, double opening and teleport chaining |
| Explicit scene references and compatibility fields/class names | Preserve serialization and avoid broad rewiring; legacy `Ruin02AirRestoration` now handles four staffs |
| Per-instance property blocks and VFX overrides | Preserve shared/vendor assets while allowing runtime highlights and restoration |
| Bridge ownership and collision gating | Prevent unsafe movement/reset of traversable geometry |
| Original Ruin 2 stone handles progression | One visible route source; old farm gateway retains data without duplicate E handling |
| Reward completion separate from manual collection | Preserve the visible achievement/collection step and subsequent return unlock |
| Two named required fragments at Final Portal | Ruin III compatibility state must not substitute for implemented lessons |
| Session-only Developer bypass, Editor-only availability | Test quickly without contaminating real completion/reward state |
| Five inactive objects archived rather than deleted | Retain history while preserving live references, transforms and registrations |

## 16. Recommended next development steps

1. Read this handover and `CLAUDE.md`, confirm branch/clean baseline, then establish current Unity compile and Console status without running installers.
2. ~~Correct the wrong cameraFollow assignments~~ — done statically for all five on 26 Nov 2026. Remaining: in Play Mode, open/close each of the five panels and confirm `CameraFollow` is disabled while open and restored on close, and `UniversalAdditionalCameraData` stays enabled throughout.
3. Run a complete ordinary-player journey through both ruins and the farm. Capture prerequisite state, exact reward collection events, stone destinations, and Final Portal results. Prioritize any reproducible progression blockers.
4. Review and run compatible current checks (section 17); update path-dependent checks only when their assumptions are proven stale. Retain historical reports rather than overwriting them unnecessarily.
5. Test backend offline/error/live behavior and document a reproducible external backend setup with pinned dependencies and environment-variable names only. Avoid sending paid API requests without an implementation/test scope that includes them.
6. Agree the remaining FYP scope: ending after restoration, whether Ruin III is required, persistence, farm teaching support, and research assessment. Do not build a third fragment simply because its constant exists.
7. Make targeted UI/readability and wording improvements based on current Game-view evidence, then validate a player build and record an updated handover.

## 17. Testing and manual playtesting checklist

These are **pending tests**, not actions performed during this audit. Use Unity 6000.0.44f1, the development scene, and a fresh Play session for baseline progression. Distinguish real player input from direct method calls or injected test progress. Stop Play Mode after each isolated test and avoid saving runtime state into the authored scene.

### Existing targeted check entry points

Read each tool first: some start Play Mode, swap temporary trackers, inject input, manipulate progression, export scenes, or write reports. Older tools may fail because intended architecture has changed.

| Unity menu under `Tools/FYP` | Scope |
| --- | --- |
| `Scene Maintenance C2/Run Gameplay Verification` | Historical combined guide/jump, single-rock navigation, portal verification; writes LocalBackups verification output |
| `Scene Maintenance C1/Run Targeted Play Checks` | Jump tutorial, normal prerequisites and reward |
| `Ruin 1/Phase C/Run Interactive Practice Play Checks` | Bridge decisions, sequential snapshots, motion/collision and crossings |
| `Ruin 1/Jump J2/Run Targeted Play Checks` | Guided jump and input gating |
| `Ruin 2/Rock Navigation/Run Targeted Play Checks` | Current single-stone route and reward |
| `Ruin 2/Magical Farm H3/Run Targeted Play Checks` | Farm expression validation and row playback |
| `Ruin 2/Magical Farm H5/Run Targeted Play Checks` | Combined staff/farm reveal authorization |
| `Final Portal P1/Run Targeted Play Checks` | Named fragment combinations, guard, single visual restoration, toast |
| `Developer Farm Teleport/Run Targeted Play Checks` | Tab panel, actual UI click, farm access and no reward bypass |

Most targeted outputs are written under matching `Temp/.../PlayChecks.txt` folders. The C2 combined runner writes `LocalBackups/SceneMaintenanceC2_20261119/PlayVerification.txt`. Avoid using the older H4 access check as the authoritative expectation for current navigation. There is no claim that `dotnet build` or merely opening Unity Test Runner exercises these custom menu checks.

### Manual checklist

- [ ] Open the intended scene in the correct Editor version; review Console compile/errors and missing-script warnings. Record starting branch, scene dirty state, and Git status.
- [ ] Check WASD, Shift, mouse camera, cursor locking, Escape, and click-to-return-to-exploration behavior.
- [ ] At each hub stone, verify one E causes one teleport and held E cannot trigger a second action. Check all arrival support surfaces, camera recovery, and ruin return destinations.
- [ ] Ruin 1 book: three pages, correct text and button directions, Close/ESC restore controls; no bridge or reward state changes.
- [ ] Bridge guide: Next/Previous/Replay/slider show consistent code/stack; reaching the end or closing early does not unlock practice without Finish. Confirm no world repair occurs in guide playback.
- [ ] During both guides, both books, and Ruin 2 practice, explicitly inspect the real CameraFollow and URP camera-data enabled states. Since the 26 Nov fix, expected: CameraFollow disabled while open and restored on close; URP camera data never toggled.
- [ ] Bridge practice: try wrong answers and rapid/double submissions; answer all six gates. Observe only three repairs in 3/2/1 order, no placement at zero or on unwinding, and final empty stack.
- [ ] Close while a bridge moves; verify close waits for safe settling. Reopen, finish, and walk/run across the full bridge and connector both directions.
- [ ] Jump Rock: fresh incomplete prerequisites suppress the hint; completed bridge/guide enables it only at the supported approach. First interaction opens the eight-step guide. Early/final ESC does not substitute for Finish; later E opens input directly.
- [ ] Jump: reject invalid/noninteger/out-of-range input; try 1, 4, 5 and 6. Only 5 succeeds. Check five animation cycles, temporary platforms, trail cleanup, safe retry position, camera and controller recovery.
- [ ] Ruin 1 gem reveals only after legitimate completion. E collects once, toast expires, count changes, gem hides, reward return unlocks. Repeated E cannot duplicate the fragment.
- [ ] Ruin 2 book/guide teach calls 4..0 and returns 1..4. Finish gates ordinary staff challenge interaction.
- [ ] Staff practice: try blank/syntax/concept mistakes, correct operations with whitespace/semicolon variants, and stale/double submission. Continue controls the initial descent. Verify no staff restoration during descent or zero base case.
- [ ] Observe Air, Earth, Fire, Ice restorations once each, correct projectiles/orbs/materials, cinematic focus and recovery. Close during a cinematic and verify deferred closure; no fragment after staff completion alone.
- [ ] Original Ruin 2 stone before staff completion shows only requirements. After completion it goes to FarmEntrySpawn. Verify old gateway does not compete for E and the rock's collider does not block every approach.
- [ ] Farm console: wrong expressions do not advance; solve `n == 0`, `n - 1`, `hasCrop(row, n)`. Observe descent first, then checks in plot order 1..10 on return; empty plots count as checked.
- [ ] Queue Close/ESC during a row; confirm it finishes, restores viewport/FOV/cursor/controls, and retains completed rows. Return early from farm, re-enter from the original stone, and continue without reharvesting completed plots.
- [ ] Farm final state is exactly 3/3 rows, 30/30 checks, 15/15 crops, stack 0 and stopped playback. No fragment automatically collected on the farm.
- [ ] Return to the ritual chamber and find the revealed fragment. Before collection, original stone reports collection requirements without taking the player back into the completed farm or hub. Collect once; then original stone returns to hub.
- [ ] Final Portal with neither, only Ruin01, only Ruin02, and unrelated Ruin03 combinations remains locked and names missing required fragments. With Ruin01+Ruin02, E restores the arch once without opening the legacy quiz.
- [ ] Collect both ruins in the opposite order in another fresh session. Confirm all progress is intentionally reset between sessions.
- [ ] AI offline/timeout/HTTP/unusable JSON: local fallback appears and input remains usable. Live AI, when separately available: short phase-appropriate advice; no AI-authorized progression; late response cannot replace newer feedback.
- [ ] Developer Tab: hub/Ruin1/Ruin2/farm teleports work only when safe; jump override changes no lesson/bridge/reward flags; modal/cinematic blocking works. Player build has no usable Developer panel.
- [ ] Inspect books, guides, console, input focus, toast and world labels at 1024x768 and widescreen resolutions. Check visual contrast, clipping and button text/direction.
- [ ] Validate a player build's rendering, local backend path if required, full completion, and post-restoration behavior. Record missing ending behavior as scope, not an assumed implemented portal transition.
- [ ] Exit Play Mode, compare scene and Git status with baseline, and record fresh results separately from historical reports.

## 18. Claude Code onboarding and camera-reference fix (26 November 2026)

### Verified environment

| Item | Verified state |
| --- | --- |
| Branch / HEAD | `fyp2-claude-development` / `21a097e` (`farm basic done`) |
| Working tree before this session's edits | Modified `Packages/manifest.json`, `Packages/packages-lock.json` (Unity MCP package added); untracked `CLAUDE.md`, `FYP_HANDOVER.md` |
| Working tree after | Above plus modified `Assets/Scenes/MagicWorld_Development.unity` and untracked `LocalBackups/CameraFollowRefFix_20261126/`. **Not committed or pushed.** |
| Unity MCP | Connected; instance `FYP@4ec12a780d9662b5`, Unity 6000.0.44f1, HTTP transport (section 12) |
| Scene | `MagicWorld_Development` active, 32 roots, clean before edits; Build Settings enable only this scene (Level1/SampleScene present but disabled) |
| Console | 0 errors before and after; two vendor CS0618 warnings only |

### Discrepancies between the original audit and the project

1. **Unity MCP**: original audit found none; it is now installed and connected (uncommitted, unpinned).
2. **Working tree**: original audit reported clean; it now has the uncommitted package change above.
3. **Camera references**: original audit listed four wrong `cameraFollow` references; there were **five**. The omitted one was `AncientSpellbookManager` (`AncientSpellbookUI`, Ruin 1 introduction book).

Re-checked and still consistent with the original audit: InteractionHintManager (`coordinateInteractions = 1`, 17 targets, 12 modal panels); `requireFinalPortalFragments = 1`; AI endpoint `http://127.0.0.1:8000/feedback` and Ruin 2 feedback timeout 8 s; `MagicalFarmGateway`'s `Ruin02FarmAccess` disabled and `FarmReturnPoint`'s enabled; 15 `Carrot` objects; stale farm "H2" ready message at `Ruin02FarmSimulation.cs:43`; `E:\AIBackend\server.py` byte-identical to `LocalBackups/Ruin2AI_20261101/server.proposed.py`; root hierarchy and five archived objects.

### Camera-reference fix

All affected fields are `public MonoBehaviour cameraFollow`, so the URP component was type-compatible. Their open/close code saves `cameraFollow.enabled`, sets it false, then restores it; with the wrong reference this toggled `UniversalAdditionalCameraData` and left `CameraFollow` active.

| GameObject / component | Before | After |
| --- | --- | --- |
| `Ruin01GuidedSimulationManager` / `Ruin01GuidedSimulationUI` | `UniversalAdditionalCameraData` (961739750) | `CameraFollow` (961739754) |
| `Ruin02GuidedSimulationManager` / `Ruin02GuidedSimulationUI` | same | same fix |
| `Ruin02SpellbookManager` / `Ruin02SpellbookUI` | same | same fix |
| `Ruin02PracticeManager` / `Ruin02PracticeController` | same | same fix |
| `AncientSpellbookManager` / `AncientSpellbookUI` | same | same fix |

Already correct and untouched: `JumpChallenge`, `HarvestConsole`, `EditorDeveloperMode`. Bridge practice finds `CameraFollow` by type and is unaffected.

Method: Edit mode only, through Unity MCP. The scene file was first backed up to `LocalBackups/CameraFollowRefFix_20261126/MagicWorld_Development.unity.bak` (identical to HEAD). Each field was set through `SerializedObject` in one Undo group ("Fix cameraFollow references"), only where its current value was the URP component. None of the five objects is a prefab instance. Then the scene was saved. No script, prefab, material, setting or package was changed.

Saved scene diff: 6 lines changed. Five `cameraFollow: {fileID: 961739750}` became `{fileID: 961739754}`. One unrelated serialization-rounding change happened on save: `SpellOrb` RectTransform (`&19361177`) `m_AnchoredPosition.x` went from `-0.000061035156` to `0`. It is harmless, and runtime code positions that orb anyway. `git diff --check` was clean.

### Verification status

- **Static (done):** live in-memory references re-read after the edit. All eight `cameraFollow` fields now resolve to the single `CameraFollow` on Main Camera. The saved YAML diff matches. Console has 0 errors.
- **Play Mode (not done):** not run. Pending: open and close the Ruin 1 book, the Ruin 1 bridge guide, the Ruin 2 book, the Ruin 2 guide and Ruin 2 staff practice (including early ESC/Close, and closing during a staff cinematic). Confirm the camera stays still while each is open, orbit/follow returns after closing, cursor/controls recover, and rendering is unchanged.

### Remaining issues (unchanged scope)

No persistence. The Final Portal ends at the arch swap. Ruin III is a scaffold only. The backend is not packaged or pinned. The farm H2 message is stale. The MCP package is unpinned and uncommitted. No fresh full-playthrough or player-build results exist.

## 19. UI migration status (26 November 2026)

The full specification and decisions are in `UI_STYLE_GUIDE.md` (sections 10–11). This section is a summary for handover.

### Implemented and committed earlier: UI-0 / UI-B

- **Vanilla Caramel SDF** is the unified font.
  - `–` and `—` were added to its static atlas. The GUID is unchanged and it now has 98 characters.
  - `· … •` are unsupported, so they were replaced with `|` and `...` in player-facing strings.
  - AI hint text passes through `FypUiTheme.ForFont()` after trimming.
- **`Fyp.Development.UI.FypUiTheme`** is the shared presentation palette.
- **Scoped 9-slice sprite copies** are in `Assets/Development/UI/Sprites/`. The Bamao vendor sprites are unchanged.
- **The Magical Farm Harvest Console was rebuilt as the reference prototype:**
  - New view component `Ruin02HarvestConsoleView`.
  - The Wizard's renderers are hidden while the console is open.
  - A pedestal-and-spellbook console visual was added, with no colliders.
  - The mirrored return label was fixed, and the sky surround is opaque.

### Implemented this session, UI-C Part 1 (uncommitted at time of writing)

- **Farm refinements:**
  - The status board hides while the console is open and is restored on every close path (`Ruin02HarvestConsole.statusBoard`).
  - The completion card has a medal icon and a "Next" instruction box.
  - Call-stack text is 21 pt, and the active-step tag is shown in the stack heading.
  - The farm camera marker was moved back 8 m, and the FOV adapts to the aspect: 66° at 4:3, 52° at 16:9. The whole farm and its row labels are visible at both.
- **C1 Shared HUD:** the interaction hint is Ink on a parchment pill (`InteractionHintBackdrop` + new `FypTextBackdrop`); the reward toast and locked message are wood banners.
- **C2 Spellbooks:** both books use the unified type scale, a warm dimmer and auto-fitted InkSlate code wells. The Ruin 2 code box was enlarged so the full function fits. Navigation and content are unchanged.
- **C5 World text:** all 35 farm `TextMesh` labels were converted to `TextMeshPro` on the same objects. `Ruin02FarmSimulation.statusDisplay` (TMP) is the runtime status reference; the legacy `debugDisplay` (TextMesh) is kept but null.
  - **Older farm Editor tools that use `debugDisplay` (H1/H2/H4 installers, `FarmVisualPolishTools`) are now stale. Don't run them.**
- **C6 Developer panel:** parchment panel, wood header and paper buttons. Functionality is unchanged.

### Validation and remaining work

- Play Mode checks through Unity MCP at 1920×1080 and 1024×768 found no text overflow and 0 Console errors.
- This was not a human playtest and not a player build.
- **Remaining:**
  - UI-C3/C4 (Guided Simulation and Challenge panels).
  - The `·` in `FinalPortalInteraction.cs`.
  - The pre-existing `Ruin02PracticeController.Close()` teardown `MissingReferenceException` when Play Mode stops while practice is open.
  - Ruin 2 Phase C (Teleport Stone visuals, fragment-reveal notification) and Phase D (farm Simulation Guide) are still pending.

### UI-C3 Guided Simulations (26 November 2026, uncommitted)

- **What changed:** the bridge, magic jump and Elemental Staff guided simulation panels were restyled to the Cozy Fantasy system (`UI_STYLE_GUIDE.md` section 12): parchment panel, accent ribbon header, InkSlate code well with a GoldGlow active line, GoldGlow active stack frame, info box, themed slider and paper/moss buttons.
- **Simulation logic is unchanged:** checkpoints, the Finish gating and challenge unlocking were re-verified for bridge and staff.
- **Opt-in themed views:** shared `RecursionPanelView` and `Ruin02StaffDiagramView` gained an opt-in `useFypTheme`, enabled only on the guided instances. The challenge panels stay legacy until UI-C4.
- **Jump guide:** `Ruin01JumpGuidedSimulation` now takes its presentation colours from `FypUiTheme`, and its TOP frame highlight follows the stack.
- **Untested:** the jump guide's return-leap animation and its Finish (the Editor couldn't be focused for frame advance during testing).
- **Remaining:** UI-C4 Challenge panels.
