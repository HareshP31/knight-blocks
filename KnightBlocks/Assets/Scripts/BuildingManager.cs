using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    [Tooltip("The layer(s) the mouse raycast will hit (e.g., 'Ground' and 'Blocks').")]
    public LayerMask gridLayerMask;

    [Tooltip("The main camera (or camera used for raycasting).")]
    public Camera mainCamera;

    // --- RENAMED: This is now the 'ghost' material ---
    [Tooltip("The transparent material to use while placing.")]
    public Material ghostMaterial;
    public UIManager uiManager;

    private GameObject currentPlacingBlock;
    private Material currentFinalMaterial; // The solid material we'll apply on place
    private Vector3 currentBlockSize;
    private GameObject tempBlockPrefab;
    private Material tempBlockMaterial;
    private float oldX;
    private float oldZ;
    private bool rotated;

    void Start()
    {
        // Auto-find the camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // --- UPDATED: Check for the 'ghostMaterial' ---
        if (ghostMaterial == null)
        {
            Debug.LogError("BuildingManager: 'Ghost Material' is not assigned in the Inspector!");
        }
    }

    /// <summary>
    /// This is called by the UIManager when a block and color are chosen.
    /// </summary>
    public void SetBlockToPlace(GameObject blockPrefab, Material blockMaterial)
    {
        tempBlockPrefab = blockPrefab;
        tempBlockMaterial = blockMaterial;
        // If we're already holding a block, destroy it
        if (currentPlacingBlock != null)
        {
            Destroy(currentPlacingBlock);
        }

        // --- UPDATED: This is the robust collider-finding logic ---
        // It works for BoxColliders and CapsuleColliders
        Collider prefabCollider = blockPrefab.GetComponent<Collider>();
        if (prefabCollider is BoxCollider)
        {
            currentBlockSize = (prefabCollider as BoxCollider).size;
        }
        else if (prefabCollider is CapsuleCollider)
        {
            CapsuleCollider capsule = prefabCollider as CapsuleCollider;
            // Treat the capsule's size as a box for grid purposes
            currentBlockSize = new Vector3(capsule.radius * 2, capsule.height, capsule.radius * 2);
        }
        else
        {
            Debug.LogError("Prefab " + blockPrefab.name + " is missing a root Box or Capsule Collider! Snapping will be incorrect.");
            currentBlockSize = Vector3.one; // Fallback
        }
        oldX = currentBlockSize.x;
        oldZ = currentBlockSize.z;
        // Create the new "ghost" block at the origin
        CreateGhostBlock();
    }

    public void RotateCurrentBlock()
    {
        if (currentPlacingBlock == null)
        {
            Debug.Log("RotateCurrentBlock: No block is being placed.");
            return; // Not holding a block, do nothing
        }

        Debug.Log("Rotating block!");

        // 1. Rotate the block 90 degrees on the Y axis (around its center)
        if (rotated)
        {
            rotated = false;
            currentPlacingBlock.transform.Rotate(0, -90f, 0);
            currentBlockSize.x = oldX;
            currentBlockSize.z = oldZ;
        }
        else
        {
            rotated = true;
            currentPlacingBlock.transform.Rotate(0, 90f, 0);
            currentBlockSize.x = oldZ;
            currentBlockSize.z = oldX;
        }
        // 2. Swap the X and Z size for the grid logic
        // This is CRITICAL for the snapping logic to work correctly after rotation

        

        // Note: You might need to re-run the snapping logic here if the rotation
        // causes the current snapped position to be invalid (e.g., if you
        // bring back the IsPositionOccupied check). For now, we just rotate.
    }

    void Update()
    {
        // If we are not currently holding a block, do nothing
        if (currentPlacingBlock == null)
        {
            return;
        }
        if (!uiManager.rotateButton.activeSelf)
        {
            uiManager.ShowRotateButton(true);
        }
        // We are holding a block, so make it follow the cursor on the grid
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Raycast against the grid layer
        if (Physics.Raycast(ray, out hit, 100f, gridLayerMask))
        {
            // We hit a valid spot. Calculate the snapped position.
            Vector3 snappedPosition = GetSnappedPosition(hit);
            currentPlacingBlock.transform.position = snappedPosition;

            // --- REMOVED: All 'IsPositionOccupied' and 'isPlacementValid' logic ---

            // Check for mouse click to place the block
            if (Input.GetMouseButtonDown(0))
            {
                PlaceBlock();
            }

            if (Input.GetMouseButtonDown(1))
            {
                // Right-click to cancel placement
                Destroy(currentPlacingBlock);
                currentPlacingBlock = null;
                uiManager.ShowRotateButton(false);
            }
        }
    }

    /// <summary>
    /// Calculates the final "Lego-like" grid-snapped position.
    /// </summary>
    private Vector3 GetSnappedPosition(RaycastHit hit)
    {
        // Get the Y of the surface we hit. This is our ground.
        float groundY = hit.point.y;

        // --- THIS IS THE ROBUST SNAPPING LOGIC ---
        // 1. Find the "corner" of the 1x1 grid cell we hit
        float gridX = Mathf.Floor(hit.point.x);
        float gridZ = Mathf.Floor(hit.point.z);

        // 2. Add half the block's size to the corner.
        // This places the (centered) pivot correctly.
        float snappedX = gridX + (currentBlockSize.x / 2.0f);
        float snappedZ = gridZ + (currentBlockSize.z / 2.0f);

        // 3. Snap the Y position.
        // Let's assume a "plate" height grid of 0.27
        float plateHeight = 0.27f; // This should be your thinnest block
        float snappedY = Mathf.Round(groundY / plateHeight) * plateHeight;

        return new Vector3(snappedX, snappedY, snappedZ);
    }

    // --- REMOVED: IsPositionOccupied() function is gone ---

    // --- RENAMED: This is now a simple material-setter ---
    private void SetBlockMaterial(Material mat)
    {
        if (currentPlacingBlock == null) return;
        if (mat == null)
        {
            Debug.LogError("SetBlockMaterial: Material is null!");
            return;
        }

        Renderer[] renderers = currentPlacingBlock.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material = mat;
        }
    }

    /// <summary>
    // Finalizes the block placement.
    /// </summary>
    private void PlaceBlock()
    {
        // --- UPDATED: Set the block to its FINAL, SOLID material ---
        SetBlockMaterial(currentFinalMaterial);

        // Re-enable colliders
        Collider[] colliders = currentPlacingBlock.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = true;
        }

        // Set its layer to "Blocks" so it can be built upon
        SetLayerRecursively(currentPlacingBlock, LayerMask.NameToLayer("Blocks"));
        currentBlockSize.x = oldX;
        currentBlockSize.z = oldZ;
        currentPlacingBlock = null;
        CreateGhostBlock();
        if (rotated)
        {
            rotated = false;
            RotateCurrentBlock();
        }
    }

    /// <summary>
    /// Helper function to set the layer for a GameObject and all its children.
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void CreateGhostBlock()
    {
        if (tempBlockPrefab == null || tempBlockMaterial == null)
        {
            Debug.LogError("CreateGhostBlock: Temp block prefab or material is null!");
            return;
        }

        currentPlacingBlock = Instantiate(tempBlockPrefab, Vector3.zero, Quaternion.identity);

        // Set the ghost material
        SetBlockMaterial(ghostMaterial);

        // Store the final material for later
        currentFinalMaterial = tempBlockMaterial;

        // Disable colliders while placing
        Collider[] colliders = currentPlacingBlock.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }
}
