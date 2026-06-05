#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

/// <summary>
/// iOS 构建后处理：向生成的 Xcode 工程 Info.plist 注入 ATT 用法说明。
/// 调用 ATTrackingManager.requestTrackingAuthorization 前，Apple 要求 Info.plist
/// 含 NSUserTrackingUsageDescription，否则会崩溃 / 被拒。
/// </summary>
public static class IOSPostProcessBuild
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        plist.root.SetString(
            "NSUserTrackingUsageDescription",
            "Your data will be used to show you more relevant ads.");

        plist.WriteToFile(plistPath);
    }
}
#endif
