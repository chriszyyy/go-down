using UnityEngine;

/// <summary>
/// Forces a sane frame rate configuration on mobile.
/// Helps avoid unintended 30 FPS caps caused by platform defaults.
/// </summary>
public class FrameRateBootstrapper : MonoBehaviour
{
    [Tooltip("Target FPS during gameplay. 60 is a good default for most phones.")]
    public int targetFps = 60;

    [Tooltip("If true, disables VSync so targetFps can take effect.")]
    public bool disableVSync = true;

    private static FrameRateBootstrapper instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Only apply on mobile platforms. Leave desktop/console builds untouched.
        if (!Application.isMobilePlatform) return;

        if (instance != null) return;

        GameObject go = new GameObject("FrameRateBootstrapper");
        instance = go.AddComponent<FrameRateBootstrapper>();
        DontDestroyOnLoad(go);

        instance.Apply();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) Apply();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) Apply();
    }

    private void Apply()
    {
        if (!Application.isMobilePlatform) return;

        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }

        int fps = Mathf.Clamp(targetFps, 15, 240);

        Application.targetFrameRate = fps;
    }
}
