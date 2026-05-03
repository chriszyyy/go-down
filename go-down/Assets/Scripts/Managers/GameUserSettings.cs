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
    private const string KEY_MUSIC_VOL = "Setting_MusicVolume";
    private const string KEY_SFX_VOL = "Setting_SfxVolume";

    private static bool loaded;
    private static bool musicEnabled;
    private static bool sfxEnabled;
    private static bool vibrationEnabled;
    private static float musicVolume;
    private static float sfxVolume;

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

    /// <summary>音乐音量 0..1（默认 1 = 100%）。会乘到 GameAudioController.bgmVolume 上。</summary>
    public static float MusicVolume
    {
        get { EnsureLoaded(); return musicVolume; }
        set
        {
            EnsureLoaded();
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(musicVolume, clamped)) return;
            musicVolume = clamped;
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL, clamped);
            PlayerPrefs.Save();
        }
    }

    /// <summary>音效音量 0..1（默认 1 = 100%）。会乘到各 SFX PlayOneShot 的 volume 上。</summary>
    public static float SfxVolume
    {
        get { EnsureLoaded(); return sfxVolume; }
        set
        {
            EnsureLoaded();
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(sfxVolume, clamped)) return;
            sfxVolume = clamped;
            PlayerPrefs.SetFloat(KEY_SFX_VOL, clamped);
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
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 1f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f));

        loaded = true;
    }
}
