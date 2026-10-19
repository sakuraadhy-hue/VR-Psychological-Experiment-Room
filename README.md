# VR Psychological Experiment Room — Calm vs. Stress vs. Neutral

Final project for **Fundamentals of eXtended Reality** (Master HCI, Université Paris-Saclay, 2026).

A virtual experiment room for psychological research. A participant performs a timed colour-sorting task while the room is set to one of three environmental conditions. The application records task performance and a short questionnaire to a CSV file, so a researcher can compare how the environment affects speed, accuracy and perceived stress.

**Engine:** Unity 6.3 LTS (`6000.3.25f1`), Universal Render Pipeline · **Platform:** desktop (keyboard + mouse)

---

## Group members and contributions

| Member | Role | Contributions |
| --- | --- | --- |
| **Kayla** (Member A) | Room, environments & lighting | Git repository and Unity project setup; room shell modelling (walls, floor, ceiling, table); `MainExperiment` scene; Neutral / Calm / Stress environment sets; flickering fluorescent light (`LightFlicker.cs`); 3D ambient audio for the conditions |
| **Ana** (Member B) | Locomotion, manipulation & physics | Gliding/flying navigation (`GlidingLocomotion.cs`); ray-cast pickup at an offset distance (`RaycastGrabber.cs`); grabbable balls and sorting bins (`SortingItem.cs`, `SortingBin.cs`, `SortingStation.cs`); physics materials and Rigidbody tuning; 3D feedback sounds; demonstration video |
| **Sakura** (Member C) | Experiment flow, UI & data logging | Experiment state machine (`ExperimentManager.cs`); 60 s countdown timer and live score (3D TextMeshPro); world-space briefing, survey and completion canvases; clickable 3D push buttons (`RemoteButton.cs`); researcher control panel; Likert questionnaire (`SurveyPanel.cs`, `LikertQuestion.cs`); CSV logging (`DataLogger.cs`); reset routine; editor tool that builds the UI (`ExperimentUIBuilder.cs`); bin-hover assist in the grabber; this README |

All members: integration in the main scene, end-to-end testing and code comments.

---

## Experimental design

The participant sorts 15 coloured balls (red, green, blue) into the bin of the matching colour within **60 seconds**. Before the trial, the researcher selects one of three conditions:

| Condition | Environment | Purpose |
| --- | --- | --- |
| **Neutral** | Static neutral-white ceiling light (4500 K), plain props, silence | Control baseline |
| **Calm** | Warm amber light (2700 K), plants and wooden decoration, 3D nature sounds from a window | Restorative environment |
| **Stress** | Cold flickering fluorescent light (6500 K), flashing red beacon, 3D ticking clock and alarm beeps | High cognitive load |

**Measured variables**

- **Score**: balls placed in the correct bin (task throughput)
- **Errors**: balls placed in a wrong bin (accuracy)
- **Time remaining**: seconds left if all balls were sorted before the timer ran out
- **Stress, Focus and Comfort ratings**: 1–5 Likert questionnaire after each trial

The condition can only be changed before a trial starts, so each participant experiences a single condition for the whole trial.

---

## How the application works

1. **Briefing.** The room starts in the Neutral condition. A canvas on the north wall shows the instructions and controls.
2. **Condition.** The researcher clicks **NEUTRAL**, **CALM** or **STRESS** on the panel on the west wall. The matching environment objects are activated and the others deactivated. The selected button stays pushed in.
3. **Start.** The participant clicks the green **START EXPERIMENT** button. The canvas is hidden, revealing a 3D digital clock that counts down from `01:00.0` with a live `Score | Errors` display. Grabbing is unlocked.
4. **Sorting.** The participant picks up balls with the mouse and drops them into the bins. Each bin plays a chime (correct) or a buzzer (wrong) from its position in 3D space.
5. **End of trial.** When the timer reaches zero, or all 15 balls are sorted, grabbing is locked and the questionnaire appears.
6. **Data.** After the participant answers the three questions and clicks **SUBMIT ANSWERS**, one row is added to `results.csv`.
7. **Reset.** The researcher clicks **RESET**. All balls return to the table, the timer and counters are reset, the room returns to Neutral and the next participant ID (P001, P002, …) is prepared.

