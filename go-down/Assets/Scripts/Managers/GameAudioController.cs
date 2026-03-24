using UnityEngine;

/// <summary>
/// Central audio controller:
/// - Plays looping background music.
/// - Plays block-clear SFX when blocks are destroyed.
/// - Follows GameUserSettings.AudioEnabled at runtime.
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

    [Header("Mix")]
    [Range(0f, 1f)]
    public float bgmVolume = 0.26f;

    [Range(0f, 1f)]
    public float blockClearVolume = 0.75f;

    [Range(0f, 1f)]
    public float rewardBlockClearVolume = 0.75f;

    [Range(0f, 1f)]
    public float coinGainVolume = 0.75f;

    private bool lastAudioEnabled;

    private void Awake()
    {
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
        lastAudioEnabled = GameUserSettings.AudioEnabled;
        ApplyAudioEnabled(lastAudioEnabled);
    }

    private void OnDisable()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;
        CoinManager.OnCoinsGained -= HandleCoinsGained;
    }

    private void Update()
    {
        bool enabledNow = GameUserSettings.AudioEnabled;
        if (enabledNow == lastAudioEnabled) return;

        lastAudioEnabled = enabledNow;
        ApplyAudioEnabled(enabledNow);
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

    private void ApplyAudioEnabled(bool audioEnabled)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(bgmVolume);

            if (audioEnabled)
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
        if (!GameUserSettings.AudioEnabled) return;
        if (sfxSource == null) return;

        bool inRewardMode = ScoreManager.Instance != null && ScoreManager.Instance.GlobalScoreMultiplier > 1;
        AudioClip clip = inRewardMode && rewardBlockClearClip != null ? rewardBlockClearClip : blockClearClip;
        if (clip == null) return;

        float volume = inRewardMode ? rewardBlockClearVolume : blockClearVolume;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void HandleCoinsGained(Vector3 worldPosition, int deltaCoins)
    {
        if (!GameUserSettings.AudioEnabled) return;
        if (deltaCoins <= 0) return;
        if (sfxSource == null || coinGainClip == null) return;

        sfxSource.PlayOneShot(coinGainClip, Mathf.Clamp01(coinGainVolume));
    }
}
