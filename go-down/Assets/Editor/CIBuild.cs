using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// CI / 命令行批处理构建入口。通过 -executeMethod CIBuild.BuildIOS 调用。
// 仅 Editor 程序集使用，不会进入运行时包。
public static class CIBuild
{
    public static void BuildIOS()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        string outputPath = Path.GetFullPath(
            Path.Combine(Application.dataPath, "..", "Build"));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None,
        };

        Debug.Log($"[CIBuild] Building iOS to {outputPath} with {scenes.Length} scene(s).");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[CIBuild] iOS build SUCCEEDED: {summary.totalSize} bytes.");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[CIBuild] iOS build FAILED: result={summary.result}, errors={summary.totalErrors}.");
            EditorApplication.Exit(1);
        }
    }
}
