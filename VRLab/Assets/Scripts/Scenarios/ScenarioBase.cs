using UnityEngine;

namespace VRLab.Scenarios
{
    /// <summary>
    /// Base for playable learning scenarios. Handles scenario root activation
    /// and provides common hooks. Scenarios are plain MonoBehaviours so they
    /// can be authored in the editor and reused in multiplayer later.
    /// </summary>
    public abstract class ScenarioBase : MonoBehaviour
    {
        [SerializeField] private string displayName = "Scenario";
        [SerializeField] private GameObject scenarioRoot;

        public string DisplayName => displayName;

        public virtual void Enter()
        {
            if (scenarioRoot != null) scenarioRoot.SetActive(true);
            OnEnter();
        }

        public virtual void Exit()
        {
            OnExit();
            if (scenarioRoot != null) scenarioRoot.SetActive(false);
        }

        protected abstract void OnEnter();
        protected abstract void OnExit();
    }
}
