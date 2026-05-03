using UnityEngine;

/// <summary>
/// Central audio controller:
/// - Plays looping background music.
/// - Plays block-clear SFX when blocks are destroyed.
/// - Follows GameUserSettings.MusicEnabled / SfxEnabled at runtime.
/// </summary>
public class GameAudioController : MonoBehaviour
{
    [Header("Sources")]
    [Tooltip("Looped background music source. If empty, one will be auto-created on this object.")]
    public AudioSource bgmSource;

    [Tooltip("One-shot SFX source. If empty, one will be auto-created on this object.")]
    public AudioSource sfxSource;

    [Header("Clips")]
    [Tooltip("Background music clip.")]
    public AudioClip backgroundMusicClip;

    [Tooltip("SFX played when a block is destroyed.")]
    public AudioClip blockClearClip;

    [Tooltip("SFX played when a block is destroyed during reward mode. Falls back to normal clip if empty.")]
    public AudioClip rewardBlockClearClip;

    [Tooltip("SFX played when coins are gained (e.g., HexagonBall hits rainbow block).")]
    public AudioClip coinGainClip;

    [Tooltip("SFX played when the Reset Hexagon tool is used.")]
    public AudioClip resetToolClip;

    [Tooltip("SFX played when the Random Rainbow tool is used.")]
    public AudioClip rainbowToolClip;

    [Header("Mix")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.26f;

    [Range(0f, 1f)]
    public float blockClearVolume = 0.75f;

    [Range(0f, 1f)]
    public float rewardBlockClearVolume = 0.75f;

    [Range(0f, 1f)]
    public float coinGainVolume = 0.75f;

    [Range(0f, 1f)]
    public float resetToolVolume = 0.75f;

    [Range(0f, 1f)]
    public float rainbowToolVolume = 0.75f;

    public static GameAudioController Instance { get; private set; }

    private bool lastMusicEnabled;
    private bool lastSfxEnabled;
    private float lastMusicVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureSources();
        ConfigureSources();
    }

    private void OnEnable()
    {
        TowerBlock.OnBlockDestroyed += HandleBlockDestroyed;
        CoinManager.OnCoinsGained += HandleCoinsGained;
    }

    private void Start()
    {
        lastMusicEnabled = GameUserSettings.MusicEnabled;
        lastSfxEnabled = GameUserSettings.SfxEnabled;
        lastMusicVolume = GameUserSettings.MusicVolume;
        ApplyAudioSettings(lastMusicEnabled, lastSfxEnabled);
    }

    private void OnDisable()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;
        CoinManager.OnCoinsGained -= HandleCoinsGained;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        bool musicEnabled = GameUserSettings.MusicEnabled;
        bool sfxEnabled = GameUserSettings.SfxEnabled;
        float musicVol = GameUserSettings.MusicVolume;

        if (musicEnabled == lastMusicEnabled
            && sfxEnabled == lastSfxEnabled
            && Mathf.Approximately(musicVol, lastMusicVolume))
        {
            return;
        }

        lastMusicEnabled = musicEnabled;
        lastSfxEnabled = sfxEnabled;
        lastMusicVolume = musicVol;
        ApplyAudioSettings(musicEnabled, sfxEnabled);
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ConfigureSources()
    {
        if (bgmSource != null)
        {
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = Mathf.Clamp01(bgmVolume);
            bgmSource.clip = backgroundMusicClip;
        }

        if (sfxSource != null)
        {
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = 1f;
        }
    }

    private void ApplyAudioSettings(bool musicEnabled, bool sfxEnabled)
    {
        if (bgmSource != null)
        {
            // 用户音量 × 项目预调音量
            bgmSource.volume = Mathf.Clamp01(bgmVolume * GameUserSettings.MusicVolume);

            if (musicEnabled)
            {
                if (backgroundMusicClip != null)
                {
                    bgmSource.clip = backgroundMusicClip;
                    if (!bgmSource.isPlaying)
                    {
                        bgmSource.Play();
                    }
                }
            }
            else
            {
                if (bgmSource.isPlaying)
                {
                    bgmSource.Stop();
                }
            }
        }

        if (sfxSource != null)
        {
            sfxSource.volume = 1f;
        }
    }

    private void HandleBlockDestroyed(TowerBlock block)
    {
        if (!GameUserSettings.SfxEnabled) return;
        if (sfxSource == null) return;

        bool inRewardMode = ScoreManager.Instance != null && ScoreManager.Instance.GlobalScoreMultiplier > 1;
        AudioClip clip = inRewardMode && rewardBlockClearClip != null ? rewardBlockClearClip : blockClearClip;
        if (clip == null) return;

        float volume = inRewardMode ? rewardBlockClearVolume : blockClearVolume;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume * GameUserSettings.SfxVolume));
    }

    private void HandleCoinsGained(Vector3 worldPosition, int deltaCoins)
    {
        if (!GameUserSettings.SfxEnabled) return;
        if (deltaCoins <= 0) return;
        if (sfxSource == null || coinGainClip == null) return;

        sfxSource.PlayOneShot(coinGainClip, Mathf.Clamp01(coinGainVolume * GameUserSettings.SfxVolume));
    }

    public void PlayResetToolSfx()
    {
        PlayOneShot(resetToolClip, resetToolVolume);
    }

    public void PlayRainbowToolSfx()
    {
        PlayOneShot(rainbowToolClip, rainbowToolVolume);
    }

    private void PlayOneShot(AudioClip clip, float volume)
    {
        if (!GameUserSettings.SfxEnabled) return;
        if (sfxSource == null || clip == null) return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume * GameUserSettings.SfxVolume));
    }
}
