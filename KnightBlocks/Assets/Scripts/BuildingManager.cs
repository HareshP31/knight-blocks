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

    private GameObject currentPlacingBlock;
    private Material currentFinalMaterial; // The solid material we'll apply on place
    private Vector3 currentBlockSize;


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

        // Create the new "ghost" block at the origin
        currentPlacingBlock = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);

        // --- UPDATED: Store the FINAL material, but apply the GHOST material ---
        currentFinalMaterial = blockMaterial;
        SetBlockMaterial(ghostMaterial); // Make it transparent immediately

        // Disable colliders so it doesn't block its own raycast
        Collider[] colliders = currentPlacingBlock.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }

    void Update()
    {
        // If we are not currently holding a block, do nothing
        if (currentPlacingBlock == null)
        {
            return;
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

        // We are no longer holding this block
        currentPlacingBlock = null;
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
}
