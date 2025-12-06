using UnityEngine;

/// <summary>
/// 4x1线型方块（I型）
/// </summary>
public class LineBlock : TowerBlock
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
            return new Vector2Int(-4, -1);
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            return new Vector2Int(0, -4);
        }

        return Vector2Int.zero;
    }

    public override System.Collections.Generic.List<(int x, int y)> GetOccupiedCells(float rotationAngle)
    {
        var cells = new System.Collections.Generic.List<(int x, int y)>();

        float normalizedAngle = rotationAngle % 360f;
        if (normalizedAngle < 0) normalizedAngle += 360f;

        if (Mathf.Approximately(normalizedAngle, 0f))
        {
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((2, 0));
            cells.Add((3, 0));
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
            cells.Add((0, 3));
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((2, 0));
            cells.Add((3, 0));
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
            cells.Add((0, 3));
        }

        return cells;
    }
}