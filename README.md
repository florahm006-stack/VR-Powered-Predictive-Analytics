# VR-Powered Predictive Analytics Lab

An immersive Unity/C# VR application that lets students visualize large datasets, interact with 3D charts, and apply machine-learning techniques in a virtual space — bridging the gap between theory and practical application of predictive analytics.

Based on the assessment brief in [`Metaverse Project.pdf`](Metaverse%20Project.pdf).

---

## Table of Contents

- [Overview](#overview)
- [Objectives](#objectives)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Unity Setup](#unity-setup)
  - [ML Service Setup](#ml-service-setup)
  - [Running in VR / PC Fallback](#running-in-vr--pc-fallback)
- [Scenarios](#scenarios)
- [Accessibility](#accessibility)
- [Assessment & Feedback](#assessment--feedback)
- [Testing](#testing)
- [Milestones / Roadmap](#milestones--roadmap)
- [Edge Cases & Failure Modes](#edge-cases--failure-modes)
- [Success Criteria](#success-criteria)
- [Future Recommendations](#future-recommendations)

---

## Overview

Students put on a VR headset (or use PC fallback mode) and enter a virtual analytics lab. Inside, they can:

- Walk up to interactive 3D charts (scatter, line, bar) rendered from real datasets.
- Watch real-time simulated data feeds (e.g., a live stock ticker).
- Adjust model variables with sliders and see AI-driven predictions update instantly.
- Complete quizzes, earn points/badges, and have their learning progress tracked.

## Objectives

- Enable students to interact with large datasets in a VR space.
- Improve understanding of statistical models through real-time simulations.
- Bridge the gap between theory and practical application of machine learning.

## Features

- **VR dashboard** — explore real-time statistical models in an immersive 3D environment.
- **3D charting** — GPU-instanced scatter plots, line charts, and bar charts driven by CSV data; auto-rescaling axes and billboard labels.
- **AI-driven predictions** — adjust variables and observe outcomes in real time via a Python ML service, with automatic fallback to on-device ONNX inference.
- **Case studies** — real-world business and economic datasets (stock market, business forecasting).
- **Gamification** — points, badges, and accuracy scoring.
- **Assessment tools** — VR quiz panels, real-time feedback, progress tracking, and learning logs exported as CSV.
- **Multi-device compatibility** — VR headset (Quest 2/3, PC-VR) and PC keyboard/mouse fallback.
- **Pilot-test instrumentation** — in-app feedback form and engagement/analytics event logging.
- **Comfort first** — teleport locomotion by default, vignette on smooth motion, seated mode.

## Tech Stack

| Layer | Technology |
|---|---|
| Engine | **Unity 6000.6.x LTS**, C# |
| VR | XR Interaction Toolkit **3.6.1**, OpenXR **1.18.0** (Quest 2/3 + PC-VR), Input System |
| Rendering | Universal Render Pipeline **17.0.4**, TextMeshPro |
| ML service | Python 3, FastAPI, scikit-learn (GradientBoostingRegressor, REST) |
| On-device ML | **Unity Inference Engine (Sentis) 2.2** — ONNX models exported by `train_model.py`, offline fallback with graceful degradation to closed-form coefficients |
| Data | CSV datasets in `StreamingAssets/Data`, JSON scenario/quiz configs |
| UI/UX | World-space uGUI canvases with XRI poke/ray, grab-drag sliders, haptics, voice commands |
| Version control | Git + Git LFS |

## Architecture

```
┌────────────────────────────────────────────────────────────┐
│                      Unity (C#)                            │
│  ┌──────────┐  ┌──────────┐  ┌────────────┐  ┌─────────┐   │
│  │ Core/    │  │ Data     │  │ 3D Charts  │  │ UI/UX & │   │
│  │ GameMgr  │  │ Layer    │  │ (instanced)│  │ Access. │   │
│  │ XR Rig   │  │ CSV/Feed │  │            │  │ Voice   │   │
│  └────┬─────┘  └────┬─────┘  └─────┬──────┘  └─────────┘   │
│       │             │              │                        │
│  ┌────▼─────────────▼──────────────▼──────┐                 │
│  │  IPredictionBackend (interface)        │                 │
│  │  ├─ HttpPredictionBackend (service)    │─┐               │
│  │  ├─ SentisPredictionBackend (ONNX)    │ │               │
│  │  └─ OnnxPredictionBackend (fallback)  │ │               │
│  └────────────────────────────────────────┘ │               │
└─────────────────────────────────────────────┼───────────────┘
                                              │ HTTP / WebSocket
                              ┌───────────────▼───────────────┐
                              │  FastAPI ML service (Python)  │
                              │  /predict/linear              │
                              │  /predict/forecast            │
                              │  scikit-learn / TensorFlow    │
                              └───────────────────────────────┘
```

### Data flow

1. `DatasetLoader` reads CSV/feed → `Dataset`/`DataSeries`.
2. Charts render series via GPU-instanced draw calls.
3. Parameter sliders → debounced `PredictionClient.PredictAsync()` → chart overlay updates.
4. Quizzes and interactions → event log → CSV export for pilot evaluation.

### Key interfaces

```csharp
public interface IPredictionBackend
{
    Task<PredictionResult> PredictAsync(ModelId model, float[] features);
}
```

- `HttpPredictionBackend` — talks to the Python FastAPI service.
- `SentisPredictionBackend` — true on-device ONNX inference via Unity Inference Engine (models in `StreamingAssets/Models`).
- `OnnxPredictionBackend` — closed-form coefficient fallback (always available, keeps demos playable).

## Project Structure

```
├── Metaverse Project.pdf        # Assessment brief
├── README.md                    # This file
├── VRLab/                       # Unity 6000.6 project
│   ├── Build/VRLab.exe          # Verified Windows player build
│   └── Assets/
│       ├── Scenes/Lab.unity     # Pre-composed lab scene (LabSceneBuilder.Build)
│       ├── Editor/              # LabSceneBuilder (scene composer), RunTestsCli (batch tests)
│       ├── Scripts/
│       │   ├── Core/            # GameManager, RigSetup (XR↔PC auto), comfort locomotion
│       │   ├── Data/            # DatasetLoader (CSV), StreamingDataFeed
│       │   ├── Charts/          # GPU-instanced Chart3D, ScatterPlot3D, LineChart3D, BarChart3D
│       │   ├── ML/              # PredictionClient (3-tier failover), Http /
│       │   │                    #   SentisPredictionBackend (ONNX) / closed-form fallback
│       │   ├── Interaction/     # ParameterSlider + SliderGrabDriver (XRI grab), Debouncer,
│       │   │                    #   VoiceCommands, HapticFeedback
│       │   ├── UI/              # WorldSpaceMenu, Billboard
│       │   ├── Scenarios/       # ScenarioBase, StockMarketScenario, ForecastingScenario
│       │   └── Assessment/      # QuizSystem, LearningLogger (CSV export)
│       ├── Tests/EditMode/      # 11 tests, all passing
│       ├── XRI Starter Assets/  # Imported XRI sample (XR Origin rig)
│       └── StreamingAssets/
│           ├── Data/            # stocks.csv, sales.csv, quiz_statistics.json
│           └── Models/          # demand_model.onnx, linear_model.onnx
├── ml-service/                  # Python FastAPI service (verified)
│   ├── app.py, train_model.py, requirements.txt
│   └── models/                  # Trained joblib + ONNX exports
└── docs/                        # SETUP, ARCHITECTURE, PILOT_TESTING, REPORT_SKELETON
```

## Getting Started

### Prerequisites

- Unity Hub + **Unity 6000.6.x LTS** (Android Build Support for Quest deployment) — see `docs/SETUP.md` for exact package pins
- Python 3.10+
- A Quest 2/3 headset (optional — PC fallback mode works without one)
- Git + Git LFS

### Unity Setup

1. Open `VRLab/` in Unity Hub.
2. Packages (installed via Package Manager / manifest):
   - XR Plugin Management + **OpenXR** provider
   - XR Interaction Toolkit (with Input System)
   - TextMeshPro, Newtonsoft JSON
3. Open the main scene and press Play.

### ML Service Setup

```powershell
cd ml-service
python -m venv .venv
.venv\Scripts\Activate
pip install -r requirements.txt
uvicorn app:app --reload --port 8000
```

Endpoints:

| Endpoint | Description |
|---|---|
| `POST /predict/linear` | Linear/logistic regression prediction |
| `POST /predict/forecast` | Time-series forecast (next-N values) |

If the service is unreachable, Unity automatically switches to the on-device ONNX backend and shows a notice in the UI.

### Running in VR / PC Fallback

- **VR:** Connect a headset (Link/Air Link for PC-VR, or build an APK for Quest). OpenXR bootstraps the XR Origin with controllers, teleport locomotion, and comfort options.
- **PC fallback:** If no headset is detected, the app boots into a first-person keyboard/mouse mode with the same scenes and interactions.

## Scenarios

1. **Stock Market Simulation** — a live-tick price chart with volume bars and student picks; a prediction overlay shows the next-N forecast. Students analyze real-time trends.
2. **Business Forecasting** — adjust variables (price, marketing spend, seasonality) with sliders and watch the demand forecast update instantly. Earn points/badges for prediction accuracy.

## Accessibility

- Voice commands (keyword recognizer / speech behind an interface).
- Colorblind-safe chart palette.
- Seated mode and adjustable text size.
- Teleport locomotion default; optional smooth motion with vignette.
- Multi-device: VR headset, PC, and (roadmap) mobile.

## Assessment & Feedback

- JSON-driven multiple-choice quizzes on world-space VR panels.
- Real-time feedback on predictions and quiz answers.
- Per-session learning log (engagement time, interactions, scores) exported to CSV for the pilot evaluation (brief section 7).

## Testing

- **EditMode tests:** CSV parsing, forecast-math validation against the Python reference.
- **PlayMode tests:** chart updates on new data, prediction round-trip against a mock server.
- **Manual acceptance:** run both scenarios end-to-end on Quest 2/3 (or Link) and in PC mode; verify quiz + logging.

## Milestones / Roadmap

1. ✅ Bootstrap project, packages, git, XR rig + PC fallback.
2. ✅ Data layer + 3D charts on sample datasets.
3. ✅ Python ML service + `PredictionClient` + on-device ONNX fallback (verified live).
4. ✅ Stock Market scenario.
5. ✅ Business Forecasting scenario + gamification.
6. ✅ UI/UX polish — grab-drag VR sliders, poked UI, haptics, voice, comfort options.
7. ✅ Quiz/assessment + analytics logging.
8. ✅ Lab scene composed (`Assets/Scenes/Lab.unity`), 11/11 tests passing, Windows player builds.
9. ⬜ Quest APK build + pilot-testing session + report sections 7–9 with real data.

## Edge Cases & Failure Modes

| Case | Handling |
|---|---|
| ML service offline | Automatic fallback to local ONNX inference + UI notice |
| Empty/malformed CSV | Guard + in-app error panel |
| NaN/outliers in data | Clamped chart scales |
| No headset detected | Boot into PC fallback mode |
| Motion sickness | Teleport default, vignette on smooth move, comfort options |
| Quest performance | GPU instancing, bounded chart point budgets, quality tiers |

## Success Criteria

- Runs in VR (Quest/PC-VR via OpenXR) with a PC fallback mode. ✅ (player build verified)
- VR dashboard with interactive 3D charts from real datasets (CSV). ✅
- Two playable scenarios: Stock Market Simulation and Business Forecasting. ✅
- Working ML prediction pipeline callable from C#, with 3-tier backend failover. ✅ (verified)
- Grabbable/pokeable UI, voice commands, quiz/feedback module, progress tracking. ✅
- Report-ready documentation matching sections 1–10 of the assessment brief. ✅ (skeleton + pilot guide)

## Future Recommendations

- AI-driven tutoring inside VR.
- Multi-user collaborative VR analytics lab (Netcode-ready abstractions).
- Mobile AR companion (ARKit/ARCore).
