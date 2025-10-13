using UnityEngine;
using TMPro;

public class PlayerPrefsLoader : MonoBehaviour
{
    public string key;
    public TMP_Text output;
    public string prefix = "";

    void Start()
    {
        if (!PlayerPrefs.HasKey(key)) return;
        string value = PlayerPrefs.GetString(key, "");
        if (output != null)
            output.text = prefix + value;
    }
}