using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class GazeCursor : MonoBehaviour
{
    private RectTransform cursorRect;
    private GraphicRaycaster raycaster;
    private PointerEventData pointerEventData;
    private EventSystem eventSystem;
    private GameObject lastHoveredObject = null;
    private Canvas parentCanvas;

    public Vector2 ScreenPosition { get; private set; }

    void Start()
    {
        cursorRect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;

        if (GetComponent<Image>() != null) GetComponent<Image>().raycastTarget = false;
        if (GetComponent<RawImage>() != null) GetComponent<RawImage>().raycastTarget = false;

        gameObject.SetActive(false);
    }

    void Update()
    {
        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null) return;
        }

        pointerEventData = new PointerEventData(eventSystem);

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, cursorRect.position);

        ScreenPosition = screenPos;
        pointerEventData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        GameObject currentHoveredObject = null;

        if (results.Count > 0)
        {
            foreach (var result in results)
            {
                if (result.gameObject != gameObject)
                {
                    currentHoveredObject = result.gameObject;
                    break;
                }
            }
        }

        if (currentHoveredObject != lastHoveredObject)
        {
            if (lastHoveredObject != null)
            {
                // Send "Pointer Exit" event to the old object
                ExecuteEvents.ExecuteHierarchy(lastHoveredObject, pointerEventData, ExecuteEvents.pointerExitHandler);
            }

            if (currentHoveredObject != null)
            {
                // DEBUG: Let's see exactly what we are hitting and if it has the script
                DwellSystem checkScript = currentHoveredObject.GetComponentInParent<DwellSystem>();
                if (checkScript != null)
                {
                    Debug.Log($"Gaze hitting '{currentHoveredObject.name}' -> Found DwellSystem on '{checkScript.gameObject.name}'");
                }
                else
                {
                    Debug.Log($"Gaze hitting '{currentHoveredObject.name}' -> NO DwellSystem found on it or parents.");
                }

                // Use ExecuteHierarchy to bubble the event up to the button
                ExecuteEvents.ExecuteHierarchy(currentHoveredObject, pointerEventData, ExecuteEvents.pointerEnterHandler);
            }

            lastHoveredObject = currentHoveredObject;
        }
    }

    public void UpdatePosition(float x, float y)
    {
        if (parentCanvas == null) return;
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        float newX = x * canvasRect.rect.width;
        float newY = -y * canvasRect.rect.height;
        cursorRect.anchoredPosition = new Vector2(newX, newY);
    }

    public void ShowCursor()
    {
        gameObject.SetActive(true);
    }
}
