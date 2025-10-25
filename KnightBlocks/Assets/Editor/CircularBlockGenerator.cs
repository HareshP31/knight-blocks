using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

/*
* This is an EDITOR script.
* It adds a new menu item: "Tools > Create Block > Create 2x2 Round Block (4-Peg)".
*
* This creates a composite prefab with:
* 1. A SOLID Cylinder base (2-unit diameter, 0.8-unit height)
* 2. Four standard Cylinder pegs.
* 3. A single, clean CapsuleCollider on the root for stacking.
* 4. Uses the standard RED material.
*/

public class CircularBlockGenerator
{
    // Define standard sizes
    private const float BASE_HEIGHT = 0.8f;
    private const float BASE_DIAMETER = 2.0f;
    private const float PEG_HEIGHT = 0.2f;
    private const float PEG_DIAMETER = 0.6f;
    private const float PEG_OFFSET = 0.5f; // Distance of pegs from center


    [MenuItem("Tools/Create Block/Create 2x2 Round Block (4-Peg)")]
    public static void CreateComposite2x2Round()
    {
        // --- 1. Define Paths ---
        string blockName = "Block_2x2_Round_4Peg";
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

        // Remove its collider, we'll add one to the root
        Object.DestroyImmediate(baseObj.GetComponent<CapsuleCollider>());

        // Scale it: Y scale is height / 2 (since primitive is 2 high)
        float baseScaleY = BASE_HEIGHT / 2.0f; // 0.8 / 2 = 0.4
        // X,Z scale is just the diameter
        baseObj.transform.localScale = new Vector3(BASE_DIAMETER, baseScaleY, BASE_DIAMETER);

        // Position it: Center is half the height
        baseObj.transform.localPosition = new Vector3(0, BASE_HEIGHT / 2.0f, 0);
        baseObj.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // --- 6. Create 4 Pegs (Cylinders) ---
        // Y scale is peg height / 2 (since primitive is 2 high)
        float pegScaleY = PEG_HEIGHT / 2.0f; // 0.2 / 2 = 0.1
        Vector3 pegScale = new Vector3(PEG_DIAMETER, pegScaleY, PEG_DIAMETER);

        // Position it: Base height + half of peg height
        float pegPosY = BASE_HEIGHT + (PEG_HEIGHT / 2.0f); // 0.8 + 0.1 = 0.9

        CreatePeg(blockRoot, mat, pegScale, new Vector3(-PEG_OFFSET, pegPosY, -PEG_OFFSET), "Peg_BL");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(PEG_OFFSET, pegPosY, -PEG_OFFSET), "Peg_BR");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(-PEG_OFFSET, pegPosY, PEG_OFFSET), "Peg_FL");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(PEG_OFFSET, pegPosY, PEG_OFFSET), "Peg_FR");

        // --- 7. Add a main collider to the ROOT ---
        // This makes the whole block solid for stacking.
        CapsuleCollider rootCollider = blockRoot.AddComponent<CapsuleCollider>();
        rootCollider.center = new Vector3(0, BASE_HEIGHT / 2.0f, 0); // Center of the base
        rootCollider.radius = BASE_DIAMETER / 2.0f; // 1.0
        rootCollider.height = BASE_HEIGHT;
        rootCollider.direction = 1; // Y-Axis

        // --- 8. Save the ROOT object as Prefab ---
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(blockRoot, prefabPath);
        Debug.Log("Successfully created round block prefab at: " + prefabPath);

        // --- 9. Clean up ---
        Object.DestroyImmediate(blockRoot);

        // --- 10. Highlight the new asset ---
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

        // Remove individual collider
        Object.DestroyImmediate(peg.GetComponent<CapsuleCollider>());

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
