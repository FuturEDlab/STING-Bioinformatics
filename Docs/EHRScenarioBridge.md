# The EHR terminal and the Scenario Controller

The terminal used to run on its own clock. Each screen either sat there for N seconds —
a stand-in for "hold this while somebody talks" — or waited for a button whose only job
was to push its own sequence along. Nothing connected it to the story: the alert could
appear while Sarah was still asking you to scan, and pressing *Confirm override* moved the
screen but told the scenario nothing.

It is now driven by the scenario, in both directions, through one component:
**`EHRScenarioBridge`**, already on the terminal prefab.

```
                         EV_EHR_MethoAlert                 (world beat channels)
   ScenarioController ────────────────────────┐
        │                                     │
        │  EV_Focus  "waiting for             ▼
        │             alert.override"   EHRScenarioBridge ──▶ EHRSequencePlayer.GoToState()
        └────────────────────────────────────▲
                                              │
   EHR button ──▶ TriggerAction("Confirm override") ──▶ raises "alert.override" on EV_BioTask
```

Neither side knows the other exists. The bridge is the only thing holding both names.

---

## The two directions

### 1. Scenario → screen

Each row under **Screens** says *"when this happens in the story, show that screen"*. A row
is cued by one of two things:

| Cue | Fires when | Use it for |
|---|---|---|
| **World Beat** — a `GameEvent` asset (`EV_EHR_*`) | the scenario reaches an *Invoke Scene Event* step and raises it | the screens the story pushes at the player: patient verified, the alert, dosage confirmed |
| **When Waiting For** — a task id (`wristband.scan`) | the scenario enters a *Wait For Task* step and starts asking for that id | the screens that prompt the player to do something |

**Screen** is the **Step Name** of the screen on the `EHRSequencePlayer`, spelled exactly.

Two extra fields sit above the list:

- **Start State** — the screen the terminal opens on.
- **State While Narrating** — the screen shown whenever the scenario stops waiting for a
  task, i.e. while a line of dialogue plays. Shipped as `Paused - VO Active`, the
  *"Paused / Listen to Nurse Sarah / All input disabled"* art. A world beat arriving in the
  same moment always wins, so a screen the scenario just put up is never wiped by this.
  **Clear the field if you would rather every screen simply hold until the next cue.**

### 2. Button → scenario

Each row under **Actions** says *"when this EHR action is pressed, raise that task id"*.
`EHRSequenceActionButton` (or anything else calling `EHRSequencePlayer.TriggerAction`)
still fires actions by name; the bridge turns the name into the `EV_BioTask` id the
scenario's *Wait For Task* step is listening for.

The task is raised whatever the scenario happens to be doing. A `WaitForTaskStep` ignores
an id it is not waiting for, so a press at the wrong moment costs nothing.

---

## What is wired today

Both lists are filled in on `Assets/Prefabs/Functional 3D Objects/EHR/BigScreenFuncitonalEHRTerminal.prefab`,
which is the terminal in `Hospital Room`.

| Story beat | Cue | Screen |
|---|---|---|
| player must scan the wristband | waiting for `wristband.scan` | `User scans wristband` (1.2) |
| patient verified | `EV_EHR_PatientVerified` | `Patient scan accepted` (1.3) |
| player must scan the Methotrexate | waiting for `methotrexate.scan` | `User scans methotrexate` (1.4) |
| teratogenic alert | `EV_EHR_MethoAlert` | `Nurse talks and starts override` (1.5) |
| player must override the alert | waiting for `alert.override` | `User confirms override` (1.6) |
| alert cleared, quiz runs | `EV_MethoAdministered` | `Knowledge Check 1` (1.7) |
| player must scan the Amoxicillin | waiting for `amoxicillin.scan` | `User scans Amoxicillin` (2.1) |
| prescription + keypad | `EV_EHR_AmoxPrescription` | `Amoxicillin prescription` (2.2a) |
| player must enter the dose | waiting for `dose.entered.5000` | `Amoxicillin prescription` (2.2a) |
| player must correct the dose | waiting for `dose.corrected.500` | `Review and correct dose` (2.2b) |
| dosage confirmed | `EV_EHR_DosageConfirmed` | `Dosage confirmed` (2.3) |
| player must scan the Allopurinol | waiting for `allopurinol.scan` | `Scan meds` (3.1) |
| contraindication | `EV_EHR_Contraindication` | `High severity contraindication detected` (3.2) |
| player must override it | waiting for `contraindication.override` | `Confirm override 2` (3.3) |
| meds administered | `EV_MedsAdministered` | `Knowledge check 2` |

