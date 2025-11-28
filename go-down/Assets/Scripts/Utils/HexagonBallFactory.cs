using UnityEngine;

/// <summary>
/// 六边形球工厂 - 用于创建六边形球
/// </summary>
public static class HexagonBallFactory
{
    // 六边形球的尺寸（边长等于一个方块格子的宽度）
    // 正六边形的外接圆直径 = 2 * 边长，所以 SIZE = 2.0
    public const float HEXAGON_SIZE = 2.0f;

    /// <summary>
    /// 创建六边形球
    /// </summary>
    public static GameObject CreateHexagonBall(Vector2 position, Transform parent = null)
    {
        // 创建游戏对象
        GameObject ballObj = new GameObject("HexagonBall");
        ballObj.transform.position = position;
        ballObj.layer = LayerMask.NameToLayer("HexagonBall");

        if (parent != null)
        {
            ballObj.transform.SetParent(parent);
        }

        // 添加 SpriteRenderer
        SpriteRenderer spriteRenderer = ballObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateHexagonSprite();
        spriteRenderer.color = new Color(1f, 0.8f, 0.2f); // 金色
        spriteRenderer.sortingOrder = 10; // 确保在方块上面

        // 添加 Rigidbody2D
        Rigidbody2D rb = ballObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.drag = 0.5f;
        rb.angularDrag = 2f;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 添加 PolygonCollider2D (六边形)
        PolygonCollider2D collider = ballObj.AddComponent<PolygonCollider2D>();
        // Sprite 的实际半径计算：
        // 纹理中半径 = 128 / 2.1 ≈ 60.95 像素
        // pixelsPerUnit = 128 / 2.0 = 64
        // 世界坐标半径 = 60.95 / 64 ≈ 0.952
        // 六边形边长 ≈ 0.952，接近 1.0（方块边长）
        float colliderRadius = (128f / 2.1f) / (128f / HEXAGON_SIZE);
        collider.points = GetHexagonPoints(colliderRadius);

        // 添加 HexagonBall 脚本
        HexagonBall hexagonBall = ballObj.AddComponent<HexagonBall>();

        return ballObj;
    }

    /// <summary>
    /// 创建六边形 Sprite
    /// </summary>
    private static Sprite CreateHexagonSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        // 填充透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        Vector2 center = new Vector2(size / 2f, size / 2f);
        // 六边形占据整个纹理的大部分
        // 让六边形边长 = 半径，使得纹理中六边形尽可能大
        float radius = size / 2.1f; // 接近最大尺寸

        // 获取六边形顶点
        Vector2[] hexPoints = GetHexagonPoints(radius);

        // 填充六边形
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);

                // 检查点是否在六边形内
                if (IsPointInHexagon(point, center, hexPoints))
                {
                    // 计算到中心的距离，用于渐变效果
                    float distToCenter = Vector2.Distance(point, center);
                    float normalizedDist = distToCenter / radius;

                    // 创建渐变效果
                    Color color = Color.white;
                    if (normalizedDist > 0.85f)
                    {
                        // 边缘暗一些，形成边框
                        color = new Color(0.7f, 0.7f, 0.7f);
                    }

                    pixels[y * size + x] = color;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;

        // 创建 Sprite
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size / HEXAGON_SIZE
        );

        return sprite;
    }

    /// <summary>
    /// 获取六边形的顶点坐标（相对于中心点）
    /// </summary>
    private static Vector2[] GetHexagonPoints(float radius)
    {
        Vector2[] points = new Vector2[6];

        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad;
            points[i] = new Vector2(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle)
            );
        }

        return points;
    }

    /// <summary>
    /// 检查点是否在六边形内
    /// </summary>
    private static bool IsPointInHexagon(Vector2 point, Vector2 center, Vector2[] hexPoints)
    {
        point -= center;

        // 使用射线法判断点是否在多边形内
        int intersections = 0;
        for (int i = 0; i < hexPoints.Length; i++)
        {
            Vector2 p1 = hexPoints[i];
            Vector2 p2 = hexPoints[(i + 1) % hexPoints.Length];

            if ((p1.y > point.y) != (p2.y > point.y))
            {
                float slope = (point.y - p1.y) / (p2.y - p1.y);
                if (point.x < slope * (p2.x - p1.x) + p1.x)
                {
                    intersections++;
                }
            }
        }

        return (intersections % 2) == 1;
    }
}
