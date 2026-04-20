using UnityEngine;

/// <summary>
/// 背景系统的纯数学工具类 — 可被单元测试直接调用。
/// 从 BackgroundController 中提取的可见度/颜色/透明度计算逻辑。
/// </summary>
public static class BackgroundMathUtils
{
    /// <summary>
    /// 计算特效粒子层的可见度（0~1）。
    /// 四个Y值定义淡入→完全可见→淡出的区间：
    ///   fadeInY → fullStartY: 淡入
    ///   fullStartY → fullEndY: 完全可见
    ///   fullEndY → fadeOutY: 淡出
    /// 注意：Y值从大到小排列（fadeInY > fullStartY > fullEndY > fadeOutY）
    /// </summary>
    public static float CalculateEffectVisibility(float camY,
        float fadeInY, float fullStartY, float fullEndY, float fadeOutY)
    {
        float visibility = 0f;

        if (camY > fadeInY || camY < fadeOutY)
        {
            visibility = 0f;
        }
        else if (camY <= fadeInY && camY > fullStartY)
        {
            // 淡入区间
            visibility = (fadeInY - camY) / (fadeInY - fullStartY);
        }
        else if (camY <= fullStartY && camY >= fullEndY)
        {
            // 完全可见
            visibility = 1f;
        }
        else if (camY < fullEndY && camY >= fadeOutY)
        {
            // 淡出区间
            visibility = (camY - fadeOutY) / (fullEndY - fadeOutY);
        }

        return Mathf.Clamp01(visibility);
    }

    /// <summary>
    /// 根据Y位置在背景颜色区域之间插值。
    /// zoneYPositions 和 zoneColors 必须长度相同，按Y从大到小排列。
    /// </summary>
    public static Color EvaluateBackgroundColor(float y, float[] zoneYPositions, Color[] zoneColors)
    {
        if (zoneYPositions == null || zoneColors == null ||
            zoneYPositions.Length == 0 || zoneColors.Length == 0)
            return Color.black;

        int len = Mathf.Min(zoneYPositions.Length, zoneColors.Length);

        // 在第一个区域之上
        if (y >= zoneYPositions[0])
            return zoneColors[0];

        // 在最后一个区域之下
        if (y <= zoneYPositions[len - 1])
            return zoneColors[len - 1];

        // 找到y落在哪两个区域之间
        for (int i = 0; i < len - 1; i++)
        {
            float upperY = zoneYPositions[i];
            float lowerY = zoneYPositions[i + 1];

            if (y <= upperY && y >= lowerY)
            {
                float t = (upperY - y) / (upperY - lowerY);
                return Color.Lerp(zoneColors[i], zoneColors[i + 1], t);
            }
        }

        return zoneColors[len - 1];
    }

    /// <summary>
    /// 计算星星的整体透明度（0~1）。
    /// 高于 fullVisibleY 时为1，低于 fadeOutY 时为0，之间线性插值。
    /// </summary>
    public static float CalculateStarsAlpha(float camY, float fullVisibleY, float fadeOutY)
    {
        if (camY >= fullVisibleY)
            return 1f;
        if (camY <= fadeOutY)
            return 0f;

        return (camY - fadeOutY) / (fullVisibleY - fadeOutY);
    }
}
