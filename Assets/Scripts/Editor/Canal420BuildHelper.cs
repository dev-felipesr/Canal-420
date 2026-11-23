using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Canal420.Editor
{
    public static class Canal420BuildHelper
    {
        private const string WindowsBuildOutputDir = "Builds/Windows";
        private const string WindowsExecutableName = "Canal420.exe";
        private const string MacBuildOutputDir = "Builds/macOS";
        private const string MacAppBundleName = "Canal 420.app";

        [MenuItem("Canal 420/Exportar Jogo para Windows (.exe)", priority = 20)]
        public static void BuildWindowsStandalone()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Canal 420 - Build",
                    "Saia do Play Mode antes de iniciar a exportação.",
                    "OK"
                );
                return;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullOutputDir = Path.Combine(projectRoot, WindowsBuildOutputDir);
            string exePath = Path.Combine(fullOutputDir, WindowsExecutableName);

            if (!Directory.Exists(fullOutputDir))
            {
                Directory.CreateDirectory(fullOutputDir);
            }

            string[] scenes = new[] { "Assets/Scenes/SampleScene.unity" };

            Debug.Log($"[Canal 420] Iniciando build nativa para Windows 64-bit em: {exePath}");

            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            BuildSummary summary;
            try
            {
                if (previousBackend != ScriptingImplementation.Mono2x)
                {
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
                }

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = exePath,
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                summary = report.summary;
            }
            finally
            {
                if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) != previousBackend)
                {
                    PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previousBackend);
                }
            }

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[Canal 420] Build de Windows concluída com sucesso!</color> Tamanho: {summary.totalSize / (1024 * 1024):F1} MB em {summary.totalTime.TotalSeconds:F1}s.");

                EditorUtility.RevealInFinder(fullOutputDir);

                EditorUtility.DisplayDialog(
                    "Build Concluída com Sucesso!",
                    $"O jogo para Windows foi gerado na pasta:\n\n{fullOutputDir}\n\n" +
                    "Para enviar aos seus amigos:\n" +
                    "1. Compacte a pasta inteira 'Windows' em um arquivo .ZIP\n" +
                    "2. Envie o .ZIP (eles devem extrair tudo e executar 'Canal420.exe').",
                    "OK"
                );
            }
            else
            {
                Debug.LogError($"[Canal 420] Falha na build de Windows: {summary.result}. Verifique o Console do Unity para detalhes.");

                EditorUtility.DisplayDialog(
                    "Falha na Build",
                    $"Ocorreu um erro durante a compilação: {summary.result}.\nConsulte o console para mais informações.",
                    "OK"
                );
            }
        }

        [MenuItem("Canal 420/Exportar Jogo para macOS (.app)", priority = 21)]
        public static void BuildMacOSStandalone()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "Canal 420 - Build",
                    "Saia do Play Mode antes de iniciar a exportação.",
                    "OK"
                );
                return;
            }

            BuildSummary summary = RunMacOSBuild(out string appPath);
            if (summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(appPath);
                EditorUtility.DisplayDialog(
                    "Build macOS Concluída!",
                    $"O aplicativo macOS (arm64 IL2CPP) foi gerado em:\n\n{appPath}",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Falha na Build macOS",
                    $"Ocorreu um erro durante a compilação: {summary.result}.\nConsulte o console para mais informações.",
                    "OK"
                );
            }
        }

        public static void BuildMacOSStandaloneBatch()
        {
            BuildSummary summary = RunMacOSBuild(out _);
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"[Canal 420] Falha na build macOS: {summary.result}");
            }
        }

        private static BuildSummary RunMacOSBuild(out string appPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullOutputDir = Path.Combine(projectRoot, MacBuildOutputDir);
            appPath = Path.Combine(fullOutputDir, MacAppBundleName);

            if (!Directory.Exists(fullOutputDir))
            {
                Directory.CreateDirectory(fullOutputDir);
            }

            DeleteBundleIfExists(appPath);

            string[] scenes = new[] { "Assets/Scenes/SampleScene.unity" };

            Debug.Log($"[Canal 420] Iniciando build nativa macOS (Apple Silicon IL2CPP) em: {appPath}");

#if UNITY_EDITOR_OSX
            ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            OSArchitecture previousArch = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.ARM64;

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = appPath,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;
                if (summary.result == BuildResult.Succeeded)
                {
                    Debug.Log($"<color=green>[Canal 420] Build macOS concluída com sucesso!</color> Tamanho: {summary.totalSize / (1024 * 1024):F1} MB em {summary.totalTime.TotalSeconds:F1}s.");
                    string rootAppPath = Path.Combine(projectRoot, MacAppBundleName);
                    SyncRootMacOSBundleIfPresent(appPath, rootAppPath);
                }
                else
                {
                    Debug.LogError($"[Canal 420] Falha na build macOS: {summary.result}. Verifique o Console do Unity para detalhes.");
                }
                return summary;
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previousBackend);
                UnityEditor.OSXStandalone.UserBuildSettings.architecture = previousArch;
            }
#else
            Debug.LogError("[Canal 420] A build macOS (Apple Silicon IL2CPP) requer o Unity Editor rodando no macOS com o módulo MacStandaloneSupport instalado.");
            return new BuildSummary { result = BuildResult.Failed };
#endif
        }

        private static void DeleteBundleIfExists(string bundlePath)
        {
            if (Directory.Exists(bundlePath))
            {
                Directory.Delete(bundlePath, recursive: true);
            }
            else if (File.Exists(bundlePath))
            {
                File.Delete(bundlePath);
            }
        }

        private static void SyncRootMacOSBundleIfPresent(string builtAppPath, string rootAppPath)
        {
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                return;
            }

            if (string.Equals(Path.GetFullPath(builtAppPath), Path.GetFullPath(rootAppPath), StringComparison.Ordinal))
            {
                return;
            }

            if (!Directory.Exists(rootAppPath) && !File.Exists(rootAppPath))
            {
                return;
            }

            DeleteBundleIfExists(rootAppPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/ditto",
                Arguments = $"\"{builtAppPath}\" \"{rootAppPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("[Canal 420] Falha ao iniciar /usr/bin/ditto para sincronizar Canal 420.app.");
                }

                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"[Canal 420] Falha ao sincronizar '{rootAppPath}' via ditto (código {process.ExitCode}): {stderr}");
                }
            }

            Debug.Log($"[Canal 420] Bundle raiz sincronizado com a build IL2CPP limpa: {rootAppPath}");
        }
    }
}
