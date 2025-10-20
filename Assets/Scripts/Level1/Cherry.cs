using System;
using UnityEngine;

public class Cherry : MonoBehaviour
{
    [SerializeField] private int scoreValue = 100;
    public Action<GameObject> OnCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager.I?.AddScore(scoreValue);
            AudioManager.I?.PlaySfx(SfxEvent.Pellet, gameObject);
            OnCollected?.Invoke(gameObject);
        }
    }
}