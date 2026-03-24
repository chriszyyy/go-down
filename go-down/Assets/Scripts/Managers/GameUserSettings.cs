using UnityEngine;

/// <summary>
/// Persistent user gameplay settings shared across UI and systems.
/// </summary>
public static class GameUserSettings
{
    private const string KEY_AUDIO = "Setting_AudioEnabled";
    private const string KEY_MUSIC = "Setting_MusicEnabled";
    private const string KEY_SFX = "Setting_SfxEnabled";
    private const string KEY_VIBRATION = "Setting_VibrationEnabled";

    private static bool loaded;
    private static bool musicEnabled;
    private static bool sfxEnabled;
    private static bool vibrationEnabled;

    public static bool MusicEnabled
    {
        get { EnsureLoaded(); return musicEnabled; }
        set
        {
            EnsureLoaded();
            if (musicEnabled == value) return;

            musicEnabled = value;

            PlayerPrefs.SetInt(KEY_MUSIC, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SfxEnabled
    {
        get { EnsureLoaded(); return sfxEnabled; }
        set
        {
            EnsureLoaded();
            if (sfxEnabled == value) return;

            sfxEnabled = value;

            PlayerPrefs.SetInt(KEY_SFX, value ? 1 : 0);
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

        bool legacyAudio = PlayerPrefs.GetInt(KEY_AUDIO, 1) == 1;
        musicEnabled = PlayerPrefs.GetInt(KEY_MUSIC, legacyAudio ? 1 : 0) == 1;
        sfxEnabled = PlayerPrefs.GetInt(KEY_SFX, legacyAudio ? 1 : 0) == 1;
        vibrationEnabled = PlayerPrefs.GetInt(KEY_VIBRATION, 1) == 1;

        loaded = true;
    }
}
