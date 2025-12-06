using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// L5方块 - 3x3 等长L形
/// 实际占用5个格子
/// </summary>
public class L5Block : TowerBlock
{
    /// <summary>
    /// 获取当前旋转角度下的左下角坐标（相对于原点的偏移）
    /// </summary>
    public override Vector2Int GetBottomLeftCorner(float rotationAngle)
    {
        float normalizedAngle = rotationAngle % 360f;
        if (normalizedAngle < 0) normalizedAngle += 360f;

        if (Mathf.Approximately(normalizedAngle, 0f))
        {
            return new Vector2Int(0, 0);
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            return new Vector2Int(-3, 0);
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            return new Vector2Int(-1, -3);
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            return new Vector2Int(0, -3);
        }

        return Vector2Int.zero;
    }

    public override List<(int x, int y)> GetOccupiedCells(float rotationAngle)
    {
        var cells = new List<(int x, int y)>();

        float normalizedAngle = rotationAngle % 360f;
        if (normalizedAngle < 0) normalizedAngle += 360f;

        if (Mathf.Approximately(normalizedAngle, 0f))
        {
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((2, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((2, 0));
            cells.Add((2, 1));
            cells.Add((2, 2));
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            cells.Add((-2, 2));
            cells.Add((-1, 2));
            cells.Add((0, 2));
            cells.Add((0, 1));
            cells.Add((0, 0));
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
            cells.Add((1, 2));
            cells.Add((2, 2));
        }

        return cells;
    }
}
