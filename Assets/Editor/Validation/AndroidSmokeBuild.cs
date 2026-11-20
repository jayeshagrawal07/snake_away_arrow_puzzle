using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidSmokeBuild
{
    public static void BuildLatestDevelopmentApk()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes are configured for the Android build.");

        string outputPath = Path.Combine(Path.GetTempPath(), "SnakeEscape_Latest_Development.apk");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Android development build failed: {report.summary.result}");

        Debug.Log($"Android development APK created at {outputPath} ({report.summary.totalSize} bytes).");
    }
}
