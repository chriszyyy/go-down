using System;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using AOT;
#endif

/// <summary>
/// iOS App Tracking Transparency (ATT) 封装。
/// AdMob 在 iOS 上使用 IDFA 投放个性化广告前，Apple 要求先弹出 ATT 授权请求。
/// 非 iOS 平台 / 编辑器下为空实现，直接标记完成。
///
/// 用法：调用 Request() 后轮询 IsComplete（原生回调可能在后台线程，故用轮询而非直接回调，
/// 避免从非主线程调用 Unity API）。
/// </summary>
public static class AppTrackingTransparencyHelper
{
    public enum Status
    {
        NotDetermined = 0,
        Restricted = 1,
        Denied = 2,
        Authorized = 3,
        NotAvailable = -1,
    }

    private static volatile bool s_complete;
    private static volatile int s_status = (int)Status.NotAvailable;

    public static bool IsComplete => s_complete;
    public static Status CurrentStatus => (Status)s_status;

#if UNITY_IOS && !UNITY_EDITOR
    private delegate void AttCallback(int status);

    [DllImport("__Internal")]
    private static extern void HexDrop_RequestTrackingAuthorization(AttCallback callback);

    [MonoPInvokeCallback(typeof(AttCallback))]
    private static void OnComplete(int status)
    {
        // 原生线程回调：只写入字段，主线程通过轮询读取
        s_status = status;
        s_complete = true;
    }

    public static void Request()
    {
        if (s_complete) return;
        try
        {
            HexDrop_RequestTrackingAuthorization(OnComplete);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ATT] Request failed: {e.Message}");
            s_status = (int)Status.NotAvailable;
            s_complete = true;
        }
    }
#else
    public static void Request()
    {
        // 非 iOS / 编辑器：ATT 不适用，立即标记完成
        s_status = (int)Status.NotAvailable;
        s_complete = true;
    }
#endif
}
