using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Camera Setup")]
    public Transform targetPoint;
    private Transform cameraTransform;

    [Header("Settings")]
    public float rotationSpeed = 30f;
    public float zoomSpeed = 20f;
    public float minZoomDistance = 5f;
    public float maxZoomDistance = 50f;

    private float currentRotationInput = 0f;
    private float currentZoomInput = 0f;

    void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("CameraManager: No main camera found in scene!");
            return;
        }

        if (targetPoint == null)
        {
            Debug.LogError("CameraManager: Target Point is not set!");
            return;
        }

        cameraTransform.LookAt(targetPoint.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (targetPoint == null || cameraTransform == null) return;

        //handle rotation
        if (currentRotationInput != 0f)
        {
            cameraTransform.RotateAround(
                targetPoint.position,
                Vector3.up,
                currentRotationInput * rotationSpeed * Time.deltaTime
            );
        }

        //handle zoom
        if (currentZoomInput != 0f)
        {
            float currentDistance = Vector3.Distance(cameraTransform.position, targetPoint.position);
            float newDistance = currentDistance - (currentZoomInput * zoomSpeed * Time.deltaTime);

            newDistance = Mathf.Clamp(newDistance, minZoomDistance, maxZoomDistance);
            Vector3 directionToCamera = (cameraTransform.position - targetPoint.position).normalized;
            cameraTransform.position = targetPoint.position + (directionToCamera * newDistance);

            cameraTransform.LookAt(targetPoint.position);
        }
    }

    public void SetRotation(float direction)
    {
        currentRotationInput = direction;
    }

    public void SetZoom(float direction)
    {
        currentZoomInput = direction;
    }
}
