using UnityEngine;

/// <summary>
/// Persistent user gameplay settings shared across UI and systems.
/// </summary>
public static class GameUserSettings
{
    private const string KEY_AUDIO = "Setting_AudioEnabled";
    private const string KEY_VIBRATION = "Setting_VibrationEnabled";

    private static bool loaded;
    private static bool audioEnabled;
    private static bool vibrationEnabled;

    public static bool AudioEnabled
    {
        get { EnsureLoaded(); return audioEnabled; }
        set
        {
            EnsureLoaded();
            if (audioEnabled == value) return;
            audioEnabled = value;
            PlayerPrefs.SetInt(KEY_AUDIO, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool VibrationEnabled
    {
        get { EnsureLoaded(); return vibrationEnabled; }
        set
        {
            EnsureLoaded();
            if (vibrationEnabled == value) return;
            vibrationEnabled = value;
            PlayerPrefs.SetInt(KEY_VIBRATION, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void Reload()
    {
        loaded = false;
        EnsureLoaded();
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;

        audioEnabled = PlayerPrefs.GetInt(KEY_AUDIO, 1) == 1;
        vibrationEnabled = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;

        loaded = true;
    }
}
