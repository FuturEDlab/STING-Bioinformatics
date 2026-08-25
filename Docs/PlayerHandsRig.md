# Player hands: the new XRI rig, and how the game stays rig-agnostic

**Scene to open:** `Assets/Scenes/Testing Scenes/Mohamed Test Scene.unity`

That scene is the Hospital Room scenario running on the new hands
(`Assets/Prefabs/Player/VR Player.prefab`) instead of the BNG "army guy"
(`XR Rig Full Body`). It plays with a mouse at a desk and on a headset with
controllers, from the same scene, with nothing to switch.

`Assets/Scenes/Hospital Room.unity` is untouched and still runs the army guy. Both scenes
share the same gameplay scripts.

---

## 1. Why there is an adapter layer at all

The army-guy prefab was never just an XR origin. It also carried:

| What it carried | Who read it |
| --- | --- |
| `InputBridge` (controller buttons) | `Interact`, `Draggable`, `ScannerTool`, `ScenarioDebugHUD` |
| `BNGPlayerController` + capsule | `Interact` (glow distance), `PickUpGroup`, `DraggableGroup`, `GrabStability` |
| `HandController` × 2 | `DraggableGroup`, `KeyBoardBehavior`, `TVBehavior` |
| `ScreenFader` | `TeleportStep` |
| `Grabbable` on every prop | `ScannerTool`, `ScenarioTarget`, `GrabStability`, `CapsulePro` |

Eleven scripts named those types directly, which is what tied all of them to one prefab.
They now go through one small facade instead, so the same script runs unchanged on either
rig — and a third rig later means editing one file rather than eleven.

---

## 2. The facade — `Assets/Scripts/Player Rig/`

| File | What it is |
| --- | --- |
| **`Rig.cs`** | The one thing gameplay talks to. `Rig.Body`, `Rig.Head`, `Rig.LeftHand`, `Rig.RightHand`, `Rig.BodyCollider`, the button reads (`Rig.RightTriggerDown` …), `Rig.TeleportTo`, `Rig.FadeToBlack`. |
| **`GrabHandle.cs`** | "Is this prop being held?" — answers for a BNG `Grabbable`, an XRI `XRGrabInteractable`, or the desktop mouse. |
| **`PlayerRig.cs`** | Sits on the new rig's root. Resolves head/hands/capsule/fade from the `XROrigin` and does the room-scale-correct teleport. |
| **`XRInputRouter.cs`** | Raw controller buttons via the Input System, bound in code so one call works for a Quest controller *and* the XR Interaction Simulator. |
| **`ScreenFade.cs`** | Replaces BNG's `ScreenFader`. A world-space canvas, so it renders to a headset and needs no URP shader. |
| **`XRGrabBridge.cs`** | Converts any leftover BNG `Grabbable` to `XRGrabInteractable` at runtime, so old prefabs dragged into a new-hands scene still pick up. |
| **`DesktopPlayMode.cs`** | Decides headset / mouse / simulator at startup. |
| **`DesktopMousePointer.cs`** | The mouse interaction layer. |

### How `Rig` picks a rig

One question, asked once: **is there a `PlayerRig` component in the loaded scene?**

* **Yes** → the XRI path. BNG is never touched, and in particular `InputBridge.Instance` is
  never read — that getter *creates* an `InputBridge` GameObject when it cannot find one, so
  asking it "is BNG here?" would answer by manufacturing the thing it was asked about.
* **No** → the BNG path, exactly as before.

Nothing has to be configured for that to happen. A miss is re-searched at most once per
frame, so a BNG scene does not pay for a scene-wide search on every property read.

---

## 3. What the mouse can do

`DesktopPlayMode` on the VR Player root comes up in **Mouse Pointer** mode when no headset is
running, and stands down completely when one is.

| Input | Effect |
| --- | --- |
| **Left click** | Use whatever is under the cursor. On a prop, pick it up. |
| **Left click while holding** | Passes through to the held item — the scanner fires. Does *not* drop it and does *not* swap to another prop. |
| **Q** | Put the held item down. |
| **Scroll** | Push the held item away / pull it closer. |
| **Hold right mouse** | Look around. |
| **WASD** (shift to hurry) | Walk. |

