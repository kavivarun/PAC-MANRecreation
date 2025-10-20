using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerPellet : MonoBehaviour
{
    [SerializeField] private int scoreValue = 50;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        LevelManager.I?.AddScore(scoreValue);
        LevelManager.I?.StartGhostScaredTimer();
        AudioManager.I?.PlaySfx(SfxEvent.Pellet, gameObject);
        Destroy(gameObject);
    }
}