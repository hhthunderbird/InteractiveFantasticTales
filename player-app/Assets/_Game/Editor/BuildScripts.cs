using UnityEditor;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace IFT.BuildScripts
{
    public static class BuildScripts
    {
        private const string BuildDir = "build";

        private static bool IsCI => Environment.GetEnvironmentVariable("CI") == "true"
            || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

        [MenuItem("IFT/Build/Android APK (Debug)")]
        public static void BuildAndroidApk()
        {
            BuildAndroid(bundle: false, development: true);
        }

        [MenuItem("IFT/Build/Android AAB (Release)")]
        public static void BuildAndroidAab()
        {
            BuildAndroid(bundle: true, development: false);
        }

        [MenuItem("IFT/Build/iOS Xcode Project")]
        public static void BuildIOS()
        {
            var buildPath = Path.Combine(BuildDir, "iOS");
            if (Directory.Exists(buildPath))
                Directory.Delete(buildPath, true);

            PlayerSettings.iOS.appleDeveloperTeamID = Environment.GetEnvironmentVariable("APPLE_TEAM_ID") ?? "";
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.ift.player");

            Debug.Log($"[IFT Build] Unity {Application.unityVersion} | Target: iOS Xcode");

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[IFT Build] Nenhuma cena no Build Settings! Adicione em File > Build Settings");
                EditorApplication.Exit(1);
            }

            var buildReport = BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.iOS, BuildOptions.None);

            if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[IFT Build] iOS Xcode exportado: {buildPath}");
            }
            else
            {
                Debug.LogError($"[IFT Build] iOS build FAILED: {buildReport.summary.totalErrors} errors");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("IFT/Build/All")]
        public static void BuildAll()
        {
            BuildAndroid(bundle: true, development: false);
            BuildAndroid(bundle: false, development: true);
            BuildIOS();
        }

        private static void BuildAndroid(bool bundle, bool development)
        {
            // game-ci sets up keystore automatically via androidKeystore* params
            // BuildScripts only handles it when running locally (not CI)
            EditorUserBuildSettings.buildAppBundle = bundle;
            EditorUserBuildSettings.development = development;
            EditorUserBuildSettings.allowDebugging = development;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            if (!development && bundle && !IsCI)
            {
                var keystorePath = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
                if (!string.IsNullOrEmpty(keystorePath) && File.Exists(keystorePath))
                {
                    PlayerSettings.Android.useCustomKeystore = true;
                    PlayerSettings.Android.keystoreName = keystorePath;
                    PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS") ?? "";
                    PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME") ?? "";
                    PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS") ?? "";
                    Debug.Log("[IFT Build] Keystore configurado localmente");
                }
                else
                {
                    Debug.LogWarning("[IFT Build] ANDROID_KEYSTORE_PATH nao definido — usando debug key");
                }
            }

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.ift.player");

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[IFT Build] Nenhuma cena no Build Settings! Adicione em File > Build Settings");
                EditorApplication.Exit(1);
            }

            // Output dir: game-ci sets buildPath via unity-builder, local builds use BuildDir
            var outputDir = IsCI
                ? Path.GetDirectoryName(EditorUserBuildSettings.GetBuildLocation(BuildTarget.Android))
                    ?? Path.Combine(BuildDir, bundle ? "Android_AAB" : "Android_APK")
                : Path.Combine(BuildDir, bundle ? "Android_AAB" : "Android_APK");

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var ext = bundle ? ".aab" : ".apk";
            var fileName = $"IFT_{(development ? "Debug" : "Release")}{ext}";
            var fullPath = Path.Combine(outputDir, fileName);

            Debug.Log($"[IFT Build] Unity {Application.unityVersion} | Android {(bundle ? "AAB" : "APK")} {(development ? "(Debug)" : "(Release)")}");
            Debug.Log($"[IFT Build] Output: {fullPath}");

            var buildReport = BuildPipeline.BuildPlayer(scenes, fullPath, BuildTarget.Android,
                development ? BuildOptions.Development : BuildOptions.None);

            if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                var fi = new FileInfo(fullPath);
                Debug.Log($"[IFT Build] SUCESSO: {fi.Name} ({fi.Length / 1024 / 1024} MB)");
            }
            else
            {
                Debug.LogError($"[IFT Build] FAILED: {buildReport.summary.totalErrors} errors");
                EditorApplication.Exit(1);
            }
        }
    }
}
