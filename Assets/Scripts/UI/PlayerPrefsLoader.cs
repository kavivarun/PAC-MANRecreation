using UnityEngine;
using TMPro;

public class PlayerPrefsLoader : MonoBehaviour
{
    public enum ValueType { Int, Float, String, Time }

    public string key;
    public ValueType valueType = ValueType.String;
    public TMP_Text output;
    public string defaultValue = "";
    
    [Header("Update Settings")]
    [SerializeField] private bool updateDynamically = true;
    [SerializeField] private float updateInterval = 0.1f; 
    
    private string lastValue;
    private float updateTimer;

    void Start()
    {
        UpdateValue();
    }
    
    void Update()
    {
        if (!updateDynamically) return;
     
        updateTimer += Time.deltaTime;
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            string newValue = GetCurrentValue();
            if (newValue != lastValue)
            {
                lastValue = newValue;
                if (output != null)
                   output.text = newValue;
            }
        }
    }
    
    public void UpdateValue()
    {
        string value = GetCurrentValue();
        lastValue = value;
        if (output != null)
          output.text = value;
    }
  
    private string GetCurrentValue()
    {
        if (string.IsNullOrEmpty(key)) return defaultValue;

        string value = defaultValue;

        switch (valueType)
        {
            case ValueType.Int:
                value = PlayerPrefs.HasKey(key)
                    ? PlayerPrefs.GetInt(key, int.TryParse(defaultValue, out var i) ? i : 0).ToString()
                    : defaultValue;
                break;

            case ValueType.Float:
                value = PlayerPrefs.HasKey(key)
                    ? PlayerPrefs.GetFloat(key, float.TryParse(defaultValue, out var f) ? f : 0f).ToString()
                    : defaultValue;
                break;

            case ValueType.String:
                value = PlayerPrefs.HasKey(key)
                    ? PlayerPrefs.GetString(key, defaultValue)
                    : defaultValue;
                break;
    
            case ValueType.Time:
                if (!PlayerPrefs.HasKey(key)) return defaultValue;
                float timer = PlayerPrefs.GetFloat(key, 0);
                int minutes = Mathf.FloorToInt(timer / 60f);
                int seconds = Mathf.FloorToInt(timer % 60f);
                int millis = Mathf.FloorToInt((timer * 100f) % 100f);
                value = $"{minutes:00}:{seconds:00}:{millis:00}";
                break;
        }

        return value;
    }
}
