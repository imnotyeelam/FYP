# Ruin 1 Phase B2 — complete floating bridge motion

Branch: `fyp2-development-branch`. Unity 6000.0.44f1 / URP 17.0.4. Development scene: `Assets/Scenes/MagicWorld_Development.unity`. Phase C has not started.

## One bridge, four sequential sections

- `Bridge`: fixed starting section, unchanged.
- `Bridge (1)`: existing B1 motion, future `placeStone(3)`, unchanged scene components and poses.
- `Bridge (2)`: new motion, future `placeStone(2)`.
- `Bridge (3)`: new motion including the end connector, future `placeStone(1)`.

The current live scene was inspected before installation. Unity was in Edit Mode with a clean active scene, while the B1 work was already uncommitted in Git. Existing changes were preserved. A live copy was saved at `Temp/FloatingBridgeB2/LiveBefore.unity`; installation did not reload or discard the scene.

## Updated hierarchy

```text
Bridge (2)                         original root transform preserved
├─ MotionRoot                      Empty + existing FloatingIslandMotion
│  ├─ flying_stone_006              existing prefab instance
│  ├─ flying_stone_008              existing prefab instance
│  └─ flying_stone_003              existing prefab instance
└─ Poses                           Empty, local identity
   ├─ RepairedPose                 Empty marker
   └─ BrokenPose                   Empty marker

Bridge (3)                         original root transform preserved
├─ MotionRoot                      Empty + existing FloatingIslandMotion
│  ├─ flying_stone_006              existing prefab instance
│  ├─ flying_stone_008              existing prefab instance
│  ├─ flying_stone_003              existing prefab instance
│  └─ PhaseA_EndConnector           original stair prefab instance
└─ Poses
   ├─ RepairedPose
   └─ BrokenPose

Ruin01_Area/Gameplay
└─ FloatingBridgeMotionRig          new Empty + explicit reference component
```

No mesh, collider, material, Rigidbody, source prefab, or duplicate connector was created. Existing children were reparented with world transforms preserved. The largest element-wise world-matrix differences were 2.384186e-7 for Bridge (2) and 3.051758e-5 for Bridge (3), including the connector: floating-point reparenting tolerance, not a designed layout adjustment.

The original root transforms remain:

| Root | Position | Y rotation | Uniform scale |
|---|---|---:|---:|
| Bridge | (-435.33,33.218,59.638) | 0 | 0.692959 |
| Bridge (1) | (-435.33,35.009,82.68) | 51.08 | 0.5774658 |
| Bridge (2) | (-435.33,33.566,105.09) | 207.9 | approximately 0.692959 |
| Bridge (3) | (-435.33,35.009,128.53) | -0.1 | 0.5774658 |

## MotionRoot / pose marker transforms

All values below are world-space MotionRoot pivot poses, not the underlying main mesh pivots.

| Section | Repaired position | Repaired rotation XYZ | Broken position | Broken rotation XYZ |
|---|---|---|---|---|
| Bridge (1), preserved | (-435.33,43.23788,82.68) | (0,51.08,0) | (-457.1,64.75,84) | (10,71.08,-12) |
| Bridge (2) | (-435.33,43.44067,105.09) | (0,207.9,0) | (-409.33,71.44066,105.09) | (-10,182.9,14) |
| Bridge (3) | (-435.33,43.23788,128.53) | (0,-0.1,0) | (-461.33,69.23788,128.53) | (12,17.9,-14) |

Each new MotionRoot and RepairedPose uses local position (0,14.25,0), local rotation identity, and local scale (1,1,1). The original parent scale is retained; scale is not animated. Pose marker scales are not used for interpolation. Broken poses offset module 2 to the right and module 3 to the left, with independent tilt and vertical clearance. Broken endpoint bounds were verified separate from static scene renderers and each other.

All three modules retain `startBroken=true` and `moveDuration=4` seconds. The saved Edit Mode scene is repaired; Play Mode begins with all three modules broken.

