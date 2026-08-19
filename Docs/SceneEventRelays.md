# Adding a Scene Event Relay

A scenario step is a ScriptableObject, and a ScriptableObject **cannot hold a reference to
anything in the scene**. So when the scenario wants the room to react — the EHR screen to
change, a chime to play, the lights to go red — it raises a named channel instead, and a
small scene component listens on that channel and does the actual work.

That component is the **`SceneEventRelay`**. One per channel you want to react to.

```
scenario step  ──raises──▶  EV_EHR_MethoAlert  ──heard by──▶  SceneEventRelay
                              (asset)                            │
                                                                 └─▶ your scene objects
```

---

## Read this before you place one

**A relay must live on a GameObject that is always active.** It subscribes in `OnEnable`,
so a relay on a deactivated object is not listening at all.

This bites hardest in the obvious case: putting the `EV_OpenAssessment` relay *on the
question panel you keep switched off*. It can never hear the event that was supposed to
switch it on. Keep relays on a separate always-on object and have them reach into the
things they control.

---

## Step by step

### 1. Make a home for them

1. In the Hierarchy, right-click ▸ **Create Empty**.
2. Name it **`Scenario Events`**. Leave it at the scene root, active, and never disable it.

All your relays can live on this one object — a GameObject can hold as many
`SceneEventRelay` components as you like.

### 2. Add the relay

1. Select `Scenario Events`.
2. **Add Component** ▸ search `Scene Event Relay`.

### 3. Point it at a channel

Set **Channel** to the event asset you want to react to. They all live in
`Assets/Scenario/Bioinformatics/` and start with `EV_`. Either drag the asset in, or click
the little circle at the right of the field and pick it by name.

### 4. Say what should happen

The **Response** field is a standard UnityEvent:

1. Click the **+** under Response.
2. Drag the scene object you want to affect into the **object slot** (the one showing
   `None (Object)`).
3. Open the **function dropdown** and pick what to call. Choose from the **runtime**
   section, not the static one, so the change survives play mode.

A few you will use constantly:

| To do this | Object to drag in | Function |
|---|---|---|
| Show something | the object | `GameObject ▸ SetActive` (tick the box) |
| Hide something | the object | `GameObject ▸ SetActive` (leave unticked) |
| Play a sound | an AudioSource | `AudioSource ▸ Play` |
| Change EHR text | a TMP text | `TextMeshProUGUI ▸ text` and type the string |
| Play an animation | an Animator | `Animator ▸ SetTrigger` and type the trigger name |

Add as many rows as you want — one event can fire five things at once.

### 5. Check it fires

Tick **Debug Logging** on the relay. The console then prints
`[SceneEventRelay] 'Scenario Events' fired on 'EV_EHR_MethoAlert'.` each time it runs. Use
the debug panel's world-beat buttons to fire a channel on demand rather than replaying the
whole scenario.

---

## The one relay you actually need right now

Everything EHR-related is optional — those steps fire and move on, so the story runs
without them. **`EV_OpenAssessment` is different**: step 45 raises it and then *waits* for
`EV_AssessmentComplete`. With no relay, the scenario stalls on the last step forever.

1. On `Scenario Events`, add a `SceneEventRelay`.
2. **Channel** → `EV_OpenAssessment`.
3. Response ▸ **+** ▸ drag in the **Question Panels** object ▸ function
   **`QuestionPanelManager ▸ OpenPanel`**.

Use `OpenPanel()` rather than `GameObject ▸ SetActive`. It switches on the panel *and any
inactive parents* and opens it on the title page — `SetActive` on a child of a disabled
parent silently does nothing.

The other half is already done if you followed step 6b: **Panel Closed Event** on
`QuestionPanelManager` → `EV_AssessmentComplete`, which fires from the exit button and
releases the final step.

### How to tell whether it worked

Three things now report on this, so you never have to guess:

1. **At startup**, the wiring report prints a PROBLEM for any step that waits on a channel
   nothing is listening to.
2. **When the step runs**, if the channel has no listeners the console prints an error
   naming the channel: `raised 'EV_OpenAssessment' but NOTHING IS LISTENING … the scenario
   will stall here`. Seeing that error means the step definitely ran and the relay is the
   missing piece. *Not* seeing it means the scenario never got that far.
3. **The debug panel** shows `Waiting on scene event: EV_AssessmentComplete` while the step
   is blocked, with a listener count underneath.

### Opening straight on major select

`OpenPanel()` shows the panel's title page, so the player still has to press its Start
button. To skip that, add a **second row** to the same relay's Response: `OpenPanel` first,
then `QuestionPanelManager ▸ GoToMajor`. Order matters — `OpenPanel` is what makes the
hierarchy active, so it has to run first.

---

## The full channel list

None of them block the scenario, apart from `EV_OpenAssessment`.

**The EHR screen no longer needs a relay.** The terminal prefab carries an
`EHRScenarioBridge` that listens on the `EV_EHR_*` beats itself and switches the screen —
see `EHRScenarioBridge.md`. Add a relay on those channels for the rest of the beat: the
beep, the chime, the room lighting, Sarah's animation. A channel can have as many listeners
as you like.

| Channel | What it should do | Screen |
|---|---|---|
| `EV_EHR_PatientVerified` | beep + `PATIENT VERIFIED: JOHNSON, M. (Male, 68)` | bridge |
| `EV_EHR_MethoAlert` | beep, pulsing red triangle, 3D teratogenic warning | bridge |
| `EV_MethoAdministered` | alert clears; Sarah administers the Methotrexate | bridge |
| `EV_EHR_AmoxPrescription` | prescription + free-text `Last dose: 5000mg` + keypad | bridge |
| `EV_EHR_DosageConfirmed` | pleasant chime + `Dosage Confirmed.` | bridge |
| `EV_EHR_Contraindication` | big beep + `CONTRAINDICATION!` warning | bridge |
| `EV_MedsAdministered` | Sarah administers the remaining medications | bridge |
| `EV_TimeSkip30Min` | fade to black, show `30 Minutes Later...` | relay |
| `EV_Scene3B_Emergency` | emergency ambience, dim red room, rashes | relay |
| `EV_Scene3B_FadeOut` | slow fade out | relay |
| `EV_OpenAssessment` | open the question panel — **required**, see above | relay |

---

## Troubleshooting

| Symptom | Cause |
|---|---|
| Relay never fires | Its GameObject is inactive — move it to an always-on object |
| Relay never fires | Channel field empty (it now warns on startup) |
| Fires but nothing happens | The Response row's object slot is empty, or no function chosen |
| Worked in editor, not in build | You picked a *static* function instead of a runtime one |
| Panel doesn't appear | Used `SetActive` on a child of a disabled parent — use `OpenPanel()` |
