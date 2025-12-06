using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// L3方块 - 2x3 L形
/// 实际占用4个格子
/// </summary>
public class L3Block : TowerBlock
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
            // 注意锚点x偏移1
            return new Vector2Int(-1, -3);
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            return new Vector2Int(0, -2);
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
            // 0度: L形状，pivot在底-左(0,0)
            // #     <- (0,2)
            // #     <- (0,1)
            // ##    <- (0,0) + (1,0)
            cells.Add((0, 0)); // pivot点
            cells.Add((1, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            // 90度: 绕(0,0)逆时针旋转90度
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((2, 0));
            cells.Add((2, 1));
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            // 180度: 绕(0,0)旋转180度
            // 注意x偏移-1,保证下端贴紧
            cells.Add((-1, 2));
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            // 270度: 绕(0,0)逆时针270度 = 顺时针90度
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((1, 1));
            cells.Add((2, 1));
        }

        return cells;
    }
}
