using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace VRLab.Assessment
{
    [Serializable]
    public class QuizQuestion
    {
        public string Question;
        public string[] Options = new string[4];
        public int CorrectIndex;
    }

    /// <summary>
    /// JSON-driven quiz displayed on world-space VR panels. Tracks score and
    /// logs each answer to the LearningLogger for pilot evaluation.
    /// Question bank: StreamingAssets/Data/quiz_*.json as {"questions":[...]}.
    /// </summary>
    public class QuizSystem : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI[] optionLabels = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Config")]
        [SerializeField] private string quizFile = "Data/quiz_statistics.json";

        private List<QuizQuestion> questions = new();
        private int current;
        private int correct;

        public event Action<int /*correct*/, int /*total*/> OnQuizCompleted;

        private void Start() => LoadQuiz();

        private void LoadQuiz()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, quizFile);
            if (!System.IO.File.Exists(path))
            {
                feedbackText?.SetText("Quiz file missing.");
                return;
            }
            var wrapper = JsonUtility.FromJson<QuizWrapper>(System.IO.File.ReadAllText(path));
            questions = wrapper.questions != null ? new List<QuizQuestion>(wrapper.questions) : new List<QuizQuestion>();
            ShowQuestion(0);
        }

        private void ShowQuestion(int index)
        {
            if (index < 0 || index >= questions.Count)
            {
                OnQuizCompleted?.Invoke(correct, questions.Count);
                feedbackText?.SetText($"Quiz complete: {correct}/{questions.Count}");
                return;
            }
            current = index;
            var q = questions[index];
            questionText?.SetText(q.Question);
            for (int i = 0; i < optionLabels.Length; i++)
                if (optionLabels[i] != null)
                    optionLabels[i].text = i < q.Options.Length ? q.Options[i] : "";
        }

        /// <summary>Called by VR option buttons with the chosen option index.</summary>
        public void Answer(int optionIndex)
        {
            if (questions.Count == 0) return;
            bool right = optionIndex == questions[current].CorrectIndex;
            if (right) correct++;

            LearningLogger.Instance?.Log("quiz_answer",
                $"q{current}|correct={right}");

            feedbackText?.SetText(right ? "Correct!" : "Not quite — check the chart and try the next one.");
            ShowQuestion(current + 1);
        }

        [Serializable]
        private class QuizWrapper
        {
            public QuizQuestion[] questions;
        }
    }
}
