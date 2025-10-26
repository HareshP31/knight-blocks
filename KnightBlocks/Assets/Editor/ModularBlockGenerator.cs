using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

/*
* This is a MODULAR block generator.
*
* It adds a new menu item: "Tools > Create Block > Modular Generator"
* This opens a window that lets you define the block's properties.
*
* It contains TWO classes:
* 1. ModularBlockGeneratorWindow: The pop-up window (GUI).
* 2. BlockGenerationLogic: The refactored logic from your script.
*/

// --- CLASS 1: The Pop-up Window (GUI) ---

public class ModularBlockGeneratorWindow : EditorWindow
{
    // These are the variables for our input fields
    private int pegsX = 2;
    private int pegsZ = 2;
    private float blockHeight = 0.8f;
    private string blockName = "Block_2x2_Brick";
    private Material blockMaterial;

    // Add the new menu item that opens this window
    [MenuItem("Tools/Create Block/Modular Generator")]
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one
        GetWindow<ModularBlockGeneratorWindow>("Modular Block Generator");
    }

    // This runs to draw the UI in the window
    void OnGUI()
    {
        GUILayout.Label("Block Properties", EditorStyles.boldLabel);

        // Input fields for the user
        pegsX = EditorGUILayout.IntField("Pegs (Width)", pegsX);
        pegsZ = EditorGUILayout.IntField("Pegs (Depth)", pegsZ);
        blockHeight = EditorGUILayout.FloatField("Block Height", blockHeight);
        blockName = EditorGUILayout.TextField("Prefab Name", blockName);

        // Material slot
        blockMaterial = (Material)EditorGUILayout.ObjectField("Block Material", blockMaterial, typeof(Material), false);

        // --- Preset Buttons ---
        GUILayout.Space(10);
        GUILayout.Label("Presets", EditorStyles.centeredGreyMiniLabel);
        if (GUILayout.Button("2x4 Brick"))
        {
            pegsX = 2;
            pegsZ = 4;
            blockHeight = 0.8f;
            blockName = "Block_2x4_Brick";
        }
        if (GUILayout.Button("2x2 Plate (Flatter)"))
        {
            pegsX = 2;
            pegsZ = 2;
            blockHeight = 0.27f; // Approx 1/3 of a 0.8 brick
            blockName = "Block_2x2_Plate";
        }
        
        // --- The "Generate" Button ---
        GUILayout.Space(20);
        if (GUILayout.Button("Generate Block", GUILayout.Height(40)))
        {
            // --- Error checking ---
            if (pegsX < 1 || pegsZ < 1)
            {
                Debug.LogError("Block must be at least 1x1.");
                return;
            }
            if (string.IsNullOrEmpty(blockName))
            {
                Debug.LogError("Block Name cannot be empty.");
                return;
            }
            if (blockMaterial == null)
            {
                Debug.LogWarning("No material selected. A default material will be created.");
                // We pass null, and the logic will create one
            }
            
            // --- Call the logic ---
            BlockGenerationLogic.GenerateBlock(pegsX, pegsZ, blockHeight, blockName, blockMaterial);
        }
    }
}


// --- CLASS 2: The Block Generation Logic (Your script, but modular) ---

public static class BlockGenerationLogic
{
    // Define standard sizes
    private const float PEG_HEIGHT = 0.2f;
    private const float PEG_DIAMETER = 0.6f;
    private const float TOP_THICKNESS = 0.2f;
    private const float WALL_THICKNESS = 0.2f;

