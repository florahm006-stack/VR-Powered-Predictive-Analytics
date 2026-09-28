using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRLab.Interaction
{
    /// <summary>
    /// Voice command interface (accessibility requirement). Uses
    /// UnityEngine.Windows.Speech.KeywordRecognizer on Windows/Editor builds;
    /// on other platforms the system degrades gracefully (commands can still
    /// be triggered from buttons).
    /// </summary>
    public class VoiceCommands : MonoBehaviour
    {
        [Serializable]
        public class Command
        {
            public string Keyword;
            public UnityEngine.Events.UnityEvent OnRecognized;
        }

        [SerializeField] private List<Command> commands = new List<Command>
        {
            new Command { Keyword = "next" },
            new Command { Keyword = "back" },
            new Command { Keyword = "reset" },
            new Command { Keyword = "help" },
        };

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private UnityEngine.Windows.Speech.KeywordRecognizer recognizer;
#endif

        private void Start()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var keywords = new System.Collections.Generic.List<string>();
                foreach (var c in commands) keywords.Add(c.Keyword);
                if (keywords.Count == 0) return;

                recognizer = new UnityEngine.Windows.Speech.KeywordRecognizer(keywords.ToArray());
                recognizer.OnPhraseRecognized += HandleRecognized;
                recognizer.Start();
                Debug.Log("[VoiceCommands] Listening for keywords: " + string.Join(", ", keywords));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VoiceCommands] Speech unavailable: {e.Message}");
            }
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private void HandleRecognized(UnityEngine.Windows.Speech.PhraseRecognizedEventArgs args)
        {
            foreach (var c in commands)
                if (string.Equals(c.Keyword, args.text, StringComparison.OrdinalIgnoreCase))
                    c.OnRecognized?.Invoke();
        }

        private void OnDestroy()
        {
            if (recognizer == null) return;
            recognizer.OnPhraseRecognized -= HandleRecognized;
            recognizer.Stop();
            recognizer.Dispose();
        }
#endif
    }
}
