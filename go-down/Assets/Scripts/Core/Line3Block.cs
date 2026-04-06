using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 3格线型方块（短I型）
/// </summary>
public class Line3Block : TowerBlock
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
            return new Vector2Int(-1, 0);
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            return new Vector2Int(-3, -1);
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
            // 水平: ###
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((2, 0));
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            // 垂直:
            // #
            // #
            // #
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            // 水平: ###
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((2, 0));
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            // 垂直:
            // #
            // #
            // #
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
        }

        return cells;
    }
}
