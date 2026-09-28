using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace VRLab.Assessment
{
    /// <summary>
    /// Logs learning events (engagement time, interactions, quiz answers,
    /// scenario completions) to a CSV in persistentDataPath for the pilot
    /// evaluation (brief section 7).
    /// </summary>
    public class LearningLogger : MonoBehaviour
    {
        public static LearningLogger Instance { get; private set; }

        private string filePath;
        private readonly StringBuilder buffer = new();
        private float sessionStart;
        private int flushCounter;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            string fileName = $"learning_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            filePath = Path.Combine(Application.persistentDataPath, fileName);
            sessionStart = Time.realtimeSinceStartup;

            File.WriteAllText(filePath, "timestamp,elapsed_s,event,detail\n");
            Log("session_start", SystemInfo.deviceModel);
        }

        public void Log(string eventType, string detail = "")
        {
            string detailSafe = (detail ?? "").Replace(",", ";").Replace("\n", " ");
            buffer.AppendLine(string.Join(",",
                DateTime.Now.ToString("o", CultureInfo.InvariantCulture),
                (Time.realtimeSinceStartup - sessionStart).ToString("F1", CultureInfo.InvariantCulture),
                eventType,
                detailSafe));

            if (++flushCounter % 10 == 0) Flush();
        }

        public void Flush()
        {
            if (buffer.Length == 0) return;
            File.AppendAllText(filePath, buffer.ToString());
            buffer.Clear();
        }

        private void OnApplicationQuit()
        {
            Log("session_end");
            Flush();
        }
    }
}