Actions:

| EHR action | Task id raised |
|---|---|
| `Scan patient wristband` | `wristband.scan` |
| `Scan methotrexate` | `methotrexate.scan` |
| `Confirm override` | `alert.override` |
| `Scan Amoxicillin` | `amoxicillin.scan` |
| `Fix dosage typo` | `dose.entered.5000` |
| `Confirm dosage change` | `dose.corrected.500` |
| `Scan Allopurinol` | `allopurinol.scan` |
| `Override contraindication` | `contraindication.override` |

---

## Testing it without a headset

Press Play in `Hospital Room` and hold down **Space** on the debug panel. Space satisfies
whatever gate the scenario is blocked on, so the story walks forward — and because every
EHR screen is now cued off that same story, the terminal walks with it. One key takes you
through all eighteen screens in order.

Two switches make it talk while you do that:

- **`EHRScenarioBridge ▸ Report Wiring On Start`** (on) prints a line per cue at startup and
  flags any that cannot resolve — a screen name that does not exist, a row with no cue, a
  screen nothing can ever show.
- **`EHRScenarioBridge ▸ Debug Logging`** logs every screen change and every task raised,
  with the reason: `waiting for 'alert.override' -> screen 'User confirms override'`.

The component's context menu also has **Report EHR Wiring** so you can run the check
without entering play mode.

---

## Changing it

**Add a screen.** Add the step to `EHRSequencePlayer ▸ Sequence Steps` (image + a Step Name
nothing else uses), then add a row under **Screens** naming that step and the beat or task
id that should bring it up.

**Move a screen to a different moment.** Change the cue on its row. Nothing else moves.

**Add a moment the scenario does not have yet.** Every prompt screen rides on a
*Wait For Task* step that already exists, and every pushed screen on an *Invoke Scene Event*
step. If you need a screen at a point with neither, add an `InvokeSceneEventStep` asset with
a new `EV_` channel — see `ScenarioWiring.md`.

**Rename a step.** The cue rows address screens by Step Name, so rename in both places. The
wiring report will name the stale row if you forget.

---

## Things that will bite you

**Step Names must be unique.** Cues resolve by name and take the first match. The five
duplicate names the sequence used to have (`User scans meds` twice, `Next scene` twice,
`Review and correct dose` twice) were renamed for this.

**`Scenario Driven` must stay ticked** on the `EHRSequencePlayer`. With it off the terminal
also runs its own step timers and advances on its own button presses, so the screen has two
drivers and drifts out of the story. The wiring report calls this out as a PROBLEM. Untick
it only to demo the terminal on its own, away from the scenario.

**A world beat does not need a `SceneEventRelay` any more** — not for the screen, anyway.
The bridge subscribes to the `EV_EHR_*` channels directly. You still want a relay on those
channels for everything *else* the beat should do: the beep, the chime, the room lighting.
Both can listen to the same channel.

**The four EHR gates are raised by the keyboard, not by the terminal itself.** The screen
has no buttons on it yet, so `KeyboardBaked` in `Hospital Room` carries one `ScenarioTarget`
per gate — `alert.override`, `dose.entered.5000`, `dose.corrected.500`,
`contraindication.override` — all set to *Click / press* with **Require Focus** on, sharing
one `ScenarioHighlight`. Only the one the scenario is asking for glows or accepts input, so
the same keyboard serves all four moments in turn. The keyboard's existing
`Interact ▸ On Interact` event calls `OnClicked()` on each of them, which means the player
presses the keyboard exactly as they do everywhere else: stand within 1.4 m and press the
interact button. In the editor a plain mouse click on the keyboard works too.

The dose gates are a stand-in: pressing the keyboard when the scenario asks for the dose
raises `dose.entered.5000`, and pressing it again when the scenario asks for the correction
raises `dose.corrected.500`. Real numeric entry can replace it later by raising the same two
ids — nothing else changes.

The Actions list stays useful for the other route: put an `EHRSequenceActionButton` on the
screen with Action Name = `Confirm override` and it satisfies the same gate, no code change.

One wrinkle worth knowing: the keyboard also has the project's own `Interact` proximity
glow, which swaps its material when the player comes near. That and `ScenarioHighlight`
both write to the same renderer, so while you are standing at the keyboard the pulse can
look off for a moment. It is cosmetic — neither one leaves the material wrong. If it
bothers you, give the highlight a dedicated outline mesh under **Show While Glowing**
instead.
