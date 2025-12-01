using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// L4方块 - 2x4 L形
/// 实际占用5个格子
/// </summary>
public class L4Block : TowerBlock
{
    public override List<(int x, int y)> GetOccupiedCells(float rotationAngle)
    {
        var cells = new List<(int x, int y)>();
        
        float normalizedAngle = rotationAngle % 360f;
        if (normalizedAngle < 0) normalizedAngle += 360f;

        if (Mathf.Approximately(normalizedAngle, 0f))
        {
            // 0度: L形状 (2x4，占5格)
            // ##
            // #
            // #
            // #
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((0, 1));
            cells.Add((0, 2));
            cells.Add((0, 3));
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            // 90度: 旋转后 (4x2，占5格)
            // #
            // ####
            cells.Add((0, 0));
            cells.Add((0, 1));
            cells.Add((1, 1));
            cells.Add((2, 1));
            cells.Add((3, 1));
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            // 180度: 倒L形状 (2x4，占5格)
            //  #
            //  #
            //  #
            // ##
            cells.Add((1, 0));
            cells.Add((1, 1));
            cells.Add((1, 2));
            cells.Add((0, 3));
            cells.Add((1, 3));
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            // 270度: 旋转后 (4x2，占5格)
            // ####
            //    #
            cells.Add((0, 0));
            cells.Add((1, 0));
            cells.Add((2, 0));
            cells.Add((3, 0));
            cells.Add((3, 1));
        }

        return cells;
    }
}
