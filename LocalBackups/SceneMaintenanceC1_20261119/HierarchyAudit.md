# C1 hierarchy audit — MagicWorld_Development

Read-only inspection of the live scene on 19 November 2026. No objects were renamed, moved, reparented, enabled, disabled or deleted for this audit. The live scene, including manual edits, was copied to `Before_163010475.unity` before testing.

Inspected 1,071 GameObjects, including inactive objects, component enabled states, shared-interaction registrations and serialized scene-object references. No missing-script components were found. References below exclude a component's reference to its own GameObject and internal references within the proposed subtree. Transform parent/child links were excluded from dependency counts. Runtime name lookups and editor tooling were also searched where relevant. Absence of serialized references alone does not establish that active geometry is unnecessary.

## Low-risk archive proposals — do not archive their parents

| Exact hierarchy path | Active self / hierarchy | Purpose | Detected external references | Evidence and archive scope | Risk |
|---|---|---|---|---|---|
| `Ruin01_RelocationRoot/Ruin01_Area/SpawnPoints/L1_Spawnpoint/Cube (2)` | Off / Off | Old spawn placeholder geometry | None | MeshFilter, MeshRenderer and BoxCollider only; already inactive. Archive only the Cube child. `L1_Spawnpoint` is the live destination of `MainHub/Ruin1 → RuinEntrance.destination` and must remain. | Low |
| `Ruin01_RelocationRoot/Ruin01_Area/Interactables/RecursionObservationPoint/Cube` | Off / Off | Old observation interaction placeholder geometry | None | MeshFilter, MeshRenderer and BoxCollider only; already inactive. The parent remains referenced by the disabled legacy visualizer, so propose only the Cube child. | Low |
| `Canvas/RuinInteractionHint` | Off / Off | Superseded standalone hint label | None | TMP label has no detected external references. The coordinated manager uses `Canvas/InteractionHint`, a different live label. | Low |
| `Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway/GatewayLabel` | Off / Off | Retired H4 entrance sign | None | Already hidden when navigation was consolidated into the original Return Magic Rock. No gameplay script on this sign. | Low |
| `Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway/FarmGatewayLandmark` | Off / Off | Retired H4 duplicate entrance rock visual | None outside this visual subtree | Already hidden; child MeshCollider disabled. LODGroup/renderer references are internal. Archive only this visual subtree, never the gateway parent or return spawn. | Low |

These are proposals only. Their current states and transforms were not changed.

## Suspected abandoned prototype objects — visual review required

| Exact hierarchy path | Active self / hierarchy | Position | Purpose/evidence | Detected references | Risk |
|---|---|---|---|---|---|
| `Portal` | On / On | `(-519.2, 0, 1604.7)` | Primitive Cube mesh (built-in mesh fileID 10202), renderer and solid BoxCollider; no scripts. Appears to be an old portal placeholder, not the live `MainHub/BrokenPortal`. | No external serialized references; no exact runtime name lookup found in the source search. | Medium: active visible/collidable geometry; abandonment is inferred, not proven. |
| `Ruin1` | On / On | `(-516, 0, 1537.5)` | Same primitive Cube component pattern; likely old ruin placeholder, distinct from `MainHub/Ruin1`. | No external serialized references or exact runtime name lookup found. | Medium: active geometry; review before any future archive. |
| `Ruin2` | On / On | `(-575, 0, 1653.4)` | Same primitive Cube component pattern; distinct from the live `Ruin02_Area`. | No external serialized references or exact runtime name lookup found. | Medium: active geometry; review before any future archive. |
| `Ruin3` | On / On | `(-463, 0, 1655)` | Same primitive Cube component pattern; distinct from `Ruin03_Area`. | No external serialized references or exact runtime name lookup found. | Medium: active geometry; review before any future archive. |

## Disabled or apparently unused objects that must remain

