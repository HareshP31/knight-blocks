using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class FirebaseManager : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void InitFirebase(string configJson, string unityObjectName);
    [DllImport("__Internal")]
    private static extern void SaveCreation(string creationJson);
    [DllImport("__Internal")]
    private static extern void LoadCreations(string unityObjectName);
    // --------------------------------------------------------

    [TextArea(10, 15)]
    public string firebaseConfigJson = @"...";

    void Start()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            InitFirebase(firebaseConfigJson, gameObject.name);
        #else
            Debug.Log("Not in a WebGL build. Skipping Firebase init.");
        #endif
    }

    public void OnFirebaseInitialized()
    {
        Debug.Log("C# side: Firebase is initialized! Ready to use.");
        TestSave();
    }

    public void OnCreationsLoaded(string jsonResult)
    {
        Debug.Log("C# side: Received creations JSON: " + jsonResult);
        if (string.IsNullOrEmpty(jsonResult) || jsonResult == "[]")
        {
           Debug.Log("No creations loaded or list is empty.");
           return;
        }

        CreationsList loadedData = JsonUtility.FromJson<CreationsList>(jsonResult);

        if (loadedData != null && loadedData.items != null)
        {
            Debug.Log($"Successfully parsed {loadedData.items.Count} creations.");
            foreach (Creation creation in loadedData.items)
            {
                Debug.Log("Loaded creation name: " + creation.creationName);
            }
        }
        else
        {
            Debug.LogError("Failed to parse creations list from JSON.");
        }
    }

    public void OnFirebaseError(string error)
    {
        Debug.LogError("C# side: Firebase Error: " + error);
    }

    public void TestSave()
    {
        Creation testCreation = new Creation
        {
            creationName = "My C# Creation (JsonUtility)",
            authorName = "UnityUser",
            blocks = new List<BlockData>
            {
                new BlockData
                {
                    prefabName = "Cube",
                    materialName = "Blue",
                    position = new Vec3(Vector3.zero),
                    rotation = new Quat(Quaternion.identity)
                }
            }
        };

        string json = JsonUtility.ToJson(testCreation);
        Debug.Log("Sending JSON: " + json);

        #if UNITY_WEBGL && !UNITY_EDITOR
            SaveCreation(json);
        #endif
    }

    public void TestLoad()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
            LoadCreations(gameObject.name);
        #endif
    }
}