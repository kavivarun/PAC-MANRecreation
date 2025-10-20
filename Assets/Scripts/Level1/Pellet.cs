using UnityEngine;

public class Pellet : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager.I?.AddScore(scoreValue);
            AudioManager.I?.PlaySfx(SfxEvent.Pellet, gameObject);
            Destroy(gameObject);
        }
    }
}