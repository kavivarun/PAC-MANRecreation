using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeController : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [SerializeField] private string upgradeName;
    [SerializeField] private int[] costsPerLevel = { 10, 20, 30, 40 }; 
    [SerializeField] private int maxLevel = 4;
    [SerializeField] private float[] upgradeValues = { 1f, 1.5f, 2f, 3f };

    [Header("UI Elements")]
    [SerializeField] private Transform levelGridParent;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private Sprite unfilledSprite;
    [SerializeField] private Sprite filledSprite;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("PlayerPrefs Keys")]
    [SerializeField] private string playerPrefMoneyKey = "PlayerMoney";
    [SerializeField] private string playerPrefUpgradeKey = "Upgrade_";
    
    [Header("Update Settings")]
    [SerializeField] private bool updateDynamically = true;
    [SerializeField] private float updateInterval = 0.1f; 

    int currentLevel;
    Image[] levelIcons;
    private float lastKnownMoney = -1f; 
    private float updateTimer;

    void Start()
    {
        ValidateArrays();
        InitUI();
        LoadUpgrade();
        EnsureMinimumLevel();
        UpdateUI();
        lastKnownMoney = PlayerPrefs.GetFloat(playerPrefMoneyKey, 0);

        if (levelUpButton)
            levelUpButton.onClick.AddListener(OnLevelUpPressed);
    }
    
    void Update()
    {
        if (!updateDynamically) return;
        
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            CheckForMoneyChanges();
        }
    }
    
    void CheckForMoneyChanges()
    {
        float currentMoney = PlayerPrefs.GetFloat(playerPrefMoneyKey, 0);
        if (currentMoney != lastKnownMoney)
        {
            lastKnownMoney = currentMoney;
            UpdateButtonState();
        }
    }

    void ValidateArrays()
    {
        // Ensure costsPerLevel array has enough elements
        if (costsPerLevel.Length != maxLevel)
        {
            Debug.LogWarning($"{upgradeName}: costsPerLevel array length ({costsPerLevel.Length}) doesn't match maxLevel ({maxLevel}). This may cause issues.");
        }

        // Ensure upgradeValues array has enough elements
        if (upgradeValues.Length != maxLevel)
        {
            Debug.LogWarning($"{upgradeName}: upgradeValues array length ({upgradeValues.Length}) doesn't match maxLevel ({maxLevel}). This may cause issues.");
        }
    }

    void InitUI()
    {
        if (levelGridParent.childCount > 0)
        {
            levelIcons = new Image[levelGridParent.childCount];
            for (int i = 0; i < levelGridParent.childCount; i++)
                levelIcons[i] = levelGridParent.GetChild(i).GetComponent<Image>();
        }
        else
        {
            levelIcons = new Image[maxLevel];
            for (int i = 0; i < maxLevel; i++)
            {
                GameObject icon = new GameObject($"Level_{i + 1}", typeof(Image));
                icon.transform.SetParent(levelGridParent, false);

                var rt = icon.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(32, 32);
                rt.localScale = Vector3.one;
                rt.anchoredPosition = new Vector2(i * 36, 0);

                var img = icon.GetComponent<Image>();
                img.sprite = unfilledSprite;
                levelIcons[i] = img;
            }
        }
    }

    void LoadUpgrade()
    {
        currentLevel = PlayerPrefs.GetInt(playerPrefUpgradeKey + upgradeName + "_Level", 0);
    }

    void EnsureMinimumLevel()
    {
        if (currentLevel < 1)
        {
            currentLevel = 1;
            SaveUpgrade();
        }
    }

    void SaveUpgrade()
    {
        PlayerPrefs.SetInt(playerPrefUpgradeKey + upgradeName + "_Level", currentLevel);
        PlayerPrefs.SetFloat(playerPrefUpgradeKey + upgradeName + "_Value", GetUpgradeValue());
        PlayerPrefs.Save();
    }

    float GetUpgradeValue()
    {
        return upgradeValues[Mathf.Clamp(currentLevel - 1, 0, upgradeValues.Length - 1)];
    }

    int GetCostForNextLevel()
    {
        if (currentLevel >= maxLevel) return 0; // No cost if already at max level
        return costsPerLevel[Mathf.Clamp(currentLevel, 0, costsPerLevel.Length - 1)];
    }

    void OnLevelUpPressed()
    {
        float money = PlayerPrefs.GetFloat(playerPrefMoneyKey, 0);
        int nextLevelCost = GetCostForNextLevel();

        if (currentLevel >= maxLevel)
        {
            Debug.Log($"{upgradeName}: Max level reached");
            UpdateButtonState();
            return;
        }

        if (money < nextLevelCost)
        {
            Debug.Log($"{upgradeName}: Not enough money. Need {nextLevelCost}, have {money}");
            UpdateButtonState();
            return;
        }

        money -= nextLevelCost;
        currentLevel++;

        PlayerPrefs.SetFloat(playerPrefMoneyKey, money);
        SaveUpgrade();
        UpdateUI();
    }

    void UpdateUI()
    {
        for (int i = 0; i < levelIcons.Length; i++)
            levelIcons[i].sprite = i < currentLevel ? filledSprite : unfilledSprite;

        UpdateButtonState();
    }

    void UpdateButtonState()
    {
        float money = PlayerPrefs.GetFloat(playerPrefMoneyKey, 0);
        int nextLevelCost = GetCostForNextLevel();

        if (currentLevel >= maxLevel)
        {
            levelUpButton.interactable = false;
            if (buttonText) buttonText.text = "MAX LEVEL";
        }
        else if (money < nextLevelCost)
        {
            levelUpButton.interactable = false;
            if (buttonText) buttonText.text = $"Need ${nextLevelCost}";
        }
        else
        {
            levelUpButton.interactable = true;
            if (buttonText) buttonText.text = $"Upgrade (${nextLevelCost})";
        }
    }

    public void ForceUpdateUI()
    {
        CheckForMoneyChanges();
        UpdateUI();
    }
}