---

## Implemented features

- **Navigation (gliding/flying metaphor):** smooth, accelerated gliding in the look direction with WASD, rising/descending with E/Q; a CharacterController prevents passing through walls and the table.
- **Manipulation by ray casting at an offset distance:** a ray from the camera through the cursor picks up a ball, which then floats along the ray; the scroll wheel changes its distance. When the cursor points at a bin, the held ball hovers above the bin's opening so that releasing it drops it in.
- **Activation / deactivation at runtime:** environment sets per condition, the participant canvas, the START button, and sorted balls (hidden, then restored on reset).
- **Canvas + 3D TextMeshPro:** world-space canvases (briefing, questionnaire with UI buttons, completion message) and 3D TextMeshPro text (countdown clock, score, researcher status screen, button labels).
- **Physics:** Rigidbody balls with physics materials (bounce and friction), trigger colliders in the bins, kinematic holding while carried.
- **Lighting controlled during the interaction:** a different lighting setup per condition, including a scripted flickering fluorescent light in the Stress condition.
- **3D audio:** spatialised feedback sounds on each bin and ambient sound sources for the Calm and Stress conditions.
- **Countdown timer:** 60 s timer driven by `Time.deltaTime`, turning red in the last 10 seconds.
- **Scripting of behaviours and logic:** experiment state machine, event-based scoring (`SortingBin.OnItemSorted`), survey validation, CSV logging and a complete reset.
- **Reset button:** returns every interactive object, the timer, the UI and the environment to their initial state for the next participant.

---

## Controls

| Input | Action |
| --- | --- |
| W / A / S / D | Glide forward / left / back / right (in the look direction) |
| E / Q | Rise / descend |
| Left Shift | Glide faster |
| Hold right mouse button + move mouse (or arrow keys) | Look around |
| Hold left mouse button on a ball | Pick it up; release to drop |
| Scroll wheel | Move the held ball nearer / further |
| Left click | Press 3D buttons and questionnaire buttons |

---

## Running the project

1. Open the project folder in **Unity 6.3 LTS (6000.3.25f1)** through Unity Hub.
2. Open `Assets/Scenes/MainExperiment.unity`.
3. Press **Play**.

If the experiment UI is missing from the scene, it can be rebuilt with **Tools → Experiment → Build Experiment UI**.

---

## Output data (`results.csv`)

In the Unity Editor the file is written to the project folder (next to `Assets`); in a built application it is written to `Application.persistentDataPath`. The full path is printed in the Console when Play mode starts. One row is added per completed trial:

```
ParticipantID,Condition,Score,Errors,TimeRemaining,StressRating,FocusRating,ComfortRating,Timestamp
P001,Stress,14,3,0.0,4,2,1,2026-10-19 18:29:32
```

Numbers always use a dot as the decimal separator, so the file opens correctly in Excel, R or Python on any system language. Participant numbering continues from the rows already in the file.

---

## Main scripts

| Script | Description |
| --- | --- |
| `ExperimentManager.cs` | Trial state machine, conditions, timer, score, survey and reset |
| `DataLogger.cs` | Appends trial results to `results.csv` |
| `RemoteButton.cs` | Clickable 3D push button with press animation |
| `SurveyPanel.cs` / `LikertQuestion.cs` | Post-trial questionnaire |
| `Editor/ExperimentUIBuilder.cs` | Editor menu tool that builds and wires the experiment UI |
| `GlidingLocomotion.cs` | Gliding/flying navigation |
| `RaycastGrabber.cs` | Ray-cast pickup at an offset distance, with bin-hover assist |
| `SortingItem.cs` / `SortingBin.cs` / `SortingStation.cs` | Balls, bins with trigger detection, station reset and grabbing control |
| `LightFlicker.cs` | Erratic fluorescent flicker for the Stress condition |

---

## Demonstration video

*(link or file path to be added)*

---

## Use of AI tools

Claude (Anthropic) was used as an assistant to help write and debug the experiment UI, timer, questionnaire and data-logging scripts, the editor UI-building tool, and the bin-hover assist, and to help draft this README. All features were tested in Unity by the group.
