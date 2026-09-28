# Setup Guide — VR-Powered Predictive Analytics Lab

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| Unity Hub | any | Used to install/manage editors |
| Unity Editor | **6000.6.3f1** | Installed on this machine: `C:\Program Files\Unity\Hub\Editor\6000.6.3f1`. Android Build Support module needed for Quest builds |
| Python | 3.10+ | For the ML service (verified with 3.14) |
| Git + Git LFS | any | LFS tracks `.apk`, `.onnx`, media |

> **Version pins matter:** this editor (Unity 6.6) turns older XR-package
> deprecated APIs into hard compile errors. The project is pinned to
> XRI **3.6.1**, OpenXR **1.18.0**, Inference Engine (Sentis) **2.2.0**,
> URP **17.0.4** in `Packages/manifest.json`. Do not downgrade these.

## First run

1. **Open the project:** Unity Hub → Add → `VRLab/`. First open resolves
   packages from `Packages/manifest.json`. The XRI **Starter Assets** sample is
   already imported at `Assets/XRI Starter Assets/`.
2. **Scene:** `Assets/Scenes/Lab.unity` is pre-composed (menu, both scenario
   stations, quiz panel, XR rig from Starter Assets, PC fallback rig, EventSystem
   with XR UI input module). To regenerate after edits, call:
   ```
   Unity.exe -batchmode -projectPath VRLab -executeMethod VRLab.EditorTools.LabSceneBuilder.Build -quit
   ```
3. **XR configuration:** Edit → Project Settings → XR Plug-in Management →
   enable **OpenXR** (PC tab: your OpenXR runtime; Android tab for Quest).
   Add the interaction profiles for your controllers.
4. **ML service** (optional but recommended): see `ml-service/README.md`.
   Without it, the app automatically falls back to **on-device ONNX inference**
   (Unity Inference Engine, models in `Assets/StreamingAssets/Models/`), shown
   in the UI as `ML: offline (local model)`.
5. **Run tests:** Test Runner → EditMode → Run All (currently **11/11 passing**),
   or from CLI:
   ```
   Unity.exe -batchmode -projectPath VRLab -executeMethod VRLab.EditorTools.RunTestsCli.RunEditMode -quit
   ```
   (Result line `[RunTestsCli] RESULT passed=…` appears in the `-logFile` log.
   PlayMode tests are not run headless — batchmode play-mode hangs on this
   machine; run them from the Test Runner GUI.)

## Windows player

```
Unity.exe -batchmode -projectPath VRLab -buildWindows64Player VRLab/Build/VRLab.exe -quit
```
Note: first build takes a while — Inference Engine ships many compute shaders.
Output: `VRLab/Build/VRLab.exe` (verified building successfully).

## Quest build

1. Switch platform to **Android**, Texture Compression = ASTC.
2. XR Plug-in Management → Android → enable **OpenXR** (+ Meta Quest features).
3. Min API 24+, IL2CPP + ARM64, Graphics API Vulkan.
4. Build → deploy APK to the headset. Learning logs land in
   `persistentDataPath` (`Android/data/<package>/files`).

## PC fallback mode

No headset? `RigSetup` auto-detects this and runs the FPS controller —
WASD + mouse; scroll over sliders to change values; all scenarios, the menu and
quizzes work. Press **Esc** to release the cursor.

## Interacting

- **VR:** teleport with the thumbstick (default; smooth locomotion available via
  `ComfortLocomotion`), grab slider handles to drag them, poke UI buttons.
- **Voice commands:** `next`, `back`, `reset`, `help` (Windows speech keywords).
