using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// L3方块 - 2x3 L形
/// 实际占用4个格子
/// </summary>
public class L3Block : TowerBlock
{
    public override List<(int x, int y)> GetOccupiedCells(float rotationAngle)
    {
        var cells = new List<(int x, int y)>();

        float normalizedAngle = rotationAngle % 360f;
        if (normalizedAngle < 0) normalizedAngle += 360f;

        if (Mathf.Approximately(normalizedAngle, 0f))
        {
            // 0度: 正确的L形状 (2x3，占4格) - 基础形状
            // #
            // #
            // ##
            cells.Add((0, 0)); // 左下
            cells.Add((1, 0)); // 右下 (L的水平部分)
            cells.Add((0, 1)); // 左中
            cells.Add((0, 2)); // 左上
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            // 90度: 逆时针旋转90度 (3x2，占4格)
            //   #
            // ###
            cells.Add((0, 0)); // 左下
            cells.Add((1, 0)); // 中下
            cells.Add((2, 0)); // 右下
            cells.Add((2, 1)); // 右上
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            // 180度: 逆时针旋转180度 (2x3，占4格)
            // ##
            //  #
            //  #
            cells.Add((0, 2)); // 左上
            cells.Add((1, 2)); // 右上
            cells.Add((1, 1)); // 右中
            cells.Add((1, 0)); // 右下
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            // 270度: 逆时针旋转270度 (3x2，占4格)
            // ###
            // #
            cells.Add((0, 1)); // 左上
            cells.Add((0, 0)); // 左下
            cells.Add((1, 0)); // 中下
            cells.Add((2, 0)); // 右下
        }

        return cells;
    }
}
