using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PowerPelletL2 : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Level2Manager.I?.AddScore(scoreValue);
        Level2Manager.I?.getBullet();
        AudioManager.I?.PlaySfx(SfxEvent.Pellet, gameObject);
        Destroy(gameObject);
    }
}