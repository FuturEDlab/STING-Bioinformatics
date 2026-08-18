# Player rig swap — BNG "XR Rig Full Body" → XRI "VR Player"

**Issue:** #132 · **Scene:** `Assets/Scenes/Hospital Room.unity` (the only scene in Build Settings)

The army-guy rig (BNG VR Interaction Framework's `XR Rig Full Body`) is gone from the hospital
scene. Everything that hung off its XR origin now runs off `Assets/Prefabs/Player/VR Player.prefab`,
the XR Interaction Toolkit 3.4.1 rig with the new hands.

---

## Why it was not just a drag-and-drop

The old rig was not only an XR origin. Its root carried BNG's `InputBridge` singleton, its
`PlayerController` child carried the character capsule and all locomotion, its hands carried
`HandController`/`Grabber`/`RemoteGrabber`, and `ScreenFader` sat on the centre-eye anchor.
Eleven of the project's own scripts read from those directly, so deleting the rig would have
silently broken glow, dragging, pick-up, scanning and the scenario's teleport step.

Rather than teach every one of those scripts about XRI, the rig-shaped dependencies were
pulled behind a small adapter, and only the adapter knows what rig is underneath. Swapping the
rig again later is now a prefab change, not a code change.

### New adapter — `Assets/Scripts/Player Rig/`

| Script | Replaces | What it gives the rest of the game |
|---|---|---|
| `PlayerRig.cs` | `BNGPlayerController`, `HandController` | `Head`, `LeftHand`, `RightHand`, `Body`, `BodyCollider`, `Fade`, and `TeleportTo()`. `PlayerRig.Instance` finds the rig in the scene, so nothing has to be wired by hand. |
| `XRInputRouter.cs` | `InputBridge.Instance` | `LeftTriggerDown`, `RightGrip`, `XButtonDown`, … Bindings are written in code against `<XRController>{LeftHand}/…`, so the same call works for a Quest controller and for the simulator's virtual one. |
| `ScreenFade.cs` | `BNG.ScreenFader` | `DoFadeIn()` / `DoFadeOut()` / `FadeInSpeed`. Draws a world-space canvas on the camera — an overlay canvas is not rendered to a headset, and a custom shader would have to be kept out of URP's stripping list. |
| `DesktopSimulatorSpawner.cs` | `BNG.VREmulator` | Spawns the XR Interaction Simulator when no headset is running, and stays out of the way when one is. |

---

## Mouse in the editor, controllers in the headset — one input path

`Desktop Simulator` in the scene holds `DesktopSimulatorSpawner`, pointed at the XR Interaction
Simulator prefab that already ships in `Assets/Samples/XR Interaction Toolkit/3.4.1/`.

- **Press Play with no headset** → the simulator spawns and publishes a virtual HMD and one
  virtual controller per hand.
- **Build to a headset** → `XRSettings.isDeviceActive` is true, nothing spawns, real tracking
  is used.

The point of driving the simulator rather than writing a separate mouse-picking mode is that
there is only one input path to keep working: the rig's tracked pose drivers, XRI's
interactors and `XRInputRouter` all read the simulated devices exactly as they read hardware.
Desktop play therefore exercises the same code that ships.

The simulator does not stand down by itself, which is why it is spawned rather than left
sitting in the scene — on device it would publish a second set of phantom controllers next to
the real ones.

### Desktop controls

| Key | Does |
|---|---|
| Hold right mouse | Look / aim whatever is being manipulated |
| `W` `A` `S` `D`, `Q` `E` | Move |
| `[` / `]` / `H` | Take hold of the left hand / right hand / head |
| `Tab` | Cycle which device the mouse drives |
| `G` | Grip — this is **grab** |
| `T` | Trigger |
| `1` / `2` | Primary / secondary face button (X-A / Y-B) |
| `X` | Full control sheet, in play mode |

Those map onto the project's `InteractInput` options as: `X or A` → `1`, `Y or B` → `2`,
`Trigger` → `T`, `Grip` → `G`. `ScannerTool` also keeps its own **E** / **left-click**
fallbacks, which work with no simulator at all.

---

## What changed in the scene

- `XR Rig Full Body` deleted (23 YAML blocks: the instance, its handles, and six components
  the scene had added onto it).
- `VR Player` added at the old rig's x/z and heading, **dropped to floor level**. The old rig
  was left floating 1.69 m above the floor (`y = 1.81`, room floor ≈ `0.122`) and fell on
  start; it now spawns at `y = 0.15`.
- `XR Interaction Manager` added. XRI creates one on demand, but an explicit one keeps it out
  of the "why did an object appear in my hierarchy" category.
- `Desktop Simulator` added.
- `EventSystem`: `InputSystemUIInputModule` → **`XRUIInputModule`**, so the hand ray can click
  the world-space canvases (EHR terminal, question panels). It keeps mouse and touch input, so
  desktop UI still works.
- The five authored BNG `Grabbable`s (three medication bottles, syringe, scanner) became
  `XRGrabInteractable`. The BNG `GrabbableRingHelper` on the syringe was dropped — per the
  issue, the blue/orange remote-grab rings are not being carried over.

### Re-wired references

| Component | Field | Now points at |
|---|---|---|
| `PickUpGroup` (PickUpItems) | `playerCollider` | rig `CharacterController` |
| `DraggableGroup` (DragItems) | `playerCollider`, `leftHand`, `rightHand` | rig capsule + hand transforms |
| `DraggableGroup` | `playerRb` | **removed** — nothing ever read it |
| `ScenarioController` | `player`, `screenFade` | left empty; resolved from `PlayerRig.Instance` at runtime |
| `ScenarioController` | `playerRig` | **removed** — `PlayerRig` covers it |
| `KeyBoardBehavior`, `TVBehavior` | `leftHand`, `rightHand` | rig hand transforms |
| `ScannerTool` | `beam` | **cleared** — see below |

Every one of these also falls back to `PlayerRig.Instance` in `Awake`/`Start` if the slot is
empty, so a future rig swap cannot silently strand them again.

> **`ScannerTool.beam` was wrong before this change.** It pointed at the *old rig's teleport
> marker* `LineRenderer`, not at any beam belonging to the scanner. Rather than carry the
> mistake over it is cleared; the scanner works without it. If you want a visible beam, add a
> `LineRenderer` to the scanner itself and drop it in.

## What changed on the `VR Player` prefab

- `PlayerRig` on the root, `ScreenFade` on Main Camera.
- **`GravityProvider`** on `Locomotion`. The old rig had BNG's `PlayerGravity`; without an
  equivalent the new rig just hovers wherever it is placed. It also keeps the
  `CharacterController`'s centre matched to the tracked camera height each frame, which the
  stock capsule (height 2, centre 0 — half of it under the floor) needs.
- Both direct-interactor objects got:
  - **tags `LeftHand` / `RightHand`** — `Interact` and `Draggable` detect a nearby hand by
    tag, so tagging the new colliders keeps that gameplay working untouched;
  - **a kinematic `Rigidbody`** — Unity only raises `OnTriggerEnter` when one side has a body,
    and props in the `InteractItems` group have theirs deliberately stripped. BNG's `Grabber`
    carried one for the same reason; without it the TV and the keyboard never light up;
  - **layer 2, Ignore Raycast** — the hand spheres sit exactly where the scanner beam and the
    keyboard/TV rays start. On Default they would block those casts. This is where BNG put its
    grabbers, for the same reason;
  - **grab radius 0.1 → 0.15**, matching the reach the props were tuned against.

## Grab behaviour mapping

| BNG | XRI | Why |
|---|---|---|
| `GrabPhysics.PhysicsJoint` | `MovementType.VelocityTracking` | Held objects still collide with the world instead of passing through it |
| `SecondaryGrabBehavior.SwapHands` | `InteractableSelectMode.Single` | The second hand takes the object off the first |
| `RemoteGrabbable = false` | direct interactors only | Unchanged — no far grab |
| `GrabbableRingHelper` | — | Dropped, per the issue |

---

## How this was verified

- **`Assembly-CSharp` compiles clean** against the real Unity reference set (Roslyn, exit 0;
  only pre-existing warnings, most of them from BNG itself).
- **Scene and prefab YAML validate**: 285 scene blocks, no duplicate fileIDs, **zero dangling
  local references**, all 46 `SceneRoots` entries resolve, and each new stripped handle
  resolves to exactly the intended object inside `VR Player.prefab`.
- **Zero BNG components remain** anywhere in `Hospital Room.unity`.
- Each of the five converted interactables was checked to still have the `Rigidbody` and
  collider `XRGrabInteractable` requires.

**Not verified:** play-mode behaviour. The Editor holds the project lock, so nothing here has
actually been run. Everything below is structurally correct but is the list worth eyeballing
in the first session:

1. **Grab feel.** Velocity tracking is the nearest match to BNG's joint grab, but damping and
   throw scale are XRI defaults and may want a pass.
2. **Spawn point.** `y = 0.15` was derived from surrounding furniture, not measured against
   the floor collider. Gravity will settle it either way.
3. **UI ray is right-hand only.** The `Ray Interactor` and `Teleport Interactor` live under
   `Right Hand` in the prefab; the left hand has a direct interactor only. That is how the
   prefab was built — flagging it in case both hands were meant to point.
4. **Other scenes still reference BNG.** Only `Hospital Room` was migrated. The scenes under
   `Testing Scenes/`, `Old Main Scenes/` and `Examples/` still hold BNG rigs, and because the
   shared scripts moved to XRI their grab-related Inspector slots there will read as empty.
   None of them are in Build Settings. BNG itself was left in the project, so nothing else broke.

---

## One thing noticed in passing, not changed

The three medication bottles and the syringe carry `GrabStability`, but they sit at the **root**
of the scene rather than under `PickUpItems`. `GrabStability.Start` does
`GetComponentInParent<PickUpGroup>()` and then dereferences it, so on those four it throws a
null reference at startup and the component stays inert — no table snapping, no floor
correction, no player-collision ignore for them.

This predates the rig swap (the parenting is untouched by this change; the only prefab-instance
difference in the scene is the rig itself) and is left alone deliberately: the fix is either
re-parenting those four under `PickUpItems` or making `GrabStability` fall back to the group
the way the other scripts now do, and both are behaviour changes that want their own review.
