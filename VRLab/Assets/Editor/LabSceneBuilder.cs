using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRLab.Assessment;
using VRLab.Charts;
using VRLab.Core;
using VRLab.Data;
using VRLab.Interaction;
using VRLab.ML;
using VRLab.Scenarios;
using VRLab.UI;

namespace VRLab.EditorTools
{
    /// <summary>
    /// CLI entry point: Unity.exe -batchmode -projectPath <proj>
    ///   -executeMethod VRLab.EditorTools.LabSceneBuilder.Build -quit
    /// Composes the main VR lab scene: managers, rigs (XR from Starter Assets +
    /// PC fallback), world-space menu, both scenario stations, and a quiz panel.
    /// Success marker: "[LabSceneBuilder] DONE".
    /// </summary>
    public static class LabSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Lab.unity";
        private const string Green = "2E8B57", Blue = "3B6EA5", Amber = "C47F17";

        public static void Build()
        {
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                BuildEnvironment();
                var managers = BuildManagers();
                var pcRig = BuildPcFallbackRig();
                var rigSetup = BuildRigSetup(pcRig);
                var menu = BuildMenu();
                var stock = BuildStockStation();
                var forecast = BuildForecastStation();
                var quiz = BuildQuizPanel();

                // Wire GameManager scenarios.
                Wire(managers.GetComponent<GameManager>(),
                    ("scenarios", new ScenarioBase[] { stock.GetComponent<ScenarioBase>(), forecast.GetComponent<ScenarioBase>() }),
                    ("startScenarioIndex", -1));

                // Wire menu buttons.
                var wsm = menu.GetComponent<WorldSpaceMenu>();
                Wire(wsm, ("scenarios", new ScenarioBase[] { stock.GetComponent<ScenarioBase>(), forecast.GetComponent<ScenarioBase>() }));

                Directory.CreateDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.SaveAssets();
                Debug.Log("[LabSceneBuilder] DONE " + ScenePath);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[LabSceneBuilder] FAILED: " + e);
                EditorApplication.Exit(4);
            }
        }

        // ---------------------------------------------------------------- scene pieces

        private static void BuildEnvironment()
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(4, 1, 4); // 40m x 40m
            Tint(floor, HexColor(Green, 0.25f));

            var wallsMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            wallsMat.color = new Color(0.12f, 0.14f, 0.18f);
        }

        private static GameObject BuildManagers()
        {
            var go = new GameObject("Managers");
            go.AddComponent<GameManager>();
            go.AddComponent<PredictionClient>();
            go.AddComponent<LearningLogger>();
            return go;
        }

        private static GameObject BuildPcFallbackRig()
        {
            var go = new GameObject("PCFallbackRig");
            go.transform.position = new Vector3(0, 1.6f, -4);
            var camGo = new GameObject("PCCamera");
            camGo.transform.SetParent(go.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            go.AddComponent<CharacterController>();
            go.AddComponent<PCFallbackController>();

            // EventSystem with XRI UI input module so controllers can press uGUI buttons.
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var uiModule = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
            if (uiModule != null) es.AddComponent(uiModule);
            else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            return go;
        }

        private static RigSetup BuildRigSetup(GameObject pcRig)
        {
            var go = new GameObject("RigSetup");
            var rig = go.AddComponent<RigSetup>();

            GameObject xrRig = null;
            var prefabGuid = AssetDatabase.FindAssets("t:Prefab \"XR Origin (VR)\"",
                new[] { "Assets/XRI Starter Assets" }).FirstOrDefault()
                ?? AssetDatabase.FindAssets("t:Prefab \"XR Origin\"").FirstOrDefault();
            if (prefabGuid != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(prefabGuid));
                xrRig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                xrRig.name = "XRRig";
                xrRig.AddComponent<ComfortLocomotion>();
            }
            else
            {
                xrRig = new GameObject("XRRig (placeholder)");
                Debug.LogWarning("[LabSceneBuilder] XR Origin prefab not found in Starter Assets; placeholder created.");
            }

            Wire(rig, ("xrRig", xrRig), ("pcFallbackRig", pcRig));
            return rig;
        }

        private static GameObject BuildMenu()
        {
            var panel = CreateCanvasPanel("ScenarioMenu", new Vector3(0, 1.6f, 2.2f), new Vector2(1.6f, 0.9f));
            var wsm = panel.AddComponent<WorldSpaceMenu>();

            var status = AddText(panel, "Status", "Select a scenario:", new Vector2(0, 0.28f), 36);
            var backend = AddText(panel, "Backend", "ML: —", new Vector2(0, 0.40f), 24);
            AddButton(panel, "Stock Market", new Vector2(-0.35f, 0)).onClick.AddListener(() => wsm.SelectScenario(0));
            AddButton(panel, "Forecasting", new Vector2(0.35f, 0)).onClick.AddListener(() => wsm.SelectScenario(1));
            Wire(wsm, ("statusText", status.GetComponent<TextMeshProUGUI>()),
                      ("backendIndicator", backend.GetComponent<TMP_Text>()));
            return panel;
        }

        private static GameObject BuildStockStation()
        {
            var root = StationRoot("StockMarketScenario", new Vector3(-4, 0, 4));
            var s = AddScenario<StockMarketScenario>(root);

            var content = root.transform.Find("Content").gameObject;
            var price = AddChart<LineChart3D>(content, "PriceChart", new Vector3(0, 1.4f, 0));
            var forecast = AddChart<LineChart3D>(content, "ForecastOverlay", new Vector3(0, 1.4f, 0.02f));
            Tint(forecast, HexColor(Amber));
            var volume = AddChart<BarChart3D>(content, "VolumeChart", new Vector3(0, 0.55f, 0));

            var feed = root.AddComponent<StreamingDataFeed>();
            Wire(s, ("feed", feed), ("priceChart", price.GetComponent<Chart3D>()),
                    ("volumeChart", volume.GetComponent<Chart3D>()),
                    ("forecastOverlay", forecast.GetComponent<LineChart3D>()));
            return root;
        }

        private static GameObject BuildForecastStation()
        {
            var root = StationRoot("ForecastingScenario", new Vector3(4, 0, 4));
            var s = AddScenario<ForecastingScenario>(root);
            root.AddComponent<Debouncer>();

            var content = root.transform.Find("Content").gameObject;
            var price = AddSlider(content, "Price", new Vector3(-0.5f, 1.2f, 0), 5, 200);
            var marketing = AddSlider(content, "Marketing", new Vector3(0, 1.2f, 0), 0, 10000);
            var season = AddSlider(content, "Season", new Vector3(0.5f, 1.2f, 0), -1, 1);
            var chart = AddChart<LineChart3D>(content, "ForecastChart", new Vector3(0, 2.0f, 0));

            var demand = AddText(content, "DemandReadout", "—", new Vector2(0, 1.55f), 28, worldSpaceAt: new Vector3(0, 1.55f, 0));
            var score = AddText(content, "ScoreReadout", "Score: 0", new Vector2(0, 1.75f), 28, worldSpaceAt: new Vector3(0, 1.75f, 0));

            Wire(s, ("priceSlider", price), ("marketingSlider", marketing), ("seasonSlider", season),
                    ("forecastChart", chart.GetComponent<LineChart3D>()),
                    ("demandReadout", demand.GetComponent<TextMeshProUGUI>()),
                    ("scoreReadout", score.GetComponent<TextMeshProUGUI>()));
            return root;
        }

        private static GameObject BuildQuizPanel()
        {
            var panel = CreateCanvasPanel("QuizPanel", new Vector3(0, 1.6f, 6), new Vector2(2.2f, 1.4f));
            panel.AddComponent<QuizSystem>();
            var q = panel.GetComponent<QuizSystem>();
            var question = AddText(panel, "Question", "Question…", new Vector2(0, 0.45f), 28);
            var feedback = AddText(panel, "Feedback", "", new Vector2(0, -0.55f), 24);
            var options = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var b = AddButton(panel, $"Option{i}", new Vector2(i % 2 == 0 ? -0.5f : 0.5f, 0.25f - (i / 2) * 0.35f));
                options[i] = b.GetComponentInChildren<TextMeshProUGUI>();
                b.onClick.AddListener(() => q.Answer(idx));
            }
            Wire(q, ("questionText", question.GetComponent<TextMeshProUGUI>()),
                    ("feedbackText", feedback.GetComponent<TextMeshProUGUI>()),
                    ("optionLabels", options));
            return panel;
        }

        // ---------------------------------------------------------------- helpers

        private static GameObject StationRoot(string name, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.position = pos;

            // Content container toggled by ScenarioBase.Enter/Exit.
            var content = new GameObject("Content");
            content.transform.SetParent(root.transform, false);

            var podium = GameObject.CreatePrimitive(PrimitiveType.Cube);
            podium.name = "Podium";
            podium.transform.SetParent(content.transform, false);
            podium.transform.localPosition = new Vector3(0, 0.5f, 0);
            podium.transform.localScale = new Vector3(2.4f, 1.0f, 0.8f);
            Tint(podium, HexColor(Blue, 0.6f));
            return root;
        }

        /// <summary>Attach a scenario component and point its scenarioRoot at the Content child.</summary>
        private static T AddScenario<T>(GameObject root) where T : ScenarioBase
        {
            var s = root.AddComponent<T>();
            var content = root.transform.Find("Content");
            Wire(s, ("scenarioRoot", content != null ? content.gameObject : root));
            root.SetActive(true); // root must stay active; ScenarioBase toggles Content
            content?.gameObject.SetActive(false);
            return s;
        }

        private static GameObject AddChart<T>(GameObject parent, string name, Vector3 localPos) where T : Chart3D
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;

            // Chart needs a point mesh + material; use a cube from a hidden prototype.
            var proto = GameObject.CreatePrimitive(PrimitiveType.Cube);
            proto.name = name + "_Proto";
            proto.transform.SetParent(parent.transform, false);
            proto.SetActive(false);
            var mesh = proto.GetComponent<MeshFilter>().sharedMesh;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = HexColor(Green),
                enableInstancing = true // required by Graphics.RenderMeshInstanced
            };

            var chart = go.AddComponent<T>();
            Wire(chart, ("pointMesh", mesh), ("pointMaterial", mat));
            return go;
        }

        private static ParameterSlider AddSlider(GameObject parent, string label, Vector3 localPos, float min, float max)
        {
            var go = new GameObject("Slider_" + label);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;

            var track = GameObject.CreatePrimitive(PrimitiveType.Cube);
            track.transform.SetParent(go.transform, false);
            track.transform.localScale = new Vector3(0.5f, 0.02f, 0.02f);
            Tint(track, Color.gray);

            var handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = "Handle";
            handle.transform.SetParent(go.transform, false);
            handle.transform.localScale = Vector3.one * 0.06f;
            Tint(handle, HexColor(Amber));

            var slider = go.AddComponent<ParameterSlider>();
            Wire(slider, ("min", min), ("max", max), ("value", (min + max) / 2f),
                         ("label", label), ("handle", handle.transform));

            // VR grab: kinematic rigidbody + grab interactable on the handle,
            // SliderGrabDriver maps handle position -> slider value.
            var rb = handle.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var grabType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
            if (grabType != null)
            {
                var grab = handle.AddComponent(grabType);
                var driver = go.AddComponent<SliderGrabDriver>();
                Wire(driver, ("grab", grab));
            }
            return slider;
        }

        private static GameObject CreateCanvasPanel(string name, Vector3 position, Vector2 sizeMeters)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            // TrackedDeviceGraphicRaycaster enables controller poke/ray presses on uGUI.
            var trackedRaycaster = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (trackedRaycaster != null) go.AddComponent(trackedRaycaster);
            else go.AddComponent<GraphicRaycaster>();
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.transform.position = position;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizeMeters * 1000f;
            go.transform.localScale = Vector3.one * 0.001f;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var brt = (RectTransform)bg.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            return go;
        }

        private static GameObject AddText(GameObject parent, string name, string content, Vector2 anchored, int fontSize,
            Vector3? worldSpaceAt = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            if (parent.GetComponent<Canvas>() == null && worldSpaceAt.HasValue)
            {
                // World-space floating label.
                var canvasGo = new GameObject(name + "_Canvas", typeof(RectTransform), typeof(Canvas), typeof(Billboard));
                canvasGo.transform.SetParent(parent.transform, false);
                canvasGo.transform.localPosition = worldSpaceAt.Value;
                canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var crt = (RectTransform)canvasGo.transform;
                crt.sizeDelta = new Vector2(1200, 160);
                canvasGo.transform.localScale = Vector3.one * 0.001f;
                go.transform.SetParent(canvasGo.transform, false);
            }
            else
            {
                go.transform.SetParent(parent.transform, false);
            }

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(1200, fontSize * 1.6f);
            return go;
        }

        private static Button AddButton(GameObject canvasPanel, string name, Vector2 anchored)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvasPanel.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(0.55f * 1000f, 140);
            go.GetComponent<Image>().color = HexColor(Blue);

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = name;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            ((RectTransform)label.transform).anchorMin = Vector2.zero;
            ((RectTransform)label.transform).anchorMax = Vector2.one;
            ((RectTransform)label.transform).offsetMin = ((RectTransform)label.transform).offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private static void Tint(GameObject go, Color c)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var m = new Material(r.sharedMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")));
                m.color = c;
                r.sharedMaterial = m;
            }
        }

        private static void Wire(Component target, params (string field, object value)[] fields)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in fields)
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogWarning($"[LabSceneBuilder] No field '{field}' on {target.GetType().Name}"); continue; }
                SetValue(p, value);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(SerializedProperty p, object value)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Float: p.floatValue = Convert.ToSingle(value); break;
                case SerializedPropertyType.Integer: p.intValue = Convert.ToInt32(value); break;
                case SerializedPropertyType.String: p.stringValue = (string)value; break;
                case SerializedPropertyType.Boolean: p.boolValue = (bool)value; break;
                case SerializedPropertyType.ObjectReference:
                    if (value is Array arr)
                    {
                        p.arraySize = arr.Length;
                        for (int i = 0; i < arr.Length; i++)
                            p.GetArrayElementAtIndex(i).objectReferenceValue = arr.GetValue(i) as UnityEngine.Object;
                    }
                    else p.objectReferenceValue = value as UnityEngine.Object;
                    break;
                default:
                    if (value is Array genArr && p.isArray)
                    {
                        p.arraySize = genArr.Length;
                        for (int i = 0; i < genArr.Length; i++)
                            SetValue(p.GetArrayElementAtIndex(i), genArr.GetValue(i));
                    }
                    else Debug.LogWarning($"[LabSceneBuilder] Unsupported property type {p.propertyType} for {p.displayName}");
                    break;
            }
        }



        private static Color HexColor(string hex, float? alpha = null)
        {
            var c = new Color(
                Convert.ToInt32(hex.Substring(0, 2), 16) / 255f,
                Convert.ToInt32(hex.Substring(2, 2), 16) / 255f,
                Convert.ToInt32(hex.Substring(4, 2), 16) / 255f);
            if (alpha.HasValue) c.a = alpha.Value;
            return c;
        }
    }
}
