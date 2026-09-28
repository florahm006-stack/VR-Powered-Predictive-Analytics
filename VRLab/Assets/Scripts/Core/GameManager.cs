using UnityEngine;
using VRLab.Scenarios;

namespace VRLab.Core
{
    /// <summary>
    /// Central session/state manager. Owns scene flow and references shared services.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scenarios")]
        [SerializeField] private ScenarioBase[] scenarios;
        [SerializeField] private int startScenarioIndex = -1; // -1 = stay in lobby

        public ScenarioBase ActiveScenario { get; private set; }
        public bool IsXRActive => RigSetup.Instance != null && RigSetup.Instance.IsXRActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (startScenarioIndex >= 0 && startScenarioIndex < scenarios.Length)
                EnterScenario(scenarios[startScenarioIndex]);
        }

        public void EnterScenario(ScenarioBase scenario)
        {
            if (ActiveScenario != null)
                ActiveScenario.Exit();

            ActiveScenario = scenario;
            ActiveScenario.Enter();
            Debug.Log($"[GameManager] Entered scenario: {scenario.DisplayName}");
        }

        public void ExitActiveScenario()
        {
            if (ActiveScenario == null) return;
            Debug.Log($"[GameManager] Exited scenario: {ActiveScenario.DisplayName}");
            ActiveScenario.Exit();
            ActiveScenario = null;
        }
    }
}
