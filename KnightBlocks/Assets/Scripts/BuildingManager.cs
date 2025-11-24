using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Header("Configuration")]
    public LayerMask gridLayerMask;
    public Camera mainCamera;
    public UIManager uiManager;
    public Material ghostMaterial;

    [Header("AI Control")]
    [Tooltip("You MUST assign the GazeCursor GameObject here!")]
    public GazeCursor gazeCursor;

    // Internal state
    private GameObject currentPlacingBlock;
    private Material currentFinalMaterial;
    private Vector3 currentBlockSize;
    private GameObject tempBlockPrefab;
    private Material tempBlockMaterial;
    private float oldX, oldZ;
    private bool rotated;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Debug check on start
        if (gazeCursor == null)
        {
            Debug.LogError("⛔ CRITICAL ERROR: GazeCursor is NULL in BuildingManager Inspector!");
        }
        else
        {
            Debug.Log("✅ BuildingManager found GazeCursor: " + gazeCursor.gameObject.name);
        }
    }

    // ... (SetBlockToPlace and RotateCurrentBlock remain the same) ...
    public void SetBlockToPlace(GameObject blockPrefab, Material blockMaterial)
    {
        tempBlockPrefab = blockPrefab;
        tempBlockMaterial = blockMaterial;
        if (currentPlacingBlock != null) Destroy(currentPlacingBlock);

        Collider prefabCollider = blockPrefab.GetComponent<Collider>();
        if (prefabCollider is BoxCollider) currentBlockSize = (prefabCollider as BoxCollider).size;
        else if (prefabCollider is CapsuleCollider)
        {
            CapsuleCollider c = prefabCollider as CapsuleCollider;
            currentBlockSize = new Vector3(c.radius * 2, c.height, c.radius * 2);
        }
        else currentBlockSize = Vector3.one;

        oldX = currentBlockSize.x;
        oldZ = currentBlockSize.z;
        CreateGhostBlock();
    }

    public void RotateCurrentBlock()
    {
        if (currentPlacingBlock == null) return;
        if (rotated)
        {
            rotated = false;
            currentPlacingBlock.transform.Rotate(0, -90f, 0);
            currentBlockSize.x = oldX; currentBlockSize.z = oldZ;
        }
        else
        {
            rotated = true;
            currentPlacingBlock.transform.Rotate(0, 90f, 0);
            currentBlockSize.x = oldZ; currentBlockSize.z = oldX;
        }
    }

    void Update()
    {
        if (currentPlacingBlock == null) return;
        if (!uiManager.rotateButton.activeSelf) uiManager.ShowRotateButton(true);

        // --- FORCED GAZE TRACKING (Mouse Disabled) ---
        Vector3 targetScreenPosition = Vector3.zero;

        if (gazeCursor != null && gazeCursor.gameObject.activeInHierarchy)
        {
            // Get the screen position from the sprite
            targetScreenPosition = gazeCursor.ScreenPosition;

            // Visual Debug: Draw a line in Scene view to see where it thinks the cursor is
            Ray debugRay = mainCamera.ScreenPointToRay(targetScreenPosition);
            Debug.DrawRay(debugRay.origin, debugRay.direction * 100, Color.red);
        }
        else
        {
            // If connection is broken, log it and return (Block will freeze)
            Debug.LogWarning($"⛔ GazeCursor Issue: Null? {gazeCursor == null}, Active? {(gazeCursor != null ? gazeCursor.gameObject.activeInHierarchy : false)}");
            return;
        }

        // Raycast using the Gaze position
        Ray ray = mainCamera.ScreenPointToRay(targetScreenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, gridLayerMask))
        {
            currentPlacingBlock.transform.position = GetSnappedPosition(hit);
        }
    }

    // ... (GetSnappedPosition, SetBlockMaterial, SetLayerRecursively, CreateGhostBlock remain the same) ...
    private Vector3 GetSnappedPosition(RaycastHit hit)
    {
        float groundY = hit.point.y;
        float gridX = Mathf.Floor(hit.point.x);
        float gridZ = Mathf.Floor(hit.point.z);
        float snappedX = gridX + (currentBlockSize.x / 2.0f);
        float snappedZ = gridZ + (currentBlockSize.z / 2.0f);
        float plateHeight = 0.27f;
        float snappedY = Mathf.Round(groundY / plateHeight) * plateHeight;
        return new Vector3(snappedX, snappedY, snappedZ);
    }
    private void SetBlockMaterial(Material mat)
    {
        if (currentPlacingBlock == null || mat == null) return;
        foreach (Renderer r in currentPlacingBlock.GetComponentsInChildren<Renderer>()) r.material = mat;
    }
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }
    private void CreateGhostBlock()
    {
        if (tempBlockPrefab == null || tempBlockMaterial == null) return;
        currentPlacingBlock = Instantiate(tempBlockPrefab, Vector3.zero, Quaternion.identity);
        SetBlockMaterial(ghostMaterial);
        currentFinalMaterial = tempBlockMaterial;
        foreach (Collider c in currentPlacingBlock.GetComponentsInChildren<Collider>()) c.enabled = false;
    }

    // --- PUBLIC METHODS FOR AI ---
    public void PlaceBlock()
    {
        if (currentPlacingBlock == null) return;
        SetBlockMaterial(currentFinalMaterial);
        foreach (Collider c in currentPlacingBlock.GetComponentsInChildren<Collider>()) c.enabled = true;
        SetLayerRecursively(currentPlacingBlock, LayerMask.NameToLayer("Blocks"));
        currentBlockSize.x = oldX; currentBlockSize.z = oldZ;
        currentPlacingBlock = null;
        CreateGhostBlock();
        if (rotated) { rotated = false; RotateCurrentBlock(); }
    }

    public void CancelPlacement()
    {
        if (currentPlacingBlock != null)
        {
            Destroy(currentPlacingBlock);
            currentPlacingBlock = null;
            uiManager.ShowRotateButton(false);
        }
    }
}