using UnityEngine;
using System;
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public GameState CurrentState { get; private set; }

    public static event Action<GameState> OnGameStateChangedGlobal;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetState(GameState newState)
    {
        if (newState == CurrentState) return;

        CurrentState = newState;

        AudioManager.I?.OnGameStateChanged(newState);
        OnGameStateChangedGlobal?.Invoke(newState);
    }
}
