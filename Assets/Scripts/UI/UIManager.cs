using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static AudioManager;

public class UIManager : MonoBehaviour
{
    public static UIManager I;
    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
    public void LoadLevel1()
    {
        SceneManager.LoadScene("Level01Scene");
        AudioManager.I.OnGameStateChanged(GameState.Intro);
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("LevelGeneratorScene");
        AudioManager.I.OnGameStateChanged(GameState.Intro);
    }

    public void LoadUpgrades()
    {
        SceneManager.LoadScene("UpgradesScene");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("StartScene");
        AudioManager.I.OnGameStateChanged(GameState.Boot);
    }
}