The cursor dot turns **green** over something usable and **blue** while carrying.

None of this is a parallel implementation of the game's rules. A click ends up calling the
same `ScenarioTarget.Activate()` and `Interact.onInteract` a tracked hand calls, and a
carried prop reports itself as held through `GrabHandle` — so the scanner's *Must Be Held*
gate, tasks that complete on pickup, and `GrabStability`'s settle-down all behave exactly as
they do on device.

**Clicking things that are not props.** `Click Completes Scenario Tasks` (on by default)
lets a click finish a task directly on things you cannot pick up — the EHR gates, the
wristband — so the whole scenario can be walked through at a desk. Turn it off when the
scanner itself is what you are testing, and the scanner becomes the only way to scan. Either
way the scenario's focus gate still applies: clicking out of turn does nothing, exactly as
it does in a headset.

### The other desktop mode

Set **Desktop Mode → XR Interaction Simulator** on `DesktopPlayMode` to get Unity's simulator
instead: virtual HMD, virtual controller per hand, driven from the keyboard
(`[` / `]` to take a hand, `G` grip, `T` trigger, `X` for the control sheet). Slower to drive,
but it exercises the exact code path the headset will — use it when the question is
"will this work on device?" rather than "does the scenario advance?".

---

## 4. Pushing to a headset

Nothing to change. Plug in the headset, build, run.

`DesktopPlayMode` detects the headset, switches the mouse pointer off, and hands the rig back
to its tracked pose drivers and XRI's interactors. The one thing worth checking is that
**Mohamed Test Scene** is ticked in *Build Profiles → Scene List* — it was added there next to
Hospital Room.

### How the headset is detected, and why it is not one line

`XRSettings.isDeviceActive` is only true once the XR **display** is running. On a Quest build
XR Management has its loader up before the first scene loads, but the OpenXR session needs a
few more frames to reach a running state — so on device, on frame one, with a headset on the
player's head, that property is routinely still **false**.

The first build to a Quest 3S read it once in `Awake`, concluded "no headset", and started
mouse-pointer mode on the headset. `DesktopMousePointer` switches the head's `TrackedPoseDriver`
off so it cannot fight the mouse for the camera's rotation — so the camera stopped tracking and
stayed at the camera offset, which in Floor tracking mode `XROrigin` moves to y = 0. The result
on device was **eyes at floor level, with the two hands parked in front of them** by
`ParkHandsIfInsideHead`.

So the question is asked differently now:

* **No XR loader at all** — that is a desk. Answered on the first frame, no delay.
* **A loader is up but the display has not started** — a headset mid-launch. Wait for it, up to
  `Headset Grace Seconds` (5 s by default, a couple of frames in practice).
* **A headset arrives after the scene is up** — a watchdog stands the desktop helpers down,
  destroys a spawned simulator, and puts head tracking back.

There is a second lock on the same failure: `DesktopMousePointer` will not switch head tracking
off while `HeadsetRunning`, whatever brought it up. Head tracking is the one thing on the rig a
player cannot give back to themselves.

---

## 5. What changed in the scene

Built from `Hospital Room.unity`, then:

* **Removed** the `XR Rig Full Body` prefab instance and the 23 objects belonging to it
  (including a `Draggable` and a couple of lights that had been added onto the rig in-scene).
* **Added** `VR Player` at `(6.8, 0.15, -11.71)`, heading `-40.64°` — the army guy's exact
  spot and heading, but at floor level. The old root sat 1.15 m up because its capsule hung
  below it; an `XROrigin` in Floor tracking mode *is* the floor, so leaving it at 1.15 would
  drop the player in from head height on start.
