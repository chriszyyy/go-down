using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// L2方块 - 3格L型（类似小三角）
/// 占用3个格子
/// </summary>
public class L2Block : TowerBlock
{
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
            return new Vector2Int(-2, 0);
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            return new Vector2Int(-1, -2);
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
            // 0度: L形
            // #.    <- (0,1)
            // ##    <- (0,0) + (1,0)
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((0, 1));
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            // 90度:
            // .#    <- (1,1)
            // ##    <- (0,0) + (1,0)
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((1, 1));
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((-1, 1));
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            // 270度:
            // ##    <- (0,1) + (1,1)
            // #.    <- (0,0)
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((1, 1));
        }

        return cells;
    }
}
