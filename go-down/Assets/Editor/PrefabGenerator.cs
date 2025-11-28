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
        EditorGUILayout.HelpBox(
            "这会自动创建:\n" +
            "• 所有方块的Sprite\n" +
            "• 配置好的Prefab\n" +
            "• 保存到 Assets/Prefabs/Blocks/",
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

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ 所有方块Prefab生成完成！");
        EditorUtility.DisplayDialog("完成", "所有方块Prefab已生成到 Assets/Prefabs/Blocks/", "确定");
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

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        TowerBlock block = go.AddComponent<TowerBlock>();
        block.blockTypeName = "正方形方块";
        block.scoreValue = 40;
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

        TowerBlock block = go.AddComponent<TowerBlock>();
        block.blockTypeName = "L3方块";
        block.scoreValue = 30;
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

        TowerBlock block = go.AddComponent<TowerBlock>();
        block.blockTypeName = "L4方块";
        block.scoreValue = 40;
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

        TowerBlock block = go.AddComponent<TowerBlock>();
        block.blockTypeName = "L5方块";
        block.scoreValue = 50;
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

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        TowerBlock block = go.AddComponent<TowerBlock>();
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
        float gap = 0.025f;

        // L形状（2格宽，blocks格高）：
        // █      ← 顶部
        // █      
        // ██     ← 底部

        // Sprite的pivot在中心，宽度=2，高度=blocks
        float halfWidth = 1f;
        float halfHeight = blocks / 2f;

        return new Vector2[]
        {
            new Vector2(-halfWidth + gap, -halfHeight + gap),           // ①左下角
            new Vector2(halfWidth - gap, -halfHeight + gap),            // ②右下角
            new Vector2(halfWidth - gap, -halfHeight + 1f - gap),       // ③右下方块顶部右角
            new Vector2(-halfWidth + 1f - gap, -halfHeight + 1f - gap), // ④转折点（内角）
            new Vector2(-halfWidth + 1f - gap, halfHeight - gap),       // ⑤右上角
            new Vector2(-halfWidth + gap, halfHeight - gap),            // ⑥左上角
        };
    }

    // 创建等长L形碰撞器的点（3x3的L形）
    private static Vector2[] CreateEqualLShapeColliderPoints()
    {
        float gap = 0.025f;

        // 等长L形（3x3）：
        // █      
        // █      
        // ███    

        // Sprite的pivot在中心，宽度=3，高度=3
        float halfSize = 1.5f;

        return new Vector2[]
        {
            new Vector2(-halfSize + gap, -halfSize + gap),           // ①左下角
            new Vector2(halfSize - gap, -halfSize + gap),            // ②右下角
            new Vector2(halfSize - gap, -halfSize + 1f - gap),       // ③右下第一格顶部右角
            new Vector2(-halfSize + 1f - gap, -halfSize + 1f - gap), // ④转折点（内角）
            new Vector2(-halfSize + 1f - gap, halfSize - gap),       // ⑤右上角
            new Vector2(-halfSize + gap, halfSize - gap),            // ⑥左上角
        };
    }

    // 创建正方形Sprite
    private static Sprite CreateSquareSprite(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
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

    // 创建L形Sprite
    private static Sprite CreateLShapeSprite(int blocks, Color color)
    {
        int size = 64;
        int width = 2 * size;
        int height = blocks * size;

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // L形: 左边一列 + 底部右边一格
        // 左边一列
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * width + x] = color;
            }
        }

        // 底部右边一格
        for (int y = 0; y < size; y++)
        {
            for (int x = size; x < width; x++)
            {
                pixels[y * width + x] = color;
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

    // 创建等长L形Sprite (3x3)
    private static Sprite CreateEqualLShapeSprite(Color color)
    {
        int size = 64;
        int width = 3 * size;  // 3格宽
        int height = 3 * size; // 3格高

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 等长L形: 左边一列(3格高) + 底部右边两格
        // 左边一列 (3格高)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < size; x++)
            {
                pixels[y * width + x] = color;
            }
        }

        // 底部右边两格
        for (int y = 0; y < size; y++)
        {
            for (int x = size; x < width; x++)
            {
                pixels[y * width + x] = color;
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

    // 创建横条Sprite
    private static Sprite CreateLineSprite(int blocks, Color color)
    {
        int size = 64;
        int width = blocks * size;
        int height = size;

        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
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
}
