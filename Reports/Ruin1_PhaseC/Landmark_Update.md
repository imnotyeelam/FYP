# Bridge practice landmark update

## Finding

The interaction is owned by `Ruin01_Area/Gameplay/BridgePractice` (`BridgePracticeController`, enabled at Play Mode startup). Its `interactionPoint` field refers to the Empty GameObject `Ruin01_Area/Interactables/BridgePracticeInteractionPoint` at world `(-435.33, 43.57382, 53.638)`. The shared range is 3 m. `InteractionHintManager` has coordinated nearest-target selection enabled and contains the controller once. The old Cube visualizer is disabled and unregistered. Before this update, the Empty had only a Transform and no visible representation.

The Ruin 1 spawn at `(-440.2, 41.714, 4.6703)` is 49.24 m from the point; the Magic Book at `(-426.37, 43.555, 9.3999)` is 45.14 m away. The read-only Unity raycast audit found supported walkable surfaces along the center approach (`x≈-435.33`, `z=9…53.638`), changing from `Ruin1_Ground` to the fixed `Bridge` platform near `z=50`. This is geometric support evidence; full player traversal from Spawn and Book remains a manual Play Mode check. At the interaction itself, a 7×5 sampled footprint lies on `Bridge/flying_stone_003`.

## Changes

| Path | Change |
|---|---|
| `Ruin01_Area/Interactables/BridgePracticeInteractionPoint/PracticeMagicRock` | New scene instance of existing `Assets/FantasyForest/Prefabs/Landscape/MagicRock_01.prefab`, world `(-433.33, 43.16715, 53.638)`, scale `(1.35, 1.35, 1.35)`. Both source materials use `Universal Render Pipeline/Lit`. Its one collider is disabled on this scene instance to avoid obstructing the approach. Source prefab and shared materials are unchanged. |
| `Ruin01_Area/Interactables/BridgePracticeInteractionPoint/PracticeBeaconLight` | New scene-only cyan Point Light at `(-433.33, 44.66715, 53.638)`, range 7, intensity 1.6, no shadows. |
| `BridgePracticeController.SharedHint` | Incomplete-state prompt changed to the exact text `Press [E] to Restore the Bridge`. Completed-state review prompt is unchanged. |

The rock and light are children of the original interaction marker and have no interaction component. The marker itself, bridge geometry, poses, colliders, six decisions, trace, Spellbook, teleport objects and Level1 are unchanged.

## Verification

The completed Unity Play Mode integration run reported **PASS, 1,385 assertions, 0 Console errors**. It checked the visible marker, disabled decorative collider, enabled beacon, exact shared hint, a single queued E press and held E, Spellbook modal exclusion, movement/cursor recovery, all six decisions, 22 checkpoints, three repairs, and twelve repaired-bridge CharacterController crossings. See `Temp/BridgePracticeC/PlayChecks.txt`. Play Mode before/after scene exports are byte-identical. Scene YAML comparison against the saved pre-install copy found zero removed old records and only the interaction marker's child list changed among 1,318 existing records. Protected Level1, source algorithm, six-decision model, Spellbook code, manifest and project settings hashes remain identical.

## Manual check

After reading the Magic Book, head along the central ground route toward increasing world Z to the fixed first bridge section. Look for the cyan-marked dark stone on the right side of the approach. At about 3 m from the original invisible marker, the shared UI should display `Press [E] to Restore the Bridge`; press E once to open practice. Test walking around the stone, then close with Escape and confirm normal movement/camera/cursor. Because it is decorative and has no collider, it cannot block the repaired route. If the stone is still hard to spot from the Book at the current lighting or camera angle, inspect its visibility in the Game view before adjusting this scene instance or its light.
