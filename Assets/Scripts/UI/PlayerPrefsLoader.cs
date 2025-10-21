using UnityEngine;
using TMPro;

public class PlayerPrefsLoader : MonoBehaviour
{
    public enum ValueType { Int, Float, String, Time }

    public string key;
    public ValueType valueType = ValueType.String;
    public TMP_Text output;
    public string defaultValue = "";

    void Start()
    {
        if (string.IsNullOrEmpty(key)) return;

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
                if (!PlayerPrefs.HasKey(key)) return;
                float timer = PlayerPrefs.GetFloat(key, 0);
                int minutes = Mathf.FloorToInt(timer / 60f);
                int seconds = Mathf.FloorToInt(timer % 60f);
                int millis = Mathf.FloorToInt((timer * 100f) % 100f);
                value = $"{minutes:00}:{seconds:00}:{millis:00}";
                break;

        }

        if (output != null)
            output.text = value;
    }
}
