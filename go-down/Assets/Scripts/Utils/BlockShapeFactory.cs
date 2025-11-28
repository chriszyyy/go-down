using UnityEngine;

/// <summary>
/// 方块形状工厂 - 创建各种形状的方块组
/// </summary>
public static class BlockShapeFactory
{
    /// <summary>
    /// 创建形状（作为单一物理整体）
    /// </summary>
    /// <param name="shapeType">形状类型</param>
    /// <param name="position">中心位置（世界坐标）</param>
    /// <param name="blockSize">单个方块的大小</param>
    /// <param name="parent">父对象</param>
    /// <returns>形状组GameObject</returns>
    public static GameObject CreateShape(BlockShapeType shapeType, Vector2 position, float blockSize, Transform parent = null)
    {
        // 获取形状数据
        BlockShapeData shapeData = BlockShapeManager.GetShapeData(shapeType);

        // 计算形状中心点
        Vector2 center = Vector2.zero;
        foreach (Vector2Int gridPos in shapeData.positions)
        {
            center += new Vector2(gridPos.x, gridPos.y);
        }
        center /= shapeData.positions.Length;
        center *= blockSize;

        // 创建父对象（形状组）- 作为单一物理整体
        GameObject shapeGroup = new GameObject($"Shape_{shapeData.shapeName}");
        shapeGroup.transform.position = position + center;
        shapeGroup.layer = LayerMask.NameToLayer("Block");

        if (parent != null)
        {
            shapeGroup.transform.SetParent(parent);
        }

        // 添加 Rigidbody2D（初始为运动学）
        Rigidbody2D rb = shapeGroup.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = shapeData.positions.Length; // 质量与方块数相关
        rb.drag = 0.5f;
        rb.angularDrag = 1f;

        // 添加形状组组件
        BlockShapeGroup groupComponent = shapeGroup.AddComponent<BlockShapeGroup>();
        groupComponent.shapeType = shapeType;
        groupComponent.shapeColor = shapeData.color;

        // 创建每个方块（作为子对象，用于视觉和点击检测）
        foreach (Vector2Int gridPos in shapeData.positions)
        {
            GameObject block = new GameObject($"Block_{gridPos.x}_{gridPos.y}");
            block.transform.SetParent(shapeGroup.transform);
            block.transform.localPosition = new Vector2(gridPos.x * blockSize, gridPos.y * blockSize) - center;
            block.layer = LayerMask.NameToLayer("Block");

            // 添加 SpriteRenderer
            SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = BlockFactory.CreateBlockSprite();
            sr.color = shapeData.color;

            // 添加 BoxCollider2D（用于点击检测）
            BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(blockSize, blockSize);

            // 添加 Block 组件（用于点击检测）
            Block blockComponent = block.AddComponent<Block>();
        }

        // 在父对象上添加复合碰撞器
        BoxCollider2D[] colliders = shapeGroup.GetComponentsInChildren<BoxCollider2D>();
        foreach (var collider in colliders)
        {
            collider.usedByComposite = true;
        }

        CompositeCollider2D compositeCollider = shapeGroup.AddComponent<CompositeCollider2D>();
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;

        return shapeGroup;
    }

    /// <summary>
    /// 创建随机形状
    /// </summary>
    public static GameObject CreateRandomShape(Vector2 position, float blockSize, Transform parent = null)
    {
        BlockShapeType randomType = BlockShapeManager.GetRandomShapeType();
        return CreateShape(randomType, position, blockSize, parent);
    }
}
