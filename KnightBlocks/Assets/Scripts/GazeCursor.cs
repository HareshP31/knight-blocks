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

    void Start()
    {
        cursorRect = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<parentCanvas>();
        raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
        eventSystem = eventSystem.current;

        gameObject.SetActive(false);
    }

    void Update()
    {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = cursorRect.position;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        GameObject currentHoveredObject = null;
        if (results.Count > 0)
        {
            currentHoveredObject = results[0].gameObject;
        }

        if (currentHoveredObject != lastHoveredObject)
        {
            if (lastHoveredObject != null)
            {
                // Send "Pointer Exit" event to the old object
                ExecuteEvents.Execute(lastHoveredObject, pointerEventData, ExecuteEvents.pointerExitHandler);
            }

            if (currentHoveredObject != null)
            {
                // Send "Pointer Enter" event to the new object
                ExecuteEvents.Execute(currentHoveredObject, pointerEventData, ExecuteEvents.pointerEnterHandler);
            }

            lastHoveredObject = currentHoveredObject;
        }
    }

    public void UpdatePosition(float x, float y)
    {
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // Convert normalized (0-1) coordinates to canvas (0-width/height)
        // NOTE: Your JS seems to invert Y already. If your cursor is upside-down,
        // change 'y * canvasHeight' to '(1.0f - y) * canvasHeight'.
        float newX = x * canvasWidth;
        float newY = y * canvasHeight;

        cursorRect.anchoredPosition = new Vector2(newX, newY);
    }

    public void PerformAction(string actionType)
    {
        // We only care about the object we are *currently* hovering over
        if (lastHoveredObject == null)
        {
            return;
        }

        if (actionType == "select") // Right wink (from your AIEngine.js)
        {
            // This simulates a "left click"
            Debug.Log("Triggering Click on: " + lastHoveredObject.name);
            ExecuteEvents.Execute(lastHoveredObject, pointerEventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(lastHoveredObject, pointerEventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(lastHoveredObject, pointerEventData, ExecuteEvents.pointerClickHandler);
        }
        else if (actionType == "deselect") // Left wink
        {
            // This simulates a "cancel" or "back" action (like pressing Esc)
            Debug.Log("Triggering Cancel on: " + lastHoveredObject.name);
            ExecuteEvents.Execute(lastHoveredObject, pointerEventData, ExecuteEvents.cancelHandler);
        }
    }

    public void ShowCursor()
    {
        gameObject.SetActive(true);
    }
}
