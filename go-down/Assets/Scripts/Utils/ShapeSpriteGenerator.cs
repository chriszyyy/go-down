using UnityEngine;

/// <summary>
/// 完整形状 Sprite 生成器 - 生成一体化的俄罗斯方块形状
/// </summary>
public static class ShapeSpriteGenerator
{
    private const int PIXELS_PER_BLOCK = 32; // 每个方块单元的像素大小
    private const int BORDER_WIDTH = 2;      // 边框宽度

    /// <summary>
    /// 创建 I 形状 Sprite (一字形 4格)
    /// ████
    /// </summary>
    public static Sprite CreateIShape()
    {
        int width = PIXELS_PER_BLOCK * 4;
        int height = PIXELS_PER_BLOCK * 1;
        Color color = new Color(0.0f, 0.9f, 0.9f); // 青色

        return CreateRectangleSprite(width, height, color);
    }

    /// <summary>
    /// 创建 O 形状 Sprite (正方形 2x2)
    /// ██
    /// ██
    /// </summary>
    public static Sprite CreateOShape()
    {
        int size = PIXELS_PER_BLOCK * 2;
        Color color = new Color(0.9f, 0.9f, 0.0f); // 黄色

        return CreateRectangleSprite(size, size, color);
    }

    /// <summary>
    /// 创建 T 形状 Sprite
    /// ███
    ///  █
    /// </summary>
    public static Sprite CreateTShape()
    {
        int width = PIXELS_PER_BLOCK * 3;
        int height = PIXELS_PER_BLOCK * 2;
        Color color = new Color(0.6f, 0.0f, 0.9f); // 紫色

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        // 填充透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 绘制 T 形状
        // 上部：三个方块
        FillRect(pixels, width, height, 0, PIXELS_PER_BLOCK, width, PIXELS_PER_BLOCK, color);
        // 下部：中间一个方块
        FillRect(pixels, width, height, PIXELS_PER_BLOCK, 0, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK, color);

        // 添加边框
        DrawBorder(pixels, width, height, 0, PIXELS_PER_BLOCK, width, PIXELS_PER_BLOCK);
        DrawBorder(pixels, width, height, PIXELS_PER_BLOCK, 0, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK);

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PIXELS_PER_BLOCK);
    }

    /// <summary>
    /// 创建 L 形状 Sprite
    /// █
    /// █
    /// ██
    /// </summary>
    public static Sprite CreateLShape()
    {
        int width = PIXELS_PER_BLOCK * 2;
        int height = PIXELS_PER_BLOCK * 3;
        Color color = new Color(0.9f, 0.6f, 0.0f); // 橙色

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 左侧竖条
        FillRect(pixels, width, height, 0, 0, PIXELS_PER_BLOCK, height, color);
        // 底部右方块
        FillRect(pixels, width, height, PIXELS_PER_BLOCK, 0, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK, color);

        DrawBorder(pixels, width, height, 0, 0, PIXELS_PER_BLOCK, height);
        DrawBorder(pixels, width, height, PIXELS_PER_BLOCK, 0, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK);

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PIXELS_PER_BLOCK);
    }

    /// <summary>
    /// 创建 J 形状 Sprite
    ///  █
    ///  █
    /// ██
    /// </summary>
    public static Sprite CreateJShape()
    {
        int width = PIXELS_PER_BLOCK * 2;
        int height = PIXELS_PER_BLOCK * 3;
        Color color = new Color(0.0f, 0.0f, 0.9f); // 蓝色

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 右侧竖条
        FillRect(pixels, width, height, PIXELS_PER_BLOCK, 0, PIXELS_PER_BLOCK, height, color);
        // 底部左方块
        FillRect(pixels, width, height, 0, 0, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK, color);

        DrawBorder(pixels, width, height, PIXELS_PER_BLOCK, 0, PIXELS_PER_BLOCK, height);
        DrawBorder(pixels, width, height, 0, 0, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK);

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PIXELS_PER_BLOCK);
    }

    /// <summary>
    /// 创建 S 形状 Sprite
    ///  ██
    /// ██
    /// </summary>
    public static Sprite CreateSShape()
    {
        int width = PIXELS_PER_BLOCK * 3;
        int height = PIXELS_PER_BLOCK * 2;
        Color color = new Color(0.0f, 0.9f, 0.0f); // 绿色

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 上部右侧
        FillRect(pixels, width, height, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK * 2, PIXELS_PER_BLOCK, color);
        // 下部左侧
        FillRect(pixels, width, height, 0, 0, PIXELS_PER_BLOCK * 2, PIXELS_PER_BLOCK, color);

        DrawBorder(pixels, width, height, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK * 2, PIXELS_PER_BLOCK);
        DrawBorder(pixels, width, height, 0, 0, PIXELS_PER_BLOCK * 2, PIXELS_PER_BLOCK);

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PIXELS_PER_BLOCK);
    }

    /// <summary>
    /// 创建 Z 形状 Sprite
    /// ██
    ///  ██
    /// </summary>
    public static Sprite CreateZShape()
    {
        int width = PIXELS_PER_BLOCK * 3;
        int height = PIXELS_PER_BLOCK * 2;
        Color color = new Color(0.9f, 0.0f, 0.0f); // 红色

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 上部左侧
        FillRect(pixels, width, height, 0, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK * 2, PIXELS_PER_BLOCK, color);
        // 下部右侧
        FillRect(pixels, width, height, PIXELS_PER_BLOCK, 0, PIXELS_PER_BLOCK * 2, PIXELS_PER_BLOCK, color);

        DrawBorder(pixels, width, height, 0, PIXELS_PER_BLOCK, PIXELS_PER_BLOCK * 2, PIXELS_PER_BLOCK);
        DrawBorder(pixels, width, height, PIXELS_PER_BLOCK, 0, PIXELS_PER_BLOCK * 2, PIXELS_PER_BLOCK);

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PIXELS_PER_BLOCK);
    }

    /// <summary>
    /// 创建单个方块 Sprite
    /// █
    /// </summary>
    public static Sprite CreateSingleBlock()
    {
        int size = PIXELS_PER_BLOCK;
        Color color = new Color(0.8f, 0.8f, 0.8f); // 灰色

        return CreateRectangleSprite(size, size, color);
    }

    /// <summary>
    /// 创建矩形 Sprite（带边框）
    /// </summary>
    private static Sprite CreateRectangleSprite(int width, int height, Color fillColor)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        // 填充颜色
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = fillColor;
        }

        // 绘制边框
        Color borderColor = new Color(0.2f, 0.2f, 0.2f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < BORDER_WIDTH || x >= width - BORDER_WIDTH ||
                    y < BORDER_WIDTH || y >= height - BORDER_WIDTH)
                {
                    pixels[y * width + x] = borderColor;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), PIXELS_PER_BLOCK);
    }

    /// <summary>
    /// 填充矩形区域
    /// </summary>
    private static void FillRect(Color[] pixels, int textureWidth, int textureHeight,
                                  int startX, int startY, int width, int height, Color color)
    {
        for (int y = startY; y < startY + height && y < textureHeight; y++)
        {
            for (int x = startX; x < startX + width && x < textureWidth; x++)
            {
                if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                {
                    pixels[y * textureWidth + x] = color;
                }
            }
        }
    }

    /// <summary>
    /// 绘制边框
    /// </summary>
    private static void DrawBorder(Color[] pixels, int textureWidth, int textureHeight,
                                    int startX, int startY, int width, int height)
    {
        Color borderColor = new Color(0.2f, 0.2f, 0.2f);

        for (int y = startY; y < startY + height && y < textureHeight; y++)
        {
            for (int x = startX; x < startX + width && x < textureWidth; x++)
            {
                if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight)
                {
                    // 绘制边框
                    if (x < startX + BORDER_WIDTH || x >= startX + width - BORDER_WIDTH ||
                        y < startY + BORDER_WIDTH || y >= startY + height - BORDER_WIDTH)
                    {
                        pixels[y * textureWidth + x] = borderColor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 根据形状类型获取对应的 Sprite
    /// </summary>
    public static Sprite GetShapeSprite(TetrisShapeType shapeType)
    {
        switch (shapeType)
        {
            case TetrisShapeType.I: return CreateIShape();
            case TetrisShapeType.O: return CreateOShape();
            case TetrisShapeType.T: return CreateTShape();
            case TetrisShapeType.L: return CreateLShape();
            case TetrisShapeType.J: return CreateJShape();
            case TetrisShapeType.S: return CreateSShape();
            case TetrisShapeType.Z: return CreateZShape();
            case TetrisShapeType.Single: return CreateSingleBlock();
            default: return CreateSingleBlock();
        }
    }
}
