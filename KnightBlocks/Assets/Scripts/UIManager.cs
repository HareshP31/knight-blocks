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

    [Header("Rotation Menu")]
    public GameObject rotateButton;

    [Header("AI Bridge")]
    public AIBridgeCommunicator aiBridge;
    private bool isAIInitialized = false;

    public int yOffset;
    GameObject tempSelectedShape;


    private void Awake()
    {
        DwellSystem.coroutineRunner = this;
    }

    void Start()
    {
        if (rotateButton == null)
        {
            Debug.LogError("UIManager: 'Rotate Button' is not assigned in the Inspector!");
        }

        colorMaterials = Resources.LoadAll<Material>("ColorMaterials");
        blockPrefabs = Resources.LoadAll<GameObject>("BlockPrefabs");

        InitializeBlockList();
        InitializeColorList();
        InitializeRotateControls();
    }

    void Update()
    {
        if (isAIInitialized)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isAIInitialized = true;

            if (aiBridge != null)
            {
                Debug.Log("UIManager: First click detected, initiating AI system...");
                aiBridge.InitiateAISystem();
            }
            else
            {
                Debug.LogError("UIManager: 'AI Bridge' is not assigned in the Inspector! Cannot start AI.");
            }
        }
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

            DwellSystem dwellComponent = newButton.GetComponent<DwellSystem>();
            if (dwellComponent != null)
            {
                dwellComponent.onDwell.AddListener(() => OnColorSelected(mat));
            }
        }
    }

    void InitializeRotateControls()
    {
        if (rotateButton != null)
        {
            DwellSystem dwellComponent = rotateButton.GetComponent<DwellSystem>();
            if (dwellComponent != null)
            {
                dwellComponent.onDwell.AddListener(() => OnRotatePressed());
            }
            else
            {
                Debug.LogError($"Rotate Button '{rotateButton.name}' is missing the DwellSystem component!");
            }
        }
    }

    void OnBlockSelected(GameObject blockShape)
    {
        Debug.Log("Selected shape: " + blockShape.name);

        tempSelectedShape = blockShape;

        OpenColorList();
    }

    void OnColorSelected(Material mat)
    {
        Debug.Log("Selected Color: " + mat.name);

        buildingManager.SetBlockToPlace(tempSelectedShape, mat);

        ReturnToMain();
    }

    public void OnRotatePressed()
    {
        Debug.Log("Rotate Button Dwell Detected!");
        if (buildingManager != null)
        {
            buildingManager.RotateCurrentBlock();
        }
        else
        {
            Debug.LogError("UIManager.OnRotatePressed: BuildingManager is not assigned!");
        }
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

    public void ShowRotateButton(bool show)
    {
        if (rotateButton != null)
        {
            rotateButton.SetActive(show);
        }
    }
}
