using UnityEngine;

/// <summary>
/// 4x1线型方块（I型）
/// </summary>
public class LineBlock : TowerBlock
{
    /// <summary>
    /// 获取线型方块实际占用的格子（4x1 或 1x4）
    /// </summary>
    public override System.Collections.Generic.List<(int x, int y)> GetOccupiedCells(float rotationAngle)
    {
        var cells = new System.Collections.Generic.List<(int x, int y)>();

        // 标准化角度
        float normalizedAngle = rotationAngle % 360f;
        if (normalizedAngle < 0) normalizedAngle += 360f;

        // 90度或270度时，垂直放置 (1x4)
        if (Mathf.Approximately(normalizedAngle, 90f) || Mathf.Approximately(normalizedAngle, 270f))
        {
            cells.Add((0, 0)); // 底部
            cells.Add((0, 1));
            cells.Add((0, 2));
            cells.Add((0, 3)); // 顶部
        }
        else
        {
            // 0度或180度时，水平放置 (4x1)
            cells.Add((0, 0)); // 左端
            cells.Add((1, 0));
            cells.Add((2, 0));
            cells.Add((3, 0)); // 右端
        }

        return cells;
    }
}