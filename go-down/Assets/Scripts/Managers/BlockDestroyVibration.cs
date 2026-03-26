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

    private void HandleBlockDestroyed(TowerBlock block)
    {
        if (!GameUserSettings.VibrationEnabled) return;

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

    private static void TriggerVibration()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}
