# Running the scenario in FIXED_SCALING_SCENE

Follow this once and you will be able to play the whole story end to end — including the
parts of the EHR that do not exist yet, which you raise by hand from an on-screen panel.

Everything referenced here already exists on disk:

| What | Where |
|---|---|
| The scenario (45 steps) | `Assets/Scenario/Bioinformatics/SC_Bioinformatics.asset` |
| Steps + event channels | `Assets/Scenario/Bioinformatics/` |
| Voice-over (51 clips) | `Assets/Audio/Narration/Narrator`, `.../NurseSarah` |
| Caption blobs (54 sprites) | `Assets/Images/Captions/` — **already assigned to every line** |

> First time you open Unity after this, let it finish importing. The 105 new files
> (audio + captions) all have their `.meta` written already, so GUIDs will not churn and
> nothing will re-link.

---

## Step 1 — The scenario object

1. Open `Assets/Scenes/FIXED_SCALING_SCENE.unity`.
2. Create an empty GameObject at the root, name it **`Scenario`**.
3. Add the **Scenario Controller** component.
4. Set **Scenario** → `SC_Bioinformatics`.
5. Leave **Play On Start** ticked.

Now fill in the **Context** section:

| Field | What to drop in |
|---|---|
| Vo Source | see step 2 |
| Caption Display | see step 3 |
| Focus Channel | `EV_Focus` |
| Player | the `XR Rig Full Body` object (its `BNGPlayerController`) |
| Player Rig | the same rig's transform |

---

## Step 2 — Voice-over audio source

The scene currently has **no AudioSource at all**, so nothing would be audible.

1. Add a child of `Scenario` called **`VO Source`**.
2. Add an **Audio Source**: untick *Play On Awake*, set **Spatial Blend to 0 (2D)** so the
   narrator is not positioned in the room.
3. Optional: set its **Output** to the Narration group in `Assets/Audio/MainAudioMixer`.
4. Drag it into **Context ▸ Vo Source**.

---

## Step 3 — Captions

The 54 Figma blobs are already assigned to their lines. All that is missing is the surface
that draws them.

1. Create a **UI ▸ Canvas** named **`Caption Canvas`**.
   - For a first desktop test, leave it as **Screen Space – Overlay**.
   - For VR, set **Render Mode: World Space**, parent it under the rig's camera, and scale
     it to roughly `0.001` with a position about `1.5 m` forward and slightly down.
2. Inside it add a **UI ▸ Image**, name it `Blob`. Set **Preserve Aspect** on — the blobs
   are 968×168, so let it size naturally near the bottom of the view.
3. Inside it add a **UI ▸ Text – TextMeshPro**, name it `Fallback Text`, and place it over
   the same spot.
4. Put the **Caption Display** component on the `Caption Canvas` root and assign:
   - **Root** → `Caption Canvas` (or an inner panel if you want the canvas to stay on)
   - **Blob Image** → `Blob`
   - **Caption Text** → `Fallback Text`
5. Drag the `Caption Display` into **Context ▸ Caption Display**.

Captions now appear and disappear on their own, one blob per phrase. Nothing else to wire.

Two behaviours worth knowing, because they look like bugs but are not:

- A blob **stays on screen across two clips** when a line has more captions than
  recordings. Sarah's *"See? Childbearing age? He's a 68-year-old man."* is one blob over
  two audio files, and it is meant to hold.
- The **last phrase of the opening narration has no blob**. Figma exported six for that
  line but the narrator recorded seven, so blob *f* holds while *"introduce new risks."*
  plays. Export one more blob for that phrase and drop it into
  `S1_01_Narr_Welcome ▸ Phrases ▸ Element 6 ▸ Caption` to finish it.

---

## Step 4 — The debug panel (this is the part that lets you skip the EHR)

