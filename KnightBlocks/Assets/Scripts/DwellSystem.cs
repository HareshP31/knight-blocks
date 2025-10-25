using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public class DwellSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float dwellTime = 1.5f;

    public float activationCooldown = 1f;

    public UnityEvent onDwell;
    public static bool isAnyButtonCoolingDown = false;

    private bool isPointerOver = false;
    private float dwellTimer = 0f;
    private bool isTimerRunning = false;

    public static MonoBehaviour coroutineRunner;

    [Header("Pointer Hold Events")]
    public UnityEvent onPointerEnterEvent;
    public UnityEvent onPointerExitEvent;

    private void Update()
    {
        if (isPointerOver)
        {
            if (!isTimerRunning)
            {
                if (!isAnyButtonCoolingDown)
                {
                    isTimerRunning = true;
                    dwellTimer = 0f;
                }
            }
            else
            {
                dwellTimer += Time.deltaTime;

                if (dwellTimer >= dwellTime)
                {
                    TriggerDwell();
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        onPointerEnterEvent.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        isTimerRunning = false;
        dwellTimer = 0f;
        onPointerExitEvent.Invoke();
    }

    private void TriggerDwell()
    {
        Debug.Log("Dwell complete on " + gameObject.name);
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
        Debug.Log("Starting global cooldown for dwell buttons.");
        isAnyButtonCoolingDown = true;
        yield return new WaitForSeconds(cooldownTime);
        isAnyButtonCoolingDown = false;
        Debug.Log("Global cooldown for dwell buttons ended.");
    }

    private void OnDisable()
    {
        isPointerOver = false;
        isTimerRunning = false;
        dwellTimer = 0f;
    }
}