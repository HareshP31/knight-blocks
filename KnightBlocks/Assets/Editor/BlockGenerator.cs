using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

/*
* This is an EDITOR script.
* It adds a new menu item to Unity: "Tools > Create Simple Block > Create 2x2 Block (with Pegs)".
*
* UPDATE:
* - The Base is now a 5-part composite (1 top + 4 walls) to create a HOLLOW bottom.
* - Added a single BoxCollider to the root object so it can stack properly.
* - Fixed peg height/position to sit correctly on the top plate.
*/
public class SimpleBlockGenerator
{
    [MenuItem("Tools/Create Simple Block/Create 2x2 Block (with Pegs)")]
    public static void CreateComposite2x2()
    {
        // --- 1. Define Paths ---
        string prefabDir = "Assets/Prefabs";
        string prefabPath = prefabDir + "/Block_2x2_Composite.prefab";
        string matPath = "Assets/Materials/M_Block_Red_Simple.mat";

        // --- 2. Create Directories if they don't exist ---
        if (!Directory.Exists(Application.dataPath + "/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!Directory.Exists(Application.dataPath + "/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // --- 3. Create or Find the Material (URP-SAFE VERSION) ---
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = FindCoreShader();
            mat = new Material(shader);

            // Set color based on pipeline
            if (GraphicsSettings.currentRenderPipeline != null &&
                GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("UniversalRenderPipeline"))
            {
                mat.SetColor("_BaseColor", new Color(0.8f, 0.1f, 0.1f));
            }
            else
            {
                mat.color = new Color(0.8f, 0.1f, 0.1f); // Built-in
            }
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // --- 4. Create the ROOT object ---
        GameObject blockRoot = new GameObject("Block_2x2_Composite");
        blockRoot.transform.position = Vector3.zero;

        // --- 5. Create the HOLLOW Base (Replaces the single cube) ---
        CreateHollowBase(blockRoot, mat);

        // --- 6. Create 4 Pegs (Cylinders) ---
        // Pegs are 0.2f tall
        Vector3 pegScale = new Vector3(0.6f, 0.2f, 0.6f);
        // Base top is at Y=0.4f. Peg half-height is 0.1f. So peg center is Y=0.5f.
        float pegPosY = 0.5f;
        float pegOffset = 0.5f;

        CreatePeg(blockRoot, mat, pegScale, new Vector3(-pegOffset, pegPosY, -pegOffset), "Peg_BL");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(pegOffset, pegPosY, -pegOffset), "Peg_BR");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(-pegOffset, pegPosY, pegOffset), "Peg_FL");
        CreatePeg(blockRoot, mat, pegScale, new Vector3(pegOffset, pegPosY, pegOffset), "Peg_FR");

        // --- 7. Add a main collider to the ROOT ---
        // This makes the whole block solid for stacking.
        BoxCollider bc = blockRoot.AddComponent<BoxCollider>();
        bc.size = new Vector3(2f, 0.8f, 2f);

        // --- 8. Save the ROOT object as Prefab ---
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(blockRoot, prefabPath);
        Debug.Log("Successfully created composite block prefab at: " + prefabPath);

        // --- 9. Clean up ---
        Object.DestroyImmediate(blockRoot);

        // --- 10. Highlight the new asset ---
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;
    }

    /// <summary>
    /// Creates the 5-part hollow base (1 top, 4 walls)
    /// </summary>
    private static void CreateHollowBase(GameObject parent, Material mat)
    {
        float totalWidth = 2.0f;
        float totalHeight = 0.8f;
        float totalDepth = 2.0f;
        float topThickness = 0.2f;    // How thick the top plate is
        float wallThickness = 0.2f;   // How thick the side walls are

        float wallHeight = totalHeight - topThickness; // 0.6
        // Top plate center Y = (total height / 2) - (top thickness / 2) = 0.4 - 0.1 = 0.3
        float topPlateY = (totalHeight / 2f) - (topThickness / 2f);
        // Wall center Y = topPlateY - (top thickness / 2) - (wall height / 2) = 0.3 - 0.1 - 0.3 = -0.1
        float wallY = topPlateY - (topThickness / 2f) - (wallHeight / 2f);

        // Create an empty parent for the base parts
        GameObject baseRoot = new GameObject("Base");
        baseRoot.transform.parent = parent.transform;
        baseRoot.transform.localPosition = Vector3.zero;

        // 1. Top Plate
        CreateBasePart(baseRoot, mat,
            new Vector3(totalWidth, topThickness, totalDepth),
            new Vector3(0, topPlateY, 0), "Top_Plate");

        // 2. Back Wall ("North")
        CreateBasePart(baseRoot, mat,
            new Vector3(totalWidth, wallHeight, wallThickness),
            new Vector3(0, wallY, (totalDepth / 2f) - (wallThickness / 2f)), "Wall_North");

        // 3. Front Wall ("South")
        CreateBasePart(baseRoot, mat,
            new Vector3(totalWidth, wallHeight, wallThickness),
            new Vector3(0, wallY, -(totalDepth / 2f) + (wallThickness / 2f)), "Wall_South");

        // 4. Right Wall ("East")
        CreateBasePart(baseRoot, mat,
            new Vector3(wallThickness, wallHeight, totalDepth - (wallThickness * 2f)), // Shorter to fit inside
            new Vector3((totalWidth / 2f) - (wallThickness / 2f), wallY, 0), "Wall_East");

        // 5. Left Wall ("West")
        CreateBasePart(baseRoot, mat,
            new Vector3(wallThickness, wallHeight, totalDepth - (wallThickness * 2f)), // Shorter to fit inside
            new Vector3(-(totalWidth / 2f) + (wallThickness / 2f), wallY, 0), "Wall_West");
    }

    /// <summary>
    /// Helper function to create one part of the base (a cube)
    /// </summary>
    private static void CreateBasePart(GameObject parent, Material mat, Vector3 scale, Vector3 position, string name)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.parent = parent.transform;
        Object.DestroyImmediate(part.GetComponent<BoxCollider>()); // Remove individual colliders
        part.transform.localScale = scale;
        part.transform.localPosition = position;
        part.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    /// <summary>
    /// Helper function to create one peg (a cylinder)
    /// </summary>
    private static void CreatePeg(GameObject parent, Material mat, Vector3 scale, Vector3 position, string name)
    {
        GameObject peg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        peg.name = name;
        peg.transform.parent = parent.transform;
        Object.DestroyImmediate(peg.GetComponent<CapsuleCollider>()); // Remove individual colliders
        peg.transform.localScale = scale;
        peg.transform.localPosition = position;
        peg.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    /// <summary>
    /// Finds the correct shader for the current Render Pipeline (Built-in, URP, or HDRP)
    /// </summary>
    private static Shader FindCoreShader()
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
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
            shader = Shader.Find("Simple Lit");
            if (shader != null)
            {
                Debug.Log("URP 'Simple Lit' shader found.");
                return shader;
            }
        }
        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            Debug.Log("Built-in 'Standard' shader found.");
            return standardShader;
        }
        Debug.LogWarning("Could not find 'Lit' or 'Standard' shader. Falling back to 'Legacy Shaders/Diffuse'. Your material will not be pink.");
        return Shader.Find("Legacy Shaders/Diffuse");
    }
}