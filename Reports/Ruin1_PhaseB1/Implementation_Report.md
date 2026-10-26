# Ruin 1 — Phase B1: Bridge (1) motion prototype

Implemented on `fyp2-development-branch`, in `Assets/Scenes/MagicWorld_Development.unity`. Only Bridge (1) was adapted. No recursion, practice, reward, or other island integration was added.

## Incoming scene and preservation

Unity was in Edit Mode with a clean scene. The current layout had changed since Phase A. The incoming Bridge (1) root was approximately P=(-435.33,35.009,82.68), R=(0,51.08,0), uniform scale=0.5774658. Those root values were preserved. The current arrangement, rather than Phase A's older coordinates, is the repaired state.

A live scene backup was saved as a copy at `Temp/FloatingBridgeB1/LiveBefore.unity`. No scene was reloaded or discarded during installation. The initial Git working tree was clean.

## Scene hierarchy

```text
Bridge (1)                         existing root, transform unchanged
├─ MotionRoot                      new Empty + FloatingIslandMotion
│  ├─ flying_stone_006              existing prefab instance
│  ├─ flying_stone_008              existing prefab instance
│  └─ flying_stone_003              existing prefab instance
└─ Poses                           new Empty, local identity
   ├─ RepairedPose                 new Empty marker
   └─ BrokenPose                   new Empty marker
```

No mesh, renderer, collider, or Rigidbody was added. The original three MeshColliders are referenced by the motion component; new stripped collider-reference records in the scene YAML do not represent additional collider instances. No source Prefab changes were applied.

The existing root's only change is its child list. The three original prefab instances were reparented with world transforms preserved; their local positions now account for the new pivot. Their source meshes, material references, collider settings, and instance identities remain intact. Maximum element-wise world-matrix error during reparenting was 7.152557e-7 (floating-point rounding).

## Transforms and Inspector assignments

| Object / state | World position | World Euler rotation | Scale |
|---|---|---|---|
| MotionRoot in Edit Mode / repaired | (-435.33,43.23788,82.68) | (0,51.08,0) | World (0.5774658,0.5774658,0.5774658), local (1,1,1) |
| RepairedPose | (-435.33,43.23788,82.68) | (0,51.08,0) | Local (1,1,1) |
| BrokenPose / broken MotionRoot | (-457.1,64.75,84) | (10,71.08,-12) | Local marker scale (1,1,1); motion scale does not animate |

`MotionRoot` and `RepairedPose` have local position (0,14.25,0), local rotation (0,0,0). `Poses` has local identity. The mesh pivot is distinct from the MotionRoot pivot: the main flying_stone_003 retains its original repaired world position near (-435.33,35.009,82.68).

The earlier proposed broken position was (-457.1,63.75,84). Its conservative renderer bounds slightly overlapped the neighbouring island bounds in the current scaled layout. Raising it by 1 m clears the endpoint bounds; X/Z and proposed rotation were retained. Actual Unity renderer bounds at the broken endpoint were also verified clear of other active scene renderers.

The component on `Bridge (1)/MotionRoot` is fully assigned:

- Broken Pose → `Bridge (1)/Poses/BrokenPose`
- Repaired Pose → `Bridge (1)/Poses/RepairedPose`
- Start Broken → true
- Move Duration → 4 seconds
- Walkable Colliders → the existing MeshColliders on 006, 008, and 003 under MotionRoot. The list is rebuilt from all children, including inactive children, on Awake.

## New scripts

- `Assets/Development/Recursion/FloatingIslandMotion.cs`: independent runtime visual prototype; smooth position interpolation and quaternion rotation, with collider gating. No input polling, recursion dependency, UI ownership, progression state, material changes, or physics forces.
- `Assets/Development/Recursion/Editor/FloatingIslandPrototypeTools.cs`: Inspector Repair/Reset buttons, read-only audit, and guarded one-time installer. Installer rejects duplicate installations, unexpected children, and non-uniform/negative parent scales. Installation is Undo grouped.
- `Assets/Development/Recursion/Editor/FloatingIslandMotionPlayChecks.cs`: explicit Play Mode regression checks. Runs only when requested from its menu.