1. Add the **Scenario Debug HUD** component to the same `Scenario` object.
2. Assign **Task Channel** → `EV_BioTask`, **Focus Channel** → `EV_Focus`.
3. Expand **World beats** and add one row per EHR moment, dropping in the channel asset
   and typing a label:

   | Label | Channel |
   |---|---|
   | Patient verified | `EV_EHR_PatientVerified` |
   | Methotrexate alert | `EV_EHR_MethoAlert` |
   | Metho administered | `EV_MethoAdministered` |
   | Amox prescription | `EV_EHR_AmoxPrescription` |
   | Dosage confirmed | `EV_EHR_DosageConfirmed` |
   | Contraindication! | `EV_EHR_Contraindication` |
   | Meds administered | `EV_MedsAdministered` |
   | 30 minutes later | `EV_TimeSkip30Min` |
   | Emergency | `EV_Scene3B_Emergency` |
   | Fade out | `EV_Scene3B_FadeOut` |
   | Open assessment | `EV_OpenAssessment` |
   | Assessment done | `EV_AssessmentComplete` |

In play mode you get a panel showing the current step, what the scenario is waiting for,
and a button per beat:

- **Space** — completes whatever gate is blocking, or skips the current line if nothing is
  blocking. One key walks the entire scenario.
- **Right Arrow** — force-ends the current step even mid-sentence.
- **F1** — hide/show the panel.

**You can press Play right now and finish the whole story with Space alone.** Do that
first; it confirms the audio, the captions and the ordering before you touch a single prop.

The EHR steps never block, by the way — they fire and move on. The world-beat buttons are
there for when you want to trigger the screen change yourself at the right moment.

---

## Step 5 — Props: highlight and scanning

Three components do this work:

- **`ScenarioTarget`** — one per interactive prop. Knows which task it completes, glows
  while the scenario is asking for it, and refuses interaction at any other time (so the
  player cannot scan the Allopurinol during Scene 1 and skip half the story).
- **`ScenarioHighlight`** — the pulsing glow. Purely visual, driven by the target.
- **`ScannerTool`** — goes on the scanner only.

### 5a. The scanner

On the **`scanner`** object in the scene:

1. Add **ScenarioHighlight** (leave the default yellow).
2. Add **ScenarioTarget**:
   - Task Channel → `EV_BioTask`, Focus Channel → `EV_Focus`
   - Task Id → `scanner.pickup`
   - Trigger → **Grab / pick up**
3. Nothing else to wire for the grab. `ScenarioTarget` finds the BNG **Grabbable** on the
   object (or its parent/child) and watches `BeingHeld` itself. Note that BNG keeps its
   grab UnityEvents on a *separate* `GrabbableUnityEvents` component rather than on
   `Grabbable`, so there is no "On Grab" field on the Grabbable to wire — watching the flag
   avoids that trap entirely. No Grabbable at all? Switch Trigger to **Click**.
4. Add **ScannerTool**. The defaults are deliberately forgiving; the only thing you
   normally have to set is the aim.

   **Aiming it** — with the scanner selected, a yellow gizmo shows the beam and its cone
   live in the Scene view:
   - Drag **Aim Rotation** (X/Y/Z degrees) until the beam points out of the scanner's
     nose. This replaces having to create and rotate a child transform — an imported FBX
     whose forward axis isn't +Z is corrected right here.
   - **Aim Offset** shifts the start point to the tip.
   - Shortcut: put a scannable in front of the scanner, then use the component's context
     menu ▸ **Aim At Nearest Scannable**. It sets Aim Rotation for you; fine-tune from
     there. **Reset Aim Rotation** puts it back to zero.
   - Leave **Muzzle** empty unless you already have a tip transform you like.

   **Detection mode** — `Cone` by default, which ignores colliders entirely and asks only
   "is a scannable roughly in front of me?". That is what makes aiming at a small bottle
   label practical with a tracked controller, and it works even on props with no collider.
   Widen **Cone Angle** if aiming still feels fussy. Switch to `Physics` only if you want
   the stricter, collider-accurate feel.

   Optional: a **Line Renderer** in **Beam** to show the aim, and an AudioSource in
   **Beep**. Tick **Auto Scan On Aim** to fire without pulling a trigger at all.

### 5b. Everything that gets scanned or pressed

