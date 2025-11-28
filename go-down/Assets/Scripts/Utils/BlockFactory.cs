using UnityEngine;

/// <summary>
/// 方块工厂 - 用于创建方块预制体
/// </summary>
public static class BlockFactory
{
    // 方块的基础尺寸
    public const float BLOCK_WIDTH = 1f;
    public const float BLOCK_HEIGHT = 1f;

    // 预定义的方块颜色
    private static readonly Color[] BlockColors = new Color[]
    {
        new Color(0.2f, 0.6f, 0.9f),  // 蓝色
        new Color(0.9f, 0.3f, 0.3f),  // 红色
        new Color(0.3f, 0.9f, 0.4f),  // 绿色
        new Color(0.9f, 0.8f, 0.2f),  // 黄色
        new Color(0.8f, 0.3f, 0.9f),  // 紫色
        new Color(0.9f, 0.5f, 0.2f),  // 橙色
    };

    /// <summary>
    /// 创建一个基础方块（运行时创建，用于测试）
    /// </summary>
    public static GameObject CreateBlock(Vector2 position, Transform parent = null)
    {
        // 创建游戏对象
        GameObject blockObj = new GameObject("Block");
        blockObj.transform.position = position;
        blockObj.layer = LayerMask.NameToLayer("Block");

        if (parent != null)
        {
            blockObj.transform.SetParent(parent);
        }

        // 添加 SpriteRenderer
        SpriteRenderer spriteRenderer = blockObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateBlockSprite();
        spriteRenderer.color = GetRandomBlockColor();
        spriteRenderer.sortingOrder = 1;

        // 添加 BoxCollider2D
        BoxCollider2D collider = blockObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(BLOCK_WIDTH * 0.95f, BLOCK_HEIGHT * 0.95f); // 稍微小一点，留出间隙

        // 添加 Rigidbody2D
        Rigidbody2D rb = blockObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        rb.mass = 1f;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 添加 Block 脚本
        Block block = blockObj.AddComponent<Block>();

        return blockObj;
    }

    /// <summary>
    /// 创建一个正方形 Sprite（临时使用，实际项目中应该用美术资源）
    /// </summary>
    public static Sprite CreateBlockSprite()
    {
        // 创建一个简单的正方形纹理
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        // 填充白色
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }

        // 添加边框（黑色）
        int borderWidth = 2;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                if (x < borderWidth || x >= size - borderWidth ||
                    y < borderWidth || y >= size - borderWidth)
                {
                    pixels[y * size + x] = new Color(0.1f, 0.1f, 0.1f); // 深灰色边框
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point; // 像素风格

        // 创建 Sprite
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );

        return sprite;
    }

    /// <summary>
    /// 获取随机方块颜色
    /// </summary>
    public static Color GetRandomBlockColor()
    {
        return BlockColors[Random.Range(0, BlockColors.Length)];
    }

    /// <summary>
    /// 根据索引获取方块颜色
    /// </summary>
    public static Color GetBlockColor(int index)
    {
        return BlockColors[index % BlockColors.Length];
    }
}