## Connector and collider handling

`Bridge (3)/MotionRoot/PhaseA_EndConnector` is the original `Assets/ithappy/Fantasy/Prefabs/prop/stair_003.prefab` instance. Its repaired world transform is preserved near P=(-435.33,42.94915,134.3047), R=(0,180,0), S=(2.309863,0.461973,1.154932).

The existing runtime FloatingIslandMotion code was reused without modification. On Awake it discovers all descendant colliders, including inactive children. The ownership counts are:

- Bridge (1): 3 original non-convex MeshColliders.
- Bridge (2): 3 original non-convex MeshColliders.
- Bridge (3): 4 original non-convex MeshColliders, including the stair.

All ten colliders are disabled before motion and throughout the broken/moving states. A module's colliders enable only after it settles at its RepairedPose. Reset removes all collisions before moving. The stair therefore moves with section 3 and cannot remain as a static walkable connector. No dynamic or kinematic Rigidbody was added. There are no replacement invisible colliders.

## Stable references for Phase C

The new `FloatingBridgeMotionRig` component at `Ruin01_Area/Gameplay/FloatingBridgeMotionRig` has these serialized assignments:

| Field | Reference |
|---|---|
| fixedStartingSection | `Bridge` |
| placeStone3 | `Bridge (1)/MotionRoot` FloatingIslandMotion |
| placeStone2 | `Bridge (2)/MotionRoot` FloatingIslandMotion |
| placeStone1 | `Bridge (3)/MotionRoot` FloatingIslandMotion |
| destinationIsland | scene-root `flying_stone_003` |

`TryGetSection(int parameter, out FloatingIslandMotion section)` returns the assigned module for 3/2/1. It returns false for 0 or unmapped values. It does not call Repair, advance a lesson, poll input, award rewards, or interact with RecursionTrace. Future code can use these explicit references without searching GameObjects by name.

## Script changes

- **New:** `Assets/Development/Recursion/FloatingBridgeMotionRig.cs` and .meta: references-only bridge mapping.
- **Adapted:** `Assets/Development/Recursion/Editor/FloatingIslandPrototypeTools.cs`: explicit full scene paths with duplicate/missing-sibling rejection; guarded B2 installer; existing per-module Inspector buttons. The installer preserves B1 and rejects repeated installation. It uses one Undo group and preserves world matrices.
- **Adapted:** `Assets/Development/Recursion/Editor/FloatingIslandMotionPlayChecks.cs`: tests three explicit serialized references, variable collider counts, independent movement, full-route traversal, missing-module gating, and source-reference preservation.
- **Unchanged:** runtime `FloatingIslandMotion.cs`, its .meta, BridgeStoneView, BridgeWorldView, RecursionTrace, six-decision model, Spellbook code/UI, AI, and existing interaction scripts.

No Unity CLI, Pipeline package, MCP server, or other dependency was installed. Editor changes were made through the running Unity Editor, not by editing scene YAML.

## Unity test results

Final actual Unity Play Mode run: **PASS — 89,450 assertions, 4,502 sampled frames, zero Console errors/exceptions/assertions during the final run.**

