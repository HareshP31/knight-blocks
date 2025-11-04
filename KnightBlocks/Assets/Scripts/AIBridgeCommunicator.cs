using System.Runtime.InteropServices;
using UnityEngine;
using System.Globalization; // Required for parsing floats correctly

public class AIBridgeCommunicator : MonoBehaviour
{
    [Header("Cursor Control")]
    [Tooltip("Assign your UI Image GameObject that has the GazeCursor.cs script")]
    public GazeCursor gazeCursor;

    [DllImport("__Internal")]
    private static extern void StartAIBridge();

    public void InitiateAISystem()
    {
        Debug.Log("InitiateAISystem called in C#!");
#if !UNITY_EDITOR && UNITY_WEBGL
        Debug.Log("C# initiating AI system via StartAIBridge...");
        StartAIBridge();
#else
        Debug.LogWarning("AI System initiated, but DllImport calls are skipped in the Unity Editor.");
#endif
    }

    // --------------------------------------------------------------------------
    // 3. JavaScript -> C# Receiver Methods (FIXED)
    //    These names and parameters now match the "SendMessage" calls
    //    from the corrected main-controller.js
    // --------------------------------------------------------------------------

    /// <summary>
    /// Receives gaze data as a string "x,y" from JavaScript.
    /// Matches: SendMessage("AIBridgeReceiver", "OnGazeUpdate", "0.5,0.6");
    /// </summary>
    public void OnGazeUpdate(string data)
    {
        // data will be a string like "0.75,0.32"
        string[] parts = data.Split(',');

        if (parts.Length == 2)
        {
            if (float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float y))
            {
                // Now you have the x and y floats
                // Debug.Log($"[JS -> C#] Gaze Position: X={x:F4}, Y={y:F4}");
                if (gazeCursor != null)
                {
                    gazeCursor.UpdatePosition(x, y);
                }
            }
        }
    }

    /// <summary>
    /// Receives action commands ('select' or 'deselect') from JavaScript.
    /// Matches: SendMessage("AIBridgeReceiver", "OnAction", "select");
    /// </summary>
    public void OnAction(string actionType)
    {
        // actionType will be "select" or "deselect"
        Debug.Log($"[JS -> C#] Action Received: {actionType}");

        if (gazeCursor != null)
        {
            gazeCursor.PerformAction(actionType);
        }
    }

    /// <summary>
    /// Receives head rotation commands ('left' or 'right') from JavaScript.
    /// Matches: SendMessage("AIBridgeReceiver", "OnRotate", "left");
    /// </summary>
    public void OnRotate(string direction)
    {
        // direction will be "left" or "right"
        Debug.Log($"[JS -> C#] Rotation Received: {direction}");

        // --- TODO: Add your rotation logic here ---
    }

    /// <summary>
    /// Receives notification that JavaScript's calibration is complete.
    /// Matches: SendMessage("AIBridgeReceiver", "OnCalibrationComplete", "");
    /// </summary>
    public void OnCalibrationComplete(string emptyData)
    {
        // We don't need the 'emptyData', but the method signature must accept a string.
        Debug.Log("[JS -> C#] AI Calibration Complete! Head tracking is live.");

        if (gazeCursor != null)
        {
            gazeCursor.ShowCursor();
        }
    }
}