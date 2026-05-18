using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools/Crafting/Generate Item Icons — рендерит каждый Item-префаб
/// в PNG-спрайт и проставляет его в поле Item.icon на префабе.
/// </summary>
public static class ItemIconGenerator
{
    private const string OutputDir = "Assets/UI/ItemIcons";
    private const int IconSize = 256;

    // Папки/конкретные префабы, которые надо просканировать
    private static readonly string[] SearchFolders = { "Assets/Prefabs/items" };
    private static readonly string[] ExtraPrefabs = {
        "Assets/glue/Maker_Tool_Set_DIY_Crafting_Tools/Environment/Prefabs/Glue.prefab",
        "Assets/Tent/Demo Stylized Camping Kit/Prefabs/SleepingBag_cl  V2.prefab",
        "Assets/Repair Kit/Prefabs/SM_Workbench.prefab",
        "Assets/Repair Kit/Prefabs/SM_Scrap_Metal_01.prefab",
    };

    [MenuItem("Tools/Crafting/Generate Item Icons (all)")]
    public static void GenerateAll()
    {
        Generate(overwriteExisting: true, requireItemComponent: false);
    }

    [MenuItem("Tools/Crafting/Generate Item Icons (only missing)")]
    public static void GenerateMissing()
    {
        Generate(overwriteExisting: false, requireItemComponent: false);
    }

    private static void Generate(bool overwriteExisting, bool requireItemComponent)
    {
        if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);

        var paths = new List<string>();
        var guids = AssetDatabase.FindAssets("t:Prefab", SearchFolders);
        foreach (var g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
        foreach (var p in ExtraPrefabs) if (!paths.Contains(p)) paths.Add(p);

        int done = 0;
        try
        {
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                EditorUtility.DisplayProgressBar("Generating item icons",
                    $"{Path.GetFileName(path)} ({i + 1}/{paths.Count})",
                    (float)i / paths.Count);

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var item = prefab.GetComponent<Item>();
                if (requireItemComponent && item == null) continue;

                string iconName = Path.GetFileNameWithoutExtension(path) + "_icon.png";
                string outPath = $"{OutputDir}/{iconName}";

                if (!overwriteExisting && File.Exists(outPath))
                {
                    AssignIconIfNeeded(prefab, item, outPath);
                    done++;
                    continue;
                }

                if (RenderAndSave(prefab, outPath))
                {
                    AssignIconIfNeeded(prefab, item, outPath);
                    done++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ItemIconGenerator] Done. Processed {done}/{paths.Count} prefabs. Output: {OutputDir}");
    }

    private static void AssignIconIfNeeded(GameObject prefabAsset, Item itemOnAsset, string spritePath)
    {
        if (itemOnAsset == null) return;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"[ItemIconGenerator] Sprite not loaded yet: {spritePath}");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
        if (string.IsNullOrEmpty(assetPath)) return;

        // Документированный способ редактировать префаб-ассет
        var contents = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            var item = contents.GetComponent<Item>();
            if (item == null) return;

            var so = new SerializedObject(item);
            var iconProp = so.FindProperty("icon");
            if (iconProp == null) return;
            if (iconProp.objectReferenceValue == sprite) return;
            iconProp.objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static bool RenderAndSave(GameObject prefab, string outPath)
    {
        // Создаём preview-сцену, чтобы не пересекаться с активной
        var previewScene = EditorSceneManager.NewPreviewScene();
        GameObject instance = null;
        GameObject camGo = null;
        GameObject lightGo = null;
        RenderTexture rt = null;
        Texture2D tex = null;

        try
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, previewScene);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            // Включаем все коллайдеры/рендереры на случай SetActive(false) внутри Item.Awake (в эдиторе Awake не запускается на префабах, но на всякий случай)
            SetActiveDeep(instance, true);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            if (b.size.sqrMagnitude < 1e-6f) b = new Bounds(instance.transform.position, Vector3.one);

            // Камера
            camGo = new GameObject("__IconCam");
            SceneManager.MoveGameObjectToScene(camGo, previewScene);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = b.size.magnitude * 6f + 10f;
            cam.orthographicSize = Mathf.Max(b.extents.x, b.extents.y, b.extents.z) * 1.15f;
            cam.scene = previewScene;
            // ракурс ¾
            var dir = new Vector3(1f, 0.7f, 1f).normalized;
            cam.transform.position = b.center + dir * (b.size.magnitude + 2f);
            cam.transform.LookAt(b.center);

            // Свет
            lightGo = new GameObject("__IconLight");
            SceneManager.MoveGameObjectToScene(lightGo, previewScene);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = Color.white;
            lightGo.transform.rotation = Quaternion.Euler(40f, -35f, 0f);

            // RenderTexture
            rt = new RenderTexture(IconSize, IconSize, 16, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };
            rt.Create();

            cam.targetTexture = rt;
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            cam.Render();

            tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            cam.targetTexture = null;

            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            var ti = (TextureImporter)AssetImporter.GetAtPath(outPath);
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.mipmapEnabled = false;
                ti.spritePixelsPerUnit = 100;
                ti.SaveAndReimport();
            }
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ItemIconGenerator] Failed for {prefab.name}: {e.Message}");
            return false;
        }
        finally
        {
            if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
            if (tex != null) Object.DestroyImmediate(tex);
            if (camGo != null) Object.DestroyImmediate(camGo);
            if (lightGo != null) Object.DestroyImmediate(lightGo);
            if (instance != null) Object.DestroyImmediate(instance);
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static void SetActiveDeep(GameObject go, bool active)
    {
        go.SetActive(active);
        foreach (Transform t in go.transform) SetActiveDeep(t.gameObject, active);
    }
}
