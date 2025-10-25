using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIManager : MonoBehaviour
{
    [Header("Menu Lists")]
    public GameObject defaultList;
    public GameObject fileList;
    public GameObject blockList;

    [Header("Block Menu")]
    GameObject[] blockPrefabs;
    public GameObject returnButton;
    public BuildingManager buildingManager;
    public int yOffset;

    private void Awake()
    {
        // Tell the DwellSystem that THIS script (which is always active)
        // is the official coroutine runner.
        DwellSystem.coroutineRunner = this;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Transform buttonContainer = returnButton.transform.parent;
        RectTransform returnButtonRect = returnButton.GetComponent<RectTransform>();
        float startY = returnButtonRect.anchoredPosition.y;
        float startX = returnButtonRect.anchoredPosition.x;

        blockPrefabs = Resources.LoadAll<GameObject>("BlockPrefabs");
        int currentYChange = yOffset;
        foreach (GameObject block in blockPrefabs)
        {
            GameObject newButton = Instantiate(returnButton, buttonContainer);

            RectTransform newButtonRect = newButton.GetComponent<RectTransform>();
            newButtonRect.anchoredPosition = new Vector2(startX, startY - currentYChange);

            currentYChange += yOffset;

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            Debug.Log("Creating button for block: " + block.name + "bttxt: " + buttonText);
            if (buttonText != null)
            {
                buttonText.text = block.name;
            }

            Button buttonComponent = newButton.GetComponent<Button>();

            if (buttonComponent != null)
            {
                buttonComponent.onClick.AddListener(() => OnBlockSelected(block));
            }
        }
    }
    void OnBlockSelected(GameObject blockToPlace)
    {
        Debug.Log("Selected block: " + blockToPlace.name);
        // buildingManager.StartPlacingBlock(blockToPlace);

    }

    public void OpenFileList()
    {
        defaultList.SetActive(false);
        fileList.SetActive(true);
        blockList.SetActive(false);
    }
    public void OpenBlockList()
    {
        defaultList.SetActive(false);
        fileList.SetActive(false);
        blockList.SetActive(true);
    }
    public void ReturnToMain()
    {
        defaultList.SetActive(true);
        fileList.SetActive(false);
        blockList.SetActive(false);
    }
}