- Three distinct modules and correct 3/2/1 mappings verified; base case 0 maps to no section.
- All three start broken, with all ten unique colliders disabled.
- Each section independently repaired, settled exactly, and reset exactly, without moving the other two.
- Repeated Repair calls did not restart the four-second animation.
- Child local transforms, root scales, source mesh references, and shared material references remained unchanged throughout playback.
- Source prefab instance links were checked in Edit Mode before and after playback: all ten mesh-bearing instances retain their original prefab and mesh asset paths. See PrefabVerification.txt.
- Complete repaired route: **12/12 actual Player CharacterController.Move sweeps passed**, three lane offsets (-0.6/0/+0.6 m), walk/run speeds 4/7 m/s, both directions. Route extended from the fixed Bridge section near Z=59.638 to the destination island near Z=166.3.
- Missing Bridge (1), with the other sections repaired: **144 straight sweeps, 0 crossings**.
- Missing Bridge (2), with the other sections repaired: **110 straight sweeps, 0 crossings**.
- Missing Bridge (3), with the other sections repaired: **174 straight sweeps, 0 crossings**; the connector was disabled and moved with it.
- Missing-module sweeps used supported starting surfaces across X offsets -18..+18 m at 0.75 m intervals, both speeds and directions, without jumping. They verify sampled straight routes, not every possible curved route or external launch exploit.
- Traversals used the actual Player controller and collision geometry at deterministic 1/60 s increments. Production movement was disabled only during each test sweep and restored afterwards. These are not manual WASD runs.
- BeforePlay.unity and AfterPlay.unity copies have identical SHA256 hashes, demonstrating that Play Mode test changes did not persist into the authored scene.

The first test attempt used a prefab-instance query in Play Mode that returned empty source paths. That test-tool assertion was corrected by checking prefab links in Edit Mode before/after playback and mesh references during playback. No prefab was repaired or altered. The initial diagnostic failure is retained in InitialTestToolFailure.txt; historical Console entries were not erased.

## Preservation and existing navigation issue

Scene-record comparison found no removed original objects/components. Original changes are confined to Bridge (2)/(3) child lists, their seven reparented prefab-instance transform overrides, and Gameplay's new rig child reference. Existing B1 scene records, fixed Bridge, Cube fallback, other Ruins, Magic Book, Spawn/Return objects, interaction target lists, scene references, and project/render settings remain unchanged.

`Level1.unity` SHA256 remains `2579FABF54B79971A665C702D0D4045F4FB97E5C4A4361F405C29051CDE0B16E`. `FloatingIslandMotion.cs` SHA256 remains `9812EE440FE288097F52C3E3FF60C82C0294DB8D30A293FA01FF39F12C16E521`.

**Pre-existing mismatch, not changed in B2:**

- Current Ruin 1 Return Point marker: approximately (-436.71,45.865,167).
- Current ReturnInteractionPoint: (-432.5,53.45658,230).

The interaction is approximately 63.6 m from that marker, so it is not aligned with the current return location. B2 does not claim that the current return interaction is reachable from the new far-bank location. This requires a separately approved navigation alignment task; no teleport destination or return object was moved here.

Magic Book near (-426.37,43.555,9.3999) and Spawn near (-440.2,41.714,4.6703) are unchanged. Repaired collision geometry is preserved; broken/moving modules have no enabled collision that could introduce a new obstruction. A full Spellbook/teleport interaction regression was not rerun in this motion phase.

## Manual Play Mode verification

1. Enter Play Mode and observe the four sections as one bridge: the first is fixed; the other three begin offset and tilted.
2. Select `Bridge (1)/MotionRoot`, click **Repair Island**, and wait for Repaired. Repeat for Bridge (2), then Bridge (3).
3. Watch all smaller satellite meshes and the end connector; confirm the preferred camera view, movement speed, rotation, and intermediate clearance.
4. Walk/run across the repaired route in both directions, including the stair-to-destination join.
5. Keep the player on a fixed/safe surface before resetting any module. Confirm each reset removes its walking surface, and that the third reset also removes the connector.
6. Inspect the rig's three reference fields to confirm the intended 3/2/1 mapping. Inspector controls are developer controls, not the six-decision practice UI.
7. Exit Play Mode; the saved Edit Mode layout returns repaired. The existing observation camera still belongs to the untouched Cube fallback and was not retargeted.

The automated suite is available at **Tools → FYP → Ruin 1 → Floating Bridge B2 → Run All Module Play Checks**.

The motion system and stable references are ready for Phase C integration. The existing return-interaction mismatch must be addressed separately for end-to-end navigation. No lesson/controller connection, reward logic, Phase C work, commit, or push was performed.
