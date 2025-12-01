using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Block Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<PrefabGenerator>("Prefab Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("方块预制体生成器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("生成所有方块Prefab", GUILayout.Height(40)))
        {
            GenerateAllPrefabs();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("生成六边形球Prefab", GUILayout.Height(40)))
        {
            CreateHexagonBallPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "六边形球Prefab已生成到 Assets/Prefabs/", "确定");
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "这会自动创建:\n" +
            "• 所有方块的Sprite\n" +
            "• 六边形球的Sprite\n" +
            "• 配置好的Prefab\n" +
            "• 保存到 Assets/Prefabs/",
            MessageType.Info);
    }

    private static void GenerateAllPrefabs()
    {
        // 创建必要的文件夹
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Blocks"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Blocks");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        // 生成各种方块
        CreateSingleBlockPrefab();
        CreateSquareBlockPrefab();
        CreateL3BlockPrefab();
        CreateL4BlockPrefab();
        CreateL5BlockPrefab();
        CreateLineBlockPrefab();

        // 生成六边形球
        CreateHexagonBallPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ 所有Prefab生成完成！");
        EditorUtility.DisplayDialog("完成", "所有Prefab已生成到 Assets/Prefabs/", "确定");
    }

    // 1. 单格方块
    private static void CreateSingleBlockPrefab()
    {
        GameObject go = new GameObject("SingleBlock");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite(64, 64, Color.cyan);
        sr.sortingOrder = 0;

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.95f, 0.95f);
        collider.offset = new Vector2(0.5f, 0.5f); // 64x64 图形在 128x128 纹理中的偏移

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        TowerBlock block = go.AddComponent<TowerBlock>();
        block.blockTypeName = "单格方块";
        block.scoreValue = 10;
        block.isStatic = true;

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/SingleBlock.prefab");
        Object.DestroyImmediate(go);
    }

    // 2. 正方形方块 (2x2)
    private static void CreateSquareBlockPrefab()
    {
        GameObject go = new GameObject("SquareBlock");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite(128, 128, Color.yellow);

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.9f, 1.9f);
        collider.offset = new Vector2(1.0f, 1.0f); // 128x128 图形在 256x256 纹理中的偏移

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SquareBlock block = go.AddComponent<SquareBlock>();
        block.blockTypeName = "方形方块";
        block.scoreValue = 30;
        block.isStatic = true;

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/SquareBlock.prefab");
        Object.DestroyImmediate(go);
    }

    // 3. L3型方块
    private static void CreateL3BlockPrefab()
    {
        GameObject go = new GameObject("L3Block");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLShapeSprite(3, Color.green);

        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateLShapeColliderPoints(3);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        L3Block block = go.AddComponent<L3Block>();
        block.blockTypeName = "L3方块";
        block.scoreValue = 50;
        block.isStatic = true;

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/L3Block.prefab");
        Object.DestroyImmediate(go);
    }

    // 4. L4型方块
    private static void CreateL4BlockPrefab()
    {
        GameObject go = new GameObject("L4Block");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLShapeSprite(4, Color.magenta);

        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateLShapeColliderPoints(4);

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        L4Block block = go.AddComponent<L4Block>();
        block.blockTypeName = "L4方块";
        block.scoreValue = 70;
        block.isStatic = true;

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/L4Block.prefab");
        Object.DestroyImmediate(go);
    }

    // 5. L5型方块 (等长L形 3x3)
    private static void CreateL5BlockPrefab()
    {
        GameObject go = new GameObject("L5Block");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateEqualLShapeSprite(new Color(1f, 0.5f, 0f)); // 橙色

        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateEqualLShapeColliderPoints();

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        L5Block block = go.AddComponent<L5Block>();
        block.blockTypeName = "L5方块";
        block.scoreValue = 90;
        block.isStatic = true;

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/L5Block.prefab");
        Object.DestroyImmediate(go);
    }

    // 6. I型方块 (4格横条)
    private static void CreateLineBlockPrefab()
    {
        GameObject go = new GameObject("LineBlock");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLineSprite(4, Color.red);

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(3.9f, 0.95f); // 4格宽，留间隙
        collider.offset = new Vector2(2.0f, 0.5f); // 256x64 图形在 512x128 纹理中的偏移

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        LineBlock block = go.AddComponent<LineBlock>();
        block.blockTypeName = "I型方块";
        block.scoreValue = 40;
        block.isStatic = true;

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/LineBlock.prefab");
        Object.DestroyImmediate(go);
    }

    // ========== 辅助方法 ==========

    // 创建L形碰撞器的点（逆时针）
    private static Vector2[] CreateLShapeColliderPoints(int blocks)
    {
        float gap = 0.05f;

        // L形状（2格宽，blocks格高）在右上角区域：
        // 由于图形绘制在右上角，需要计算正确的偏移
        // 原始尺寸: 2*64 x blocks*64，纹理尺寸: 4*64 x 2*blocks*64
        // 图形中心偏移: (64, blocks*32) pixels = (1.0f, blocks*0.5f) units
        float offsetX = 1.0f; // 向右偏移一个单位
        float offsetY = (float)blocks * 0.5f; // 向上偏移

        float width = 2f;
        float height = (float)blocks;

        return new Vector2[]
        {
            new Vector2(-width/2f + gap + offsetX, -height/2f + gap + offsetY),       // ①左下角
            new Vector2(width/2f - gap + offsetX, -height/2f + gap + offsetY),        // ②右下角
            new Vector2(width/2f - gap + offsetX, -height/2f + 1f - gap + offsetY),   // ③右下方块顶部
            new Vector2(-gap + offsetX, -height/2f + 1f - gap + offsetY),             // ④转折点
            new Vector2(-gap + offsetX, height/2f - gap + offsetY),                   // ⑤左上
            new Vector2(-width/2f + gap + offsetX, height/2f - gap + offsetY),        // ⑥左上角
        };
    }

    // 创建等长L形碰撞器的点（3x3的L形）
    private static Vector2[] CreateEqualLShapeColliderPoints()
    {
        float gap = 0.05f;

        // 等长L形（3x3）在右上角区域：
        // 由于图形绘制在右上角，需要计算正确的偏移
        // 原始尺寸: 3*64 x 3*64，纹理尺寴: 6*64 x 6*64
        // 图形中心偏移: (96, 96) pixels = (1.5f, 1.5f) units
        float offsetX = 1.5f; // 向右偏移1.5个单位
        float offsetY = 1.5f; // 向上偏移1.5个单位

        float size = 3f;
        float half = size / 2f;

        return new Vector2[]
        {
            new Vector2(-half + gap + offsetX, -half + gap + offsetY),       // ①左下角
            new Vector2(half - gap + offsetX, -half + gap + offsetY),        // ②右下角
            new Vector2(half - gap + offsetX, -half + 1f - gap + offsetY),   // ③右下第一格顶部
            new Vector2(-half + 1f - gap + offsetX, -half + 1f - gap + offsetY), // ④转折点
            new Vector2(-half + 1f - gap + offsetX, half - gap + offsetY),   // ⑤左上
            new Vector2(-half + gap + offsetX, half - gap + offsetY),        // ⑥左上角
        };
    }

    // 创建正方形Sprite（图形绘制在右上角）
    private static Sprite CreateSquareSprite(int width, int height, Color color)
    {
        // 创建更大的纹理，把图形绘制在右上角
        int textureWidth = width * 2;  // 纹理宽度加倍
        int textureHeight = height * 2; // 纹理高度加倍

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 在右上角区域绘制方块（从(width,height)到(textureWidth,textureHeight)）
        for (int y = height; y < textureHeight; y++)
        {
            for (int x = width; x < textureWidth; x++)
            {
                pixels[y * textureWidth + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"Assets/Sprites/Square_{width}x{height}.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = 64f;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 创建L形Sprite（图形绘制在右上角）
    private static Sprite CreateLShapeSprite(int blocks, Color color)
    {
        int size = 64;
        int originalWidth = 2 * size;
        int originalHeight = blocks * size;

        // 创建更大的纹理，把L形绘制在右上角
        int textureWidth = originalWidth * 2;  // 纹理宽度加倍
        int textureHeight = originalHeight * 2; // 纹理高度加倍

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 在右上角绘制L形：左边一列 + 底部一行

        // 左边一列（所有blocks层） - 从右上角的左侧开始
        for (int y = originalHeight; y < textureHeight; y++)
        {
            for (int x = originalWidth; x < originalWidth + size; x++)
            {
                pixels[y * textureWidth + x] = color;
            }
        }

        // 底部一行（只在最底层） - 从右上角的底部开始
        int bottomLayerStart = originalHeight;
        for (int y = bottomLayerStart; y < bottomLayerStart + size; y++)
        {
            for (int x = originalWidth; x < textureWidth; x++)
            {
                pixels[y * textureWidth + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"Assets/Sprites/L{blocks}Shape.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = 64f;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;



            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 创建等长L形Sprite（图形绘制在右上角）
    private static Sprite CreateEqualLShapeSprite(Color color)
    {
        int size = 64;
        int originalWidth = 3 * size;  // 3格宽
        int originalHeight = 3 * size; // 3格高

        // 创建更大的纹理，把等长L形绘制在右上角
        int textureWidth = originalWidth * 2;
        int textureHeight = originalHeight * 2;

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 在右上角绘制正确等长L形：左边一列(3格高) + 底部水平一排(3格宽)
        // 左边一列 (3格高) - 从右上角的左侧开始
        for (int y = originalHeight; y < textureHeight; y++)
        {
            for (int x = originalWidth; x < originalWidth + size; x++)
            {
                pixels[y * textureWidth + x] = color;
            }
        }

        // 底部水平一排(3格宽) - 从右上角的底部开始
        int bottomLayerStart = originalHeight;
        for (int y = bottomLayerStart; y < bottomLayerStart + size; y++)
        {
            for (int x = originalWidth; x < textureWidth; x++)
            {
                pixels[y * textureWidth + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string path = "Assets/Sprites/L5Shape_Equal.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = 64f;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;



            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 创建横条Sprite（图形绘制在右上角）
    private static Sprite CreateLineSprite(int blocks, Color color)
    {
        int size = 64;
        int originalWidth = blocks * size;
        int originalHeight = size;

        // 创建更大的纹理，把横条绘制在右上角
        int textureWidth = originalWidth * 2;
        int textureHeight = originalHeight * 2;

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 在右上角绘制横条
        for (int y = originalHeight; y < textureHeight; y++)
        {
            for (int x = originalWidth; x < textureWidth; x++)
            {
                pixels[y * textureWidth + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"Assets/Sprites/Line{blocks}.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = 64f;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;



            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 保存Prefab
    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Debug.Log($"✅ 已创建: {path}");
    }

    // 创建六边形球Prefab
    private static void CreateHexagonBallPrefab()
    {
        GameObject go = new GameObject("HexagonBall");

        // Sprite
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateHexagonSprite();
        sr.sortingOrder = 1; // 确保在方块上方

        // PolygonCollider2D (六边形)
        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateHexagonColliderPoints();

        // Rigidbody2D - Dynamic (会受物理影响)
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // HexagonBall脚本
        go.AddComponent<HexagonBall>();

        // 设置Layer
        go.layer = LayerMask.NameToLayer("HexagonBall");

        SavePrefab(go, "Assets/Prefabs/HexagonBall.prefab");
        Object.DestroyImmediate(go);
    }

    // 创建六边形Sprite
    private static Sprite CreateHexagonSprite()
    {
        const float HEXAGON_DIAMETER = 2.0f; // 直径=2个方格
        int textureSize = 256;
        float radius = textureSize / 2f * 0.9f; // 略小于纹理一半，留出边缘

        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] pixels = new Color[textureSize * textureSize];

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 绘制六边形
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 point = new Vector2(x, y);
                if (IsInsideHexagon(point, center, radius))
                {
                    pixels[y * textureSize + x] = Color.white;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        string path = "Assets/Sprites/HexagonBall.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = textureSize / HEXAGON_DIAMETER; // 128 PPU，直径=2个方格
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 创建六边形碰撞器的点
    private static Vector2[] CreateHexagonColliderPoints()
    {
        const float HEXAGON_DIAMETER = 2.0f; // 直径=2个方格
        float radius = HEXAGON_DIAMETER / 2f * 0.95f; // 半径略小于1.0，留出间隙

        Vector2[] points = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.PI / 3f * i; // 60度间隔
            points[i] = new Vector2(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle)
            );
        }
        return points;
    }

    // 判断点是否在六边形内
    private static bool IsInsideHexagon(Vector2 point, Vector2 center, float radius)
    {
        Vector2 diff = point - center;
        float distance = diff.magnitude;

        if (distance > radius) return false;

        // 六边形的六个边界
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.PI / 3f * i;
            Vector2 normal = new Vector2(Mathf.Cos(angle + Mathf.PI / 6f), Mathf.Sin(angle + Mathf.PI / 6f));
            float edgeDistance = radius * Mathf.Cos(Mathf.PI / 6f);

            if (Vector2.Dot(diff, normal) > edgeDistance)
                return false;
        }

        return true;
    }
}
