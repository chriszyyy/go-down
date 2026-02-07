using UnityEngine;

/// <summary>
/// Makes a SpriteRenderer render with an animated rainbow gradient + additive glow.
/// Intended for the 1% special blocks.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RainbowGlowVisual : MonoBehaviour
{
    [Header("Tuning")]
    [Range(0f, 1f)]
    public float hueOffset = -1f; // <0 means random

    public float speed = 1.2f;
    public float scale = 2.5f;
    public float waveFreq = 10f;
    public float waveAmp = 0.15f;

    [Tooltip("Main gradient intensity")]
    public float glow = 1.6f;

    [Tooltip("Extra additive intensity (fake glow)")]
    public float additive = 0.9f;

    public float pulseSpeed = 3.0f;

    [Header("Highlight")]
    [Tooltip("Disable the prefab's inset highlight sprite. Turn this off if you want a light outline like normal blocks.")]
    public bool disableInsetHighlight = false;

    private static Shader s_shader;
    private static Material s_sharedMaterial;

    private SpriteRenderer baseRenderer;
    private SpriteRenderer highlightRenderer;

    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        baseRenderer = GetComponent<SpriteRenderer>();

        // Unity doesn't allow creating some engine objects from constructors/field initializers.
        // Create MaterialPropertyBlock here.
        mpb = new MaterialPropertyBlock();

        Transform t = transform.Find("Highlight");
        if (t != null)
        {
            highlightRenderer = t.GetComponent<SpriteRenderer>();
        }

        if (hueOffset < 0f)
        {
            hueOffset = Random.value;
        }
    }

    private void OnEnable()
    {
        EnsureMaterial();
        ApplyMaterialAndProps();

        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = !disableInsetHighlight;
        }
    }

    private void OnDisable()
    {
        // Leave the assigned material as-is; we don't want to thrash shared state.
    }

    private void LateUpdate()
    {
        // Keep per-instance parameters up-to-date.
        ApplyPropsOnly();
    }

    private static void EnsureMaterial()
    {
        if (s_sharedMaterial != null) return;

        if (s_shader == null)
        {
            s_shader = Shader.Find("GoDown/RainbowGlowSprite");
        }

        if (s_shader == null)
        {
            Debug.LogWarning("RainbowGlowVisual: Shader 'GoDown/RainbowGlowSprite' not found.");
            return;
        }

        s_sharedMaterial = new Material(s_shader);
        s_sharedMaterial.name = "RainbowGlowSprite (Shared)";
    }

    private void ApplyMaterialAndProps()
    {
        if (s_sharedMaterial == null) return;

        if (baseRenderer != null)
        {
            baseRenderer.sharedMaterial = s_sharedMaterial;
        }

        ApplyPropsOnly();
    }

    private void ApplyPropsOnly()
    {
        if (baseRenderer == null) return;
        if (s_sharedMaterial == null) return;

        if (mpb == null)
        {
            mpb = new MaterialPropertyBlock();
        }

        baseRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat("_HueOffset", Mathf.Repeat(hueOffset, 1f));
        mpb.SetFloat("_Speed", speed);
        mpb.SetFloat("_Scale", scale);
        mpb.SetFloat("_WaveFreq", waveFreq);
        mpb.SetFloat("_WaveAmp", waveAmp);
        mpb.SetFloat("_Glow", glow);
        mpb.SetFloat("_Additive", additive);
        mpb.SetFloat("_PulseSpeed", pulseSpeed);
        baseRenderer.SetPropertyBlock(mpb);
    }
}
