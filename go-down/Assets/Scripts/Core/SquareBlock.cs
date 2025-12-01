using UnityEngine;

/// <summary>
/// 2x2方形方块
/// </summary>
public class SquareBlock : TowerBlock
{
    /// <summary>
    /// 获取方形方块实际占用的格子（2x2）
    /// </summary>
    public override System.Collections.Generic.List<(int x, int y)> GetOccupiedCells(float rotationAngle)
    {
        var cells = new System.Collections.Generic.List<(int x, int y)>();

        // 2x2方形，旋转不影响占用格子
        cells.Add((0, 0)); // 左下
        cells.Add((1, 0)); // 右下
        cells.Add((0, 1)); // 左上
        cells.Add((1, 1)); // 右上

        return cells;
    }
}