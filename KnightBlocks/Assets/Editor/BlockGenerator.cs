using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

/*
* This is an EDITOR script.
* It adds a new menu item to Unity: "Tools > Create Simple Block > Create 2x2 Block (with Pegs)".
*/
public class SimpleBlockGenerator
{
    [MenuItem("Tools/Create Simple Block/Create 2x2 Block (with Pegs)")]
    public static void CreateComposite2x2()
    {
        // --- Define Paths ---
        string prefabDir = "Assets/Prefabs";
        string prefabPath = prefabDir + "/Block_2x2_Composite.prefab";
        string matPath = "Assets/Materials/M_Block_Red_Simple.mat";

        // --- Create Directories if they don't exist ---
        if (!Directory.Exists(Application.dataPath + "/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!Directory.Exists(Application.dataPath + "/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // --- Create or Find the Material (URP-SAFE VERSION) ---
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            // Find the correct shader
            Shader shader = FindCoreShader();

            mat = new Material(shader);
            mat.color = new Color(0.8f, 0.1f, 0.1f); // A nice, deep red

            // If using URP, the color is set differently
            if (GraphicsSettings.currentRenderPipeline != null &&
                GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("UniversalRenderPipeline"))
            {
                mat.SetColor("_BaseColor", new Color(0.8f, 0.1f, 0.1f));
            }

            AssetDatabase.CreateAsset(mat, matPath);
        }

        // --- 4. Create the ROOT object ---
        GameObject blockRoot = new GameObject("Block_2x2_Composite");
        blockRoot.transform.position = Vector3.zero;

        // --- 5. Create the Base (Cube) ---
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Base";
        baseObj.transform.parent = blockRoot.transform;
        baseObj.transform.localScale = new Vector3(2f, 0.8f, 2f);
        baseObj.transform.localPosition = Vector3.zero;
        baseObj.GetComponent<MeshRenderer>().sharedMaterial = mat; // Apply the safe material

        // --- 6. Create 4 Pegs (Cylinders) ---
        Vector3 pegScale = new Vector3(0.6f, 0.1f, 0.6f); // 0.6 diameter, 0.2 height
        float pegPosY = 0.5f;
        float pegOffset = 0.5f;

        CreatePeg(blockRoot, mat, pegScale, new Vector3(-pegOffset, pegPosY, -pegOffset), "Peg_BL");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(pegOffset, pegPosY, -pegOffset), "Peg_BR");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(-pegOffset, pegPosY, pegOffset), "Peg_FL");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(pegOffset, pegPosY, pegOffset), "Peg_FR");

        // --- 7. Save the ROOT object as Prefab ---
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(blockRoot, prefabPath);
        Debug.Log("Successfully created composite block prefab at: " + prefabPath);

        // --- 8. Clean up ---
        Object.DestroyImmediate(blockRoot);

        // --- 9. Highlight the new asset ---
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;
    }

    private static void CreatePeg(GameObject parent, Material mat, Vector3 scale, Vector3 position, string name)
    {
        GameObject peg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        peg.name = name;
        peg.transform.parent = parent.transform;
        Object.DestroyImmediate(peg.GetComponent<CapsuleCollider>());
        peg.transform.localScale = scale;
        peg.transform.localPosition = position;
        peg.GetComponent<MeshRenderer>().sharedMaterial = mat; // Apply the safe material
    }

    private static Shader FindCoreShader()
    {
        // Check if we are in a Scriptable Render Pipeline (URP or HDRP)
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            // We are in URP or HDRP. Let's try to find the "Lit" shader.
            // For URP it's "Universal Render Pipeline/Lit"
            // For HDRP it's "HDRP/Lit"
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                Debug.Log("URP 'Lit' shader found.");
                return shader;
            }

            shader = Shader.Find("HDRP/Lit");
            if (shader != null)
            {
                Debug.Log("HDRP 'Lit' shader found.");
                return shader;
            }

            // Fallback for some URP versions
            shader = Shader.Find("Simple Lit");
            if (shader != null)
            {
                Debug.Log("URP 'Simple Lit' shader found.");
                return shader;
            }
        }

        // If we're here, we are in the Built-in Pipeline OR a pipeline we don't recognize.
        // Try to find the "Standard" shader.
        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            Debug.Log("Built-in 'Standard' shader found.");
            return standardShader;
        }

        Debug.LogWarning("Could not find any 'Lit' or 'Standard' shader. Falling back to 'Legacy Shaders/Diffuse'. Your material will look simple and won't be pink.");
        return Shader.Find("Legacy Shaders/Diffuse");
    }
}

