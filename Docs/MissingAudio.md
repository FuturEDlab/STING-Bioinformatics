# Audio still to record

Current state: **51 clips recorded** (21 narrator, 30 Nurse Sarah), covering all of Scenes
1–3A and most of 3B. What follows is everything the script calls for that has no recording
yet, in priority order.

Drop any new file into `Assets/Audio/Narration/Narrator/` or `.../NurseSarah/` and assign
it to the phrase named in the "Goes into" column — nothing else needs changing. A phrase
with a caption but no clip already holds its caption on screen for a readable beat, so the
scenario plays through today; adding the clip just makes it speak.

---

## 1. Silent lines in the scenario — 5 phrases, 4 lines

These have captions and are playing right now as silent held captions. Recording these
completes Scenes 1 and 3B.

| # | Speaker | Line | Goes into |
|---|---|---|---|
| 1 | NARRATOR | "Click on the scanner using the trigger button." | `S1_07_Narr_ClickScanner` ▸ phrase 1 |
| 2 | NURSE SARAH | "Makes no sense. The system doesn't even check gender." | `S1_16_Sarah_JustOverrideIt` ▸ phrase 1 |
| 3 | NURSE SARAH | "Just override it while I give the medication." | `S1_16_Sarah_JustOverrideIt` ▸ phrase 2 |
| 4 | NURSE SARAH | "We ignored it because of the others… and we hurt him." | `S3B_04_Sarah_WeHurtHim` ▸ phrase 1 |
| 5 | NARRATOR | "Poor Mr. Johnson… :(" | `S3B_05_Narr_PoorMrJohnson` ▸ phrase 1 |

Line 4 is the emotional beat of the whole experience and is currently silent — worth doing
first. Line 5 reads like placeholder text in the script; consider rewriting or cutting it
rather than recording it as-is.

Also in the script but never given a caption or a step: Sarah *"starts crying, mumbling
'I'm gonna lose my job, how could I do this?'"*. If you want that, it is a new recording
and I will add a step for it.

---

## 2. The assessment — nothing is recorded, and half of it is unwritten

**No narrator audio plays in Scene 4 at all today.** The question panel has no voice-over
support; it shows text and explanation images only. Two decisions before anyone records:

- **Do you want the narrator to speak in the assessment?** If yes I need to add VO playback
  to the panel — say the word and I will wire it the same way step 19 works.
- **Six of the eight feedback lines have never been written.** The script only supplies
  narrator feedback for the *correct* answer of Questions 2 and 3.

### Question 1 — Methotrexate (universal)

All three lines **are already recorded** (`Narrator/S1 04`, `S1 05`, `S1 06`) because the
in-simulation quiz uses them. If the panel gains VO support, they can be reused here for
free. Nothing to record.

### Question 2 — Nursing path

| Answer | Feedback | Status |
|---|---|---|
| It caused Sarah to distrust alerts… ✅ | "Correct. Repeated false alerts condition clinicians to ignore warnings reflexively. When the system fails to provide relevant info, trust is lost and safety is compromised." | **written, not recorded** |
| It increased Sarah's workload, making her rush | — | **not written** |
| It delayed medication administration | — | **not written** |
| It changed the patient's allergy status | — | **not written** |

### Question 3 — Informatics path

| Answer | Feedback | Status |
|---|---|---|
| Use structured data fields with dose-range validation… ✅ | "Correct. Structured data must take priority. By implementing validation against the /MedicationRequest resource, the system can block entries that exceed safe limits." | **written, not recorded** |
| Implement spellcheck on all nursing notes | — | **not written** |
| Remove historical nurse notes to reduce clutter | — | **not written** |
| Increase the number of manual override permissions | — | **not written** |

Writing the six missing wrong-answer lines matters more than recording them. A learner who
picks a wrong answer currently gets an image and no explanation, which is the weakest part
of the assessment — Question 1 explains every wrong answer, and those two do not.

---

## 3. Optional, but the script implies them

### EHR alerts

The character list says the narrator *"also narrates relative EHR alerts"*, and none of
these are recorded. Worth doing for accessibility — right now a player who misses the
screen misses the alert entirely.

| Line | When |
|---|---|
| "Patient verified. Johnson, M. Male, sixty-eight." | `EV_EHR_PatientVerified` |
| "Warning. Patient is of childbearing age. Methotrexate is teratogenic." | `EV_EHR_MethoAlert` |
| "Dosage confirmed." | `EV_EHR_DosageConfirmed` |
| "Warning. Contraindication. Amoxicillin is contraindicated with Allopurinol. Do not administer." | `EV_EHR_Contraindication` |

These would become Narrator steps inserted next to the matching event step. Tell me if you
want them and I will add the steps and captions.

### Framing lines

| Line | Where | Note |
|---|---|---|
| "Select your major." | Scene 4 opening | Not in the script; the panel shows it as text |
| Asking the quiz question aloud | Step 19 | Not in the script — the panel shows the question. `Question Vo` on `S1_19_Quiz_MethoAlert` is empty and ready if you record one |
| "Thanks for playing. Core concepts learned…" + the four bullets | Final summary | Text only in the script; a read-out would close the experience better than silence |
| "Thirty minutes later." | `EV_TimeSkip30Min` | The script shows this as on-screen text only |

---

## 4. Not audio, but a matching gap

One **caption blob** is missing: the tail of the opening narration, *"introduce new
risks."* The audio for it exists (`Narrator/S1 01 p7`), but Figma exported only six blobs
for that seven-phrase line, so blob *f* currently holds through it. Export one more and
drop it into `S1_01_Narr_Welcome ▸ Phrases ▸ Element 6 ▸ Caption`.

---

## Summary

| Group | Count | Blocking? |
|---|---|---|
| Silent scenario lines | 5 phrases | No — captions hold, but two are important beats |
| Assessment feedback, written but unrecorded | 2 lines | No — no VO in the panel yet either |
| Assessment feedback, **unwritten** | 6 lines | Writing them is the real gap |
| EHR alert narration | 4 lines | Optional |
| Framing / summary lines | 4 lines | Optional |
| Missing caption blob | 1 image | Cosmetic |

**If you record only one batch, make it group 1** — five short lines that remove every
silent gap from the story itself.
