# Bioinformatics Scenario — Reference

> **Setting this up for the first time? Read `ScenarioSetup.md` instead** — it is a
> step-by-step walkthrough for getting the scenario running in `FIXED_SCALING_SCENE`.
> This file is the reference for what each channel and task id means.

- **Scenario asset:** `Assets/Scenario/Bioinformatics/SC_Bioinformatics.asset` (45 steps)
- **Steps + event channels:** `Assets/Scenario/Bioinformatics/`
- **Voice-over:** `Assets/Audio/Narration/Narrator/` (21 clips), `Assets/Audio/Narration/NurseSarah/` (30 clips)
- **Captions:** `Assets/Images/Captions/` (54 sprites, all already assigned to their lines)

---

## 1. Captions

Captions are new. A voice-over line is now a list of **phrases**, and each phrase carries
its own caption:

| Field | Meaning |
|---|---|
| `clip` | the recorded phrase (one of the imported mp3s) |
| `caption` | the Figma caption blob **sprite** — this is what actually shows |
| `captionText` | plain-text fallback, used only when no blob is assigned |

**All 54 blobs are already assigned**, matched to their lines by reading the text off each
image. Scene setup is in `ScenarioSetup.md` step 3; there is no per-step wiring.

Because the blobs were split by text length and the audio by phrase, the two do not always
line up one-to-one. A phrase with **no caption of its own keeps the previous blob on
screen**, which covers both mismatches:

- Sarah's *"See? Childbearing age? He's a 68-year-old man."* is one blob spanning two clips.
- The opening narration has six blobs for seven clips, so the last one holds while
  *"introduce new risks."* plays. Exporting one more blob and dropping it into
  `S1_01_Narr_Welcome ▸ Phrases ▸ Element 6 ▸ Caption` completes it — the only blob missing.

A phrase with a caption but **no clip** also works: the caption holds for a reading-speed
duration. Four lines use this because they were never recorded
(`S1_07_Narr_ClickScanner`, `S1_16_Sarah_JustOverrideIt`, `S3B_04_Sarah_WeHurtHim`,
`S3B_05_Narr_PoorMrJohnson`) — each starts speaking the moment a clip is dropped in.

**Player toggle:** `CaptionDisplay.CaptionsEnabled` persists in PlayerPrefs (on by
default). Wire a settings `Toggle` to the instance method `SetCaptionsEnabled(bool)`.

---

## 2. ScenarioController

Add a `ScenarioController` to the hospital scene and set:

- **Scenario** → `SC_Bioinformatics`
- **Context ▸ Vo Source** → an `AudioSource` routed to the Narration mixer group
- **Context ▸ Caption Display** → the `CaptionDisplay` from step 1
- **Context ▸ Focus Channel** → `EV_Focus` (drives which prop is glowing)
- **Context ▸ Player / Player Rig** → the BNG player
- **Context ▸ Question Panels** → one row: question `MethoAlert_Q` → the in-sim quiz panel

Use the component's context menu: **Fill Binding Rows From Scenario** creates the empty
rows, then **Validate Scene Bindings** reports anything still unassigned. Run the second
one before every playtest — it catches gaps in the console instead of mid-session.

---

## 3. Gameplay gates (`WaitForTask` steps)

Nine steps pause the scenario until the player does something. Each listens on the shared
`EV_BioTask` channel for one id.

The normal way to satisfy one is a **`ScenarioTarget`** on the prop (see
`ScenarioSetup.md` step 5) — it glows while its task is being asked for, blocks
interaction at every other moment, and raises the id itself. `TaskEventRaiser` is still
there for props that already have their own interaction logic and just need to report in.

| Task Id | Fires when the player… | Put the target on |
|---|---|---|
| `scanner.pickup` | picks up the barcode scanner | scanner grabbable |
| `wristband.scan` | scans Mr. Johnson's wristband | wristband / scan trigger |
| `methotrexate.scan` | scans the Methotrexate bottle | Methotrexate bottle |
| `alert.override` | dismisses the false teratogenic alert | EHR keyboard |
| `amoxicillin.scan` | scans the Amoxicillin bottle | Amoxicillin bottle |
| `dose.entered.5000` | types the bad 5000 mg dose | numeric keypad confirm |
| `dose.corrected.500` | corrects the dose to 500 mg | numeric keypad confirm |
| `allopurinol.scan` | scans the Allopurinol bottle | Allopurinol bottle |
| `contraindication.override` | overrides the real contraindication | EHR keyboard |

Both keypad gates are raised by the same confirm button, so it needs to know which dose
was entered — raise `dose.entered.5000` only when the value is 5000 and
`dose.corrected.500` only when it is 500. An unmatched id is ignored, so a raise at the
wrong moment is harmless.

