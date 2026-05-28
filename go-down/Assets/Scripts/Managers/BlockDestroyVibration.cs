using UnityEngine;
using System.Collections;

/// <summary>
/// Triggers mobile vibration when a block is destroyed.
/// Respects GameUserSettings.VibrationEnabled.
/// </summary>
public class BlockDestroyVibration : MonoBehaviour
{
    [Tooltip("Minimum seconds between vibration triggers to avoid excessive buzzing.")]
    public float minIntervalSeconds = 0.04f;

    [Tooltip("Android one-shot vibration duration in milliseconds (short haptic feel).")]
    [Range(5, 120)]
    public int androidVibrationMs = 18;

    [Tooltip("Android vibration amplitude (1-255). -1 uses device default.")]
    [Range(-1, 255)]
    public int androidAmplitude = -1;

    [Tooltip("How many vibration pulses are played for each destroy event.")]
    [Range(1, 6)]
    public int pulsesPerTrigger = 1;

    [Tooltip("Interval between pulses (seconds). Used when pulsesPerTrigger > 1.")]
    public float pulseIntervalSeconds = 0.06f;

    private float lastVibrateTime = -999f;
    private Coroutine pulseRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<BlockDestroyVibration>() != null) return;

        GameObject go = new GameObject("BlockDestroyVibration");
        go.AddComponent<BlockDestroyVibration>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        TowerBlock.OnBlockDestroyed += HandleBlockDestroyed;
    }

    private void OnDisable()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }

    // 全局关闭：方块销毁震动功能整体禁用。
    // 即使 GameUserSettings.VibrationEnabled = true，也不会触发震动。
    private const bool VIBRATION_FEATURE_ENABLED = false;

    private void HandleBlockDestroyed(TowerBlock block)
    {
#pragma warning disable CS0162 // 全局 feature flag 关闭时下方代码确实不可达
        if (!VIBRATION_FEATURE_ENABLED) return;
        if (!GameUserSettings.VibrationEnabled) return;
#pragma warning restore CS0162

        float now = Time.unscaledTime;
        if (now - lastVibrateTime < Mathf.Max(0f, minIntervalSeconds)) return;

        lastVibrateTime = now;

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        int count = Mathf.Max(1, pulsesPerTrigger);
        if (count == 1)
        {
            TriggerVibration();
            return;
        }

        pulseRoutine = StartCoroutine(PlayPulsePattern(count));
    }

    private IEnumerator PlayPulsePattern(int count)
    {
        float wait = Mathf.Max(0f, pulseIntervalSeconds);

        for (int i = 0; i < count; i++)
        {
            TriggerVibration();

            if (i < count - 1 && wait > 0f)
            {
                yield return new WaitForSecondsRealtime(wait);
            }
        }

        pulseRoutine = null;
    }

    private void TriggerVibration()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Use Android native API to control vibration duration and make it short.
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null)
                {
                    int duration = Mathf.Max(1, androidVibrationMs);

                    if (androidAmplitude >= 1)
                    {
                        using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)duration, androidAmplitude))
                        {
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        vibrator.Call("vibrate", (long)duration);
                    }

                    return;
                }
            }
        }
        catch
        {
            // Fallback below.
        }

        Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS Handheld.Vibrate duration is system-controlled.
        Handheld.Vibrate();
#endif
    }
}
