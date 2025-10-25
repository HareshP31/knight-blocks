using System.Collections.Generic; // For List
using UnityEngine; // Still useful for Vector3/Quaternion conversion

// --- NEW Serializable Structs ---
[System.Serializable]
public struct Vec3
{
    public float x;
    public float y;
    public float z;

    // Helper to convert from Unity's Vector3
    public Vec3(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    // Helper to convert back to Unity's Vector3
    public Vector3 ToVector3() { return new Vector3(x, y, z); }
}

[System.Serializable]
public struct Quat
{
    public float x;
    public float y;
    public float z;
    public float w;

    // Helper to convert from Unity's Quaternion
    public Quat(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
    // Helper to convert back to Unity's Quaternion
    public Quaternion ToQuaternion() { return new Quaternion(x, y, z, w); }
}
// ------------------------------

[System.Serializable] // REQUIRED
public class BlockData
{
    public string prefabName;
    public string materialName;
    public Vec3 position; // Use our custom struct
    public Quat rotation; // Use our custom struct
}

[System.Serializable] // REQUIRED
public class Creation
{
    public string creationName;
    public string authorName;
    public List<BlockData> blocks = new List<BlockData>();
}

// --- NEW Wrapper Class for Loading ---
// JsonUtility can't parse a top-level array, so JS will send back {"items": [...]}
[System.Serializable]
public class CreationsList
{
    public List<Creation> items;
}
// ------------------------------------