---

## 4. World beats (`InvokeSceneEvent` steps)

These steps raise a channel and move on immediately. A step asset is a ScriptableObject
and cannot reference scene objects, so each one needs a **`SceneEventRelay`** in the
scene: set its **Channel** to the asset below and wire its **Response** UnityEvent to the
scene methods that should fire (EHR screen state, SFX, lighting, animation).

| Channel | What should happen |
|---|---|
| `EV_EHR_PatientVerified` | beep + `PATIENT VERIFIED: JOHNSON, M. (Male, 68)` |
| `EV_EHR_MethoAlert` | beep, pulsing red triangle, 3D teratogenic warning above the terminal |
| `EV_MethoAdministered` | alert clears; Sarah administers the Methotrexate |
| `EV_EHR_AmoxPrescription` | Amoxicillin prescription + free-text `Last dose: 5000mg` + keypad |
| `EV_EHR_DosageConfirmed` | pleasant chime + `Dosage Confirmed.` |
| `EV_EHR_Contraindication` | big beep + `CONTRAINDICATION!` warning |
| `EV_MedsAdministered` | Sarah administers the remaining medications |
| `EV_TimeSkip30Min` | fade to black, show `30 Minutes Later...` |
| `EV_Scene3B_Emergency` | emergency ambience, dim red room, rashes on Mr. Johnson |
| `EV_Scene3B_FadeOut` | slow fade out |
| `EV_OpenAssessment` | open the Question Panel prefab (Scene 4) |

`EV_TimeSkip30Min` and `EV_Scene3B_FadeOut` do **not** block — the scenario continues
while the fade plays. If a beat needs to finish before the next line, use the
`Teleport` step's fade instead, or say so and I'll switch those two to wait for a
completion channel.

---

## 5. Scene 4 — the post-experience assessment

`S4_01_OpenAssessment` is the only step that waits: it raises `EV_OpenAssessment` and
blocks until `EV_AssessmentComplete` comes back. The Question Panel prefab owns the whole
of Scene 4 (major select → universal question → major question → summary → exit), so:

1. Relay `EV_OpenAssessment` → the panel's activate method.
2. On `QuestionPanelManager`, set the new **Panel Closed Event** field to
   `EV_AssessmentComplete`. It fires from `ExitQuestionPanel()`, which the exit button
   already calls, and that releases the final step.

Without step 2 the scenario stops at the last step, so it is worth double-checking.

---

## 6. In-simulation quiz feedback

`S1_19_Quiz_MethoAlert` now speaks **a different narrator line per answer** rather than
one generic right/wrong clip. The three lines are wired index-aligned with
`MethoAlert_Q`:

| Answer | Narrator clips | Line |
|---|---|---|
| A — Patient is allergic | `Narrator/S1 04 p1–p2` | "Incorrect. There is no documented allergy…" |
| B — Logic failed to filter for patient sex ✅ | `Narrator/S1 05 p1–p4` | "Correct. The CDS logic checked age…" |
| C — Medication expired | `Narrator/S1 06 p1–p3` | "Incorrect. Medication expiration…" |

The step is set to unlimited retries and will not advance until the player answers
correctly, which matches the script's `[SIMULATION PAUSE]`. To let a wrong answer through
after N attempts, set `Allowed Tries` and tick `Advance On Fail`.

---

## Gaps in the source material

Clip-to-line mapping came from the filenames, confirmed against measured audio durations
and then again against the caption text itself — the blob set accounted for all 54 slots
with nothing left over, which is a strong cross-check. Three gaps remain, all in the
recordings rather than the wiring:

1. **Four lines have captions but no audio.** `S1_07_Narr_ClickScanner`,
   `S1_16_Sarah_JustOverrideIt`, `S3B_04_Sarah_WeHurtHim` and `S3B_05_Narr_PoorMrJohnson`
   play as caption-only beats held for a readable duration. Drop a clip into any of them
   and it starts speaking; nothing else changes.
2. **One caption blob is missing** — the tail of the opening narration
   (*"introduce new risks."*). See section 1.
3. **Scene 3B is worth 30 seconds of your ear.** The folder has three clips
   (`S3B 03 p1–p3`) but the captions describe three separate Sarah lines. Clip durations
   put `p1` firmly on *"His blood pressure is dropping!"*, so `p2`/`p3` are wired to the
   whispered *"Oh no… the alert"* line and the third line is caption-only. If that plays
   back wrong, the fix is moving clips between `S3B_02`, `S3B_03` and `S3B_04`.

`captionText` on each phrase mirrors its blob and is only used as a fallback when the
sprite is missing, so it never needs hand-editing.
