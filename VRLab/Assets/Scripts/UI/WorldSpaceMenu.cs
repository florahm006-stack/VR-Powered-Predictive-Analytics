using TMPro;
using UnityEngine;
using VRLab.Core;
using VRLab.Scenarios;

namespace VRLab.UI
{
    /// <summary>
    /// World-space scenario menu: lists available scenarios as a text list and
    /// routes selection to GameManager. Buttons are uGUI world-space canvas
    /// (XRI Poke Interactor supports uGUI presses; PC mode uses mouse).
    /// </summary>
    public class WorldSpaceMenu : MonoBehaviour
    {
        [SerializeField] private ScenarioBase[] scenarios;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TMP_Text backendIndicator;

        private void Start()
        {
            if (ML.PredictionClient.Instance != null)
            {
                ML.PredictionClient.Instance.OnBackendChanged += b =>
                {
                    if (backendIndicator != null)
                        backendIndicator.text = b == "http" ? "ML: online service" : "ML: offline (local model)";
                };
            }
            SetStatus("Select a scenario to begin.");
        }

        public void SelectScenario(int index)
        {
            if (index < 0 || index >= scenarios.Length) return;
            GameManager.Instance?.EnterScenario(scenarios[index]);
            SetStatus($"Running: {scenarios[index].DisplayName}");
        }

        public void ExitToLobby()
        {
            GameManager.Instance?.ExitActiveScenario();
            SetStatus("Scenario closed.");
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }
    }
}
