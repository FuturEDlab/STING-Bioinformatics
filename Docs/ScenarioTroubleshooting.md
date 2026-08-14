# Nothing glows / the scanner won't scan

## Start here — press Play and read the Console

The ScenarioController now prints a **wiring report** the moment the scenario starts
(`Report Wiring On Start`, on by default). You can also run it any time from the
component's context menu ▸ **Report Scenario Wiring**, or the *Print wiring report* button
on the debug panel.

It walks every gate in the scenario and tells you, per task, whether a prop in the scene
can actually satisfy it:

```
=== SCENARIO WIRING REPORT ===
PROBLEM  Context ▸ Focus Channel is empty — NOTHING will ever glow, and every
         ScenarioTarget with 'Require Focus' on will refuse input. Assign EV_Focus.
Found 2 ScenarioTarget(s) in the scene.
  ok     'scanner.pickup' → scanner
PROBLEM  'Wristband' (wristband.scan) uses a DIFFERENT Focus Channel than the
         controller — it will never glow or accept input.
```

Each `ScenarioTarget` also warns on startup if its Focus Channel, Task Channel, or Task Id
is empty. Those three empty slots are silent at runtime, which is exactly why this looked
like a code problem.

---

## The five things that actually cause this

### 1. The Focus Channel isn't wired (glow **and** scanning both dead)

This is the one that produces *both* your symptoms at once, so check it first.

`EV_Focus` has to be the **same asset** in two places:

- ScenarioController ▸ **Context ▸ Highlighting ▸ Focus Channel**
- **every** `ScenarioTarget` ▸ Focus Channel

Without it nothing is ever "the current task", so no prop glows, and every target with
*Require Focus* ticked refuses to be scanned. The report above names any mismatch.

### 2. Emission doesn't work that way in URP (glow only)

This one was my bug. The project is **URP 17.4**, where emission only renders if the
material has the `_EMISSION` keyword enabled — and the `MaterialPropertyBlock` I was using
*cannot* switch a shader keyword. On any material without Emission already ticked, the
highlight was a silent no-op.

`ScenarioHighlight` now swaps in a material instance with emission force-enabled while
glowing and restores the originals afterwards. It also tints the **base colour** by
default (`Base Color Tint`, 0.35), so the pulse is visible even on a shader that ignores
emission entirely.

If it is still too subtle against the room lighting, raise **Intensity** to 3–4, or raise
**Base Color Tint** toward 1. For a guaranteed-visible highlight regardless of shader, put
an outline/halo object in **Show While Glowing** — that just gets switched on and off.

To check a prop in isolation: enter play mode, then use the component's context menu ▸
**Test Glow**.

### 3. The scanner was inert (scanning only)

Two hard blockers, both fixed:

- **`Must Be Held` with no Grabbable.** The scene has only one BNG `Grabbable` in it. If
  the scanner isn't one, `BeingHeld` is never true and the tool was permanently dead. It
  now treats a scanner with no Grabbable as always held, and logs a warning once.
- **No trigger without a headset.** `InputBridge.Instance` is null in the editor without
  VR, so the trigger check could never fire. There are now desktop fallbacks: press
  **E** or **left-click** to scan. Or tick **Auto Scan On Aim** and it fires the moment
  the beam lands on a valid target — the fastest way to test.

I also moved the spherecast origin slightly behind the muzzle. A spherecast ignores
anything already overlapping it at the start, so a bottle pressed right against the nose
of the scanner used to register as nothing.

### 4. Grabbing the scanner doesn't advance the step

Two causes, both fixed:

- **There was nothing to wire.** The setup doc used to say "wire the Grabbable's On Grab
  event". BNG's `Grabbable` has no such field — its grab UnityEvents live on a *separate*
  `GrabbableUnityEvents` component. `ScenarioTarget` now finds the `Grabbable` (on itself,
  a parent, or a child) and watches its `BeingHeld` flag directly, so a grab is detected
  with no wiring at all. If it can't find one it now says so in the console.
- **You picked it up too early.** Grabbing the scanner during the opening narration meant
  you were *already holding it* when step 8 arrived, so there was no pick-up left to
  detect and the scenario waited forever. A target that is already held when its task
  comes round now fires immediately.

### 5. The beam is pointing the wrong way (scanning only)

On an imported FBX the model's forward is often not +Z, so the beam shoots out of the side
or the back of the scanner.

- Select the scanner. A **yellow gizmo** draws the beam and its cone live in the Scene
  view, so you can see exactly where it goes — it turns green when locked onto a target.
