using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

/*
* This is an EDITOR script.
* It adds a new menu item: "Tools > Create Block > Create 1x1 Round Plate".
*
* This creates a composite prefab with:
* 1. A solid Cylinder "plate" base (1-unit diameter, 0.27-unit height)
* 2. A single standard Cylinder peg in the center.
* 3. Correctly scaled colliders.
* 4. Uses the standard RED material.
*/

public class RoundPlateGenerator
{
    // Define standard sizes
    private const float PLATE_HEIGHT = 0.27f; // 1/3 of a 0.8 brick
    private const float BASE_DIAMETER = 1.0f;
    private const float PEG_HEIGHT = 0.2f;
    private const float PEG_DIAMETER = 0.6f;


    [MenuItem("Tools/Create Block/Create 1x1 Round Plate")]
    public static void CreateComposite1x1Round()
    {
        // --- 1. Define Paths ---
        string blockName = "Block_1x1_Round_Plate";
        string prefabDir = "Assets/Prefabs";
        string prefabPath = prefabDir + "/" + blockName + ".prefab";
        // USE THE STANDARD RED MATERIAL
        string matPath = "Assets/Materials/M_Block_Red_Simple.mat";

        // --- 2. Create Directories ---
        if (!Directory.Exists(Application.dataPath + "/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!Directory.Exists(Application.dataPath + "/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // --- 3. Get or Create Material ---
        Material mat = GetOrCreateMaterial(matPath);

        // --- 4. Create the ROOT object ---
        GameObject blockRoot = new GameObject(blockName);
        blockRoot.transform.position = Vector3.zero;

        // --- 5. Create the Base (Cylinder) ---
        // A primitive cylinder is 2 units high and 1 unit in diameter.
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "Base";
        baseObj.transform.parent = blockRoot.transform;

        // Scale it: Y scale is height / 2 (since primitive is 2 high)
        float baseScaleY = PLATE_HEIGHT / 2.0f; // 0.27 / 2 = 0.135
        // X,Z scale is just the diameter
        baseObj.transform.localScale = new Vector3(BASE_DIAMETER, baseScaleY, BASE_DIAMETER);

        // Position it: Center is half the height
        baseObj.transform.localPosition = new Vector3(0, PLATE_HEIGHT / 2.0f, 0);
        baseObj.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Adjust its default collider
        CapsuleCollider baseCollider = baseObj.GetComponent<CapsuleCollider>();
        baseCollider.center = Vector3.zero;
        baseCollider.radius = 0.5f; // Radius is 0.5 * scale (which is 0.5)
        baseCollider.height = 2.0f; // Height is 2.0 * scale (which is 0.27)


        // --- 6. Create 1 Peg (Cylinder) in the center ---
        // Y scale is peg height / 2 (since primitive is 2 high)
        float pegScaleY = PEG_HEIGHT / 2.0f; // 0.2 / 2 = 0.1
        Vector3 pegScale = new Vector3(PEG_DIAMETER, pegScaleY, PEG_DIAMETER);

        // Position it: Base height + half of peg height
        float pegPosY = PLATE_HEIGHT + (PEG_HEIGHT / 2.0f); // 0.27 + 0.1 = 0.37

        CreatePeg(blockRoot, mat, pegScale, new Vector3(0, pegPosY, 0), "Peg_Center");

        // --- 7. Save the ROOT object as Prefab ---
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(blockRoot, prefabPath);
        Debug.Log("Successfully created 1x1 round plate prefab at: " + prefabPath);

        // --- 8. Clean up ---
        Object.DestroyImmediate(blockRoot);

        // --- 9. Highlight the new asset ---
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;
    }

    /// <summary>
    /// Helper function to create one peg (a cylinder)
    /// </summary>
    private static void CreatePeg(GameObject parent, Material mat, Vector3 scale, Vector3 position, string name)
    {
        GameObject peg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        peg.name = name;
        peg.transform.parent = parent.transform;

        // We KEEP the collider, just adjust it
        CapsuleCollider pegCollider = peg.GetComponent<CapsuleCollider>();
        pegCollider.center = Vector3.zero;
        pegCollider.radius = 0.5f;
        pegCollider.height = 2.0f;

        peg.transform.localScale = scale;
        peg.transform.localPosition = position;
        peg.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    /// <summary>
    /// Finds or creates the standard RED material
    /// </summary>
    private static Material GetOrCreateMaterial(string matPath)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = FindCoreShader();
            mat = new Material(shader);

            Color red = new Color(0.8f, 0.1f, 0.1f);

            if (GraphicsSettings.currentRenderPipeline != null &&
                GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("UniversalRenderPipeline"))
            {
                mat.SetColor("_BaseColor", red);
            }
            else
            {
                mat.color = red; // Built-in
            }
            AssetDatabase.CreateAsset(mat, matPath);
        }
        return mat;
    }

    /// <summary>
    /// Finds the correct shader for the current Render Pipeline (Built-in, URP, or HDRP)
    /// </summary>
    private static Shader FindCoreShader()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) return shader;
            shader = Shader.Find("HDRP/Lit");
            if (shader != null) return shader;
            shader = Shader.Find("Simple Lit");
            if (shader != null) return shader;
        }
        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null) return standardShader;

        Debug.LogWarning("Could not find 'Lit' or 'Standard' shader. Falling back to 'Legacy Shaders/Diffuse'.");
        return Shader.Find("Legacy Shaders/Diffuse");
    }
}