Each script has a new Unity-generated .meta file. Existing BridgeStoneView and BridgeWorldView remain unchanged; this prototype does not reuse their Cube-specific BoxCollider assumptions.

## Animation and collision behaviour

Edit Mode stays repaired. On entering Play Mode, Awake moves the complete module to BrokenPose and disables all three walkable colliders. Repair Island moves it to RepairedPose over four unscaled seconds, using smoothstep position interpolation and quaternion Slerp. Repeated Repair calls do not restart the same motion.

All three colliders remain disabled during either motion direction. They enable only after the module settles exactly at RepairedPose. Reset to Broken removes all three collisions before starting the reverse animation and leaves them disabled at BrokenPose. Mid-animation reversal is supported. If a marker or island is externally moved away from the settled repaired pose, collision fails closed. Disabling the component in Play Mode also removes its walkable collision.

No Rigidbody was added, dynamic or otherwise. Non-convex MeshColliders remain on the original mesh instances. No invisible replacement collider remains at the repaired location while the island is broken or moving.

## Unity verification

Automated actual Unity Play Mode results: **7,719 assertions across 1,096 sampled frames, zero Console errors/exceptions/assertions**.

- All three original mesh children are present and move rigidly together.
- Broken startup is exactly at its marker, with all three colliders disabled.
- No module collider ray hits remain at the repaired centre while broken.
- Broken endpoint renderer bounds are clear of other active scene renderers.
- Repair settles at the exact position/rotation and enables all three original MeshColliders.
- Duplicate Repair does not restart the animation.
- Twelve actual Player CharacterController.Move sweeps passed over the repaired module and both adjoining joins: lane offsets -0.6/0/+0.6 m, speeds 4/7 m/s, both directions, approximately Z=66.68..100.68 in the current layout. The production movement component was disabled only during these deterministic 1/60 s collision sweeps and restored afterwards. These were actual Unity controller collisions, not point probes or manual WASD tests.
- Repaired → broken reset passed, including immediate collision removal.
- Mid-animation Repair → Reset reversal passed.
- Child local matrices, root scale, source mesh references, and shared material references remained invariant throughout animation.
- BeforePlay.unity and AfterPlay.unity copies had identical SHA256 hashes, confirming runtime test changes did not persist into Edit Mode.

The Inspector Repair Island and Reset to Broken buttons were also clicked manually through the Unity Editor UI. Scene view showed the island moving as one piece, settling into its original layout, and returning to its floating pose. Play Mode was exited using Unity's Play button; the authored repaired state returned.

Serialized scene comparison confines original-object changes to Bridge (1)'s child list and its three reparented prefab-instance transform overrides. There are no changes to other island groups, PhaseA_EndConnector, Spawn/Return points, Magic Book, Cube demonstration, UI, scene references, or settings. Level1 SHA256 remains `2579FABF54B79971A665C702D0D4045F4FB97E5C4A4361F405C29051CDE0B16E`.

Spawn and Spellbook transforms, references, and original repaired collision layout are unchanged. In broken/moving states this module has no enabled collider that could obstruct them. Shared interaction dispatch and a full Spellbook UI regression were not separately rerun in this pass.

## Manual visual checks / limitations

1. Enter Play Mode and select `Bridge (1)/MotionRoot`.
2. Observe in Scene view; the existing observation camera still belongs to the untouched Cube fallback.
3. Click **Repair Island**, watch the full descent/rotation and the joins, then walk across only after it settles.
4. Keep the player on the fixed island before clicking **Reset to Broken**. This standalone developer prototype does not yet provide a gameplay occupancy guard or pause movement/camera.
5. Confirm the preferred camera framing and perceived speed, especially the two smaller satellite stones and clearance near neighbouring island edges. Endpoint bounds and collider gating passed; exhaustive mesh-to-mesh visual intersection analysis for every intermediate pose was not performed.
6. Exit Play Mode to return to the saved repaired Edit Mode layout. Edit the two pose markers outside Play Mode if you want different authored poses.

No recursion action is connected yet. The intended future mapping is `placeStone(3)` → this module, but neither Spellbook playback nor the six-decision lesson invokes it. Bridge (2), Bridge (3), reward logic, and camera migration require separate approval. No commit or push was performed.
