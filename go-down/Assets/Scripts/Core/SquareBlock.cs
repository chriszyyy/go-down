using UnityEngine;

/// <summary>
/// 2x2方形方块
/// </summary>
public class SquareBlock : TowerBlock
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
            return new Vector2Int(-2, -2);
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            return new Vector2Int(0, -2);
        }

        return Vector2Int.zero;
    }

    public override System.Collections.Generic.List<(int x, int y)> GetOccupiedCells(float rotationAngle)
    {
        var cells = new System.Collections.Generic.List<(int x, int y)>
        {
            (0, 0),
            (1, 0),
            (0, 1),
            (1, 1)
        };

        return cells;
    }
}