using System.Runtime.InteropServices;
using UnityEngine;

public class AIBridgeCommunicator : MonoBehaviour
{
    // --------------------------------------------------------------------------
    // 1. C# -> JavaScript Functions (Calling the AI System Start)
    //    These MUST match the function names defined in AIBridge.jslib exactly.
    // --------------------------------------------------------------------------

    [DllImport("__Internal")]
    private static extern void StartAIBridge();

    // --------------------------------------------------------------------------
    // 2. Public Interface (Called by Unity to start the system)
    // --------------------------------------------------------------------------

    /// <summary>
    /// Initiates the AI Engine by calling the JavaScript bridge function.
    /// This should be called once the Unity scene is ready (e.g., from a UI button or Start()).
    /// </summary>
    public void InitiateAISystem()
    {
        Debug.Log("InitiateAISystem called in C#!");
        // The DllImport calls are only valid in a WebGL build.
#if !UNITY_EDITOR && UNITY_WEBGL
            Debug.Log("C# initiating AI system via StartAIBridge...");
            StartAIBridge();
#else
        Debug.LogWarning("AI System initiated, but DllImport calls are skipped in the Unity Editor.");
        // For editor testing, you might mock data calls here.
#endif
    }

    // --------------------------------------------------------------------------
    // 3. JavaScript -> C# Receiver Methods (Called by main-controller.js via AIBridge.jslib)
    //    The names and signatures MUST match the placeholders in AIBridge.jslib.
    // --------------------------------------------------------------------------

    /// <summary>
    /// Receives normalized head/gaze coordinates from JavaScript.
    /// Matches GazeEventReceiver in AIBridge.jslib.
    /// </summary>
    public void GazeEventReceiver(float x, float y)
    {
        // Data is normalized (0.0 to 1.0) and inverted (x is mirrored).
        // Use this for cursor position or camera movement in Unity.
        Debug.Log($"[JS -> C#] Gaze/Head Position: X={x:F4}, Y={y:F4}");

        // Example: Convert to screen pixel coordinates if needed
        // Vector3 screenPos = new Vector3(x * Screen.width, y * Screen.height, 0);
    }

    /// <summary>
    /// Receives action commands ('select' or 'deselect') from JavaScript.
    /// Matches ActionReceiver in AIBridge.jslib.
    /// </summary>
    public void ActionReceiver(string actionType)
    {
        // Use this to trigger button clicks or object selection.
        Debug.Log($"[JS -> C#] Action Received: {actionType}");
    }

    /// <summary>
    /// Receives head rotation commands ('left' or 'right') from JavaScript.
    /// Matches RotationReceiver in AIBridge.jslib.
    /// </summary>
    public void RotationReceiver(string direction)
    {
        // Use this to pan a camera or scroll a menu.
        Debug.Log($"[JS -> C#] Rotation Received: {direction}");
    }

    /// <summary>
    /// Receives notification that JavaScript's dynamic calibration is complete.
    /// Matches CalibrationCompleteReceiver in AIBridge.jslib.
    /// </summary>
    public void CalibrationCompleteReceiver()
    {
        // Use this to hide a "Calibrating..." UI screen in Unity.
        Debug.Log("[JS -> C#] AI Calibration Complete! Head tracking is live.");
    }
}