Same recipe for each: add **ScenarioHighlight** + **ScenarioTarget**, set both channels,
set the task id, set the trigger mode. Make sure each has a **Collider**.

| Object in scene | Task Id | Trigger | Glow |
|---|---|---|---|
| `Patient` → wristband | `wristband.scan` | Scan | yellow |
| Methotrexate bottle | `methotrexate.scan` | Scan | yellow |
| `KeyboardBaked` | `alert.override` | Click | yellow |
| Amoxicillin bottle | `amoxicillin.scan` | Scan | yellow |
| Keypad confirm (5000) | `dose.entered.5000` | Click | yellow |
| Keypad confirm (500) | `dose.corrected.500` | Click | yellow |
| Allopurinol bottle | `allopurinol.scan` | Scan | yellow |
| `KeyboardBaked` (2nd) | `contraindication.override` | Click | **red** |

Two practical notes:

- **The scene has only one medicine bottle** (`uploads_files_4176934_medicine+bottle+fbx`).
  Duplicate it twice and label them Methotrexate / Amoxicillin / Allopurinol; each copy
  gets its own `ScenarioTarget` with its own task id.
- **The keyboard is used three times** with three different task ids. Put three
  `ScenarioTarget` components on it (Unity allows duplicates of the same component) — each
  one arms itself only when its own task is being asked for, so they never collide. The
  red one is the contraindication override.
- The **wristband** is probably a child of `Patient`; put the target on whatever object has
  the collider you want the beam to hit.

### Testing props without a headset

Every `ScenarioTarget` has **Allow Mouse Click In Editor** ticked. In play mode you can
just **click the prop** with the mouse to complete it, as long as it has a Collider. That
plus the debug panel means the entire scenario is playable on a desktop.

---

## Step 6 — The question panel

> **If you followed an earlier version of this doc, ignore it.** It told you to find a
> `Quiz` component on the prefab. There isn't one — `Question Panels.prefab` uses
> **`QuestionPanelManager`** with the image-based `BioQuestions` bank, while `Quiz` belongs
> to the older text-based prefab. The scenario now drives *your* panel directly, so there
> is nothing to hunt for.

**One panel serves both jobs**: the in-simulation quiz in Scene 1 (step 19) and the whole
post-experience assessment in Scene 4 (step 45). You drop it in once.

### 6a. Put the panel in the scene

1. Drag **`Assets/Prefabs/UI/Question Prefab/Question Panels.prefab`** into the scene.
2. Position it where the player can read it — in front of the EHR terminal works well.
   For VR its Canvas should be **World Space**; scale around `0.001` and put it at roughly
   eye height about 1.5 m from where the player stands.
3. Select the prefab root and find the **Question Panel Manager** component. Check that
   its **Question Bank** field is set to **`BioQuestions`**. Everything else on it
   (the page objects, answer buttons, selection groups) is already wired inside the prefab.

### 6b. Connect it to the scenario — two fields

On the **ScenarioController**, under **Context**:

| Field | Value |
|---|---|
| **Question Panel** | the `QuestionPanelManager` from the prefab you just dropped in |

On the **QuestionPanelManager** itself:

| Field | Value |
|---|---|
| **Panel Closed Event** | `EV_AssessmentComplete` |

That is the whole hookup. Ignore **Context ▸ Question Panels** (the list of question-asset
→ panel rows) — that belongs to the older text quiz and stays empty.

### 6c. What happens at runtime

**Step 19, mid-simulation.** The scenario calls `ShowSingleQuestion` on your panel with the
bank's **universal question** — *"What caused this faulty alert?"*, the Methotrexate one.
The panel skips its title, major-select and summary pages and opens straight on the
question page. When the player confirms an answer:

- your panel shows its correct/incorrect explanation image as it already does,
- the scenario plays **a different narrator line depending on which answer was picked** —
  the three recorded takes, with captions,
- a wrong answer re-asks the question; a correct one closes the panel and the story
  continues with Sarah's *"The trick is knowing when to trust the system…"*.

