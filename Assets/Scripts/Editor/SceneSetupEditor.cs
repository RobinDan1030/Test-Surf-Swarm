using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneSetupEditor : EditorWindow
{
    [MenuItem("Tools/Setup 2D Boid Scene")]
    static void SetupScene()
    {
        // Clear existing objects
        foreach (GameObject go in FindObjectsOfType<GameObject>())
        {
            if (!go.isStatic && go.tag != "MainCamera")
                DestroyImmediate(go);
        }

        // --- Camera ---
        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 15f;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.nearClipPlane = -100f;
        cam.farClipPlane = 100f;
        camObj.AddComponent<AudioListener>();
        Camera2DController camCtrl = camObj.AddComponent<Camera2DController>();
        camObj.tag = "MainCamera";

        // --- Boid Manager ---
        GameObject managerObj = new GameObject("BoidManager");
        BoidManager boidManager = managerObj.AddComponent<BoidManager>();

        // --- Boundary ---
        GameObject boundaryObj = new GameObject("Boundary");
        BoundaryRenderer boundary = boundaryObj.AddComponent<BoundaryRenderer>();

        // --- Configure Boundary Sizes ---
        boidManager.boundarySize = new Vector2(20f, 20f);
        boundary.boundarySize = new Vector2(20f, 20f);

        // --- Create Boid Prefab ---
        GameObject boidPrefab = CreateBoidPrefab();
        boidManager.boidPrefab = boidPrefab;
        boidManager.initialBoidCount = 50;

        // --- Link Camera to Manager ---
        camCtrl.boidManager = boidManager;
        camCtrl.orthoSize = 12f;

        // --- Save Boid as Prefab ---
        string prefabDir = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        PrefabUtility.SaveAsPrefabAsset(boidPrefab, prefabDir + "/Boid.prefab");
        boidManager.boidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabDir + "/Boid.prefab");
        DestroyImmediate(boidPrefab);

        // --- Save Scene ---
        string scenePath = "Assets/Scenes/2D_Swarm.unity";
        string sceneDir = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(sceneDir))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }
        EditorSceneManager.SaveOpenScenes();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Setup Complete", "2D Boid Scene created successfully!\n\nPress Play to start.", "OK");
    }

    static GameObject CreateBoidPrefab()
    {
        // 确保目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        // 创建三角形网格并保存为 asset
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(0f, 0.5f, 0f),
            new Vector3(-0.3f, -0.3f, 0f),
            new Vector3(0.3f, -0.3f, 0f)
        };
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, "Assets/Prefabs/BoidMesh.asset");

        // 创建黄色材质并保存为 asset
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.yellow;
        AssetDatabase.CreateAsset(mat, "Assets/Materials/BoidMaterial.mat");

        AssetDatabase.SaveAssets();

        // 创建 GameObject，赋值 asset 引用
        GameObject boid = new GameObject("Boid");
        boid.transform.localScale = Vector3.one * 0.5f;

        MeshFilter mf = boid.AddComponent<MeshFilter>();
        mf.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Prefabs/BoidMesh.asset");

        MeshRenderer mr = boid.AddComponent<MeshRenderer>();
        mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoidMaterial.mat");

        boid.AddComponent<Boid2D>();
        boid.SetActive(false);
        return boid;
    }
}
