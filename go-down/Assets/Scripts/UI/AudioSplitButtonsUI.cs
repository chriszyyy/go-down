using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Two-button audio settings controller.
/// - One button toggles background music.
/// - One button toggles SFX.
/// Optional icon swapping is supported for both buttons.
/// </summary>
public class AudioSplitButtonsUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button musicButton;
    public Button sfxButton;

    [Header("Optional State Icons")]
    public Image musicStateImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    public Image sfxStateImage;
    public Sprite sfxOnSprite;
    public Sprite sfxOffSprite;

    private void OnEnable()
    {
        if (musicButton != null)
        {
            musicButton.onClick.RemoveListener(OnMusicButtonClicked);
            musicButton.onClick.AddListener(OnMusicButtonClicked);
        }

        if (sfxButton != null)
        {
            sfxButton.onClick.RemoveListener(OnSfxButtonClicked);
            sfxButton.onClick.AddListener(OnSfxButtonClicked);
        }

        RefreshIcons();
    }

    private void OnDisable()
    {
        if (musicButton != null)
            musicButton.onClick.RemoveListener(OnMusicButtonClicked);

        if (sfxButton != null)
            sfxButton.onClick.RemoveListener(OnSfxButtonClicked);
    }

    private void OnMusicButtonClicked()
    {
        GameUserSettings.MusicEnabled = !GameUserSettings.MusicEnabled;
        RefreshIcons();
    }

    private void OnSfxButtonClicked()
    {
        GameUserSettings.SfxEnabled = !GameUserSettings.SfxEnabled;
        RefreshIcons();
    }

    private void RefreshIcons()
    {
        ApplyStateIcon(musicStateImage, GameUserSettings.MusicEnabled, musicOnSprite, musicOffSprite);
        ApplyStateIcon(sfxStateImage, GameUserSettings.SfxEnabled, sfxOnSprite, sfxOffSprite);
    }

    private static void ApplyStateIcon(Image target, bool isOn, Sprite onSprite, Sprite offSprite)
    {
        if (target == null) return;

        Sprite s = isOn ? onSprite : offSprite;
        if (s != null)
        {
            target.sprite = s;
        }
    }
}