- Drag **Aim Rotation** (X/Y/Z degrees) until the beam points out of the nose. No child
  transform needed.
- Faster: put a scannable in front of it and use the context menu ▸ **Aim At Nearest
  Scannable**, which sets the rotation for you.

If aiming is merely *fussy* rather than wrong, leave **Detection Mode** on `Cone` and
raise **Cone Angle**. Cone mode ignores colliders completely and just picks the scannable
closest to the beam axis, so it cannot be defeated by a missing or oddly-shaped collider.

---

## Turn on the debug logging

`ScannerTool` has a **Debug Logging** tick. With it on you get a running commentary:

- `Locked on 'Amoxicillin' (task 'amoxicillin.scan')` — aiming works
- `Beam hit 'Cart', which has no ScenarioTarget on it or any parent` — you're hitting the
  wrong collider
- `'Amoxicillin' refused the scan — the scenario is not asking for 'amoxicillin.scan' yet`
  — aiming and wiring are fine, the story just isn't there yet
- Nothing at all — the beam is missing everything; it's the Muzzle direction or the
  object has no Collider

---

---

# The question panel doesn't appear

## First: is the scenario even reaching it?

The quiz is **step 19**. Getting there needs `methotrexate.scan` and `alert.override` to be
satisfied — so if you have only set up the scanner and the wristband so far, the run stops
at step 13 and everything after it, including the panel *and every remaining nurse line*,
never happens. That single cause explains both "no panel" and "the nurse isn't talking".

Check the debug panel's **Waiting on:** line. If it says `methotrexate.scan`, that is your
answer — press **Space** to satisfy it and keep going, or **Tab** to jump gate to gate.
The startup wiring report also lists every task id with no prop behind it.

## If it *is* reaching step 19

The console logs `[PanelQuestionStep] Showing "What caused this faulty alert?"` when the
step fires. If you see that line and still no panel:

- **An inactive parent.** This was a real bug, now fixed. `ShowSingleQuestion` used to
  switch on the `QP` child only — and `SetActive` on a child does nothing while a parent is
  inactive, which it is whenever you disable the prefab root to keep the panel hidden
  during the simulation. The manager now switches its whole ancestor chain on, and puts it
  back when the panel closes.
- **Context ▸ Question Panel is empty.** The startup report now calls this out explicitly.
- **The panel is in the scene but out of view.** Most likely if its Canvas is still
  *Screen Space – Overlay* while you are in the headset, or if it is behind you. Select it
  in the Hierarchy while the game is running and check the Scene view.

## "The nurse doesn't ask the question"

That one is expected, not a fault. **There is no recorded line for asking the quiz
question** — the script has the narrator speak only the *feedback*, after an answer is
picked, and the question text itself is shown on the panel. That is why `Question Vo` on
`S1_19_Quiz_MethoAlert` is empty.

If you want a spoken prompt, record one and drop it into that step's **Question Vo** list;
it will play as the panel appears, and the step needs no other change.

---

## The most likely non-bug: you were too early

**The scanner does not glow until step 8.** Steps 1–7 are about a minute of narration
before Sarah says "Go ahead. Pick up the scanner." Nothing is supposed to glow before
that — the whole point of the focus system is that only the object you need *right now*
lights up.

So when testing props, don't sit through it:

- **Tab** — skip straight to the next thing the player has to do
- **Space** — satisfy the current gate (or skip the current line)
- **Right Arrow** — force-end the current step
- **F1** — hide the panel

Press **Tab** once from the start and you land directly on `scanner.pickup` with the
scanner glowing.

---

## Quick checklist for one prop

1. Does it have a **Collider**? (No collider, no scan and no mouse click.)
2. `ScenarioTarget`: Task Channel = `EV_BioTask`, Focus Channel = `EV_Focus`, Task Id
   spelled exactly as in the table in `ScenarioWiring.md`.
3. Trigger mode matches how you intend to use it (Scan / Grab / Click).
4. `ScenarioHighlight` on the same object — and check what renderers it picked up. If the
   wristband is part of the patient's mesh, the component will glow **the entire patient**;
   assign just the wristband renderer to **Targets**, or give the wristband its own object.
5. Press **Tab** until the report says that task is the current one, then interact.

## Note on the wristband specifically

If the wristband is not a separate mesh, there is nothing for the highlight to tint on its
own. Two options: give it its own child object with its own renderer and put the target
there, or leave the highlight's renderer list empty and instead put a small glowing
quad/halo in **Show While Glowing** positioned over the wrist. The second is usually
faster and reads better in VR.
