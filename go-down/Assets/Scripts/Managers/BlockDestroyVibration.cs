using UnityEngine;

/// <summary>
/// Triggers mobile vibration when a block is destroyed.
/// Respects GameUserSettings.VibrationEnabled.
/// </summary>
public class BlockDestroyVibration : MonoBehaviour
{
    [Tooltip("Minimum seconds between vibration triggers to avoid excessive buzzing.")]
    public float minIntervalSeconds = 0.04f;

    private float lastVibrateTime = -999f;

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
    }

    private void HandleBlockDestroyed(TowerBlock block)
    {
        if (!GameUserSettings.VibrationEnabled) return;

        float now = Time.unscaledTime;
        if (now - lastVibrateTime < Mathf.Max(0f, minIntervalSeconds)) return;

        lastVibrateTime = now;
        TriggerVibration();
    }

    private static void TriggerVibration()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
