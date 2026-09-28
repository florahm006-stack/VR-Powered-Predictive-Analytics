using System;
using System.Collections;
using UnityEngine;

namespace VRLab.Interaction
{
    /// <summary>
    /// Debounces rapid slider value changes so expensive work (predictions)
    /// runs only after the user pauses.
    /// </summary>
    public class Debouncer : MonoBehaviour
    {
        [SerializeField] private float delaySeconds = 0.25f;

        private Coroutine pending;

        public void Schedule(Action action)
        {
            if (pending != null) StopCoroutine(pending);
            pending = StartCoroutine(Run(action));
        }

        private IEnumerator Run(Action action)
        {
            yield return new WaitForSeconds(delaySeconds);
            pending = null;
            action?.Invoke();
        }
    }
}
