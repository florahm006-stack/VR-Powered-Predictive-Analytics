# Report Skeleton — VR-Powered Predictive Analytics Lab

Matches the suggested structure in `Metaverse Project.pdf`. Fill each section
as milestones complete.

## 1. Cover Page
Title: *VR-Powered Predictive Analytics Lab: Enhancing Data Visualization and
Machine Learning in Virtual Reality*. Group members & ITS numbers, date.

## 2. Table of Contents
Auto-generate in Word/LaTeX.

## 3. Introduction
- Overview: how VR + ML enhance understanding of big data and predictive analytics.
- Objectives: VR interaction with large datasets; real-time statistical
  simulations; bridge theory ↔ practical ML.

## 4. Needs Assessment
- Challenges: difficulty visualizing complex datasets, no hands-on interaction
  in traditional learning.
- Review existing VR educational tools; limitations of 2D dashboards.
  → reference `docs/ARCHITECTURE.md` (instanced 3D charts vs. dashboards).

## 5. Design of the Immersive Experience
- Learning objectives (measurable): after the lab, students can (a) interpret a
  live trend, (b) explain a regression slope, (c) predict demand effects of
  price/marketing/seasonality, (d) evaluate model fit visually.
- Scenarios: stock market simulation; business forecasting. → Codes:
  `VRLab/Assets/Scripts/Scenarios/`.
- Accessibility: voice commands (`VoiceCommands.cs`), multi-device
  (VR + PC fallback via `RigSetup.cs`), comfort options (`ComfortLocomotion.cs`).

## 6. Development Process
- Tools: Unity 6000.6 LTS + C# + XRI 3.6.1 + OpenXR 1.18.0 + URP 17; Python
  FastAPI + scikit-learn; **Unity Inference Engine (Sentis 2.2)** with ONNX
  exports for true offline inference; GPU-instanced 3D charts; XRI grab/poke UI.
- Steps: VR dashboard + 3D charts, AI-driven predictive models (HTTP service →
  on-device ONNX → closed-form, automatic failover), interaction testing
  (11 EditMode tests + manual acceptance). → cite `docs/SETUP.md`,
  `docs/ARCHITECTURE.md`.

## 7. Pilot Testing and Evaluation
- Methodology: → `docs/PILOT_TESTING.md` (cohorts, pre/post quiz, SUS-style
  questionnaire, automatic CSV logs via `LearningLogger`).
- Feedback & adjustments: latency tuning, UI refinements, comfort fixes.

## 8. Implementation Plan
- Curriculum integration: business analytics / CS / statistics courses.
- Teacher training workshops (use `docs/SETUP.md` as handout).
- Long-term evaluation: pre/post performance data (quiz deltas).

## 9. Conclusion
- Findings (fill after pilot): engagement + comprehension outcomes.
- Future work: AI tutoring in VR, multi-user collaborative lab, AR companion.

## 10. References
Harvard style — Unity/XRI/OpenXR docs, scikit-learn paper, VR-in-education
literature.
