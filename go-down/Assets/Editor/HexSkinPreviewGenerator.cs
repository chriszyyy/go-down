using System.IO;
using UnityEditor;
using UnityEngine;

public static class HexSkinPreviewGenerator
{
    private const string OutputDir = "Assets/UI/Sprites/HexPreviews";
    private const int PreviewLayer = 31;

    [MenuItem("Tools/Generate Hex Skin Previews")]
    public static void Generate()
    {
        Directory.CreateDirectory(OutputDir);

        GenerateOne("gold", new Color(1.00f, 0.78f, 0.20f, 1f));
        GenerateOne("blue", new Color(0.16f, 0.52f, 0.96f, 1f));
        GenerateOne("green", new Color(0.22f, 0.80f, 0.40f, 1f));
        GenerateOne("purple", new Color(0.64f, 0.35f, 0.95f, 1f));
        GenerateOne("red", new Color(0.93f, 0.23f, 0.36f, 1f));
        GenerateOne("rainbow", Color.white, true);

        AssetDatabase.Refresh();
        Debug.Log("Generated hex skin previews in " + OutputDir);
    }

    private static void GenerateOne(string id, Color color, bool rainbow = false)
    {
        int size = 512;
        var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 8;

        var camGo = new GameObject("PreviewCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.cullingMask = 1 << PreviewLayer;
        cam.orthographic = true;
        cam.orthographicSize = 1.15f;
        cam.transform.position = new Vector3(0f, 0f, -5f);
        cam.transform.rotation = Quaternion.identity;
        cam.targetTexture = rt;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HexagonBall.prefab");
        GameObject hexGo = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject("HexPreviewFallback");
        hexGo.name = "HexPreview";
        hexGo.transform.position = Vector3.zero;
        hexGo.transform.rotation = Quaternion.identity;
        hexGo.transform.localScale = Vector3.one;

        var hpv = hexGo.GetComponent<HexPrismVisual>();
        if (hpv == null) hpv = hexGo.AddComponent<HexPrismVisual>();
        hpv.liveTweak = false;
        hpv.gem = true;

        // 强制构建视觉
        var awake = typeof(HexPrismVisual).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        awake.Invoke(hpv, null);
        SetLayerRecursive(hexGo, PreviewLayer);
        var matField = typeof(HexPrismVisual).GetField("materialInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mat = matField.GetValue(hpv) as Material;
        if (mat != null)
        {
            if (rainbow)
            {
                mat.SetColor("_BaseColor", new Color(1f, 0.85f, 0.35f, 1f));
                mat.SetFloat("_RainbowSkin", 1f);
                mat.SetFloat("_GemDispersion", 0.9f);
                mat.SetFloat("_GemTint", 0.6f);
            }
            else
            {
                mat.SetFloat("_RainbowSkin", 0f);
                mat.SetColor("_BaseColor", color);
            }
        }

        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();

        string path = $"{OutputDir}/hex-{id}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());

        Object.DestroyImmediate(tex);
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(hexGo);
        Object.DestroyImmediate(camGo);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 256;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            if (child != null) SetLayerRecursive(child.gameObject, layer);
        }
    }
}
