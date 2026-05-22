using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace RogueHero5.Editor
{
    [InitializeOnLoad]
    public static class RogueHero5BatchTestRunner
    {
        private const string WatchKey = "RogueHero5.BatchTestRunner.WatchPlayerResults";
        private const string ExpectedResultPathKey = "RogueHero5.BatchTestRunner.ExpectedResultPath";
        private const string PlayerResultPathKey = "RogueHero5.BatchTestRunner.PlayerResultPath";

        private static TestRunnerApi testRunnerApi;
        private static string expectedResultPath;
        private static string playerResultPath;
        private static double runStartedAt;
        private static bool watchingForPlayerResults;

        static RogueHero5BatchTestRunner()
        {
            if (!SessionState.GetBool(WatchKey, false))
            {
                return;
            }

            expectedResultPath = SessionState.GetString(ExpectedResultPathKey, string.Empty);
            playerResultPath = SessionState.GetString(PlayerResultPathKey, string.Empty);
            runStartedAt = EditorApplication.timeSinceStartup;
            watchingForPlayerResults = true;
            EditorApplication.update -= ExitWhenPlayerResultsExist;
            EditorApplication.update += ExitWhenPlayerResultsExist;
        }

        public static void RunEditModeAndExit()
        {
            RunAndExit(TestMode.EditMode, "TestResults-EditMode.xml");
        }

        public static void RunPlayModeAndExit()
        {
            RunAndExit(TestMode.PlayMode, "TestResults-PlayMode.xml");
        }

        private static void RunAndExit(TestMode mode, string resultFileName)
        {
            expectedResultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", resultFileName));
            playerResultPath = Path.Combine(Application.persistentDataPath, "TestResults.xml");
            runStartedAt = EditorApplication.timeSinceStartup;

            if (mode == TestMode.PlayMode && File.Exists(playerResultPath))
            {
                File.Delete(playerResultPath);
            }

            testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunnerApi.RegisterCallbacks(new ExitOnRunFinished(expectedResultPath));

            watchingForPlayerResults = mode == TestMode.PlayMode;
            if (watchingForPlayerResults)
            {
                SessionState.SetBool(WatchKey, true);
                SessionState.SetString(ExpectedResultPathKey, expectedResultPath);
                SessionState.SetString(PlayerResultPathKey, playerResultPath);
                EditorApplication.update += ExitWhenPlayerResultsExist;
            }
            else
            {
                ClearPlayerResultWatch();
            }

            testRunnerApi.Execute(new ExecutionSettings(new Filter
            {
                testMode = mode
            }));
        }

        private static void ExitWhenPlayerResultsExist()
        {
            if (string.IsNullOrEmpty(expectedResultPath))
            {
                expectedResultPath = SessionState.GetString(ExpectedResultPathKey, string.Empty);
            }

            if (string.IsNullOrEmpty(playerResultPath))
            {
                playerResultPath = SessionState.GetString(PlayerResultPathKey, string.Empty);
            }

            if (!watchingForPlayerResults || EditorApplication.timeSinceStartup - runStartedAt < 2.0)
            {
                return;
            }

            if (!File.Exists(playerResultPath) || new FileInfo(playerResultPath).Length == 0)
            {
                return;
            }

            File.Copy(playerResultPath, expectedResultPath, true);
            int failed = ReadFailureCount(expectedResultPath);
            Debug.Log($"RogueHero5 PlayMode player results copied: {expectedResultPath}. Failed: {failed}");
            watchingForPlayerResults = false;
            EditorApplication.update -= ExitWhenPlayerResultsExist;
            ClearPlayerResultWatch();
            EditorApplication.Exit(failed > 0 ? 1 : 0);
        }

        private static void ClearPlayerResultWatch()
        {
            SessionState.SetBool(WatchKey, false);
            SessionState.SetString(ExpectedResultPathKey, string.Empty);
            SessionState.SetString(PlayerResultPathKey, string.Empty);
        }

        private static int ReadFailureCount(string resultPath)
        {
            XmlDocument document = new XmlDocument();
            document.Load(resultPath);
            string failed = document.DocumentElement?.GetAttribute("failed");
            return int.TryParse(failed, out int count) ? count : 1;
        }

        private sealed class ExitOnRunFinished : ICallbacks
        {
            private readonly string resultPath;

            public ExitOnRunFinished(string resultPath)
            {
                this.resultPath = resultPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"RogueHero5 batch test run started: {testsToRun.FullName}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                watchingForPlayerResults = false;
                EditorApplication.update -= ExitWhenPlayerResultsExist;
                ClearPlayerResultWatch();
                TestRunnerApi.SaveResultToFile(result, resultPath);
                Debug.Log($"RogueHero5 batch test run finished: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped. Results: {resultPath}");
                EditorApplication.Exit(result.FailCount > 0 ? 1 : 0);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
