using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeController : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [SerializeField] private string upgradeName;
    [SerializeField] private int costPerLevel;
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

    int currentLevel;
    Image[] levelIcons;

    void Start()
    {
        InitUI();
        LoadUpgrade();
        EnsureMinimumLevel();
        UpdateUI();

        if (levelUpButton)
            levelUpButton.onClick.AddListener(OnLevelUpPressed);
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

    void OnLevelUpPressed()
    {
        int money = PlayerPrefs.GetInt(playerPrefMoneyKey, 0);

        if (currentLevel >= maxLevel)
        {
            Debug.Log($"{upgradeName}: Max level reached");
            UpdateButtonState();
            return;
        }

        if (money < costPerLevel)
        {
            Debug.Log($"{upgradeName}: Not enough money");
            UpdateButtonState();
            return;
        }

        money -= costPerLevel;
        currentLevel++;

        PlayerPrefs.SetInt(playerPrefMoneyKey, money);
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
        int money = PlayerPrefs.GetInt(playerPrefMoneyKey, 0);

        if (currentLevel >= maxLevel)
        {
            levelUpButton.interactable = false;
            if (buttonText) buttonText.text = "MAX LEVEL";
        }
        else if (money < costPerLevel)
        {
            levelUpButton.interactable = false;
            if (buttonText) buttonText.text = $"Need ${costPerLevel}";
        }
        else
        {
            levelUpButton.interactable = true;
            if (buttonText) buttonText.text = $"Upgrade (${costPerLevel})";
        }
    }
}
    