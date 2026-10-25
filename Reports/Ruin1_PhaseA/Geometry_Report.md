# Ruin 1 floating bridge — Phase A geometry integration

Completed on branch `fyp2-development-branch`, Unity 6000.0.44f1, in `Assets/Scenes/MagicWorld_Development.unity` only. Phase B was not started.

## Live-state preservation

Unity was in Edit Mode with unsaved manual changes. A live scene copy was preserved at `Temp/BridgeGeometryPhaseA/LiveBefore.unity` without reloading or clearing the dirty scene. The final scene was saved through Unity, including those manual changes.

The live scene differed from the previous audit: the fixed `Bridge` root was already at (-432.1,31.4,44.1), scale (1.2,1.2,1.2); `Bridge (2)` was already at (-432.1,32,122.8), scale (1.2,1.2,1.2). The destination `flying_stone_003` was already at (-434.66,17.7,228.8). These are the user's incoming values, not changes made in this pass.

Serialized live-before/live-after comparison found no removed original components. Every original component was identical except `Bridge (3)`'s Transform child list, which gained the new connector reference. Original transforms, MeshColliders, gameplay components, UI, teleports, Spellbook, and Cube demonstration were preserved. Level1 SHA256 remained `2579FABF54B79971A665C702D0D4045F4FB97E5C4A4361F405C29051CDE0B16E`.

## Exact scene edit

One new existing-prefab instance:

| Field | Value |
|---|---|
| Hierarchy | `Bridge (3)/PhaseA_EndConnector` |
| Source | `Assets/ithappy/Fantasy/Prefabs/prop/stair_003.prefab` |
| World position | (-432.1,48.25,173.4) |
| World rotation | (0,180,0) degrees |
| World/local scale | (4,0.8,2) |
| Approximate local position | (0.01745,13.75,9.99998) |
| Local rotation | (0,180.1,0), equivalently (0,-179.9,0) |
| Rendering | Original mesh and shared URP material; no changes to either source |
| Collision | Existing non-convex MeshCollider, enabled, non-trigger; no Rigidbody, extra invisible collider, or source-prefab edit |
| Future ownership | Third animated module, `placeStone(1)` |

The connector is already a child of `Bridge (3)`. In Phase B it must move with that module and be included in its collider enable/disable set. Its collision is enabled now because Phase A tests the repaired state. No permanent independent connector was placed between the fixed starting island and an unrepaired module. No MotionRoot or animation/controller binding was added.

## Before/after geometry

Measurements used actual live Unity MeshCollider raycasts at approximately 0.05 m Z intervals, supplemented by the actual Player CharacterController. The older four gap estimates did not describe the incoming live layout.

| Join | Previous audit | Incoming live scene | After this pass |
|---|---:|---|---|
| Bridge → Bridge (1) | 2.25 m | No sampled centre-corridor gap; user's enlargement already joined it | Unchanged; traversal passed |
| Bridge (1) → Bridge (2) | 1.50 m | No sampled centre-corridor gap | Unchanged; traversal passed |
| Bridge (2) → Bridge (3) | 1.75 m | No sampled centre-corridor gap | Unchanged; traversal passed |
| Bridge (3) → destination | 3.25 m | Approximately 1.00–1.10 m horizontal gap, 2.36–2.48 m rise | Existing stair spans the join; traversal passed |

The stair mesh retains small approximately 0.10–0.20 m openings between some tread surfaces. These are not metre-scale unsupported gaps: the actual 0.6 m diameter CharacterController traversed them in both directions. Typical sampled stair rises were approximately 0.23–0.28 m, within the existing 0.3 m step setting. Natural ravine openings remain beside the walking connection; the entire island perimeter is not a walkable bridge deck.

## Actual Unity Play Mode verification

Controller settings were unchanged: height 1.95 m, radius 0.3 m, centre (0,1.05,0), skin width 0.08 m, step offset 0.3 m, slope limit 45 degrees. Production walking/running speeds remained 4/7 m/s.

1. Baseline: actual Player `CharacterController.Move` sweeps at 1/60 s, three lanes X=-432.1/-433.5/-430.7, both speeds and directions, Z=50..205. All six forward cases snagged at the final cliff. Reverse cases reached the start but dropped off the unconnected ledge.
2. Connector: the same twelve cases all passed, crossing every module and reaching the destination. No falling below the safe route or collision snag was observed. Tests span 2.8 m between outer lane centres, plus the controller radius.
3. Production movement: real Play Mode frames, existing `PlayerMovement.Update`, actual Player controller, virtual W/S and Shift input. All four isolated-input cases passed over the complete 155 m route:

| Case | Result | Time excluding initial settling | Maximum continuous ungrounded interval |
|---|---|---:|---:|
| Walk forward | PASS | 38.73 s | 0.016 s |
| Walk reverse | PASS | 38.73 s | 0.097 s |
| Run forward | PASS | 22.14 s | 0.052 s |
| Run reverse | PASS | 22.13 s | 0.232 s |

The original movement and camera scripts were not edited. Camera follow was disabled only within the temporary Play Mode test and restored by leaving Play Mode. Physical keyboard devices were temporarily excluded from Unity's runtime input test, then re-enabled. No input or project settings were changed. Console error/exception/assert count during the final production test: zero.

An earlier non-isolated input run had one invalid reverse-walk case: the existing shared return interaction dispatched a teleport to MainHub. Its exact input source was not established. This is retained in `ProductionInputFirstAttempt.txt`; the isolated rerun passed. No interaction logic was changed to mask that event.

The raw `maxPivotHeightAboveSurface` metric in the input log is a single vertical ray distance. It can include steep side faces beneath a capsule that is supported by adjacent surfaces; it must not be interpreted as airborne height. Grounding intervals are measured from the actual controller.

## Navigation and remaining checks

The new collider is confined to the final approach around Z=173..183. No new collider overlaps the existing Spawn, Magic Book, or Return Interaction. The full serialized references and transforms of those objects are unchanged. Their interaction actions were not exhaustively rerun in this geometry pass.

Fast descending movement still briefly loses grounding over stairs/slopes (up to about 0.23 s in real-frame testing). No fall or snag occurred, but a manual camera-follow walk/run pass is recommended to judge the feel and visual stair joins. Test diagonal approaches within the connector width; the surrounding ravine edges intentionally remain fall hazards.

Phase A is ready for Phase B planning on the tested walking corridor. Phase B still needs explicit approval, broken-pose sweep/camera tests, collider gating including this connector, and incomplete-repair bypass verification. No changes were made to `BridgeStoneView`, `BridgeWorldView`, RecursionTrace, the six-decision model, rewards, Spellbook simulation, AI, shared assets, lighting, or settings. The original Cube bridge remains a working fallback at its original location.

The temporary Editor diagnostic helper was removed from Assets after testing (archived under `Temp/BridgeGeometryPhaseA`); no runtime test component or gameplay script remains from this pass. No commit or push was performed.
