using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public class DwellSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    [Tooltip("If checked, the action fires every frame while hovering (good for Zoom/Rotate). If unchecked, it waits for the timer (good for Menus).")]
    public bool continuousTrigger = false;
    public float dwellTime = 1.5f;
    public float activationCooldown = 1f;

    [Header("Events")]
    public UnityEvent onDwell;
    public UnityEvent onPointerExit;

    public static bool isAnyButtonCoolingDown = false;
    private bool isPointerOver = false;
    private float dwellTimer = 0f;
    private bool isTimerRunning = false;
    public static MonoBehaviour coroutineRunner;

    private void Awake()
    {
        if (coroutineRunner == null) coroutineRunner = this;
    }

    private void Update()
    {
        if (isPointerOver)
        {
            if (continuousTrigger)
            {
                onDwell.Invoke();
            }
            else
            {
                if (!isTimerRunning && !isAnyButtonCoolingDown)
                {
                    isTimerRunning = true;
                    dwellTimer = 0f;
                }

                if (isTimerRunning)
                {
                    dwellTimer += Time.deltaTime;
                    if (dwellTimer >= dwellTime)
                    {
                        TriggerDwellClick();
                    }
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        isTimerRunning = false;
        dwellTimer = 0f;
        onPointerExit.Invoke();
    }

    private void TriggerDwellClick()
    {
        Debug.Log("Dwell Click on " + gameObject.name);
        isTimerRunning = false;
        dwellTimer = 0f;

        onDwell.Invoke();

        if (coroutineRunner != null)
        {
            coroutineRunner.StartCoroutine(GlobalCooldown(activationCooldown));
        }
    }

    private static IEnumerator GlobalCooldown(float cooldownTime)
    {
        isAnyButtonCoolingDown = true;
        yield return new WaitForSeconds(cooldownTime);
        isAnyButtonCoolingDown = false;
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isTimerRunning = false;
        dwellTimer = 0f;
    }
}