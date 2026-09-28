using System;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace VRLab.EditorTools
{
    /// <summary>
    /// CLI entry point: Unity.exe -batchmode -projectPath <proj> -executeMethod VRLab.EditorTools.RunTestsCli.RunEditMode -quit
    /// Writes a summary of EditMode test results into Editor.log (captured via -logFile)
    /// and exits non-zero on failure or zero tests.
    /// </summary>
    public static class RunTestsCli
    {
        public static void RunEditMode() => Run(TestMode.EditMode, synchronous: true);
        public static void RunPlayMode() => Run(TestMode.PlayMode, synchronous: false);

        private static Handle activeHandle;

        private static void Run(TestMode mode, bool synchronous)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            activeHandle = new Handle();
            api.RegisterCallbacks(activeHandle);
            api.Execute(new ExecutionSettings
            {
                filters = new[] { new Filter { testMode = mode } },
                runSynchronously = synchronous
            });

            if (synchronous)
            {
                while (!activeHandle.Done) System.Threading.Thread.Sleep(200);
                Finish();
            }
            else
            {
                // PlayMode is always async — pump via editor update callbacks.
                EditorApplication.update += Pump;
            }
        }

        private static void Pump()
        {
            if (activeHandle == null || !activeHandle.Done) return;
            EditorApplication.update -= Pump;
            Finish();
        }

        private static void Finish()
        {
            var h = activeHandle;
            Debug.Log($"[RunTestsCli] RESULT passed={h.Passed} failed={h.Failed} skipped={h.Skipped}::\n{h.Failures}");
            EditorApplication.Exit(h.Failed > 0 || h.Passed == 0 ? 3 : 0);
        }

        private class Handle : ICallbacks
        {
            public bool Done;
            public int Passed, Failed, Skipped;
            public string Failures = "";

            void ICallbacks.RunStarted(ITestAdaptor testsToRun) { }

            void ICallbacks.TestStarted(ITestAdaptor test) { }

            void ICallbacks.TestFinished(ITestResultAdaptor result)
            {
                if (!result.Test.IsSuite)
                {
                    switch (result.TestStatus)
                    {
                        case TestStatus.Passed: Passed++; break;
                        case TestStatus.Failed: Failed++; Failures += $"  FAILED {result.Test.FullName}: {result.Message}\n"; break;
                        default: Skipped++; break;
                    }
                }
                else if (result.TestStatus != TestStatus.Inconclusive && result.Test.Name == "EditMode")
                {
                    Done = result.TestStatus == TestStatus.Passed || result.TestStatus == TestStatus.Failed || result.TestStatus == TestStatus.Skipped;
                }
            }

            void ICallbacks.RunFinished(ITestResultAdaptor result)
            {
                foreach (var child in result.Children) CountLeaf((ITestResultAdaptor)child);
                Done = true;
            }

            private void CountLeaf(ITestResultAdaptor r)
            {
                foreach (var c in r.Children) CountLeaf((ITestResultAdaptor)c);
            }
        }
    }
}