    /// <summary>
    /// This is the main function that builds the block
    /// </summary>
    public static void GenerateBlock(int pegsX, int pegsZ, float totalHeight, string blockName, Material mat)
    {
        // --- 1. Calculate main dimensions ---
        float totalWidth = (float)pegsX;
        float totalDepth = (float)pegsZ;

        // --- 2. Define Paths ---
        string prefabDir = "Assets/Prefabs";
        string prefabPath = prefabDir + "/" + blockName + ".prefab";
        string matPath = "Assets/Materials/" + blockName + "_Mat.mat";

        // --- 3. Create Directories ---
        if (!Directory.Exists(Application.dataPath + "/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (mat == null && !Directory.Exists(Application.dataPath + "/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        // --- 4. Get or Create Material ---
        if (mat == null)
        {
            mat = GetOrCreateMaterial(matPath);
        }

        // --- 5. Create the ROOT object ---
        GameObject blockRoot = new GameObject(blockName);
        blockRoot.transform.position = Vector3.zero;

        // --- 6. Create the HOLLOW Base ---
        CreateHollowBase(blockRoot, mat, totalWidth, totalDepth, totalHeight);

        // --- 7. Create Pegs ---
        CreatePegs(blockRoot, mat, pegsX, pegsZ, totalHeight);
        
        // --- 8. Add a main collider to the ROOT ---
        BoxCollider bc = blockRoot.AddComponent<BoxCollider>();
        bc.size = new Vector3(totalWidth, totalHeight, totalDepth);
        // Center the collider. Base is centered at 0,0,0, but height is calculated from bottom
        bc.center = new Vector3(0, totalHeight / 2f, 0); 

        // --- 9. Save as Prefab ---
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(blockRoot, prefabPath);
        Debug.Log("Successfully created block prefab at: " + prefabPath);

        // --- 10. Clean up ---
        Object.DestroyImmediate(blockRoot);

        // --- 11. Highlight ---
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;
    }

    /// <summary>
    /// Creates the 5-part hollow base (1 top, 4 walls)
    /// </summary>
    private static void CreateHollowBase(GameObject parent, Material mat, float totalWidth, float totalDepth, float totalHeight)
    {
        float wallHeight = totalHeight - TOP_THICKNESS;
        
        // Top plate center Y
        float topPlateY = totalHeight - (TOP_THICKNESS / 2f);
        // Wall center Y
        float wallY = wallHeight / 2f; 

        GameObject baseRoot = new GameObject("Base");
        baseRoot.transform.parent = parent.transform;
        baseRoot.transform.localPosition = Vector3.zero;

        // 1. Top Plate
        CreateBasePart(baseRoot, mat,
            new Vector3(totalWidth, TOP_THICKNESS, totalDepth),
            new Vector3(0, topPlateY, 0), "Top_Plate");

        // 2. Back Wall ("North")
        CreateBasePart(baseRoot, mat,
            new Vector3(totalWidth, wallHeight, WALL_THICKNESS),
            new Vector3(0, wallY, (totalDepth / 2f) - (WALL_THICKNESS / 2f)), "Wall_North");

        // 3. Front Wall ("South")
        CreateBasePart(baseRoot, mat,
            new Vector3(totalWidth, wallHeight, WALL_THICKNESS),
            new Vector3(0, wallY, -(totalDepth / 2f) + (WALL_THICKNESS / 2f)), "Wall_South");

        // 4. Right Wall ("East")
        CreateBasePart(baseRoot, mat,
            new Vector3(WALL_THICKNESS, wallHeight, totalDepth - (WALL_THICKNESS * 2f)),
            new Vector3((totalWidth / 2f) - (WALL_THICKNESS / 2f), wallY, 0), "Wall_East");

        // 5. Left Wall ("West")
        CreateBasePart(baseRoot, mat,
            new Vector3(WALL_THICKNESS, wallHeight, totalDepth - (WALL_THICKNESS * 2f)),
            new Vector3(-(totalWidth / 2f) + (WALL_THICKNESS / 2f), wallY, 0), "Wall_West");
    }

    /// <summary>
    /// Creates a grid of pegs based on X and Z dimensions
    /// </summary>
    private static void CreatePegs(GameObject parent, Material mat, int pegsX, int pegsZ, float totalHeight)
    {
        Vector3 pegScale = new Vector3(PEG_DIAMETER, PEG_HEIGHT, PEG_DIAMETER);
        
        // Pegs sit on top of the base
        float pegPosY = totalHeight + (PEG_HEIGHT / 2f); 

        for (int x = 0; x < pegsX; x++)
        {
            for (int z = 0; z < pegsZ; z++)
            {
                // Calculate position for this peg
                // We offset by half the total size, then add 0.5 for the peg's center
                float pegPosX = (float)x - (pegsX / 2.0f) + 0.5f;
                float pegPosZ = (float)z - (pegsZ / 2.0f) + 0.5f;
                
                string pegName = $"Peg_{x}_{z}";
                CreatePeg(parent, mat, pegScale, new Vector3(pegPosX, pegPosY, pegPosZ), pegName);
            }
        }
    }

    /// <summary>
    /// Helper to create one part of the base (a cube)
    /// </summary>
    private static void CreateBasePart(GameObject parent, Material mat, Vector3 scale, Vector3 position, string name)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.parent = parent.transform;
        Object.DestroyImmediate(part.GetComponent<BoxCollider>());
        part.transform.localScale = scale;
        part.transform.localPosition = position;
        part.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    /// <summary>
    /// Helper to create one peg (a cylinder)
    /// </summary>
    private static void CreatePeg(GameObject parent, Material mat, Vector3 scale, Vector3 position, string name)
    {
        GameObject peg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        peg.name = name;
        peg.transform.parent = parent.transform;
        Object.DestroyImmediate(peg.GetComponent<CapsuleCollider>());
        peg.transform.localScale = scale;
        peg.transform.localPosition = position;
        peg.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    /// <summary>
    /// Creates a new material or logs a warning
    /// </summary>
    private static Material GetOrCreateMaterial(string matPath)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = FindCoreShader();
            mat = new Material(shader);
            
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
        return mat;
    }

    /// <summary>
    /// Finds the correct shader for the current Render Pipeline
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
