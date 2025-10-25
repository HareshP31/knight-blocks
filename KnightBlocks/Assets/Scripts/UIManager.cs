using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class UIManager : MonoBehaviour
{
    [Header("Menu Lists")]
    public GameObject defaultList;
    public GameObject fileList;
    public GameObject blockList;
    public GameObject colorList;

    [Header("Block Menu")]
    GameObject[] blockPrefabs;
    public GameObject blockReturnButton;
    public BuildingManager buildingManager;

    [Header("Color Menu")]
    public GameObject colorReturnButton;
    Material[] colorMaterials;

    public int yOffset;

    GameObject tempSelectedShape;


    private void Awake()
    {
        // Tell the DwellSystem that THIS script (which is always active)
        // is the official coroutine runner.
        DwellSystem.coroutineRunner = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        colorMaterials = Resources.LoadAll<Material>("ColorMaterials");
        blockPrefabs = Resources.LoadAll<GameObject>("BlockPrefabs");

        InitializeBlockList();
        InitializeColorList();
    }

    void InitializeBlockList()
    {
        Transform buttonContainer = blockReturnButton.transform.parent;
        RectTransform returnButtonRect = blockReturnButton.GetComponent<RectTransform>();
        float startY = returnButtonRect.anchoredPosition.y;
        float startX = returnButtonRect.anchoredPosition.x;
        int currentYChange = yOffset;

        foreach (GameObject blockShape in blockPrefabs)
        {
            GameObject newButton = Instantiate(blockReturnButton, buttonContainer);
            newButton.SetActive(true); // Make sure it's visible

            RectTransform newButtonRect = newButton.GetComponent<RectTransform>();
            newButtonRect.anchoredPosition = new Vector2(startX, startY - currentYChange);

            currentYChange += yOffset;

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = blockShape.name;
            }

            // 3. FIX: Changed from onClick to onDwell to use your DwellSystem
            DwellSystem dwellComponent = newButton.GetComponent<DwellSystem>();
            if (dwellComponent != null)
            {
                dwellComponent.onDwell.AddListener(() => OnBlockSelected(blockShape));
            }
        }
    }

    void InitializeColorList()
    {
        // --- Use the COLOR button's parent and template ---
        Transform buttonContainer = colorReturnButton.transform.parent;
        RectTransform returnButtonRect = colorReturnButton.GetComponent<RectTransform>();
        float startY = returnButtonRect.anchoredPosition.y;
        float startX = returnButtonRect.anchoredPosition.x;
        int currentYChange = yOffset; // Reset offset for this new list

        foreach (Material mat in colorMaterials)
        {
            // --- Instantiate the COLOR return button ---
            GameObject newButton = Instantiate(colorReturnButton, buttonContainer);
            newButton.SetActive(true); // Make sure it's visible

            RectTransform newButtonRect = newButton.GetComponent<RectTransform>();
            newButtonRect.anchoredPosition = new Vector2(startX, startY - currentYChange);

            currentYChange += yOffset;

            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = mat.name;
            }
            
            // 3. FIX: Changed from onClick to onDwell to use your DwellSystem
            DwellSystem dwellComponent = newButton.GetComponent<DwellSystem>();
            if (dwellComponent != null)
            {
                dwellComponent.onDwell.AddListener(() => OnColorSelected(mat));
            }
        }
    }
    void OnBlockSelected(GameObject blockShape)
    {
        Debug.Log("Selected shape: " + blockShape.name);
        
        // 1. Store the chosen shape
        tempSelectedShape = blockShape;

        // 2. Open the color menu
        OpenColorList();
    }

    void OnColorSelected(Material mat)
    {
        Debug.Log("Selected Color: " + mat.name);

        // 1. Tell the BuildingManager to get ready with BOTH choices
        //buildingManager.SetBlockToPlace(tempSelectedShape, mat);

        // 2. Go back to the main screen
        ReturnToMain();
    }

    public void OpenFileList()
    {
        defaultList.SetActive(false);
        fileList.SetActive(true);
        blockList.SetActive(false);
        colorList.SetActive(false);
    }
    public void OpenBlockList()
    {
        defaultList.SetActive(false);
        fileList.SetActive(false);
        blockList.SetActive(true);
        colorList.SetActive(false);
    }
    public void ReturnToMain()
    {
        defaultList.SetActive(true);
        fileList.SetActive(false);
        blockList.SetActive(false);
        colorList.SetActive(false);
    }

    public void OpenColorList()
    {
        defaultList.SetActive(false);
        fileList.SetActive(false);
        blockList.SetActive(false);
        colorList.SetActive(true);
    }
}