| Exact hierarchy path | Current state | Dependencies / reason to retain | Risk of archiving |
|---|---|---|---|
| `Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway` | GameObject On; `Ruin02FarmAccess` disabled and unregistered | `Ruin02ReturnGate.farmAccess` still reads its exact Staff prerequisites, Farm destination and console references. Its child return spawn is live. This is retired input, not unused configuration. | High |
| `Ruin02_Area/Gameplay/RitualChamber/MagicalFarmGateway/FarmRitualReturnSpawn` | On | `Ruin02_MagicalFarm/FarmReturnPoint → Ruin02FarmAccess.destination`. | High |
| `MainHub/BrokenPortal` | On; legacy `PortalInteraction` disabled; `FinalPortalInteraction` enabled | Live P1 E target. Disabled legacy component is still referenced by `BaseCaseChallenge.portalInteraction` and persistent Button callbacks. Do not archive the object or legacy component during C1. | High |
| `Canvas/ChallengePanel` | Off | Legacy quiz is intentionally closed, but referenced by `PortalInteraction.challengePanel`, the shared modal list, `BridgePracticeController.otherModalPanels` and the legacy visualizer. Not a safe standalone archive. | High |
| `Ruin01_RelocationRoot/Ruin01_Area/Interactables/ReturnInteractionPoint` | On; `RuinEntrance` disabled | Still in the shared target list; children include the active Jump Rock and Developer approach marker. Disabling or moving the parent would break the Jump anchor. The disabled return component was recorded, not altered. | High |
| `Ruin01_RelocationRoot/Ruin01_Area/Gameplay/RuinRecursionVisualizer` | On; component disabled | Superseded observation interaction, no detected incoming serialized references. However `RuinOneDressing`, `InstallWorldFirstBridge` and `BridgePracticeTools` find it by type or exact path and read its serialized references. Review/retire those editor dependencies before proposing an archive. | Medium–High |
| `Ruin01_RelocationRoot/Ruin01_Area/Interactables/RecursionObservationPoint` | On | `RuinRecursionVisualizer.interactionPoint`; keep even if its inactive Cube child is later archived. | Medium–High |
| `Ruin02_MagicalFarm/HarvestDebugDisplay` | On | `Ruin02FarmSimulation.debugDisplay` is required and updated during H2/H3 playback. Its debug-looking name does not mean it is abandoned. | High |
| `Player/PolyArtWizardStandardMat` | On; Animator enabled; nested legacy movement/controller disabled | Live Wizard visual and Animator. The active player controller belongs to the parent `Player`. Do not archive the visual hierarchy because some components are disabled. | High |

## Protected live systems and environment

Keep `Player`, `Main Camera`, `Canvas/InteractionHint`, `InteractionHintManager`, `EventSystem`, `GameManager`, `FragmentProgressTracker`, `EditorDeveloperMode`, and the active tutorial/input/reward panels and their controllers.

Keep the entire live Jump route under `Ruin01_RelocationRoot/Ruin01JumpRoute`, both Flying Stones, Jump Rock and approach marker, original Guided Simulation, Bridge Practice/restoration, Fragment of Calling and target return. Temporary `MagicTrigger01`–`MagicTrigger05` visuals/colliders are intentionally hidden/disabled outside their runtime segment pauses.

Keep `Ruin02_Area`, `Ruin02_MagicalFarm`, all 30 plots/crops/markers, Staff restoration, Farm console/return, reward and consolidated return rock. Keep `Ruin03_Area`: optional progression does not make its gateway and serialized destinations disposable.

Keep `MainHub`, `PortalRestorationManager`, `AncientSpellbookManager`, `SkyDome`, `Directional Light`, `Ruin2 Environment`, `TerrainGroup_0`, `New Environment`, and `FX_Fog_01` / `(1)` / `(2)`. Environment objects can be visually necessary without serialized incoming references. `Level1.unity` is outside this maintenance scope.

The root separator objects (`分割线-----Manager`, `分割线-----Level1`, `分割线+1`) appear to be organization aids, not gameplay placeholders. They were left untouched.

## Jump Hint diagnosis

The Jump controller, active rock anchor, 2.5 m range, priority 100, single shared registration, surface MeshCollider and J2 modal reference are valid. The rock's current world position is `(-1539.817, 345.180, 182.395)`; the existing Developer approach is `(-1539.817, 345.295, 180.795)` and raycasts onto `flying_stone_003`.

In the fresh Play session both `BridgePracticeController.IsComplete` and `GuidedLessonCompleted` were false. The hint was therefore intentionally ineligible. Developer teleport alone did not unlock it; the session-only testing unlock made it eligible and displayed the correct shared hint at 1.6035 m from the rock. No modal, registration, rendering-alpha, collider or priority fault was found. No gameplay or scene wiring change is justified by that diagnosis.

Raw evidence: `JumpAudit.txt`, `RuntimeDiagnosis.txt` and `HierarchySnapshot.json` in this folder. Targeted normal-progression verification is recorded separately in `E:/FYP/Temp/SceneMaintenanceC1/PlayChecks.txt`.

## Completed Play Mode verification

- Original Guided Lesson and six-question Bridge practice were completed through their existing APIs; no persistent progression flags were injected.
- The shared hint appeared near the current rock only. At `JumpStartPoint`, outside the 2.5 m rock radius, the target was ineligible and an actual E key event opened neither J2 nor Jump Input.
- Actual E near the rock opened J2 first. Previous/Next, early ESC, final-checkpoint ESC, reopening, explicit Finish and subsequent E-to-Input all passed.
- Developer teleport alone did not meet prerequisites. The Editor-only testing unlock made E usable without marking J2 or rewards complete.
- Five animated jumps, platform cleanup, successful landing, player/camera/cursor restoration and shared E Fragment of Calling collection passed. Play errors: 0.
- No runtime gameplay script or scene object was changed. The scene file and jump-controller source match their pre-audit backups. There is no justified hint repair to apply; completing the original prerequisites, or explicitly selecting the testing unlock for that Play session, resolves the observed locked state.
