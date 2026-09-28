# Pilot Testing & Evaluation Guide

(Brief section 7 — conduct pilot tests with students and educators; measure
engagement and learning effectiveness.)

## Before the session

1. Confirm the ML service is reachable (or demo the offline fallback on purpose
   to discuss robustness).
2. Update the quiz bank: `VRLab/Assets/StreamingAssets/Data/quiz_statistics.json`.
3. Note each participant ID; it's entered at session start.

## During the session (≈ 20 min per student)

1. **Lobby (2 min):** orient the student; explain teleport (thumbstick) vs.
   smooth locomotion; check comfort settings.
2. **Scenario 1 — Stock Market (7 min):** watch the live chart, describe the
   trend, compare with the forecast overlay. Voice command "next" to progress.
3. **Scenario 2 — Business Forecasting (7 min):** adjust price/marketing/
   seasonality sliders; try to hit the target demand (points awarded). Discuss
   how the model responds to each variable.
4. **Quiz (4 min):** 5-question panel; answers logged automatically.

## Data collected automatically

`LearningLogger` writes a CSV per session (persistentDataPath):

| Column | Meaning |
|---|---|
| `timestamp` | ISO-8601 wall clock |
| `elapsed_s` | Seconds since session start (engagement proxy) |
| `event` | `session_start`, `quiz_answer`, `scenario_*`, custom |
| `detail` | e.g. `q2|correct=true` |

## Measurements

- **Learning effectiveness:** quiz score vs. a written pre-test
  (same questions) — compute delta per student.
- **Engagement:** total session time, interaction count, scenario completion.
- **Usability (SUS-style):** 5-point exit questionnaire; log comfort issues
  (nausea, controller confusion).

## Feedback & adjustments loop

- File issues per participant (usability, performance, content).
- Real-time interaction speed: if prediction latency > ~300 ms, lower
  debounce to 0.15 s or pre-warm the ML service.
- Iterate, then re-run with a new cohort; keep CSV logs versioned by date.
