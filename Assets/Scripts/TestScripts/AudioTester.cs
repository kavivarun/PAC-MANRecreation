using UnityEngine;

public class AudioTester : MonoBehaviour
{
    void Update()
    {
        //Test Game States (music / crossfades)
        if (Input.GetKeyDown(KeyCode.Alpha1))
            AudioManager.I.OnGameStateChanged(GameState.Boot);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            AudioManager.I.OnGameStateChanged(GameState.Intro);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            AudioManager.I.OnGameStateChanged(GameState.Playing);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            AudioManager.I.OnGameStateChanged(GameState.PowerMode);

        if (Input.GetKeyDown(KeyCode.Alpha5))
            AudioManager.I.OnGameStateChanged(GameState.AlienDead);

        if (Input.GetKeyDown(KeyCode.Alpha6))
            AudioManager.I.OnGameStateChanged(GameState.LevelCleared);

        if (Input.GetKeyDown(KeyCode.Alpha7))
            AudioManager.I.OnGameStateChanged(GameState.Dying);


        //Test SFX events
        if (Input.GetKeyDown(KeyCode.Q))
            AudioManager.I.PlaySfx(SfxEvent.Pellet);

        if (Input.GetKeyDown(KeyCode.W))
            AudioManager.I.PlaySfx(SfxEvent.PowerPickup);

        if (Input.GetKeyDown(KeyCode.E))
            AudioManager.I.PlaySfx(SfxEvent.WallHit);

        if (Input.GetKeyDown(KeyCode.R))
            AudioManager.I.PlaySfx(SfxEvent.Step);


        //Volume control for mixer (optional)
        if (Input.GetKeyDown(KeyCode.Minus)) // lower music
            AudioManager.I.SetMusicDb(-20f);

        if (Input.GetKeyDown(KeyCode.Equals)) // reset music
            AudioManager.I.SetMusicDb(0f);

        if (Input.GetKeyDown(KeyCode.LeftBracket)) // lower sfx
            AudioManager.I.SetSfxDb(-20f);

        if (Input.GetKeyDown(KeyCode.RightBracket)) // reset sfx
            AudioManager.I.SetSfxDb(0f);
    }
}
