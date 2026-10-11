#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RiverRush.Editor
{
    /// <summary>
    /// Local Unity 6 build shortcuts. A valid Unity editor and platform modules
    /// are required. GitHub's license-free source audit does NOT create APK/EXE.
    /// </summary>
    public static class RiverRushBuildMenu
    {
        [MenuItem("River Rush/Build/Windows x64 EXE")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64,
                "Builds/Windows/RiverRush3D.exe");
        }

        [MenuItem("River Rush/Build/Android APK")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, "Builds/Android/RiverRush3D.apk");
        }

        private static void Build(BuildTarget target, string outputRelativePath)
        {
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                Debug.LogError("Switch active build platform to " + target +
                    " under File > Build Profiles (or Build Settings), then try again. " +
                    "Do not compile into a different active target in this shortcut.");
                return;
            }

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && File.Exists(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("No enabled scenes. First run River Rush > Create Prototype Scene.");
                return;
            }

            string outputPath = Path.GetFullPath(outputRelativePath);
            string folder = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("River Rush " + target + " build succeeded: " + outputPath +
                    " (" + report.summary.totalSize + " bytes)");
            }
            else
            {
                Debug.LogError("River Rush build failed (" + report.summary.result +
                    "). Review Console and Editor.log; no successful artifact is available.");
            }
        }
    }
}
#endif
