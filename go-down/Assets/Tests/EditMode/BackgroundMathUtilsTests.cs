using NUnit.Framework;
using UnityEngine;

/// <summary>
/// BackgroundMathUtils 单元测试 — 验证背景系统的可见度、颜色插值和星星透明度计算。
/// </summary>
[TestFixture]
public class BackgroundMathUtilsTests
{
    // 使用实际的星云层Y范围作为测试数据
    private const float NebulaFadeIn = -20f;
    private const float NebulaFullStart = -60f;
    private const float NebulaFullEnd = -350f;
    private const float NebulaFadeOut = -480f;

    private const float Tolerance = 0.001f;

    // ═══════════════════════════════════════════════════════════
    //  CalculateEffectVisibility 测试
    // ═══════════════════════════════════════════════════════════

    [Test]
    public void EffectVisibility_AboveFadeIn_ReturnsZero()
    {
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            0f, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(0f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_BelowFadeOut_ReturnsZero()
    {
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            -500f, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(0f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_AtFadeInY_ReturnsZero()
    {
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            NebulaFadeIn, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(0f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_AtFullStartY_ReturnsOne()
    {
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            NebulaFullStart, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(1f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_MidFadeIn_ReturnsHalf()
    {
        float midY = (NebulaFadeIn + NebulaFullStart) / 2f; // -40
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            midY, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(0.5f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_FullyVisible_ReturnsOne()
    {
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            -200f, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(1f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_AtFullEndY_ReturnsOne()
    {
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            NebulaFullEnd, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(1f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_MidFadeOut_ReturnsHalf()
    {
        float midY = (NebulaFullEnd + NebulaFadeOut) / 2f; // -415
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            midY, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(0.5f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_AtFadeOutY_ReturnsZero()
    {
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            NebulaFadeOut, NebulaFadeIn, NebulaFullStart, NebulaFullEnd, NebulaFadeOut);
        Assert.AreEqual(0f, result, Tolerance);
    }

    [Test]
    public void EffectVisibility_EmberRange_FullyVisible()
    {
        // 使用岩浆火星的Y范围交叉验证
        float result = BackgroundMathUtils.CalculateEffectVisibility(
            -3000f, -2400f, -2550f, -3900f, -4200f);
        Assert.AreEqual(1f, result, Tolerance);
    }

    // ═══════════════════════════════════════════════════════════
    //  EvaluateBackgroundColor 测试
    // ═══════════════════════════════════════════════════════════

    private static readonly float[] TwoZonePositions = { 10f, -50f };
    private static readonly Color[] TwoZoneColors = { Color.red, Color.blue };

    [Test]
    public void BackgroundColor_AboveFirstZone_ReturnsFirstColor()
    {
        Color result = BackgroundMathUtils.EvaluateBackgroundColor(
            100f, TwoZonePositions, TwoZoneColors);
        AssertColorsEqual(Color.red, result);
    }

    [Test]
    public void BackgroundColor_BelowLastZone_ReturnsLastColor()
    {
        Color result = BackgroundMathUtils.EvaluateBackgroundColor(
            -5000f, TwoZonePositions, TwoZoneColors);
        AssertColorsEqual(Color.blue, result);
    }

    [Test]
    public void BackgroundColor_ExactlyOnZone_ReturnsThatColor()
    {
        Color result = BackgroundMathUtils.EvaluateBackgroundColor(
            -50f, TwoZonePositions, TwoZoneColors);
        AssertColorsEqual(Color.blue, result);
    }

    [Test]
    public void BackgroundColor_MidpointBetweenZones_LerpsCorrectly()
    {
        // Y=-20 is at t = (10 - (-20)) / (10 - (-50)) = 30/60 = 0.5
        Color result = BackgroundMathUtils.EvaluateBackgroundColor(
            -20f, TwoZonePositions, TwoZoneColors);
        Color expected = Color.Lerp(Color.red, Color.blue, 0.5f);
        AssertColorsEqual(expected, result);
    }

    [Test]
    public void BackgroundColor_EmptyZones_ReturnsBlack()
    {
        Color result = BackgroundMathUtils.EvaluateBackgroundColor(
            0f, new float[0], new Color[0]);
        AssertColorsEqual(Color.black, result);
    }

    [Test]
    public void BackgroundColor_NullZones_ReturnsBlack()
    {
        Color result = BackgroundMathUtils.EvaluateBackgroundColor(0f, null, null);
        AssertColorsEqual(Color.black, result);
    }

    [Test]
    public void BackgroundColor_SingleZone_ReturnsThatColor()
    {
        Color result = BackgroundMathUtils.EvaluateBackgroundColor(
            -100f, new float[] { 0f }, new Color[] { Color.green });
        AssertColorsEqual(Color.green, result);
    }

    // ═══════════════════════════════════════════════════════════
    //  CalculateStarsAlpha 测试
    // ═══════════════════════════════════════════════════════════

    private const float StarsFullVisible = 20f;
    private const float StarsFadeOut = -900f;

    [Test]
    public void StarsAlpha_AboveFullVisible_ReturnsOne()
    {
        float result = BackgroundMathUtils.CalculateStarsAlpha(100f, StarsFullVisible, StarsFadeOut);
        Assert.AreEqual(1f, result, Tolerance);
    }

    [Test]
    public void StarsAlpha_AtFullVisible_ReturnsOne()
    {
        float result = BackgroundMathUtils.CalculateStarsAlpha(StarsFullVisible, StarsFullVisible, StarsFadeOut);
        Assert.AreEqual(1f, result, Tolerance);
    }

    [Test]
    public void StarsAlpha_BelowFadeOut_ReturnsZero()
    {
        float result = BackgroundMathUtils.CalculateStarsAlpha(-1000f, StarsFullVisible, StarsFadeOut);
        Assert.AreEqual(0f, result, Tolerance);
    }

    [Test]
    public void StarsAlpha_AtFadeOut_ReturnsZero()
    {
        float result = BackgroundMathUtils.CalculateStarsAlpha(StarsFadeOut, StarsFullVisible, StarsFadeOut);
        Assert.AreEqual(0f, result, Tolerance);
    }

    [Test]
    public void StarsAlpha_Midpoint_ReturnsHalf()
    {
        float midY = (StarsFullVisible + StarsFadeOut) / 2f; // -440
        float result = BackgroundMathUtils.CalculateStarsAlpha(midY, StarsFullVisible, StarsFadeOut);
        Assert.AreEqual(0.5f, result, Tolerance);
    }

    // ═══════════════════════════════════════════════════════════
    //  工具方法
    // ═══════════════════════════════════════════════════════════

    private static void AssertColorsEqual(Color expected, Color actual)
    {
        Assert.AreEqual(expected.r, actual.r, Tolerance, "Red 分量不匹配");
        Assert.AreEqual(expected.g, actual.g, Tolerance, "Green 分量不匹配");
        Assert.AreEqual(expected.b, actual.b, Tolerance, "Blue 分量不匹配");
        Assert.AreEqual(expected.a, actual.a, Tolerance, "Alpha 分量不匹配");
    }
}