In this mode the panel's own 10-second *"Next in Ns"* countdown is suppressed, because the
narration decides the pacing. Your normal flow is untouched.

**Step 45, Scene 4.** Here the panel runs its full flow on its own — major select →
universal question → major question → summary → exit. See step 7.

### 6d. Answer order matters

The per-answer narration is matched by **index** to the answers in `BioQuestions ▸
Universal Question ▸ Answers`:

| Index | Answer | Narrator line |
|---|---|---|
| 0 | Patient is allergic | *"Incorrect. There is no documented allergy…"* |
| 1 | Logic failed to filter for patient sex ✅ | *"Correct. The CDS logic checked age…"* |
| 2 | Medication expired | *"Incorrect. Medication expiration…"* |

If you ever reorder those answers in the bank, reorder **Per Answer Feedback Vo** on
`S1_19_Quiz_MethoAlert` to match.

### 6e. If you skip this

The step logs a clear error and moves on, so a first playthrough is never blocked.

---

## Step 7 — Scene 4, the assessment

The last step raises `EV_OpenAssessment` and waits for `EV_AssessmentComplete`.

1. Add a **SceneEventRelay** on an always-active object, channel `EV_OpenAssessment`,
   response → **`QuestionPanelManager ▸ OpenPanel`**. Full walkthrough with screenshots of
   the fields in **`Docs/SceneEventRelays.md`**.

   Use `OpenPanel()` rather than `GameObject ▸ SetActive` — it switches on the panel *and
   any inactive parents*, which `SetActive` on a child cannot do. And keep the relay off
   the panel itself: a relay on a deactivated object never hears anything, so it could
   never open the very panel it lives on.
2. **Panel Closed Event** → `EV_AssessmentComplete` (already done in step 6b). It fires
   from `ExitQuestionPanel()`, which your exit button already calls.

Until the relay exists the scenario simply sits on the final step — press the
*Assessment done* button on the debug panel to finish the run.

### The Methotrexate question appears twice

That is what the script specifies: once in the simulation (step 19) and again as
"Question 1" of the assessment. If you would rather not ask it twice, tick
**Skip Universal Question In Assessment** on `QuestionPanelManager`. The assessment then
goes straight to the major-specific question, and the summary score adjusts on its own.

---

## Step 8 — World beats, for real this time

When you are ready to replace the debug buttons with real behaviour, add a
**SceneEventRelay** per channel and wire its response to your EHR screen logic. The
channel list is in step 4; `Docs/ScenarioWiring.md` describes what each one should show.

---

## Order I would actually do this in

1. Steps 1–4, then **press Play and hold Space** through all 45 steps. You are checking
   that the voice-over plays in the right order and the captions match what you hear.
2. Step 5a + the wristband only. Play again, pick up the scanner, scan the wristband.
   That proves the glow → scan → advance loop works.
3. The rest of step 5.
4. Steps 6 and 7.
5. Step 8 as the EHR gets built.

---

## If something looks wrong

**Press Play and read the Console first.** The controller prints a wiring report naming
any gate the scene cannot satisfy and any channel mismatch. `Docs/ScenarioTroubleshooting.md`
goes through each cause in detail.

| Symptom | Cause |
|---|---|
| No audio | Context ▸ Vo Source empty, or the AudioSource is 3D and behind you |
| No captions | Caption Display not assigned on the controller, or its Root is inactive |
| Nothing glows | Focus Channel missing on the controller **or** on the prop — both need `EV_Focus` |
| Glow still invisible | Raise Intensity to 3–4 or Base Color Tint toward 1; or use Show While Glowing |
| Prop does nothing when clicked | No Collider, or its task is not the one being asked for right now |
| Scanner never locks on | Muzzle's +Z is not pointing out of the scanner's nose — check the cyan gizmo |
| Scanner locks on but won't fire | Press **E** or left-click, or tick Auto Scan On Aim (no VR trigger in the editor) |
| Scenario stalls | Check the debug panel's "Waiting on" line — that is the task id nothing is raising |
| "Nothing glows at the start" | Expected — the scanner glows at step 8. Press **Tab** to skip to it |