* **Added** an `XR Interaction Manager`.
* **EventSystem**: `InputSystemUIInputModule` and BNG's `VRUISystem` out, XRI's
  `XRUIInputModule` in. That one module drives world-space canvases from the hand ray *and*
  from the mouse, so it covers the headset and the desk. Two enabled input modules on one
  EventSystem fight over the pointer, which is why the old one was removed rather than left
  alongside.
* **Rewired** the 13 references that pointed into the old rig:

  | Reference | Now |
  | --- | --- |
  | `PickUpGroup.playerCollider`, `DraggableGroup.playerCollider` | VR Player's `CharacterController` |
  | `KeyBoardBehavior` / `TVBehavior` `leftHand` / `rightHand` | VR Player's Left Hand / Right Hand |
  | `DraggableGroup.playerRb`, `.leftHand`, `.rightHand` (BNG types) | empty — falls back to `Rig` |
  | `ScenarioController` `player` / `playerRig` / `screenFader` | empty — `Rig` resolves the rig |
  | `ScannerTool.beam` | empty (it pointed at the BNG teleport arc's line renderer, which was never a sensible scanner beam) |

The empty slots are deliberate, not oversights: those fields fall back to whichever rig is in
the scene, which is what makes the same prefab work in both scenes.

---

## 6. What changed on the prefab

`Assets/Prefabs/Player/VR Player.prefab` gained:

* `PlayerRig`, `XRGrabBridge`, `DesktopPlayMode`, `DesktopMousePointer` on the root.
* `ScreenFade` on **Main Camera**.
* On **both direct interactors**: tag `LeftHand` / `RightHand`, layer `Ignore Raycast`, and a
  kinematic `Rigidbody`.

Those last three are exactly what the old BNG `Grabber` colliders carried, and they are
load-bearing:

* `Interact` and `Draggable` decide "is a hand near me?" from `OnTriggerEnter` with those two
  **tags**. Without them, nothing glows and nothing drags.
* Trigger events need a `Rigidbody` on one of the two colliders. Pick-up props have one, but
  `Interact`-only props have theirs stripped by `InteractableGroup` — so the hand has to
  bring one.
* Layer `Ignore Raycast` (2) keeps the hand out of gameplay raycasts while still colliding
  with everything: verified against the project's collision matrix, layer 2 collides with
  Default, Grabb, Collidable, UI and Floor.

---

## 7. Things worth knowing about the rig as built

* **Only the right hand has a ray.** `VR Player` carries the Ray Interactor and the Teleport
  Interactor on the right hand; the left has a direct interactor only. So world-space UI —
  the question panels — is clicked with the right hand in a headset, and with the mouse at a
  desk. If left-handed UI is wanted, duplicate the Ray Interactor onto the left hand.
* **`Tools ▸ STING ▸ Set Up VR UI Input` refuses to run on this scene.** That tool installs
  BNG's `VRUISystem` and switches the desktop module off, which is right for Hospital Room
  and would take the panels away from both the hand ray and the mouse here. It now checks for
  a `PlayerRig` and says so rather than doing it.
* **The character capsule's height is runtime-driven.** It is authored at 2 m; XRI's
  `XRBodyTransformer` resizes it to the tracked head each frame. The toolkit's own reference
  rig is authored the same way, so this is normal rather than something left unset.
* **The hand models are rolled 180° from the pose the FBX bind pose suggests.** `Left Hand
  Model` / `Right Hand Model` (BNG's Oculus hand meshes, `l_hand_skeletal_lowres.fbx` and its
  fist/pinch blend tree) sit under the hand anchors at ∓90° about Z. Read straight off the
  prefab's bind pose that looks like a correct hand — fingers along the anchor's forward, thumb
  on its up side, palm inward. On device it came out rolled over: **palms facing outward, thumbs
  underneath**. The bind pose is not what renders — the Animator's blend tree poses the wrist —
  so the roll was corrected against what the headset actually shows, not against the YAML.

  The correction is a 180° roll about the Z axis **through the wrist bone**, not through the
  model root: rolling about the root would have swung each hand ~10 cm to the far side of its
  controller. So both the rotation and the root position moved:

  | | Rotation (Z) | Root position |
  | --- | --- | --- |
  | Left Hand Model | +90° → **−90°** | (−0.001, 0.001, −0.035) → **(−0.099, −0.0344, −0.035)** |
  | Right Hand Model | −90° → **+90°** | (−0.001, 0.001, −0.035) → **(0.097, −0.0346, −0.035)** |

  Every bone's Z is unchanged by that and both wrists land exactly where they were, so the hands
  still sit on the controllers and still point the same way — only the roll changed. If a hand
  ever needs re-rolling, roll about the wrist and move the root to match; do not just flip the
  sign.

* **The hands have an authored rest pose, and it only matters at a desk.** `Left Hand` and
  `Right Hand` sit at `(∓0.18, -0.28, 0.32)` in camera-offset space. A `TrackedPoseDriver`
  positions them every frame *while a device is reporting a pose* — in a headset that
  overwrites the rest pose immediately, so it is never seen there. At a desk nothing reports,
  and hands left at the origin sit exactly where the camera is: a 20 cm hand model at 0 cm,
  filling the view with a couple of enormous fingers. `DesktopMousePointer` also rescues any
  hand it finds within 5 cm of the camera and logs that it did, so this cannot come back
  silently if the transforms are ever reset.

---

## 8. Not ported, deliberately

* **The blue/orange remote-grab rings.** BNG's `GrabbableRingHelper` / `RemoteGrabber`
  visuals. `XRGrabBridge` strips the helper when it converts a prop.
* **Remote grabbing itself.** The new hands grab by touch (direct interactors) plus the right
  hand's ray for UI and teleport.
* **The full-body avatar** (`Soldier_Rig`, `BodyIK`). The new rig is hands-only.

---

## 9. What to eyeball first

**What has been verified so far:** `Assembly-CSharp` compiles clean against Unity 6000.4.9f1
(full batchmode import, no errors); the XRI, XR Core Utils and Input System APIs used here
were checked against the package sources on disk rather than from memory; and the scene and
prefab YAML pass structural checks — no duplicate fileIDs, no dangling references, no orphan
stripped objects, GameObject/component and transform parent/child links agree, and every
SceneRoots entry is a real root. **One build has now been run on a Quest 3S**, which found the headset-detection bug written up
in §4 — the camera was on the floor because the desktop mouse pointer came up on device and
switched head tracking off. That is fixed. The rest of the list below is still what to put eyes
on; nothing past "you start standing on the floor" has been exercised on device yet.

Open **Mohamed Test Scene** and press Play. In rough order of "most likely to be wrong":

1. **You start standing on the floor**, not falling in or sunk in it.
2. **Green dot on hover.** Point at a medicine bottle — the cursor dot should go green.
   If it never does, the props did not get their grab component; check the Console for the
   `[XRGrabBridge] Converted N …` line.
3. **Pick up a bottle**, carry it, press `Q` to drop. It should settle on the table rather
   than roll off.
4. **Pick up the scanner and click** — it should fire. This is the `GrabHandle` path: if the
   scanner refuses, it thinks it is not being held.
5. **The EHR keyboard and the TV** respond. These use the hand-forward rays, so they exercise
   the `KeyBoardBehavior` / `TVBehavior` wiring.
6. **A question panel opens and can be clicked** with the mouse. That is `XRUIInputModule`
   plus `VRPanelAnchor` handing the canvas an event camera.
7. **A teleport step fades and moves you.** That is `Rig.TeleportTo` + `ScreenFade`.
8. **Hospital Room still plays exactly as before.** Nothing in this work should have changed
   it — but it shares the gameplay scripts, so it is the regression to check.

---

## 10. Adding a new prop

Drop it under `PickUpItems` as before. `PickUpGroup` now stamps the *right* grab component
for whichever scene is open — `XRGrabInteractable` in a new-hands scene, BNG `Grabbable` in
the army-guy scene — so there is nothing extra to remember. If you drag in an older prefab
that already carries a BNG `Grabbable`, `XRGrabBridge` converts it at startup and logs that
it did.
