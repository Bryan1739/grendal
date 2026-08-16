using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Build de desarrollo en un clic (ver TapLegends-Plan.md, seccion "MVP continuo").
    /// Genera un .apk en /builds con Development Build + Script Debugging activados.
    /// No instala por ADB automaticamente: eso queda para cuando haya un telefono conectado.
    /// </summary>
    public static class BuildTools
    {
        private const string BuildsFolder = "builds";

        [MenuItem("Tap Legends/Build Dev APK")]
        public static void BuildDevApk()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[BuildTools] No hay escenas en Build Settings. Ejecuta primero " +
                                "'Tap Legends/Setup/Build Boot + Game Scenes (v0.1)'.");
                return;
            }

            Directory.CreateDirectory(BuildsFolder);
            string version = PlayerSettings.bundleVersion;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
            string outputPath = Path.Combine(BuildsFolder, $"TapLegends_dev_v{version}_{timestamp}.apk");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[BuildTools] Build OK: {outputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
            else
                Debug.LogError($"[BuildTools] Build fallo: {report.summary.result}");
        }
    }
}
